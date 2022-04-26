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
            int? limit = null
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
            _ccFiles = new List<string>();
            _ceFiles = new List<string>();
            _ieFiles = new List<string>();
            _ueFiles = new List<string>();
            _uctFiles = new List<string>();
            _uiFiles = new List<string>();
            _umFiles = new List<string>();
            _ucFiles = new List<string>();
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

            while (rdr.Read())
            {
                Console.WriteLine("{0}", rdr.GetString(0));
                avatars.Add(rdr.GetString(0).Replace("0x", string.Empty));
            }

            connection.Close();

            try
            {
                var tipHash = _baseStore.IndexBlockHash(_baseChain.Id, _baseChain.Tip.Index);
                var tip = _baseStore.GetBlock<NCAction>(blockPolicy.GetHashAlgorithm, (BlockHash)tipHash);
                var exec = _baseChain.ExecuteActions(tip);
                var ev = exec.First();
                var avatarCount = 0;
                AvatarState avatarState;
                foreach (var avatar in avatars)
                {
                    try
                    {
                        avatarCount++;
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

                        var userEquipments = avatarState.inventory.Equipments;
                        var userCostumes = avatarState.inventory.Costumes;
                        var userMaterials = avatarState.inventory.Materials;
                        var userConsumables = avatarState.inventory.Consumables;

                        foreach (var equipment in userEquipments)
                        {
                            WriteEquipment(equipment, avatarState.agentAddress, avatarAddress);
                        }

                        foreach (var costume in userCostumes)
                        {
                            WriteCostume(costume, avatarState.agentAddress, avatarAddress);
                        }

                        foreach (var material in userMaterials)
                        {

                            WriteMaterial(material, avatarState.agentAddress, avatarAddress);
                        }

                        foreach (var consumable in userConsumables)
                        {
                            WriteConsumable(consumable, avatarState.agentAddress, avatarAddress);
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
                var stm2 = $"DELETE FROM {UEDbName}";
                var stm3 = $"DELETE FROM {UCTDbName}";
                var stm4 = $"DELETE FROM {UMDbName}";
                var stm5 = $"DELETE FROM {UCDbName}";
                var cmd2 = new MySqlCommand(stm2, connection);
                var cmd3 = new MySqlCommand(stm3, connection);
                var cmd4 = new MySqlCommand(stm4, connection);
                var cmd5 = new MySqlCommand(stm5, connection);
                var startDelete = DateTimeOffset.Now;
                connection.Open();
                cmd2.ExecuteScalar();
                connection.Close();
                var endDelete = DateTimeOffset.Now;
                Console.WriteLine("Clear UserEquipments Complete! Time Elapsed: {0}", endDelete - startDelete);
                startDelete = DateTimeOffset.Now;
                connection.Open();
                cmd3.ExecuteScalar();
                connection.Close();
                endDelete = DateTimeOffset.Now;
                Console.WriteLine("Clear UserCostumes Complete! Time Elapsed: {0}", endDelete - startDelete);
                startDelete = DateTimeOffset.Now;
                connection.Open();
                cmd4.ExecuteScalar();
                connection.Close();
                endDelete = DateTimeOffset.Now;
                Console.WriteLine("Clear UserMaterials Complete! Time Elapsed: {0}", endDelete - startDelete);
                startDelete = DateTimeOffset.Now;
                connection.Open();
                cmd5.ExecuteScalar();
                connection.Close();
                endDelete = DateTimeOffset.Now;
                Console.WriteLine("Clear UserConsumables Complete! Time Elapsed: {0}", endDelete - startDelete);

                foreach (var path in _agentFiles)
                {
                    BulkInsert(AgentDbName, path);
                }

                foreach (var path in _avatarFiles)
                {
                    BulkInsert(AvatarDbName, path);
                }

                foreach (var path in _ueFiles)
                {
                    BulkInsert(UEDbName, path);
                }

                foreach (var path in _uctFiles)
                {
                    BulkInsert(UCTDbName, path);
                }

                foreach (var path in _umFiles)
                {
                    BulkInsert(UMDbName, path);
                }

                foreach (var path in _ucFiles)
                {
                    BulkInsert(UCDbName, path);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            DateTimeOffset end = DateTimeOffset.UtcNow;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
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
                    LineTerminator = "\r\n",
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
                Console.WriteLine($"Bulk load to {tableName} failed.");
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
