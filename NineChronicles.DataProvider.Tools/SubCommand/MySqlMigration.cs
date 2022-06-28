using Nekoyume;
using Nekoyume.TableData;

namespace NineChronicles.DataProvider.Tools.SubCommand
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Cocona;
    using Libplanet;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.RocksDBStore;
    using Libplanet.Store;
    using MySqlConnector;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.BlockChain.Policy;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Serilog;
    using Serilog.Events;
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

    public class MySqlMigration
    {
        private const string AgentDbName = "Agents";
        private const string AvatarDbName = "Avatars";
        private const string CCDbName = "CombinationConsumables";
        private const string CEDbName = "CombinationEquipments";
        private const string IEDbName = "ItemEnhancements";
        private const string UEDbName = "UserEquipments";
        private const string UCTDbName = "UserCostumes";
        private const string UMDbName = "UserMaterials";
        private const string UCDbName = "UserConsumables";
        private const string USDbName = "UserStakings";
        private const string EDbName = "Equipments";
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
        private StreamWriter _eBulkFile;
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
        private List<string> _eFiles;
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
                "index",
                Description = "index of block (no entry will migrate to the chain tip).")]
            long index = 4370015
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
            Block<NCAction> genesis = _baseStore.GetBlock<NCAction>(blockPolicy.GetHashAlgorithm, gHash);
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
            // _ccFiles = new List<string>();
            // _ceFiles = new List<string>();
            // _ieFiles = new List<string>();
            // _ueFiles = new List<string>();
            // _uctFiles = new List<string>();
            // _uiFiles = new List<string>();
            // _umFiles = new List<string>();
            // _ucFiles = new List<string>();
            // _usFiles = new List<string>();
            // _eFiles = new List<string>();
            // _agentFiles = new List<string>();
            // _avatarFiles = new List<string>();
            //
            // // lists to keep track of inserted addresses to minimize duplicates
            // _agentList = new List<string>();
            // _avatarList = new List<string>();
            //
            // CreateBulkFiles();
            //
            // using MySqlConnection connection = new MySqlConnection(_connectionString);
            // connection.Open();
            //
            // var stm = "SELECT `Address` from Avatars";
            // var cmd = new MySqlCommand(stm, connection);
            //
            // var rdr = cmd.ExecuteReader();
            // List<string> avatars = new List<string>();
            // List<string> agents = new List<string>();
            //
            // while (rdr.Read())
            // {
            //     Console.WriteLine("{0}", rdr.GetString(0));
            //     avatars.Add(rdr.GetString(0).Replace("0x", string.Empty));
            // }
            //
            // connection.Close();

            try
            {
                var tipHash = _baseStore.IndexBlockHash(_baseChain.Id, _baseChain.Tip.Index);
                var tip = _baseStore.GetBlock<NCAction>(blockPolicy.GetHashAlgorithm, (BlockHash)tipHash);
                var shopTipHash = _baseStore.IndexBlockHash(_baseChain.Id, index);
                var shopTip = _baseStore.GetBlock<NCAction>(blockPolicy.GetHashAlgorithm, (BlockHash)shopTipHash);
                var exec = _baseChain.ExecuteActions(tip);
                var ev = exec.First();
                var shopExec = _baseChain.ExecuteActions(shopTip);
                var shopEv = shopExec.First();
                var avatarCount = 0;
                AvatarState avatarState;
                int interval = 1000000;
                int intervalCount = 0;
                var arenaSheet = shopEv.OutputStates.GetSheet<ArenaSheet>();
                var arenaData = arenaSheet.GetRoundByBlockIndex(shopTip.Index);
                var feeStoreAddress = Addresses.GetShopFeeAddress(arenaData.ChampionshipId, arenaData.Round);
                Console.WriteLine("ShopAddress: {0}", feeStoreAddress);

                // foreach (var avatar in avatars)
                // {
                //     try
                //     {
                //         intervalCount++;
                //         avatarCount++;
                //         Console.WriteLine("Interval Count {0} ShopAddress: {1}", intervalCount, feeStoreAddress);
                //         Console.WriteLine("Migrating {0}/{1} ShopAddress: {2}", avatarCount, avatars.Count, feeStoreAddress);
                //         var avatarAddress = new Address(avatar);
                //         try
                //         {
                //             avatarState = ev.OutputStates.GetAvatarStateV2(avatarAddress);
                //         }
                //         catch (Exception ex)
                //         {
                //             avatarState = ev.OutputStates.GetAvatarState(avatarAddress);
                //         }
                //
                //         var userEquipments = avatarState.inventory.Equipments;
                //         var userCostumes = avatarState.inventory.Costumes;
                //         var userMaterials = avatarState.inventory.Materials;
                //         var userConsumables = avatarState.inventory.Consumables;
                //
                //         foreach (var equipment in userEquipments)
                //         {
                //             var equipmentCp = CPHelper.GetCP(equipment);
                //             WriteEquipment(equipment, avatarState.agentAddress, avatarAddress);
                //             WriteRankingEquipment(equipment, avatarState.agentAddress, avatarAddress, equipmentCp);
                //         }
                //
                //         foreach (var costume in userCostumes)
                //         {
                //             WriteCostume(costume, avatarState.agentAddress, avatarAddress);
                //         }
                //
                //         foreach (var material in userMaterials)
                //         {
                //             WriteMaterial(material, avatarState.agentAddress, avatarAddress);
                //         }
                //
                //         foreach (var consumable in userConsumables)
                //         {
                //             WriteConsumable(consumable, avatarState.agentAddress, avatarAddress);
                //         }
                //
                //         if (!agents.Contains(avatarState.agentAddress.ToString()))
                //         {
                //             agents.Add(avatarState.agentAddress.ToString());
                //             if (ev.OutputStates.TryGetStakeState(avatarState.agentAddress, out StakeState stakeState))
                //             {
                //                 var stakeStateAddress = StakeState.DeriveAddress(avatarState.agentAddress);
                //                 var currency = ev.OutputStates.GetGoldCurrency();
                //                 var stakedBalance = ev.OutputStates.GetBalance(stakeStateAddress, currency);
                //                 _usBulkFile.WriteLine(
                //                     $"{tip.Index};" +
                //                     $"{avatarState.agentAddress.ToString()};" +
                //                     $"{Convert.ToDecimal(stakedBalance.GetQuantityString())};" +
                //                     $"{stakeState.StartedBlockIndex};" +
                //                     $"{stakeState.ReceivedBlockIndex};" +
                //                     $"{stakeState.CancellableBlockIndex}"
                //                 );
                //             }
                //         }
                //
                //         Console.WriteLine("Migrating Complete {0}/{1} ShopAddress: {2}", avatarCount, avatars.Count, feeStoreAddress);
                //     }
                //     catch (Exception ex)
                //     {
                //         Console.WriteLine(ex.Message);
                //     }
                //
                //     if (intervalCount == interval)
                //     {
                //         FlushBulkFiles();
                //         foreach (var path in _agentFiles)
                //         {
                //             BulkInsert(AgentDbName, path);
                //         }
                //
                //         foreach (var path in _avatarFiles)
                //         {
                //             BulkInsert(AvatarDbName, path);
                //         }
                //
                //         foreach (var path in _ueFiles)
                //         {
                //             BulkInsert(UEDbName, path);
                //         }
                //
                //         foreach (var path in _uctFiles)
                //         {
                //             BulkInsert(UCTDbName, path);
                //         }
                //
                //         foreach (var path in _umFiles)
                //         {
                //             BulkInsert(UMDbName, path);
                //         }
                //
                //         foreach (var path in _ucFiles)
                //         {
                //             BulkInsert(UCDbName, path);
                //         }
                //
                //         foreach (var path in _eFiles)
                //         {
                //             BulkInsert(EDbName, path);
                //         }
                //
                //         foreach (var path in _usFiles)
                //         {
                //             BulkInsert(USDbName, path);
                //         }
                //
                //         _agentFiles.RemoveAt(0);
                //         _avatarFiles.RemoveAt(0);
                //         _ueFiles.RemoveAt(0);
                //         _uctFiles.RemoveAt(0);
                //         _umFiles.RemoveAt(0);
                //         _ucFiles.RemoveAt(0);
                //         _eFiles.RemoveAt(0);
                //         _usFiles.RemoveAt(0);
                //         CreateBulkFiles();
                //         intervalCount = 0;
                //     }
                // }
                //
                // FlushBulkFiles();
                // DateTimeOffset postDataPrep = DateTimeOffset.Now;
                // Console.WriteLine("Data Preparation Complete! Time Elapsed: {0}", postDataPrep - start);
                //
                // var stm2 = $"RENAME TABLE {UEDbName} TO {UEDbName}_Dump; CREATE TABLE {UEDbName} LIKE {UEDbName}_Dump;";
                // var stm3 = $"RENAME TABLE {UCTDbName} TO {UCTDbName}_Dump; CREATE TABLE {UCTDbName} LIKE {UCTDbName}_Dump;";
                // var stm4 = $"RENAME TABLE {UMDbName} TO {UMDbName}_Dump; CREATE TABLE {UMDbName} LIKE {UMDbName}_Dump;";
                // var stm5 = $"RENAME TABLE {UCDbName} TO {UCDbName}_Dump; CREATE TABLE {UCDbName} LIKE {UCDbName}_Dump;";
                // var stm6 = $"RENAME TABLE {EDbName} TO {EDbName}_Dump; CREATE TABLE {EDbName} LIKE {EDbName}_Dump;";
                // var stm12 = $"RENAME TABLE {USDbName} TO {USDbName}_Dump; CREATE TABLE {USDbName} LIKE {USDbName}_Dump;";
                // var cmd2 = new MySqlCommand(stm2, connection);
                // var cmd3 = new MySqlCommand(stm3, connection);
                // var cmd4 = new MySqlCommand(stm4, connection);
                // var cmd5 = new MySqlCommand(stm5, connection);
                // var cmd6 = new MySqlCommand(stm6, connection);
                // var cmd12 = new MySqlCommand(stm12, connection);
                //
                // foreach (var path in _agentFiles)
                // {
                //     BulkInsert(AgentDbName, path);
                // }
                //
                // foreach (var path in _avatarFiles)
                // {
                //     BulkInsert(AvatarDbName, path);
                // }
                //
                // var startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd2.CommandTimeout = 300;
                // cmd2.ExecuteScalar();
                // connection.Close();
                // var endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move UserEquipments Complete! Time Elapsed: {0}", endMove - startMove);
                //
                // foreach (var path in _ueFiles)
                // {
                //     BulkInsert(UEDbName, path);
                // }
                //
                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd3.CommandTimeout = 300;
                // cmd3.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move UserCostumes Complete! Time Elapsed: {0}", endMove - startMove);
                // foreach (var path in _uctFiles)
                // {
                //     BulkInsert(UCTDbName, path);
                // }
                //
                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd4.CommandTimeout = 300;
                // cmd4.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move UserMaterials Complete! Time Elapsed: {0}", endMove - startMove);
                // foreach (var path in _umFiles)
                // {
                //     BulkInsert(UMDbName, path);
                // }
                //
                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd5.CommandTimeout = 300;
                // cmd5.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move UserConsumables Complete! Time Elapsed: {0}", endMove - startMove);
                // foreach (var path in _ucFiles)
                // {
                //     BulkInsert(UCDbName, path);
                // }
                //
                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd6.CommandTimeout = 300;
                // cmd6.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move Equipments Complete! Time Elapsed: {0}", endMove - startMove);
                // foreach (var path in _eFiles)
                // {
                //     BulkInsert(EDbName, path);
                // }
                //
                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd12.CommandTimeout = 300;
                // cmd12.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move UserStakings Complete! Time Elapsed: {0}", endMove - startMove);
                // foreach (var path in _usFiles)
                // {
                //     BulkInsert(USDbName, path);
                // }
            }
            catch (Exception e)
            {
                // Console.WriteLine(e.Message);
                // Console.WriteLine("Restoring previous tables due to error...");
                // var stm12 = $"DROP TABLE {UEDbName}; RENAME TABLE {UEDbName}_Dump TO {UEDbName};";
                // var stm13 = $"DROP TABLE {EDbName}; RENAME TABLE {UCTDbName}_Dump TO {UCTDbName};";
                // var stm14 = $"DROP TABLE {EDbName}; RENAME TABLE {UMDbName}_Dump TO {UMDbName};";
                // var stm15 = $"DROP TABLE {EDbName}; RENAME TABLE {UCDbName}_Dump TO {UCDbName};";
                // var stm16 = $"DROP TABLE {EDbName}; RENAME TABLE {EDbName}_Dump TO {EDbName};";
                // var stm17 = $"DROP TABLE {USDbName}; RENAME TABLE {USDbName}_Dump TO {USDbName};";
                // var cmd12 = new MySqlCommand(stm12, connection);
                // var cmd13 = new MySqlCommand(stm13, connection);
                // var cmd14 = new MySqlCommand(stm14, connection);
                // var cmd15 = new MySqlCommand(stm15, connection);
                // var cmd16 = new MySqlCommand(stm16, connection);
                // var cmd17 = new MySqlCommand(stm17, connection);
                // var startRestore = DateTimeOffset.Now;
                // connection.Open();
                // cmd12.CommandTimeout = 300;
                // cmd12.ExecuteScalar();
                // connection.Close();
                // var endRestore = DateTimeOffset.Now;
                // Console.WriteLine("Restore UserEquipments Complete! Time Elapsed: {0}", endRestore - startRestore);
                // startRestore = DateTimeOffset.Now;
                // connection.Open();
                // cmd13.CommandTimeout = 300;
                // cmd13.ExecuteScalar();
                // connection.Close();
                // endRestore = DateTimeOffset.Now;
                // Console.WriteLine("Restore UserCostumes Complete! Time Elapsed: {0}", endRestore - startRestore);
                // startRestore = DateTimeOffset.Now;
                // connection.Open();
                // cmd14.CommandTimeout = 300;
                // cmd14.ExecuteScalar();
                // connection.Close();
                // endRestore = DateTimeOffset.Now;
                // Console.WriteLine("Restore UserMaterials Complete! Time Elapsed: {0}", endRestore - startRestore);
                // startRestore = DateTimeOffset.Now;
                // connection.Open();
                // cmd15.CommandTimeout = 300;
                // cmd15.ExecuteScalar();
                // connection.Close();
                // endRestore = DateTimeOffset.Now;
                // Console.WriteLine("Restore UserConsumables Complete! Time Elapsed: {0}", endRestore - startRestore);
                // startRestore = DateTimeOffset.Now;
                // connection.Open();
                // cmd16.CommandTimeout = 300;
                // cmd16.ExecuteScalar();
                // connection.Close();
                // endRestore = DateTimeOffset.Now;
                // Console.WriteLine("Restore Equipments Complete! Time Elapsed: {0}", endRestore - startRestore);
                // startRestore = DateTimeOffset.Now;
                // connection.Open();
                // cmd17.CommandTimeout = 300;
                // cmd17.ExecuteScalar();
                // connection.Close();
                // endRestore = DateTimeOffset.Now;
                // Console.WriteLine("Restore UserStakings Complete! Time Elapsed: {0}", endRestore - startRestore);
            }

            // var stm7 = $"DROP TABLE {UEDbName}_Dump;";
            // var stm8 = $"DROP TABLE {UCTDbName}_Dump;";
            // var stm9 = $"DROP TABLE {UMDbName}_Dump;";
            // var stm10 = $"DROP TABLE {UCDbName}_Dump;";
            // var stm11 = $"DROP TABLE {EDbName}_Dump;";
            // var stm18 = $"DROP TABLE {USDbName}_Dump;";
            // var cmd7 = new MySqlCommand(stm7, connection);
            // var cmd8 = new MySqlCommand(stm8, connection);
            // var cmd9 = new MySqlCommand(stm9, connection);
            // var cmd10 = new MySqlCommand(stm10, connection);
            // var cmd11 = new MySqlCommand(stm11, connection);
            // var cmd18 = new MySqlCommand(stm18, connection);
            // var startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd7.CommandTimeout = 300;
            // cmd7.ExecuteScalar();
            // connection.Close();
            // var endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete UserEquipments_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd8.CommandTimeout = 300;
            // cmd8.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete UserCostumes_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd9.CommandTimeout = 300;
            // cmd9.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete UserMaterials_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd10.CommandTimeout = 300;
            // cmd10.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete UserConsumables_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd11.CommandTimeout = 300;
            // cmd11.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete Equipments_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd18.CommandTimeout = 300;
            // cmd18.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete UserStakings_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            //
            // DateTimeOffset end = DateTimeOffset.UtcNow;
            // Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
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
