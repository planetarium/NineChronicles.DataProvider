namespace NineChronicles.DataProvider.Executable.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Bencodex.Types;
    using Cocona;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Crypto;
    using Libplanet.RocksDBStore;
    using Libplanet.Store;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using MySqlConnector;
    using Nekoyume.Action;
    using Nekoyume.Action.Loader;
    using Nekoyume.Blockchain.Policy;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Serilog;
    using Serilog.Events;

    public class UserStakingMigration
    {
        private string _userStakingTableName = "UserStakings";
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain _baseChain;
        private StreamWriter _usBulkFile;
        private List<string> _usFiles;

        [Command(Description = "Migrate staking amounts of users at a specific block index to a mysql database.")]
        public void Migration(
            [Option('o', Description = "Rocksdb store path to migrate.")]
            string storePath,
            [Option(
                "mysql-server",
                Description = "Hostname of MySQL server.")]
            string mysqlServer,
            [Option(
                "mysql-port",
                Description = "Port of MySQL server.")]
            uint mysqlPort,
            [Option(
                "mysql-username",
                Description = "Name of MySQL user.")]
            string mysqlUsername,
            [Option(
                "mysql-password",
                Description = "Password of MySQL user.")]
            string mysqlPassword,
            [Option(
                "mysql-database",
                Description = "Name of MySQL database to use.")]
            string mysqlDatabase,
            [Option(
                "slack-token",
                Description = "slack token to send the migration data.")]
            string slackToken,
            [Option(
                "slack-channel",
                Description = "slack channel that receives the migration data.")]
            string slackChannel,
            [Option(
                "migration-block-index",
                Description = "Block index to migrate.")]
            long? migrationBlockIndex = null
        )
        {
            DateTimeOffset start = DateTimeOffset.UtcNow;
            var builder = new MySqlConnectionStringBuilder
            {
                Database = mysqlDatabase,
                UserID = mysqlUsername,
                Password = mysqlPassword,
                Server = mysqlServer,
                Port = mysqlPort,
                AllowLoadLocalInfile = true,
            };

            _connectionString = builder.ConnectionString;

            Console.WriteLine("Setting up RocksDBStore...");
            _baseStore = new RocksDBStore(
                storePath,
                dbConnectionCacheSize: 10000);

            long totalLength = _baseStore.CountBlocks();

            if (totalLength == 0)
            {
                throw new CommandExitedException("Invalid rocksdb-store. Please enter a valid store path", -1);
            }

            if (!(_baseStore.GetCanonicalChainId() is { } chainId))
            {
                Console.Error.WriteLine("There is no canonical chain: {0}", storePath);
                Environment.Exit(1);
                return;
            }

            if (!(_baseStore.IndexBlockHash(chainId, 0) is { } gHash))
            {
                Console.Error.WriteLine("There is no genesis block: {0}", storePath);
                Environment.Exit(1);
                return;
            }

            // Setup base store
            RocksDBKeyValueStore baseStateKeyValueStore = new RocksDBKeyValueStore(Path.Combine(storePath, "states"));
            TrieStateStore baseStateStore =
                new TrieStateStore(baseStateKeyValueStore);

            // Setup block policy
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            var blockPolicySource = new BlockPolicySource();
            IBlockPolicy blockPolicy = blockPolicySource.GetPolicy();

            // Setup base chain & new chain
            Block genesis = _baseStore.GetBlock(gHash);
            var blockChainStates = new BlockChainStates(_baseStore, baseStateStore);
            var actionEvaluator = new ActionEvaluator(
                blockPolicy.PolicyActionsRegistry,
                baseStateStore,
                new NCActionLoader());
            _baseChain = new BlockChain(blockPolicy, stagePolicy, _baseStore, baseStateStore, genesis, blockChainStates, actionEvaluator);

            // Prepare block hashes to append to new chain
            long height = _baseChain.Tip.Index;
            if (migrationBlockIndex > (int)height)
            {
                Console.Error.WriteLine(
                    "The block index point to migrate is greater than the chain tip index: {0}",
                    height);
                Environment.Exit(1);
                return;
            }

            Console.WriteLine("Start migration.");

            // files to store bulk file paths (new file created every 10000 blocks for bulk load performance)
            _usFiles = new List<string>();

            CreateBulkFiles();

            using MySqlConnection connection = new MySqlConnection(_connectionString);
            connection.Open();

            var statement = "SELECT `Address` from Avatars";
            var command = new MySqlCommand(statement, connection);

            var commandReader = command.ExecuteReader();
            List<string> avatars = new List<string>();
            List<string> agents = new List<string>();

            while (commandReader.Read())
            {
                Console.WriteLine("{0}", commandReader.GetString(0));
                avatars.Add(commandReader.GetString(0).Replace("0x", string.Empty));
            }

            connection.Close();
            _usBulkFile.WriteLine(
                     "BlockIndex;" +
                     "StakeVersion;" +
                     "AgentAddress;" +
                     "StakingAmount;" +
                     "StartedBlockIndex;" +
                     "ReceivedBlockIndex;" +
                     "CancellableBlockIndex"
                 );

            try
            {
                var tipHash = _baseStore.IndexBlockHash(_baseChain.Id, migrationBlockIndex ?? _baseChain.Tip.Index);
                var tip = _baseStore.GetBlock((BlockHash)tipHash!);
                var blockEvaluation = _baseChain.EvaluateBlock(tip);
                var evaluation = blockEvaluation.Last();
                var outputState = new World(_baseChain.GetWorldState(evaluation.OutputState));

                var avatarCount = 0;
                AvatarState avatarState;
                int intervalCount = 0;
                bool checkUserStakingTable = false;

                foreach (var avatar in avatars)
                {
                    try
                    {
                        intervalCount++;
                        avatarCount++;
                        Console.WriteLine("Interval Count {0}", intervalCount);
                        Console.WriteLine("Migrating {0}/{1}", avatarCount, avatars.Count);
                        var avatarAddress = new Address(avatar);
                        avatarState = outputState.GetAvatarState(avatarAddress);

                        if (!checkUserStakingTable)
                        {
                            _userStakingTableName = $"{_userStakingTableName}_{tip.Index}";
                            var statement1 =
                            $@"CREATE TABLE IF NOT EXISTS `data_provider`.`{_userStakingTableName}` (
                              `BlockIndex` bigint NOT NULL,
                              `StakeVersion` varchar(100) NOT NULL,
                              `AgentAddress` varchar(100) NOT NULL,
                              `StakingAmount` decimal(13,2) NOT NULL,
                              `StartedBlockIndex` bigint NOT NULL,
                              `ReceivedBlockIndex` bigint NOT NULL,
                              `CancellableBlockIndex` bigint NOT NULL,
                              `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP
                            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";
                            var command1 = new MySqlCommand(statement1, connection);
                            connection.Open();
                            command1.CommandTimeout = 300;
                            command1.ExecuteScalar();
                            connection.Close();
                            checkUserStakingTable = true;
                        }

                        if (!agents.Contains(avatarState.agentAddress.ToString()))
                        {
                            agents.Add(avatarState.agentAddress.ToString());
                            if (outputState.TryGetStakeStateV2(
                                    avatarState.agentAddress,
                                    out var stakeStateV2))
                            {
                                var stakeAddress = StakeStateV2.DeriveAddress(avatarState.agentAddress);
                                var currency = outputState.GetGoldCurrency();
                                var stakedBalance = outputState.GetBalance(stakeAddress, currency);
                                _usBulkFile.WriteLine(
                                    $"{tip.Index};" +
                                    "V2;" +
                                    $"{avatarState.agentAddress.ToString()};" +
                                    $"{Convert.ToDecimal(stakedBalance.GetQuantityString())};" +
                                    $"{stakeStateV2.StartedBlockIndex};" +
                                    $"{stakeStateV2.ReceivedBlockIndex};" +
                                    $"{stakeStateV2.CancellableBlockIndex}"
                                );
                            }

                            var agentState = outputState.GetAgentState(avatarState.agentAddress);
                            Address monsterCollectionAddress = MonsterCollectionState.DeriveAddress(
                                avatarState.agentAddress,
                                agentState.MonsterCollectionRound
                            );
                            if (outputState.TryGetLegacyState(monsterCollectionAddress, out Dictionary stateDict))
                            {
                                var monsterCollectionStates = new MonsterCollectionState(stateDict);
                                var currency = outputState.GetGoldCurrency();
                                FungibleAssetValue monsterCollectionBalance =
                                    outputState.GetBalance(monsterCollectionAddress, currency);
                                _usBulkFile.WriteLine(
                                    $"{tip.Index};" +
                                    "V1;" +
                                    $"{avatarState.agentAddress.ToString()};" +
                                    $"{Convert.ToDecimal(monsterCollectionBalance.GetQuantityString())};" +
                                    $"{monsterCollectionStates.StartedBlockIndex};" +
                                    $"{monsterCollectionStates.ReceivedBlockIndex};" +
                                    $"{monsterCollectionStates.ExpiredBlockIndex}"
                                );
                            }
                        }

                        Console.WriteLine("Migrating Complete {0}/{1}", avatarCount, avatars.Count);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                FlushBulkFiles();
                DateTimeOffset postDataPrep = DateTimeOffset.Now;
                Console.WriteLine("Data Preparation Complete! Time Elapsed: {0}", postDataPrep - start);

                var statement2 = $"DROP TABLE IF EXISTS {_userStakingTableName}_Dump;";
                var command2 = new MySqlCommand(statement2, connection);
                connection.Open();
                command2.CommandTimeout = 300;
                command2.ExecuteScalar();
                connection.Close();

                var statement3 = $"RENAME TABLE {_userStakingTableName} TO {_userStakingTableName}_Dump; CREATE TABLE {_userStakingTableName} LIKE {_userStakingTableName}_Dump;";
                var command3 = new MySqlCommand(statement3, connection);
                var startMove = DateTimeOffset.Now;
                connection.Open();
                command3.CommandTimeout = 300;
                command3.ExecuteScalar();
                connection.Close();
                var endMove = DateTimeOffset.Now;
                Console.WriteLine("Move UserStaking Complete! Time Elapsed: {0}", endMove - startMove);
                var i = 1;
                foreach (var path in _usFiles)
                {
                    string oldFilePath = path;
                    string newFilePath = Path.Combine(Path.GetTempPath(), $"UserStaking{tip.Index}#{i}.csv");
                    if (File.Exists(newFilePath))
                    {
                        File.Delete(newFilePath);
                    }

                    File.Copy(oldFilePath, newFilePath);
                    UploadFileAsync(
                        slackToken,
                        newFilePath,
                        slackChannel
                    ).Wait();

                    BulkInsert(_userStakingTableName, path);
                    i += 1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Restoring previous tables due to error...");
                var statement4 = $"DROP TABLE {_userStakingTableName}; RENAME TABLE {_userStakingTableName}_Dump TO {_userStakingTableName};";
                var command4 = new MySqlCommand(statement4, connection);
                var startRestore = DateTimeOffset.Now;
                connection.Open();
                command4.CommandTimeout = 300;
                command4.ExecuteScalar();
                connection.Close();
                var endRestore = DateTimeOffset.Now;
                Console.WriteLine("Restore UserStaking Complete! Time Elapsed: {0}", endRestore - startRestore);
            }

            var statement5 = $"DROP TABLE {_userStakingTableName}_Dump;";
            var command5 = new MySqlCommand(statement5, connection);
            var startDelete = DateTimeOffset.Now;
            connection.Open();
            command5.CommandTimeout = 300;
            command5.ExecuteScalar();
            connection.Close();
            var endDelete = DateTimeOffset.Now;
            Console.WriteLine("Delete UserStaking_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);

            DateTimeOffset end = DateTimeOffset.UtcNow;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
        }

        private async Task UploadFileAsync(string token, string path, string channels)
        {
            HttpClient client = new HttpClient();
            var multiForm = new MultipartFormDataContent();
            multiForm.Add(new StringContent(token), "token");
            multiForm.Add(new StringContent(channels), "channels");
            FileStream fs = File.OpenRead(path);
            multiForm.Add(new StreamContent(fs), "file", Path.GetFileName(path));
            var url = "https://slack.com/api/files.upload";
            var response = await client.PostAsync(url, multiForm);
            var responseJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseJson);
        }

        private void FlushBulkFiles()
        {
            _usBulkFile.Flush();
            _usBulkFile.Close();
        }

        private void CreateBulkFiles()
        {
            string usFilePath = Path.GetRandomFileName().Replace(".tmp", ".csv");
            _usBulkFile = new StreamWriter(usFilePath);
            _usFiles.Add(usFilePath);
        }

        private void BulkInsert(
            string tableName,
            string filePath)
        {
            using MySqlConnection connection = new MySqlConnection(_connectionString);
            try
            {
                DateTimeOffset start = DateTimeOffset.Now;
                Console.WriteLine($"Start bulk insert to {tableName}.");
                MySqlBulkLoader loader = new MySqlBulkLoader(connection)
                {
                    NumberOfLinesToSkip = 1,
                    TableName = tableName,
                    FileName = filePath,
                    Timeout = 0,
                    LineTerminator = Environment.OSVersion.VersionString.Contains("Win") ? "\r\n" : "\n",
                    FieldTerminator = ";",
                    Local = true,
                    ConflictOption = MySqlBulkLoaderConflictOption.Ignore,
                };

                loader.Load();
                Console.WriteLine($"Bulk load to {tableName} complete.");
                DateTimeOffset end = DateTimeOffset.Now;
                Console.WriteLine("Time elapsed: {0}", end - start);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine($"Bulk load to {tableName} failed. Retry bulk insert");
                DateTimeOffset start = DateTimeOffset.Now;
                Console.WriteLine($"Start bulk insert to {tableName}.");
                MySqlBulkLoader loader = new MySqlBulkLoader(connection)
                {
                    NumberOfLinesToSkip = 1,
                    TableName = tableName,
                    FileName = filePath,
                    Timeout = 0,
                    LineTerminator = Environment.OSVersion.VersionString.Contains("Win") ? "\r\n" : "\n",
                    FieldTerminator = ";",
                    Local = true,
                    ConflictOption = MySqlBulkLoaderConflictOption.Ignore,
                };

                loader.Load();
                Console.WriteLine($"Bulk load to {tableName} complete.");
                DateTimeOffset end = DateTimeOffset.Now;
                Console.WriteLine("Time elapsed: {0}", end - start);
            }
        }
    }
}
