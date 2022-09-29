namespace NineChronicles.DataProvider.Tools.SubCommand
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Bencodex.Types;
    using Cocona;
    using Libplanet;
    using Libplanet.Action;
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
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
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
        private const string EQDbName = "Equipments";
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain<NCAction> _baseChain;
        private StreamWriter _ccBulkFile;
        private StreamWriter _ceBulkFile;
        private StreamWriter _ieBulkFile;
        private StreamWriter _agentBulkFile;
        private StreamWriter _avatarBulkFile;
        private StreamWriter _eqBulkFile;
        private List<string> _agentList;
        private List<string> _avatarList;
        private List<string> _ccFiles;
        private List<string> _ceFiles;
        private List<string> _ieFiles;
        private List<string> _agentFiles;
        private List<string> _avatarFiles;
        private List<string> _eqFiles;

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
            Block<NCAction> genesis = _baseStore.GetBlock<NCAction>(gHash);
            _baseChain = new BlockChain<NCAction>(blockPolicy, stagePolicy, _baseStore, baseStateStore, genesis);

            // Prepare block hashes to append to new chain
            long height = _baseChain.Tip.Index;
            var ev = _baseChain.ExecuteActions(_baseChain.Tip).Last();
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
            _agentFiles = new List<string>();
            _avatarFiles = new List<string>();
            _eqFiles = new List<string>();

            // lists to keep track of inserted addresses to minimize duplicates
            _agentList = new List<string>();
            _avatarList = new List<string>();

            CreateBulkFiles();
            try
            {
                int totalCount = limit ?? (int)_baseStore.CountBlocks();
                int remainingCount = totalCount;
                int offsetIdx = 0;

                while (remainingCount > 0)
                {
                    int interval = 1000;
                    int limitInterval;
                    if (interval < remainingCount)
                    {
                        limitInterval = interval;
                    }
                    else
                    {
                        limitInterval = remainingCount;
                    }

                    var count = remainingCount;
                    foreach (var item in
                        _baseStore.IterateIndexes(_baseChain.Id, offset + offsetIdx ?? 0 + offsetIdx, limitInterval).Select((value, i) => new { i, value }))
                    {
                        var block = _baseStore.GetBlock<NCAction>(item.value);
                        Console.WriteLine("Migrating {0}/{1} #{2}", item.i, count, block.Index);

                        if (block.Index == 4285218)
                        {
                            Console.WriteLine("hi");
                        }

                        foreach (var tx in block.Transactions)
                        {
                            if (!_agentList.Contains(tx.Signer.ToString()))
                            {
                                _agentList.Add(tx.Signer.ToString());
                                _agentBulkFile.WriteLine(
                                    $"{tx.Signer.ToString()}"
                                );
                            }

                            var agentState = ev.OutputStates.GetAgentState(tx.Signer);
                            if (agentState is { } ag)
                            {
                                var avatars = ag.avatarAddresses;
                                foreach (var avatar in avatars)
                                {
                                    if (!_avatarList.Contains(avatar.Value.ToString()))
                                    {
                                        _avatarList.Add(avatar.Value.ToString());
                                        AvatarState avatarState;
                                        try
                                        {
                                            avatarState = ev.OutputStates.GetAvatarStateV2(avatar.Value);
                                        }
                                        catch (Exception ex)
                                        {
                                            avatarState = ev.OutputStates.GetAvatarState(avatar.Value);
                                        }

                                        var characterSheet = ev.OutputStates.GetSheet<CharacterSheet>();
                                        var avatarLevel = avatarState.level;
                                        var avatarArmorId = avatarState.GetArmorId();
                                        Costume avatarTitleCostume;
                                        try
                                        {
                                            avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                        }
                                        catch (Exception ex)
                                        {
                                            avatarTitleCostume = null;
                                        }

                                        int? avatarTitleId = null;
                                        if (avatarTitleCostume != null)
                                        {
                                            avatarTitleId = avatarTitleCostume.Id;
                                        }

                                        var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                        string avatarName = avatarState.name;
                                        _avatarBulkFile.WriteLine(
                                            $"{avatar.Value.ToString()};" +
                                            $"{tx.Signer.ToString()};" +
                                            $"{avatarName};" +
                                            $"{avatarLevel};" +
                                            $"{avatarTitleId ?? 0};" +
                                            $"{avatarArmorId};" +
                                            $"{avatarCp}");
                                    }
                                }
                            }

                            if (tx.CustomActions.FirstOrDefault()?.InnerAction is CombinationEquipment ce)
                            {
                                try
                                {
                                    _ceBulkFile.WriteLine(
                                        $"{ce.Id.ToString()};" +
                                        $"{ce.avatarAddress.ToString()};" +
                                        $"{tx.Signer.ToString()};" +
                                        $"{ce.recipeId};" +
                                        $"{ce.slotIndex};" +
                                        $"{ce.subRecipeId ?? 0};" +
                                        $"{block.Index};" +
                                        $"{tx.Timestamp:yyyy-MM-dd HH:mm:ss}"
                                    );
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.CustomActions.FirstOrDefault()?.InnerAction is CombinationEquipment13 ce13)
                            {
                                try
                                {
                                    _ceBulkFile.WriteLine(
                                        $"{ce13.Id.ToString()};" +
                                        $"{ce13.avatarAddress.ToString()};" +
                                        $"{tx.Signer.ToString()};" +
                                        $"{ce13.recipeId};" +
                                        $"{ce13.slotIndex};" +
                                        $"{ce13.subRecipeId ?? 0};" +
                                        $"{block.Index};" +
                                        $"{tx.Timestamp:yyyy-MM-dd HH:mm:ss}"
                                    );
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.CustomActions.FirstOrDefault()?.InnerAction is CombinationEquipment12 ce12)
                            {
                                try
                                {
                                    _ceBulkFile.WriteLine(
                                        $"{ce12.Id.ToString()};" +
                                        $"{ce12.avatarAddress.ToString()};" +
                                        $"{tx.Signer.ToString()};" +
                                        $"{ce12.recipeId};" +
                                        $"{ce12.slotIndex};" +
                                        $"{ce12.subRecipeId ?? 0};" +
                                        $"{block.Index};" +
                                        $"{tx.Timestamp:yyyy-MM-dd HH:mm:ss}"
                                    );
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.CustomActions.FirstOrDefault()?.InnerAction is CombinationEquipment11 ce11)
                            {
                                try
                                {
                                    _ceBulkFile.WriteLine(
                                        $"{ce11.Id.ToString()};" +
                                        $"{ce11.avatarAddress.ToString()};" +
                                        $"{tx.Signer.ToString()};" +
                                        $"{ce11.recipeId};" +
                                        $"{ce11.slotIndex};" +
                                        $"{ce11.subRecipeId ?? 0};" +
                                        $"{block.Index};" +
                                        $"{tx.Timestamp:yyyy-MM-dd HH:mm:ss}"
                                    );
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.CustomActions.FirstOrDefault()?.InnerAction is CombinationEquipment10 ce10)
                            {
                                try
                                {
                                    _ceBulkFile.WriteLine(
                                        $"{ce10.Id.ToString()};" +
                                        $"{ce10.avatarAddress.ToString()};" +
                                        $"{tx.Signer.ToString()};" +
                                        $"{ce10.recipeId};" +
                                        $"{ce10.slotIndex};" +
                                        $"{ce10.subRecipeId ?? 0};" +
                                        $"{block.Index};" +
                                        $"{tx.Timestamp:yyyy-MM-dd HH:mm:ss}"
                                    );
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.CustomActions.FirstOrDefault()?.InnerAction is CombinationConsumable cc)
                            {
                                try
                                {
                                    _ccBulkFile.WriteLine(
                                        $"{cc.Id.ToString()};" +
                                        $"{cc.avatarAddress.ToString()};" +
                                        $"{tx.Signer.ToString()};" +
                                        $"{cc.recipeId};" +
                                        $"{cc.slotIndex};" +
                                        $"{block.Index};" +
                                        $"{tx.Timestamp:yyyy-MM-dd HH:mm:ss}"
                                    );
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.CustomActions.FirstOrDefault()?.InnerAction is CombinationConsumable7 cc7)
                            {
                                try
                                {
                                    _ccBulkFile.WriteLine(
                                        $"{cc7.Id.ToString()};" +
                                        $"{cc7.AvatarAddress.ToString()};" +
                                        $"{tx.Signer.ToString()};" +
                                        $"{cc7.recipeId};" +
                                        $"{cc7.slotIndex};" +
                                        $"{block.Index};" +
                                        $"{tx.Timestamp:yyyy-MM-dd HH:mm:ss}"
                                    );
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.CustomActions.FirstOrDefault()?.InnerAction is ItemEnhancement ie)
                            {
                                try
                                {
                                    var state = ev.OutputStates.GetState(
                                        Addresses.GetItemAddress(ie.itemId));
                                    ITradableItem orderItem =
                                        (ITradableItem)ItemFactory.Deserialize((Dictionary)state);
                                    if (orderItem.ItemType == ItemType.Equipment)
                                    {
                                        var equipment = (Equipment)orderItem;
                                        var cp = CPHelper.GetCP(equipment);
                                        _eqBulkFile.WriteLine(
                                            $"{orderItem.TradableId.ToString()};" +
                                            $"{tx.Signer.ToString()};" +
                                            $"{ie.avatarAddress.ToString()};" +
                                            $"{equipment.Id};" +
                                            $"{cp};" +
                                            $"{equipment.level};" +
                                            $"{equipment.ItemSubType.ToString()};" +
                                            $"{tx.Timestamp:yyyy-MM-dd HH:mm:ss}");
                                    }

                                    _ieBulkFile.WriteLine(
                                        $"{ie.Id.ToString()};" +
                                        $"{ie.avatarAddress.ToString()};" +
                                        $"{tx.Signer.ToString()};" +
                                        $"{ie.itemId.ToString()};" +
                                        $"{ie.materialId.ToString()};" +
                                        $"{ie.slotIndex};" +
                                        $"{block.Index};" +
                                        $"{tx.Timestamp:yyyy-MM-dd HH:mm:ss}"
                                    );
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.CustomActions.FirstOrDefault()?.InnerAction is ItemEnhancement10 ie10)
                            {
                                try
                                {
                                    var state = ev.OutputStates.GetState(
                                        Addresses.GetItemAddress(ie10.itemId));
                                    ITradableItem orderItem =
                                        (ITradableItem)ItemFactory.Deserialize((Dictionary)state);
                                    if (orderItem.ItemType == ItemType.Equipment)
                                    {
                                        var equipment = (Equipment)orderItem;
                                        var cp = CPHelper.GetCP(equipment);
                                        _eqBulkFile.WriteLine(
                                            $"{orderItem.TradableId.ToString()};" +
                                            $"{tx.Signer.ToString()};" +
                                            $"{ie10.avatarAddress.ToString()};" +
                                            $"{equipment.Id};" +
                                            $"{cp};" +
                                            $"{equipment.level};" +
                                            $"{equipment.ItemSubType.ToString()};" +
                                            $"{tx.Timestamp:yyyy-MM-dd HH:mm:ss}");
                                    }

                                    _ieBulkFile.WriteLine(
                                        $"{ie10.Id.ToString()};" +
                                        $"{ie10.avatarAddress.ToString()};" +
                                        $"{tx.Signer.ToString()};" +
                                        $"{ie10.itemId.ToString()};" +
                                        $"{ie10.materialId.ToString()};" +
                                        $"{ie10.slotIndex};" +
                                        $"{block.Index};" +
                                        $"{tx.Timestamp:yyyy-MM-dd HH:mm:ss}"
                                    );
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.CustomActions.FirstOrDefault()?.InnerAction is ItemEnhancement9 ie9)
                            {
                                try
                                {
                                    try
                                    {
                                        var state = ev.OutputStates.GetState(
                                            Addresses.GetItemAddress(ie9.itemId));
                                        ITradableItem orderItem =
                                            (ITradableItem) ItemFactory.Deserialize((Dictionary) state);
                                        if (orderItem.ItemType == ItemType.Equipment)
                                        {
                                            var equipment = (Equipment) orderItem;
                                            var cp = CPHelper.GetCP(equipment);
                                            _eqBulkFile.WriteLine(
                                                $"{orderItem.TradableId.ToString()};" +
                                                $"{tx.Signer.ToString()};" +
                                                $"{ie9.avatarAddress.ToString()};" +
                                                $"{equipment.Id};" +
                                                $"{cp};" +
                                                $"{equipment.level};" +
                                                $"{equipment.ItemSubType.ToString()};" +
                                                $"{tx.Timestamp:yyyy-MM-dd HH:mm:ss}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }

                                    _ieBulkFile.WriteLine(
                                        $"{ie9.Id.ToString()};" +
                                        $"{ie9.avatarAddress.ToString()};" +
                                        $"{tx.Signer.ToString()};" +
                                        $"{ie9.itemId.ToString()};" +
                                        $"{ie9.materialId.ToString()};" +
                                        $"{ie9.slotIndex};" +
                                        $"{block.Index};" +
                                        $"{tx.Timestamp:yyyy-MM-dd HH:mm:ss}"
                                    );
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }
                        }

                        Console.WriteLine("Migrating Done {0}/{1} #{2}", item.i, count, block.Index);
                    }

                    if (interval < remainingCount)
                    {
                        remainingCount -= interval;
                        offsetIdx += interval;
                    }
                    else
                    {
                        remainingCount = 0;
                        offsetIdx += remainingCount;
                    }
                }

                FlushBulkFiles();
                DateTimeOffset postDataPrep = DateTimeOffset.Now;
                Console.WriteLine("Data Preparation Complete! Time Elapsed: {0}", postDataPrep - start);

                foreach (var path in _agentFiles)
                {
                    BulkInsert(AgentDbName, path);
                }

                foreach (var path in _avatarFiles)
                {
                    BulkInsert(AvatarDbName, path);
                }

                foreach (var path in _ccFiles)
                {
                    BulkInsert(CCDbName, path);
                }

                foreach (var path in _ceFiles)
                {
                    BulkInsert(CEDbName, path);
                }

                foreach (var path in _ieFiles)
                {
                    BulkInsert(IEDbName, path);
                }

                foreach (var path in _eqFiles)
                {
                    BulkInsert(EQDbName, path);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            DateTimeOffset end = DateTimeOffset.UtcNow;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
        }

        private void ProcessTasks(Task<List<ActionEvaluation>>[] taskArray)
        {
            foreach (var task in taskArray)
            {
                if (task.Result is { } data)
                {
                    foreach (var ae in data)
                    {
                        if (ae.Action is PolymorphicAction<ActionBase> action)
                        {
                            if (action.InnerAction is CombinationConsumable cc)
                            {
                                WriteCC(
                                    cc.Id,
                                    ae.InputContext.Signer,
                                    cc.avatarAddress,
                                    cc.recipeId,
                                    cc.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationConsumable2 cc2)
                            {
                                WriteCC(
                                    cc2.Id,
                                    ae.InputContext.Signer,
                                    cc2.AvatarAddress,
                                    cc2.recipeId,
                                    cc2.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationConsumable3 cc3)
                            {
                                WriteCC(
                                    cc3.Id,
                                    ae.InputContext.Signer,
                                    cc3.AvatarAddress,
                                    cc3.recipeId,
                                    cc3.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationConsumable4 cc4)
                            {
                                WriteCC(
                                    cc4.Id,
                                    ae.InputContext.Signer,
                                    cc4.AvatarAddress,
                                    cc4.recipeId,
                                    cc4.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationConsumable5 cc5)
                            {
                                WriteCC(
                                    cc5.Id,
                                    ae.InputContext.Signer,
                                    cc5.AvatarAddress,
                                    cc5.recipeId,
                                    cc5.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationEquipment ce)
                            {
                                WriteCE(
                                    ce.Id,
                                    ae.InputContext.Signer,
                                    ce.avatarAddress,
                                    ce.recipeId,
                                    ce.slotIndex,
                                    ce.subRecipeId ?? 0,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationEquipment2 ce2)
                            {
                                WriteCE(
                                    ce2.Id,
                                    ae.InputContext.Signer,
                                    ce2.AvatarAddress,
                                    ce2.RecipeId,
                                    ce2.SlotIndex,
                                    ce2.SubRecipeId ?? 0,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationEquipment3 ce3)
                            {
                                WriteCE(
                                    ce3.Id,
                                    ae.InputContext.Signer,
                                    ce3.AvatarAddress,
                                    ce3.RecipeId,
                                    ce3.SlotIndex,
                                    ce3.SubRecipeId ?? 0,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationEquipment4 ce4)
                            {
                                WriteCE(
                                    ce4.Id,
                                    ae.InputContext.Signer,
                                    ce4.AvatarAddress,
                                    ce4.RecipeId,
                                    ce4.SlotIndex,
                                    ce4.SubRecipeId ?? 0,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationEquipment5 ce5)
                            {
                                WriteCE(
                                    ce5.Id,
                                    ae.InputContext.Signer,
                                    ce5.AvatarAddress,
                                    ce5.RecipeId,
                                    ce5.SlotIndex,
                                    ce5.SubRecipeId ?? 0,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is ItemEnhancement ie)
                            {
                                WriteIE(
                                    ie.Id,
                                    ae.InputContext.Signer,
                                    ie.avatarAddress,
                                    ie.itemId,
                                    ie.materialId,
                                    ie.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is ItemEnhancement2 ie2)
                            {
                                WriteIE(
                                    ie2.Id,
                                    ae.InputContext.Signer,
                                    ie2.avatarAddress,
                                    ie2.itemId,
                                    ie2.materialId,
                                    ie2.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is ItemEnhancement3 ie3)
                            {
                                WriteIE(
                                    ie3.Id,
                                    ae.InputContext.Signer,
                                    ie3.avatarAddress,
                                    ie3.itemId,
                                    ie3.materialId,
                                    ie3.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is ItemEnhancement4 ie4)
                            {
                                WriteIE(
                                    ie4.Id,
                                    ae.InputContext.Signer,
                                    ie4.avatarAddress,
                                    ie4.itemId,
                                    ie4.materialId,
                                    ie4.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is ItemEnhancement5 ie5)
                            {
                                WriteIE(
                                    ie5.Id,
                                    ae.InputContext.Signer,
                                    ie5.avatarAddress,
                                    ie5.itemId,
                                    ie5.materialId,
                                    ie5.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is ItemEnhancement6 ie6)
                            {
                                WriteIE(
                                    ie6.Id,
                                    ae.InputContext.Signer,
                                    ie6.avatarAddress,
                                    ie6.itemId,
                                    ie6.materialId,
                                    ie6.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }
                        }
                    }
                }
            }
        }

        private List<ActionEvaluation> EvaluateBlock(Block<NCAction> block)
        {
            var evList = _baseChain.ExecuteActions(block).ToList();
            return evList;
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

            _eqBulkFile.Flush();
            _eqBulkFile.Close();
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

            string eqFilePath = Path.GetTempFileName();
            _eqBulkFile = new StreamWriter(eqFilePath);

            _agentFiles.Add(agentFilePath);
            _avatarFiles.Add(avatarFilePath);
            _ccFiles.Add(ccFilePath);
            _ceFiles.Add(ceFilePath);
            _ieFiles.Add(ieFilePath);
            _eqFiles.Add(eqFilePath);
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

        private void WriteCC(
            Guid id,
            Address agentAddress,
            Address avatarAddress,
            int recipeId,
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

            _ccBulkFile.WriteLine(
                $"{id.ToString()};" +
                $"{avatarAddress.ToString()};" +
                $"{agentAddress.ToString()};" +
                $"{recipeId};" +
                $"{slotIndex};" +
                $"{blockIndex.ToString()}");
            Console.WriteLine("Writing CC action in block #{0}", blockIndex);
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
