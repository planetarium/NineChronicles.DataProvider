namespace NineChronicles.DataProvider.Tools.SubCommand
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
        private string BARDbName = "BattleArenaRanking";
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain<NCAction> _baseChain;
        private StreamWriter _ccBulkFile;
        private StreamWriter _ceBulkFile;
        private StreamWriter _ieBulkFile;
        private StreamWriter _ueBulkFile;
        private StreamWriter _uctBulkFile;
        private StreamWriter _uiBulkFile;
        private StreamWriter _umBulkFile;
        private StreamWriter _ucBulkFile;
        private StreamWriter _usBulkFile;
        private StreamWriter _umcBulkFile;
        private StreamWriter _uncgBulkFile;
        private StreamWriter _ucyBulkFile;
        private StreamWriter _eBulkFile;
        private StreamWriter _scBulkFile;
        private StreamWriter _seBulkFile;
        private StreamWriter _sctBulkFile;
        private StreamWriter _smBulkFile;
        private StreamWriter _barBulkFile;
        private StreamWriter _agentBulkFile;
        private StreamWriter _avatarBulkFile;
        private List<string> _agentList;
        private List<string> _avatarList;
        private List<string> _ccFiles;
        private List<string> _ceFiles;
        private List<string> _ieFiles;
        private List<string> _ueFiles;
        private List<string> _uctFiles;
        private List<string> _uiFiles;
        private List<string> _umFiles;
        private List<string> _ucFiles;
        private List<string> _usFiles;
        private List<string> _umcFiles;
        private List<string> _uncgFiles;
        private List<string> _ucyFiles;
        private List<string> _eFiles;
        private List<string> _scFiles;
        private List<string> _seFiles;
        private List<string> _sctFiles;
        private List<string> _smFiles;
        private List<string> _barFiles;
        private List<string> _agentFiles;
        private List<string> _avatarFiles;

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
            Block<NCAction> genesis = _baseStore.GetBlock<NCAction>(gHash);
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
            _ccFiles = new List<string>();
            _ceFiles = new List<string>();
            _ieFiles = new List<string>();
            _ueFiles = new List<string>();
            _uctFiles = new List<string>();
            _uiFiles = new List<string>();
            _umFiles = new List<string>();
            _ucFiles = new List<string>();
            _usFiles = new List<string>();
            _umcFiles = new List<string>();
            _uncgFiles = new List<string>();
            _ucyFiles = new List<string>();
            _eFiles = new List<string>();
            _scFiles = new List<string>();
            _seFiles = new List<string>();
            _sctFiles = new List<string>();
            _smFiles = new List<string>();
            _barFiles = new List<string>();
            _agentFiles = new List<string>();
            _avatarFiles = new List<string>();

            // lists to keep track of inserted addresses to minimize duplicates
            _agentList = new List<string>();
            _avatarList = new List<string>();

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

            try
            {
                var tipHash = _baseStore.IndexBlockHash(_baseChain.Id, tipIndex ?? _baseChain.Tip.Index);
                var tip = _baseStore.GetBlock<NCAction>((BlockHash)tipHash);
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

                        if (avatarState != null)
                        {
                            var avatarLevel = avatarState.level;
                            var arenaSheet = ev.OutputStates.GetSheet<ArenaSheet>();
                            var arenaData = arenaSheet.GetRoundByBlockIndex(tip.Index);

                            if (!checkBARankingTable)
                            {
                                BARDbName = $"{BARDbName}_{arenaData.ChampionshipId}_{arenaData.Round}";
                                var stm33 =
                                $@"CREATE TABLE IF NOT EXISTS `data_provider`.`{BARDbName}` (
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
                                var cmd33 = new MySqlCommand(stm33, connection);
                                connection.Open();
                                cmd33.CommandTimeout = 300;
                                cmd33.ExecuteScalar();
                                connection.Close();
                                checkBARankingTable = true;
                            }

                            var arenaScoreAdr =
                                ArenaScore.DeriveAddress(avatarAddress, arenaData.ChampionshipId, arenaData.Round);
                            var arenaInformationAdr =
                                ArenaInformation.DeriveAddress(avatarAddress, arenaData.ChampionshipId, arenaData.Round);
                            ev.OutputStates.TryGetArenaInformation(arenaInformationAdr, out var currentArenaInformation);
                            ev.OutputStates.TryGetArenaScore(arenaScoreAdr, out var outputArenaScore);
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
                            Console.WriteLine($"No Avatar State: {avatarAddress.ToString()}");
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

                var stm23 = $"RENAME TABLE {BARDbName} TO {BARDbName}_Dump; CREATE TABLE {BARDbName} LIKE {BARDbName}_Dump;";
                var cmd23 = new MySqlCommand(stm23, connection);
                var startMove = DateTimeOffset.Now;
                connection.Open();
                cmd23.CommandTimeout = 300;
                cmd23.ExecuteScalar();
                connection.Close();
                var endMove = DateTimeOffset.Now;
                Console.WriteLine("Move BattleArenaRanking Complete! Time Elapsed: {0}", endMove - startMove);
                foreach (var path in _barFiles)
                {
                    BulkInsert(BARDbName, path);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Restoring previous tables due to error...");
                var stm33 = $"DROP TABLE {BARDbName}; RENAME TABLE {BARDbName}_Dump TO {BARDbName};";
                var cmd33 = new MySqlCommand(stm33, connection);
                var startRestore = DateTimeOffset.Now;
                connection.Open();
                cmd33.CommandTimeout = 300;
                cmd33.ExecuteScalar();
                connection.Close();
                var endRestore = DateTimeOffset.Now;
                Console.WriteLine("Restore BattleArenaRanking Complete! Time Elapsed: {0}", endRestore - startRestore);
            }

            var stm34 = $"DROP TABLE {BARDbName}_Dump;";
            var cmd34 = new MySqlCommand(stm34, connection);
            var startDelete = DateTimeOffset.Now;
            connection.Open();
            cmd34.CommandTimeout = 300;
            cmd34.ExecuteScalar();
            connection.Close();
            var endDelete = DateTimeOffset.Now;
            Console.WriteLine("Delete BattleArenaRanking_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);

            DateTimeOffset end = DateTimeOffset.UtcNow;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
            Console.WriteLine("Shop Count for {0} avatars: {1}", avatars.Count, shopOrderCount);
        }

        private void FlushBulkFiles()
        {
            _agentBulkFile.Flush();
            _agentBulkFile.Close();

            _avatarBulkFile.Flush();
            _avatarBulkFile.Close();

            _ccBulkFile.Flush();
            _ccBulkFile.Close();

            _ceBulkFile.Flush();
            _ceBulkFile.Close();

            _ieBulkFile.Flush();
            _ieBulkFile.Close();

            _ueBulkFile.Flush();
            _ueBulkFile.Close();

            _uctBulkFile.Flush();
            _uctBulkFile.Close();

            _uiBulkFile.Flush();
            _uiBulkFile.Close();

            _umBulkFile.Flush();
            _umBulkFile.Close();

            _ucBulkFile.Flush();
            _ucBulkFile.Close();

            _eBulkFile.Flush();
            _eBulkFile.Close();

            _usBulkFile.Flush();
            _usBulkFile.Close();

            _umcBulkFile.Flush();
            _umcBulkFile.Close();

            _uncgBulkFile.Flush();
            _uncgBulkFile.Close();

            _ucyBulkFile.Flush();
            _ucyBulkFile.Close();

            _scBulkFile.Flush();
            _scBulkFile.Close();

            _seBulkFile.Flush();
            _seBulkFile.Close();

            _sctBulkFile.Flush();
            _sctBulkFile.Close();

            _smBulkFile.Flush();
            _smBulkFile.Close();

            _barBulkFile.Flush();
            _barBulkFile.Close();
        }

        private void CreateBulkFiles()
        {
            string agentFilePath = Path.GetTempFileName();
            _agentBulkFile = new StreamWriter(agentFilePath);

            string avatarFilePath = Path.GetTempFileName();
            _avatarBulkFile = new StreamWriter(avatarFilePath);

            string ccFilePath = Path.GetTempFileName();
            _ccBulkFile = new StreamWriter(ccFilePath);

            string ceFilePath = Path.GetTempFileName();
            _ceBulkFile = new StreamWriter(ceFilePath);

            string ieFilePath = Path.GetTempFileName();
            _ieBulkFile = new StreamWriter(ieFilePath);

            string ueFilePath = Path.GetTempFileName();
            _ueBulkFile = new StreamWriter(ueFilePath);

            string uctFilePath = Path.GetTempFileName();
            _uctBulkFile = new StreamWriter(uctFilePath);

            string uiFilePath = Path.GetTempFileName();
            _uiBulkFile = new StreamWriter(uiFilePath);

            string umFilePath = Path.GetTempFileName();
            _umBulkFile = new StreamWriter(umFilePath);

            string ucFilePath = Path.GetTempFileName();
            _ucBulkFile = new StreamWriter(ucFilePath);

            string eFilePath = Path.GetTempFileName();
            _eBulkFile = new StreamWriter(eFilePath);

            string usFilePath = Path.GetTempFileName();
            _usBulkFile = new StreamWriter(usFilePath);

            string umcFilePath = Path.GetTempFileName();
            _umcBulkFile = new StreamWriter(umcFilePath);

            string uncgFilePath = Path.GetTempFileName();
            _uncgBulkFile = new StreamWriter(uncgFilePath);

            string ucyFilePath = Path.GetTempFileName();
            _ucyBulkFile = new StreamWriter(ucyFilePath);

            string scFilePath = Path.GetTempFileName();
            _scBulkFile = new StreamWriter(scFilePath);

            string seFilePath = Path.GetTempFileName();
            _seBulkFile = new StreamWriter(seFilePath);

            string sctFilePath = Path.GetTempFileName();
            _sctBulkFile = new StreamWriter(sctFilePath);

            string smFilePath = Path.GetTempFileName();
            _smBulkFile = new StreamWriter(smFilePath);

            string barFilePath = Path.GetTempFileName();
            _barBulkFile = new StreamWriter(barFilePath);

            _agentFiles.Add(agentFilePath);
            _avatarFiles.Add(avatarFilePath);
            _ccFiles.Add(ccFilePath);
            _ceFiles.Add(ceFilePath);
            _ieFiles.Add(ieFilePath);
            _ueFiles.Add(ueFilePath);
            _uctFiles.Add(uctFilePath);
            _uiFiles.Add(uiFilePath);
            _umFiles.Add(umFilePath);
            _ucFiles.Add(ucFilePath);
            _eFiles.Add(eFilePath);
            _usFiles.Add(usFilePath);
            _umcFiles.Add(umcFilePath);
            _uncgFiles.Add(uncgFilePath);
            _ucyFiles.Add(ucyFilePath);
            _scFiles.Add(scFilePath);
            _seFiles.Add(seFilePath);
            _sctFiles.Add(sctFilePath);
            _smFiles.Add(smFilePath);
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
                Console.WriteLine($"Start bulk insert to {tableName}.");
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

        private void WriteEquipment(
            Equipment equipment,
            Address agentAddress,
            Address avatarAddress)
        {
            try
            {
                _ueBulkFile.WriteLine(
                    $"{equipment.ItemId.ToString()};" +
                    $"{agentAddress.ToString()};" +
                    $"{avatarAddress.ToString()};" +
                    $"{equipment.ItemType.ToString()};" +
                    $"{equipment.ItemSubType.ToString()};" +
                    $"{equipment.Id};" +
                    $"{equipment.BuffSkills.Count};" +
                    $"{equipment.ElementalType.ToString()};" +
                    $"{equipment.Grade};" +
                    $"{equipment.level};" +
                    $"{equipment.SetId};" +
                    $"{equipment.Skills.Count};" +
                    $"{equipment.SpineResourcePath};" +
                    $"{equipment.RequiredBlockIndex};" +
                    $"{equipment.NonFungibleId.ToString()};" +
                    $"{equipment.TradableId.ToString()};" +
                    $"{equipment.UniqueStatType.ToString()}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void WriteRankingEquipment(
            Equipment equipment,
            Address agentAddress,
            Address avatarAddress,
            int equipmentCp)
        {
            try
            {
                _eBulkFile.WriteLine(
                    $"{equipment.ItemId.ToString()};" +
                    $"{agentAddress.ToString()};" +
                    $"{avatarAddress.ToString()};" +
                    $"{equipment.Id};" +
                    $"{equipmentCp};" +
                    $"{equipment.level};" +
                    $"{equipment.ItemSubType.ToString()}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void WriteCostume(
            Costume costume,
            Address agentAddress,
            Address avatarAddress)
        {
            try
            {
                _uctBulkFile.WriteLine(
                    $"{costume.ItemId.ToString()};" +
                    $"{agentAddress.ToString()};" +
                    $"{avatarAddress.ToString()};" +
                    $"{costume.ItemType.ToString()};" +
                    $"{costume.ItemSubType.ToString()};" +
                    $"{costume.Id};" +
                    $"{costume.ElementalType.ToString()};" +
                    $"{costume.Grade};" +
                    $"{costume.Equipped};" +
                    $"{costume.SpineResourcePath};" +
                    $"{costume.RequiredBlockIndex};" +
                    $"{costume.NonFungibleId.ToString()};" +
                    $"{costume.TradableId.ToString()}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void WriteMaterial(
            Material material,
            Address agentAddress,
            Address avatarAddress)
        {
            try
            {
                _umBulkFile.WriteLine(
                    $"{material.ItemId.ToString()};" +
                    $"{agentAddress.ToString()};" +
                    $"{avatarAddress.ToString()};" +
                    $"{material.ItemType.ToString()};" +
                    $"{material.ItemSubType.ToString()};" +
                    $"{material.Id};" +
                    $"{material.ElementalType.ToString()};" +
                    $"{material.Grade}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void WriteConsumable(
            Consumable consumable,
            Address agentAddress,
            Address avatarAddress)
        {
            try
            {
                _ucBulkFile.WriteLine(
                    $"{consumable.ItemId.ToString()};" +
                    $"{agentAddress.ToString()};" +
                    $"{avatarAddress.ToString()};" +
                    $"{consumable.ItemType.ToString()};" +
                    $"{consumable.ItemSubType.ToString()};" +
                    $"{consumable.Id};" +
                    $"{consumable.BuffSkills.Count};" +
                    $"{consumable.ElementalType.ToString()};" +
                    $"{consumable.Grade};" +
                    $"{consumable.Skills.Count};" +
                    $"{consumable.RequiredBlockIndex};" +
                    $"{consumable.NonFungibleId.ToString()};" +
                    $"{consumable.TradableId.ToString()};" +
                    $"{consumable.MainStat.ToString()}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        private void WriteAgent(
            Address? agentAddress)
        {
            if (agentAddress == null)
            {
                return;
            }

            // _agentBulkFile.WriteLine(
            //     $"{agentAddress.ToString()}");
            // _agentList.Add(agentAddress.ToString());
            // check if address is already in _agentList
            if (!_agentList.Contains(agentAddress.ToString()))
            {
                _agentBulkFile.WriteLine(
                    $"{agentAddress.ToString()}");
                _agentList.Add(agentAddress.ToString());
            }
        }

        private void WriteAvatar(
            Address? agentAddress,
            Address? avatarAddress,
            string avatarName,
            int avatarLevel,
            int? avatarTitleId,
            int avatarArmorId,
            int avatarCp)
        {
            if (agentAddress == null)
            {
                return;
            }

            if (avatarAddress == null)
            {
                return;
            }

            if (!_avatarList.Contains(avatarAddress.ToString()))
            {
                _avatarBulkFile.WriteLine(
                    $"{avatarAddress.ToString()};" +
                    $"{agentAddress.ToString()};" +
                    $"{avatarName};" +
                    $"{avatarLevel};" +
                    $"{avatarTitleId};" +
                    $"{avatarArmorId};" +
                    $"{avatarCp}");
                _avatarList.Add(avatarAddress.ToString());
            }
        }

        private void WriteCE(
            Guid id,
            Address agentAddress,
            Address avatarAddress,
            int recipeId,
            int slotIndex,
            int? subRecipeId,
            long blockIndex)
        {
            // check if address is already in _agentList
            if (!_agentList.Contains(agentAddress.ToString()))
            {
                _agentBulkFile.WriteLine(
                    $"{agentAddress.ToString()};");
                _agentList.Add(agentAddress.ToString());
            }

            // check if address is already in _avatarList
            if (!_avatarList.Contains(avatarAddress.ToString()))
            {
                _avatarBulkFile.WriteLine(
                    $"{avatarAddress.ToString()};" +
                    $"{agentAddress.ToString()};" +
                    "N/A");
                _avatarList.Add(avatarAddress.ToString());
            }

            _ceBulkFile.WriteLine(
                $"{id.ToString()};" +
                $"{avatarAddress.ToString()};" +
                $"{agentAddress.ToString()};" +
                $"{recipeId};" +
                $"{slotIndex};" +
                $"{subRecipeId ?? 0};" +
                $"{blockIndex.ToString()}");
            Console.WriteLine("Writing CE action in block #{0}", blockIndex);
        }

        private void WriteIE(
            Guid id,
            Address agentAddress,
            Address avatarAddress,
            Guid itemId,
            Guid materialId,
            int slotIndex,
            long blockIndex)
        {
            // check if address is already in _agentList
            if (!_agentList.Contains(agentAddress.ToString()))
            {
                _agentBulkFile.WriteLine(
                    $"{agentAddress.ToString()};");
                _agentList.Add(agentAddress.ToString());
            }

            // check if address is already in _avatarList
            if (!_avatarList.Contains(avatarAddress.ToString()))
            {
                _avatarBulkFile.WriteLine(
                    $"{avatarAddress.ToString()};" +
                    $"{agentAddress.ToString()};" +
                    "N/A");
                _avatarList.Add(avatarAddress.ToString());
            }

            _ieBulkFile.WriteLine(
                $"{id.ToString()};" +
                $"{avatarAddress.ToString()};" +
                $"{agentAddress.ToString()};" +
                $"{itemId.ToString()};" +
                $"{materialId.ToString()};" +
                $"{slotIndex};" +
                $"{blockIndex.ToString()}");
            Console.WriteLine("Writing IE action in block #{0}", blockIndex);
        }
    }
}
