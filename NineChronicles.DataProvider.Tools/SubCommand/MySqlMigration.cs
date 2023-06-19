namespace NineChronicles.DataProvider.Tools.SubCommand
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Bencodex.Types;
    using Cocona;
    using Lib9c.Model.Order;
    using Libplanet;
    using Libplanet.Assets;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.RocksDBStore;
    using Libplanet.Store;
    using MySqlConnector;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.BlockChain.Policy;
    using Nekoyume.Helper;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Serilog.Events;
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

    public class MySqlMigration
    {
        private string USDbName = "UserStakings";
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain<NCAction> _baseChain;
        private StreamWriter _usBulkFile;
        private List<string> _usFiles;

        [Command(Description = "Migrate action data in rocksdb store to mysql db.")]
        public void Migration(
            [Option('o', Description = "Rocksdb path to migrate.")]
            string storePath,
            [Option(
                "rocksdb-storetype",
                Description = "Store type of RocksDb (new or mono).")]
            string rocksdbStoreType,
            [Option(
                "mysql-server",
                Description = "A hostname of MySQL server.")]
            string mysqlServer,
            [Option(
                "mysql-port",
                Description = "A port of MySQL server.")]
            uint mysqlPort,
            [Option(
                "mysql-username",
                Description = "The name of MySQL user.")]
            string mysqlUsername,
            [Option(
                "mysql-password",
                Description = "The password of MySQL user.")]
            string mysqlPassword,
            [Option(
                "mysql-database",
                Description = "The name of MySQL database to use.")]
            string mysqlDatabase,
            [Option(
                "slack-token",
                Description = "slack token.")]
            string slackToken,
            [Option(
                "slack-channel",
                Description = "slack channel.")]
            string slackChannel,
            [Option(
                "offset",
                Description = "offset of block index (no entry will migrate from the genesis block).")]
            int? offset = null,
            [Option(
                "limit",
                Description = "limit of block count (no entry will migrate to the chain tip).")]
            int? limit = null,
            [Option(
                "tipIndex",
                Description = "tipIndex of chain.")]
            long? tipIndex = null
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
            if (rocksdbStoreType == "new")
            {
                _baseStore = new RocksDBStore(
                    storePath,
                    dbConnectionCacheSize: 10000);
            }
            else
            {
                throw new CommandExitedException("Invalid rocksdb-storetype. Please enter 'new' or 'mono'", -1);
            }

            long totalLength = _baseStore.CountBlocks();

            if (totalLength == 0)
            {
                throw new CommandExitedException("Invalid rocksdb-store. Please enter a valid store path", -1);
            }

            if (!(_baseStore.GetCanonicalChainId() is Guid chainId))
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
            IStagePolicy<NCAction> stagePolicy = new VolatileStagePolicy<NCAction>();
            LogEventLevel logLevel = LogEventLevel.Debug;
            var blockPolicySource = new BlockPolicySource(Log.Logger, logLevel);
            IBlockPolicy<NCAction> blockPolicy = blockPolicySource.GetPolicy();

            // Setup base chain & new chain
            Block genesis = _baseStore.GetBlock(gHash);
            _baseChain = new BlockChain<NCAction>(blockPolicy, stagePolicy, _baseStore, baseStateStore, genesis);

            // Prepare block hashes to append to new chain
            long height = _baseChain.Tip.Index;
            if (offset + limit > (int)height)
            {
                Console.Error.WriteLine(
                    "The sum of the offset and limit is greater than the chain tip index: {0}",
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

            var stm = "SELECT `Address` from Avatars";
            var cmd = new MySqlCommand(stm, connection);

            var rdr = cmd.ExecuteReader();
            List<string> avatars = new List<string>();
            List<string> agents = new List<string>();

            while (rdr.Read())
            {
                Console.WriteLine("{0}", rdr.GetString(0));
                avatars.Add(rdr.GetString(0).Replace("0x", string.Empty));
            }

            connection.Close();
            int shopOrderCount = 0;
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
                var tipHash = _baseStore.IndexBlockHash(_baseChain.Id, tipIndex ?? _baseChain.Tip.Index);
                var tip = _baseStore.GetBlock((BlockHash)tipHash);
                var exec = _baseChain.EvaluateBlock(tip);
                var ev = exec.Last();
                var avatarCount = 0;
                AvatarState avatarState;
                int interval = 1000000;
                int intervalCount = 0;
                bool checkBARankingTable = false;

                foreach (var avatar in avatars)
                {
                    try
                    {
                        intervalCount++;
                        avatarCount++;
                        Console.WriteLine("Interval Count {0}", intervalCount);
                        Console.WriteLine("Migrating {0}/{1}", avatarCount, avatars.Count);
                        var avatarAddress = new Address(avatar);
                        try
                        {
                            avatarState = ev.OutputStates.GetAvatarStateV2(avatarAddress);
                        }
                        catch (Exception ex)
                        {
                            avatarState = ev.OutputStates.GetAvatarState(avatarAddress);
                        }

                        if (!checkBARankingTable)
                        {
                            USDbName = $"{USDbName}_{tip.Index}";
                            var stm33 =
                            $@"CREATE TABLE IF NOT EXISTS `data_provider`.`{USDbName}` (
                              `BlockIndex` bigint NOT NULL,
                              `StakeVersion` varchar(100) NOT NULL,
                              `AgentAddress` varchar(100) NOT NULL,
                              `StakingAmount` decimal(13,2) NOT NULL,
                              `StartedBlockIndex` bigint NOT NULL,
                              `ReceivedBlockIndex` bigint NOT NULL,
                              `CancellableBlockIndex` bigint NOT NULL,
                              `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP
                            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";
                            var cmd33 = new MySqlCommand(stm33, connection);
                            connection.Open();
                            cmd33.CommandTimeout = 300;
                            cmd33.ExecuteScalar();
                            connection.Close();
                            checkBARankingTable = true;
                        }

                        if (!agents.Contains(avatarState.agentAddress.ToString()))
                        {
                            agents.Add(avatarState.agentAddress.ToString());
                            if (ev.OutputStates.TryGetStakeState(avatarState.agentAddress, out StakeState stakeState))
                            {
                                var stakeStateAddress = StakeState.DeriveAddress(avatarState.agentAddress);
                                var currency = ev.OutputStates.GetGoldCurrency();
                                var stakedBalance = ev.OutputStates.GetBalance(stakeStateAddress, currency);
                                _usBulkFile.WriteLine(
                                    $"{tip.Index};" +
                                    "V2;" +
                                    $"{avatarState.agentAddress.ToString()};" +
                                    $"{Convert.ToDecimal(stakedBalance.GetQuantityString())};" +
                                    $"{stakeState.StartedBlockIndex};" +
                                    $"{stakeState.ReceivedBlockIndex};" +
                                    $"{stakeState.CancellableBlockIndex}"
                                );
                            }

                            var agentState = ev.OutputStates.GetAgentState(avatarState.agentAddress);
                            Address monsterCollectionAddress = MonsterCollectionState.DeriveAddress(
                                avatarState.agentAddress,
                                agentState.MonsterCollectionRound
                            );
                            if (ev.OutputStates.TryGetState(monsterCollectionAddress, out Dictionary stateDict))
                            {
                                var mcStates = new MonsterCollectionState(stateDict);
                                var currency = ev.OutputStates.GetGoldCurrency();
                                FungibleAssetValue mcBalance =
                                    ev.OutputStates.GetBalance(monsterCollectionAddress, currency);
                                _usBulkFile.WriteLine(
                                    $"{tip.Index};" +
                                    "V1;" +
                                    $"{avatarState.agentAddress.ToString()};" +
                                    $"{Convert.ToDecimal(mcBalance.GetQuantityString())};" +
                                    $"{mcStates.StartedBlockIndex};" +
                                    $"{mcStates.ReceivedBlockIndex};" +
                                    $"{mcStates.ExpiredBlockIndex}"
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

                var stm11 = $"DROP TABLE IF EXISTS {USDbName}_Dump;";
                var cmd11 = new MySqlCommand(stm11, connection);
                connection.Open();
                cmd11.CommandTimeout = 300;
                cmd11.ExecuteScalar();
                connection.Close();

                var stm12 = $"RENAME TABLE {USDbName} TO {USDbName}_Dump; CREATE TABLE {USDbName} LIKE {USDbName}_Dump;";
                var cmd12 = new MySqlCommand(stm12, connection);
                var startMove = DateTimeOffset.Now;
                connection.Open();
                cmd12.CommandTimeout = 300;
                cmd12.ExecuteScalar();
                connection.Close();
                var endMove = DateTimeOffset.Now;
                Console.WriteLine("Move BattleArenaRanking Complete! Time Elapsed: {0}", endMove - startMove);
                var i = 1;
                foreach (var path in _usFiles)
                {
                    string oldFilePath = path;
                    string newFilePath = Path.Combine(Path.GetTempPath(), $"UserStakings{tip.Index}#{i}.csv");
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

                    BulkInsert(USDbName, path);
                    i += 1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Restoring previous tables due to error...");
                var stm17 = $"DROP TABLE {USDbName}; RENAME TABLE {USDbName}_Dump TO {USDbName};";
                var cmd17 = new MySqlCommand(stm17, connection);
                var startRestore = DateTimeOffset.Now;
                connection.Open();
                cmd17.CommandTimeout = 300;
                cmd17.ExecuteScalar();
                connection.Close();
                var endRestore = DateTimeOffset.Now;
                Console.WriteLine("Restore BattleArenaRanking Complete! Time Elapsed: {0}", endRestore - startRestore);
            }

            var stm18 = $"DROP TABLE {USDbName}_Dump;";
            var cmd18 = new MySqlCommand(stm18, connection);
            var startDelete = DateTimeOffset.Now;
            connection.Open();
            cmd18.CommandTimeout = 300;
            cmd18.ExecuteScalar();
            connection.Close();
            var endDelete = DateTimeOffset.Now;
            Console.WriteLine("Delete BattleArenaRanking_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);

            DateTimeOffset end = DateTimeOffset.UtcNow;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
            Console.WriteLine("Shop Count for {0} avatars: {1}", avatars.Count, shopOrderCount);
        }

        private void FlushBulkFiles()
        {
            _usBulkFile.Flush();
            _usBulkFile.Close();
        }

        private void CreateBulkFiles()
        {

            string usFilePath = Path.GetTempFileName().Replace(".tmp", ".csv");;
            _usBulkFile = new StreamWriter(usFilePath);
            _usFiles.Add(usFilePath);
        }

        public static async Task UploadFileAsync(string token, string path, string channels)
        {
            HttpClient client = new HttpClient();
            // we need to send a request with multipart/form-data
            var multiForm = new MultipartFormDataContent();

            // add API method parameters
            multiForm.Add(new StringContent(token), "token");
            multiForm.Add(new StringContent(channels), "channels");

            // add file and directly upload it
            FileStream fs = File.OpenRead(path);
            multiForm.Add(new StreamContent(fs), "file", Path.GetFileName(path));

            // send request to API
            var url = "https://slack.com/api/files.upload";
            var response = await client.PostAsync(url, multiForm);

            // fetch response from API
            var responseJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseJson);
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
                    LineTerminator = "\n",
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
                    LineTerminator = "\n",
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
