namespace NineChronicles.DataProvider.Executable.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Cocona;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Crypto;
    using Libplanet.RocksDBStore;
    using Libplanet.Store;
    using Libplanet.Types.Blocks;
    using MySqlConnector;
    using Nekoyume.Action;
    using Nekoyume.Action.Loader;
    using Nekoyume.Blockchain.Policy;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Serilog.Events;

    public class BattleArenaRankingMigration
    {
        private string _battleArenaRankingTableName = "BattleArenaRanking";
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain _baseChain;
        private StreamWriter _barBulkFile;
        private List<string> _barFiles;

        [Command(Description = "Migrate battle arena ranking data at a specific block index to a mysql database.")]
        public void Migration(
            [Option('o', Description = "RocksDB store path to migrate.")]
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
                Description = "Name of MySQL database.")]
            string mysqlDatabase,
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

            Log.Debug("Setting up RocksDBStore...");
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
                Log.Error("There is no canonical chain: {0}", storePath);
                Environment.Exit(1);
                return;
            }

            if (!(_baseStore.IndexBlockHash(chainId, 0) is { } gHash))
            {
                Log.Error("There is no genesis block: {0}", storePath);
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
                _ => blockPolicy.BlockAction,
                blockChainStates,
                new NCActionLoader());
            _baseChain = new BlockChain(blockPolicy, stagePolicy, _baseStore, baseStateStore, genesis, blockChainStates, actionEvaluator);

            // Prepare block hashes to append to new chain
            long height = _baseChain.Tip.Index;
            if (migrationBlockIndex > (int)height)
            {
                Log.Error(
                    "The block index point to migrate is greater than the chain tip index: {0}",
                    height);
                Environment.Exit(1);
                return;
            }

            Log.Debug("Start migration.");

            // files to store bulk file paths (new file created every 10000 blocks for bulk load performance)
            _barFiles = new List<string>();

            CreateBulkFiles();

            using MySqlConnection connection = new MySqlConnection(_connectionString);
            connection.Open();

            var statement = "SELECT `Address` from Avatars";
            var command = new MySqlCommand(statement, connection);

            var commandReader = command.ExecuteReader();
            List<string> avatars = new List<string>();

            while (commandReader.Read())
            {
                Log.Debug("{0}", commandReader.GetString(0));
                avatars.Add(commandReader.GetString(0).Replace("0x", string.Empty));
            }

            connection.Close();

            try
            {
                var tipHash = _baseStore.IndexBlockHash(_baseChain.Id, migrationBlockIndex ?? _baseChain.Tip.Index);
                var tip = _baseStore.GetBlock((BlockHash)tipHash!);
                var blockEvaluation = _baseChain.EvaluateBlock(tip);
                var evaluation = blockEvaluation.Last();
                var avatarCount = 0;
                AvatarState avatarState;
                bool checkBattleArenaRankingTable = false;

                foreach (var avatar in avatars)
                {
                    try
                    {
                        avatarCount++;
                        Log.Debug("Migrating {0}/{1}", avatarCount, avatars.Count);
                        var avatarAddress = new Address(avatar);
                        try
                        {
                            avatarState = evaluation.OutputState.GetAvatarStateV2(avatarAddress);
                        }
                        catch (Exception)
                        {
                            avatarState = evaluation.OutputState.GetAvatarState(avatarAddress);
                        }

                        if (avatarState != null)
                        {
                            var avatarLevel = avatarState.level;
                            var arenaSheet = evaluation.OutputState.GetSheet<ArenaSheet>();
                            var arenaData = arenaSheet.GetRoundByBlockIndex(tip.Index);

                            if (!checkBattleArenaRankingTable)
                            {
                                _battleArenaRankingTableName = $"{_battleArenaRankingTableName}_{arenaData.ChampionshipId}_{arenaData.Round}";
                                var statement1 =
                                $@"CREATE TABLE IF NOT EXISTS `data_provider`.`{_battleArenaRankingTableName}` (
                                    `BlockIndex` bigint NOT NULL,
                                    `AgentAddress` varchar(100) NOT NULL,
                                    `AvatarAddress` varchar(100) NOT NULL,
                                    `AvatarLevel` int NOT NULL,
                                    `ChampionshipId` int NOT NULL,
                                    `Round` int NOT NULL,
                                    `ArenaType` varchar(100) NOT NULL,
                                    `Score` int NOT NULL,
                                    `WinCount` int NOT NULL,
                                    `MedalCount` int NOT NULL,
                                    `LossCount` int NOT NULL,
                                    `Ticket` int NOT NULL,
                                    `PurchasedTicketCount` int NOT NULL,
                                    `TicketResetCount` int NOT NULL,
                                    `EntranceFee` bigint NOT NULL,
                                    `TicketPrice` bigint NOT NULL,
                                    `AdditionalTicketPrice` bigint NOT NULL,
                                    `RequiredMedalCount` int NOT NULL,
                                    `StartBlockIndex` bigint NOT NULL,
                                    `EndBlockIndex` bigint NOT NULL,
                                    `Ranking` int NOT NULL,
                                    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                    KEY `fk_BattleArenaRanking_Agent1_idx` (`AgentAddress`),
                                    KEY `fk_BattleArenaRanking_AvatarAddress1_idx` (`AvatarAddress`)
                                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";
                                var command1 = new MySqlCommand(statement1, connection);
                                connection.Open();
                                command1.CommandTimeout = 300;
                                command1.ExecuteScalar();
                                connection.Close();
                                checkBattleArenaRankingTable = true;
                            }

                            var arenaScoreAdr =
                                ArenaScore.DeriveAddress(avatarAddress, arenaData.ChampionshipId, arenaData.Round);
                            var arenaInformationAdr =
                                ArenaInformation.DeriveAddress(avatarAddress, arenaData.ChampionshipId, arenaData.Round);
                            evaluation.OutputState.TryGetArenaInformation(arenaInformationAdr, out var currentArenaInformation);
                            evaluation.OutputState.TryGetArenaScore(arenaScoreAdr, out var outputArenaScore);
                            if (currentArenaInformation != null && outputArenaScore != null)
                            {
                                _barBulkFile.WriteLine(
                                    $"{tip.Index};" +
                                    $"{avatarState.agentAddress.ToString()};" +
                                    $"{avatarAddress.ToString()};" +
                                    $"{avatarLevel};" +
                                    $"{arenaData.ChampionshipId};" +
                                    $"{arenaData.Round};" +
                                    $"{arenaData.ArenaType.ToString()};" +
                                    $"{outputArenaScore.Score};" +
                                    $"{currentArenaInformation.Win};" +
                                    $"{currentArenaInformation.Win};" +
                                    $"{currentArenaInformation.Lose};" +
                                    $"{currentArenaInformation.Ticket};" +
                                    $"{currentArenaInformation.PurchasedTicketCount};" +
                                    $"{currentArenaInformation.TicketResetCount};" +
                                    $"{arenaData.EntranceFee};" +
                                    $"{arenaData.TicketPrice};" +
                                    $"{arenaData.AdditionalTicketPrice};" +
                                    $"{arenaData.RequiredMedalCount};" +
                                    $"{arenaData.StartBlockIndex};" +
                                    $"{arenaData.EndBlockIndex};" +
                                    $"{0};" +
                                    $"{tip.Timestamp.UtcDateTime:yyyy-MM-dd}"
                                );
                            }
                        }
                        else
                        {
                            Log.Debug($"No Avatar State: {avatarAddress.ToString()}");
                        }

                        Log.Debug("Migrating Complete {0}/{1}", avatarCount, avatars.Count);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex.Message);
                    }
                }

                FlushBulkFiles();
                DateTimeOffset postDataPrep = DateTimeOffset.Now;
                Log.Debug("Data Preparation Complete! Time Elapsed: {0}", postDataPrep - start);

                var statement2 = $"RENAME TABLE {_battleArenaRankingTableName} TO {_battleArenaRankingTableName}_Dump; CREATE TABLE {_battleArenaRankingTableName} LIKE {_battleArenaRankingTableName}_Dump;";
                var command2 = new MySqlCommand(statement2, connection);
                var startMove = DateTimeOffset.Now;
                connection.Open();
                command2.CommandTimeout = 300;
                command2.ExecuteScalar();
                connection.Close();
                var endMove = DateTimeOffset.Now;
                Log.Debug("Move BattleArenaRanking Complete! Time Elapsed: {0}", endMove - startMove);
                foreach (var path in _barFiles)
                {
                    BulkInsert(_battleArenaRankingTableName, path);
                }
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
                Log.Debug("Restoring previous tables due to error...");
                var statement1 = $"DROP TABLE {_battleArenaRankingTableName}; RENAME TABLE {_battleArenaRankingTableName}_Dump TO {_battleArenaRankingTableName};";
                var command1 = new MySqlCommand(statement1, connection);
                var startRestore = DateTimeOffset.Now;
                connection.Open();
                command1.CommandTimeout = 300;
                command1.ExecuteScalar();
                connection.Close();
                var endRestore = DateTimeOffset.Now;
                Log.Debug("Restore BattleArenaRanking Complete! Time Elapsed: {0}", endRestore - startRestore);
            }

            var statement3 = $"DROP TABLE {_battleArenaRankingTableName}_Dump;";
            var command3 = new MySqlCommand(statement3, connection);
            var startDelete = DateTimeOffset.Now;
            connection.Open();
            command3.CommandTimeout = 300;
            command3.ExecuteScalar();
            connection.Close();
            var endDelete = DateTimeOffset.Now;
            Log.Debug("Delete BattleArenaRanking_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);

            DateTimeOffset end = DateTimeOffset.UtcNow;
            Log.Debug("Migration Complete! Time Elapsed: {0}", end - start);
        }

        private void FlushBulkFiles()
        {
            _barBulkFile.Flush();
            _barBulkFile.Close();
        }

        private void CreateBulkFiles()
        {
            string barFilePath = Path.GetRandomFileName();
            _barBulkFile = new StreamWriter(barFilePath);
            _barFiles.Add(barFilePath);
        }

        private void BulkInsert(
            string tableName,
            string filePath)
        {
            using MySqlConnection connection = new MySqlConnection(_connectionString);
            try
            {
                DateTimeOffset start = DateTimeOffset.Now;
                Log.Debug($"Start bulk insert to {tableName}.");
                MySqlBulkLoader loader = new MySqlBulkLoader(connection)
                {
                    TableName = tableName,
                    FileName = filePath,
                    Timeout = 0,
                    LineTerminator = Environment.OSVersion.VersionString.Contains("Win") ? "\r\n" : "\n",
                    FieldTerminator = ";",
                    Local = true,
                    ConflictOption = MySqlBulkLoaderConflictOption.Ignore,
                };

                loader.Load();
                Log.Debug($"Bulk load to {tableName} complete.");
                DateTimeOffset end = DateTimeOffset.Now;
                Log.Debug("Time elapsed: {0}", end - start);
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
                Log.Debug($"Bulk load to {tableName} failed. Retry bulk insert");
                DateTimeOffset start = DateTimeOffset.Now;
                Log.Debug($"Start bulk insert to {tableName}.");
                MySqlBulkLoader loader = new MySqlBulkLoader(connection)
                {
                    TableName = tableName,
                    FileName = filePath,
                    Timeout = 0,
                    LineTerminator = Environment.OSVersion.VersionString.Contains("Win") ? "\r\n" : "\n",
                    FieldTerminator = ";",
                    Local = true,
                    ConflictOption = MySqlBulkLoaderConflictOption.Ignore,
                };

                loader.Load();
                Log.Debug($"Bulk load to {tableName} complete.");
                DateTimeOffset end = DateTimeOffset.Now;
                Log.Debug("Time elapsed: {0}", end - start);
            }
        }
    }
}
