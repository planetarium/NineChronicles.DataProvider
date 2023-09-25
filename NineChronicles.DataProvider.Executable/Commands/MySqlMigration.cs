namespace NineChronicles.DataProvider.Executable.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Bencodex.Types;
    using Cocona;
    using Lib9c.Model.Order;
    using Libplanet.Action;
    using Libplanet.Action.Loader;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Crypto;
    using Libplanet.RocksDBStore;
    using Libplanet.Store;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using Libplanet.Types.Tx;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.Extensions.DependencyInjection;
    using MySqlConnector;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Loader;
    using Nekoyume.Blockchain.Policy;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.DataRendering;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.DataProvider.Store.Models;
    using Serilog;
    using Serilog.Events;
    using static Lib9c.SerializeKeys;

    public class MySqlMigration
    {
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain _baseChain;
        private List<string> _agentCheck;
        private List<string> _avatarCheck;
        private MySqlStore _mySqlStore;
        private BlockHash _blockHash;
        private long _blockIndex;
        private DateTimeOffset _blockTimeOffset;
        private List<BlockModel> _blockList;
        private List<TransactionModel> _txList;
        private List<AgentModel> _agentList;
        private List<AvatarModel> _avatarList;
        private List<HackAndSlashModel> _hackAndSlashList;
        private List<HasWithRandomBuffModel> _hasWithRandomBuffList;
        private List<ClaimStakeRewardModel> _claimStakeRewardList;
        private List<RunesAcquiredModel> _runesAcquiredList;
        private List<EventDungeonBattleModel> _eventDungeonBattleList;
        private List<EventConsumableItemCraftsModel> _eventConsumableItemCraftsList;
        private List<HackAndSlashSweepModel> _hackAndSlashSweepList;
        private List<CombinationConsumableModel> _combinationConsumableList;
        private List<CombinationEquipmentModel> _combinationEquipmentList;
        private List<EquipmentModel> _equipmentList;
        private List<ItemEnhancementModel> _itemEnhancementList;
        private List<ShopHistoryEquipmentModel> _buyShopEquipmentsList;
        private List<ShopHistoryCostumeModel> _buyShopCostumesList;
        private List<ShopHistoryMaterialModel> _buyShopMaterialsList;
        private List<ShopHistoryConsumableModel> _buyShopConsumablesList;
        private List<StakeModel> _stakeList;
        private List<ClaimStakeRewardModel> _claimStakeList;
        private List<MigrateMonsterCollectionModel> _migrateMonsterCollectionList;
        private List<GrindingModel> _grindList;
        private List<ItemEnhancementFailModel> _itemEnhancementFailList;
        private List<UnlockEquipmentRecipeModel> _unlockEquipmentRecipeList;
        private List<UnlockWorldModel> _unlockWorldList;
        private List<ReplaceCombinationEquipmentMaterialModel> _replaceCombinationEquipmentMaterialList;
        private List<HasRandomBuffModel> _hasRandomBuffList;
        private List<JoinArenaModel> _joinArenaList;
        private List<BattleArenaModel> _battleArenaList;
        private List<RaiderModel> _raiderList;
        private List<BattleGrandFinaleModel> _battleGrandFinaleList;
        private List<EventMaterialItemCraftsModel> _eventMaterialItemCraftsList;
        private List<RuneEnhancementModel> _runeEnhancementList;
        private List<UnlockRuneSlotModel> _unlockRuneSlotList;
        private List<RapidCombinationModel> _rapidCombinationList;
        private List<PetEnhancementModel> _petEnhancementList;
        private List<TransferAssetModel> _transferAssetList;
        private List<RequestPledgeModel> _requestPledgeList;

        [Command(Description = "Migrate action data in rocksdb store to mysql db.")]
        public void Migration(
            [Option('o', Description = "Rocksdb path to migrate.")]
            string storePath,
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
            var dbContextOptions =
                new DbContextOptionsBuilder<NineChroniclesContext>()
                    .UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString)).Options;
            var serviceCollection = new ServiceCollection();
            IServiceProvider provider = serviceCollection.BuildServiceProvider();
            IDbContextFactory<NineChroniclesContext> dbContextFactory = new DbContextFactory<NineChroniclesContext>(
                provider,
                dbContextOptions,
                new DbContextFactorySource<NineChroniclesContext>());
            _mySqlStore = new MySqlStore(dbContextFactory);

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
            LogEventLevel logLevel = LogEventLevel.Debug;
            var blockPolicySource = new BlockPolicySource(Log.Logger, logLevel);
            IBlockPolicy blockPolicy = blockPolicySource.GetPolicy();

            // Setup base chain & new chain
            Block genesis = _baseStore.GetBlock(gHash);
            var blockChainStates = new BlockChainStates(_baseStore, baseStateStore);
            var actionEvaluator = new ActionEvaluator(
                _ => blockPolicy.BlockAction,
                blockChainStates,
                new NCActionLoader());
            _baseChain = new BlockChain(blockPolicy, stagePolicy, _baseStore, baseStateStore, genesis, blockChainStates, actionEvaluator);

            // Check offset and limit value based on chain height
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

            // lists to keep track of inserted addresses to minimize duplicates
            _agentCheck = new List<string>();
            _avatarCheck = new List<string>();

            _blockList = new List<BlockModel>();
            _txList = new List<TransactionModel>();
            _agentList = new List<AgentModel>();
            _avatarList = new List<AvatarModel>();
            _hackAndSlashList = new List<HackAndSlashModel>();
            _hasWithRandomBuffList = new List<HasWithRandomBuffModel>();
            _claimStakeRewardList = new List<ClaimStakeRewardModel>();
            _runesAcquiredList = new List<RunesAcquiredModel>();
            _eventDungeonBattleList = new List<EventDungeonBattleModel>();
            _eventConsumableItemCraftsList = new List<EventConsumableItemCraftsModel>();
            _hackAndSlashSweepList = new List<HackAndSlashSweepModel>();
            _combinationConsumableList = new List<CombinationConsumableModel>();
            _combinationEquipmentList = new List<CombinationEquipmentModel>();
            _equipmentList = new List<EquipmentModel>();
            _itemEnhancementList = new List<ItemEnhancementModel>();
            _buyShopEquipmentsList = new List<ShopHistoryEquipmentModel>();
            _buyShopCostumesList = new List<ShopHistoryCostumeModel>();
            _buyShopMaterialsList = new List<ShopHistoryMaterialModel>();
            _buyShopConsumablesList = new List<ShopHistoryConsumableModel>();
            _stakeList = new List<StakeModel>();
            _claimStakeList = new List<ClaimStakeRewardModel>();
            _migrateMonsterCollectionList = new List<MigrateMonsterCollectionModel>();
            _grindList = new List<GrindingModel>();
            _itemEnhancementFailList = new List<ItemEnhancementFailModel>();
            _unlockEquipmentRecipeList = new List<UnlockEquipmentRecipeModel>();
            _unlockWorldList = new List<UnlockWorldModel>();
            _replaceCombinationEquipmentMaterialList = new List<ReplaceCombinationEquipmentMaterialModel>();
            _hasRandomBuffList = new List<HasRandomBuffModel>();
            _joinArenaList = new List<JoinArenaModel>();
            _battleArenaList = new List<BattleArenaModel>();
            _raiderList = new List<RaiderModel>();
            _battleGrandFinaleList = new List<BattleGrandFinaleModel>();
            _eventMaterialItemCraftsList = new List<EventMaterialItemCraftsModel>();
            _runeEnhancementList = new List<RuneEnhancementModel>();
            _unlockRuneSlotList = new List<UnlockRuneSlotModel>();
            _rapidCombinationList = new List<RapidCombinationModel>();
            _petEnhancementList = new List<PetEnhancementModel>();
            _transferAssetList = new List<TransferAssetModel>();
            _requestPledgeList = new List<RequestPledgeModel>();

            try
            {
                int totalCount = limit ?? (int)_baseStore.CountBlocks();
                int remainingCount = totalCount;
                int offsetIdx = 0;

                while (remainingCount > 0)
                {
                    int interval = 100;
                    int limitInterval;
                    Task<List<IActionEvaluation>>[] taskArray;
                    if (interval < remainingCount)
                    {
                        taskArray = new Task<List<IActionEvaluation>>[interval];
                        limitInterval = interval;
                    }
                    else
                    {
                        taskArray = new Task<List<IActionEvaluation>>[remainingCount];
                        limitInterval = remainingCount;
                    }

                    foreach (var item in
                        _baseStore.IterateIndexes(_baseChain.Id, offset + offsetIdx ?? 0 + offsetIdx, limitInterval).Select((value, i) => new { i, value }))
                    {
                        var block = _baseStore.GetBlock(item.value);
                        _blockList.Add(BlockData.GetBlockInfo(block));
                        _blockHash = block.Hash;
                        _blockIndex = block.Index;
                        _blockTimeOffset = block.Timestamp;
                        foreach (var tx in block.Transactions)
                        {
                            _txList.Add(TransactionData.GetTransactionInfo(block, tx));

                            // check if address is already in _agentCheck
                            if (!_agentCheck.Contains(tx.Signer.ToString()))
                            {
                                _agentList.Add(AgentData.GetAgentInfo(tx.Signer));
                                _agentCheck.Add(tx.Signer.ToString());
                            }
                        }

                        taskArray[item.i] = Task.Factory.StartNew(() =>
                        {
                            List<IActionEvaluation> actionEvaluations = EvaluateBlock(block);
                            Console.WriteLine($"Block progress: #{block.Index}/{remainingCount}");
                            return actionEvaluations;
                        });
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

                    Task.WaitAll(taskArray);
                    ProcessTasks(taskArray);
                }

                DateTimeOffset postDataPrep = _blockTimeOffset;
                Console.WriteLine("Data Preparation Complete! Time Elapsed: {0}", postDataPrep - start);
                Console.WriteLine("Start Data Migration...");
                _mySqlStore.StoreBlockList(_blockList);
                _mySqlStore.StoreTransactionList(_txList);
                _mySqlStore.StoreAgentList(_agentList);
                _mySqlStore.StoreAvatarList(_avatarList);
                _mySqlStore.StoreHackAndSlashList(_hackAndSlashList);
                _mySqlStore.StoreHasWithRandomBuffList(_hasWithRandomBuffList);
                _mySqlStore.StoreClaimStakeRewardList(_claimStakeRewardList);
                _mySqlStore.StoreRunesAcquiredList(_runesAcquiredList);
                _mySqlStore.StoreEventDungeonBattleList(_eventDungeonBattleList);
                _mySqlStore.StoreEventConsumableItemCraftsList(_eventConsumableItemCraftsList);
                _mySqlStore.StoreHackAndSlashSweepList(_hackAndSlashSweepList);
                _mySqlStore.StoreCombinationConsumableList(_combinationConsumableList);
                _mySqlStore.StoreCombinationEquipmentList(_combinationEquipmentList);
                _mySqlStore.StoreItemEnhancementList(_itemEnhancementList);
                _mySqlStore.StoreShopHistoryEquipmentList(_buyShopEquipmentsList);
                _mySqlStore.StoreShopHistoryCostumeList(_buyShopCostumesList);
                _mySqlStore.StoreShopHistoryMaterialList(_buyShopMaterialsList);
                _mySqlStore.StoreShopHistoryConsumableList(_buyShopConsumablesList);
                _mySqlStore.ProcessEquipmentList(_equipmentList);
                _mySqlStore.StoreStakingList(_stakeList);
                _mySqlStore.StoreClaimStakeRewardList(_claimStakeList);
                _mySqlStore.StoreMigrateMonsterCollectionList(_migrateMonsterCollectionList);
                _mySqlStore.StoreGrindList(_grindList);
                _mySqlStore.StoreItemEnhancementFailList(_itemEnhancementFailList);
                _mySqlStore.StoreUnlockEquipmentRecipeList(_unlockEquipmentRecipeList);
                _mySqlStore.StoreUnlockWorldList(_unlockWorldList);
                _mySqlStore.StoreReplaceCombinationEquipmentMaterialList(_replaceCombinationEquipmentMaterialList);
                _mySqlStore.StoreHasRandomBuffList(_hasRandomBuffList);
                _mySqlStore.StoreHasWithRandomBuffList(_hasWithRandomBuffList);
                _mySqlStore.StoreJoinArenaList(_joinArenaList);
                _mySqlStore.StoreBattleArenaList(_battleArenaList);
                _mySqlStore.StoreBlockList(_blockList);
                _mySqlStore.StoreEventDungeonBattleList(_eventDungeonBattleList);
                _mySqlStore.StoreEventConsumableItemCraftsList(_eventConsumableItemCraftsList);
                _mySqlStore.StoreRaiderList(_raiderList);
                _mySqlStore.StoreBattleGrandFinaleList(_battleGrandFinaleList);
                _mySqlStore.StoreEventMaterialItemCraftsList(_eventMaterialItemCraftsList);
                _mySqlStore.StoreRuneEnhancementList(_runeEnhancementList);
                _mySqlStore.StoreRunesAcquiredList(_runesAcquiredList);
                _mySqlStore.StoreUnlockRuneSlotList(_unlockRuneSlotList);
                _mySqlStore.StoreRapidCombinationList(_rapidCombinationList);
                _mySqlStore.StorePetEnhancementList(_petEnhancementList);
                _mySqlStore.StoreTransferAssetList(_transferAssetList);
                _mySqlStore.StoreRequestPledgeList(_requestPledgeList);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            DateTimeOffset end = DateTimeOffset.UtcNow;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
        }

        private void ProcessTasks(Task<List<IActionEvaluation>>[] taskArray)
        {
            foreach (var task in taskArray)
            {
                if (task.Result is { } data)
                {
                    foreach (var ae in data)
                    {
                        var actionLoader = new NCActionLoader();
                        if (actionLoader.LoadAction(_blockIndex, ae.Action) is ActionBase action)
                        {
                            // avatarNames will be stored as "N/A" for optimization
                            if (action is HackAndSlash hasAction)
                            {
                                var avatarAddress = hasAction.AvatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfo(ae.OutputState, ae.InputContext.Signer, hasAction.AvatarAddress, hasAction.RuneInfos, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction.AvatarAddress, hasAction.StageId, hasAction.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                if (hasAction.StageBuffId.HasValue)
                                {
                                    _hasWithRandomBuffList.Add(HasWithRandomBuffData.GetHasWithRandomBuffInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction.AvatarAddress, hasAction.StageId, hasAction.StageBuffId, hasAction.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                }
                            }

                            if (action is HackAndSlash19 hasAction19)
                            {
                                var avatarAddress = hasAction19.AvatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfo(ae.OutputState, ae.InputContext.Signer, hasAction19.AvatarAddress, hasAction19.RuneInfos, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash19), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction19.AvatarAddress, hasAction19.StageId, hasAction19.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                if (hasAction19.StageBuffId.HasValue)
                                {
                                    _hasWithRandomBuffList.Add(HasWithRandomBuffData.GetHasWithRandomBuffInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction19.AvatarAddress, hasAction19.StageId, hasAction19.StageBuffId, hasAction19.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                }
                            }

                            if (action is HackAndSlash18 hasAction18)
                            {
                                var avatarAddress = hasAction18.AvatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction18.AvatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash18), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction18.AvatarAddress, hasAction18.StageId, hasAction18.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                if (hasAction18.StageBuffId.HasValue)
                                {
                                    _hasWithRandomBuffList.Add(HasWithRandomBuffData.GetHasWithRandomBuffInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction18.AvatarAddress, hasAction18.StageId, hasAction18.StageBuffId, hasAction18.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                }
                            }

                            if (action is HackAndSlash17 hasAction17)
                            {
                                var avatarAddress = hasAction17.AvatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction17.AvatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash17), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction17.AvatarAddress, hasAction17.StageId, hasAction17.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                if (hasAction17.StageBuffId.HasValue)
                                {
                                    _hasWithRandomBuffList.Add(HasWithRandomBuffData.GetHasWithRandomBuffInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction17.AvatarAddress, hasAction17.StageId, hasAction17.StageBuffId, hasAction17.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                }
                            }

                            if (action is HackAndSlash16 hasAction16)
                            {
                                var avatarAddress = hasAction16.AvatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction16.AvatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash16), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction16.AvatarAddress, hasAction16.StageId, hasAction16.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                if (hasAction16.StageBuffId.HasValue)
                                {
                                    _hasWithRandomBuffList.Add(HasWithRandomBuffData.GetHasWithRandomBuffInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction16.AvatarAddress, hasAction16.StageId, hasAction16.StageBuffId, hasAction16.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                }
                            }

                            if (action is HackAndSlash15 hasAction15)
                            {
                                var avatarAddress = hasAction15.avatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction15.avatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash15), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction15.avatarAddress, hasAction15.stageId, hasAction15.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                            }

                            if (action is HackAndSlash14 hasAction14)
                            {
                                var avatarAddress = hasAction14.avatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction14.avatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash14), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction14.avatarAddress, hasAction14.stageId, hasAction14.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                            }

                            if (action is HackAndSlash13 hasAction13)
                            {
                                var avatarAddress = hasAction13.avatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction13.avatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash13), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction13.avatarAddress, hasAction13.stageId, hasAction13.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                            }

                            if (action is HackAndSlash12 hasAction12)
                            {
                                var avatarAddress = hasAction12.avatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction12.avatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash12), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction12.avatarAddress, hasAction12.stageId, hasAction12.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                            }

                            if (action is HackAndSlash11 hasAction11)
                            {
                                var avatarAddress = hasAction11.avatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction11.avatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash11), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction11.avatarAddress, hasAction11.stageId, hasAction11.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                            }

                            if (action is HackAndSlash10 hasAction10)
                            {
                                var avatarAddress = hasAction10.avatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction10.avatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash10), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction10.avatarAddress, hasAction10.stageId, hasAction10.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                            }

                            if (action is HackAndSlash9 hasAction9)
                            {
                                var avatarAddress = hasAction9.avatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction9.avatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash9), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction9.avatarAddress, hasAction9.stageId, hasAction9.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                            }

                            if (action is HackAndSlash8 hasAction8)
                            {
                                var avatarAddress = hasAction8.avatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction8.avatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash8), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction8.avatarAddress, hasAction8.stageId, hasAction8.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                            }

                            if (action is HackAndSlash7 hasAction7)
                            {
                                var avatarAddress = hasAction7.avatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction7.avatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash7), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction7.avatarAddress, hasAction7.stageId, hasAction7.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                            }

                            if (action is HackAndSlash6 hasAction6)
                            {
                                var avatarAddress = hasAction6.avatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction6.avatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash6), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction6.avatarAddress, hasAction6.stageId, hasAction6.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                            }

                            if (action is HackAndSlash5 hasAction5)
                            {
                                var avatarAddress = hasAction5.avatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction5.avatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash5), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction5.avatarAddress, hasAction5.stageId, hasAction5.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                            }

                            if (action is HackAndSlash4 hasAction4)
                            {
                                var avatarAddress = hasAction4.avatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction4.avatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash4), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction4.avatarAddress, hasAction4.stageId, hasAction4.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                            }

                            if (action is HackAndSlash3 hasAction3)
                            {
                                var avatarAddress = hasAction3.avatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction3.avatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash3), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction3.avatarAddress, hasAction3.stageId, hasAction3.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                            }

                            if (action is HackAndSlash2 hasAction2)
                            {
                                var avatarAddress = hasAction2.avatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction2.avatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash2), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction2.avatarAddress, hasAction2.stageId, hasAction2.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                            }

                            if (action is HackAndSlash0 hasAction0)
                            {
                                var avatarAddress = hasAction0.avatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasAction0.avatarAddress, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash0), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasAction0.avatarAddress, hasAction0.stageId, hasAction0.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                            }

                            if (action is IClaimStakeReward claimStakeReward)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var plainValue = (Bencodex.Types.Dictionary)claimStakeReward.PlainValue;
                                var avatarAddress = plainValue[AvatarAddressKey].ToAddress();
                                var id = ((GameAction)claimStakeReward).Id;
#pragma warning disable CS0618
                                var runeCurrency = Currency.Legacy(RuneHelper.StakeRune.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                var prevRuneBalance = ae.InputContext.PreviousState.GetBalance(
                                    avatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = ae.OutputState.GetBalance(
                                    avatarAddress,
                                    runeCurrency);
                                var acquiredRune = outputRuneBalance - prevRuneBalance;
                                var actionType = claimStakeReward.ToString()!.Split('.').LastOrDefault()?.Replace(">", string.Empty);
                                _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                    id,
                                    ae.InputContext.Signer,
                                    avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    actionType!,
                                    runeCurrency.Ticker,
                                    acquiredRune,
                                    _blockTimeOffset));
                                _claimStakeRewardList.Add(ClaimStakeRewardData.GetClaimStakeRewardInfo(claimStakeReward, ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, ae.InputContext.BlockIndex, _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ClaimStakeReward action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is EventDungeonBattle eventDungeonBattle)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var actionType = eventDungeonBattle.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                _eventDungeonBattleList.Add(EventDungeonBattleData.GetEventDungeonBattleInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    eventDungeonBattle.AvatarAddress,
                                    eventDungeonBattle.EventScheduleId,
                                    eventDungeonBattle.EventDungeonId,
                                    eventDungeonBattle.EventDungeonStageId,
                                    eventDungeonBattle.Foods.Count,
                                    eventDungeonBattle.Costumes.Count,
                                    eventDungeonBattle.Equipments.Count,
                                    eventDungeonBattle.Id,
                                    actionType!,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing EventDungeonBattle action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is EventDungeonBattleV3 eventDungeonBattle3)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var actionType = eventDungeonBattle3.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                _eventDungeonBattleList.Add(EventDungeonBattleData.GetEventDungeonBattleInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    eventDungeonBattle3.AvatarAddress,
                                    eventDungeonBattle3.EventScheduleId,
                                    eventDungeonBattle3.EventDungeonId,
                                    eventDungeonBattle3.EventDungeonStageId,
                                    eventDungeonBattle3.Foods.Count,
                                    eventDungeonBattle3.Costumes.Count,
                                    eventDungeonBattle3.Equipments.Count,
                                    eventDungeonBattle3.Id,
                                    actionType!,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing EventDungeonBattle action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is EventDungeonBattleV2 eventDungeonBattle2)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var actionType = eventDungeonBattle2.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                _eventDungeonBattleList.Add(EventDungeonBattleData.GetEventDungeonBattleInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    eventDungeonBattle2.AvatarAddress,
                                    eventDungeonBattle2.EventScheduleId,
                                    eventDungeonBattle2.EventDungeonId,
                                    eventDungeonBattle2.EventDungeonStageId,
                                    eventDungeonBattle2.Foods.Count,
                                    eventDungeonBattle2.Costumes.Count,
                                    eventDungeonBattle2.Equipments.Count,
                                    eventDungeonBattle2.Id,
                                    actionType!,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing EventDungeonBattle action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is EventDungeonBattleV1 eventDungeonBattle1)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var actionType = eventDungeonBattle1.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                _eventDungeonBattleList.Add(EventDungeonBattleData.GetEventDungeonBattleInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    eventDungeonBattle1.AvatarAddress,
                                    eventDungeonBattle1.EventScheduleId,
                                    eventDungeonBattle1.EventDungeonId,
                                    eventDungeonBattle1.EventDungeonStageId,
                                    eventDungeonBattle1.Foods.Count,
                                    eventDungeonBattle1.Costumes.Count,
                                    eventDungeonBattle1.Equipments.Count,
                                    eventDungeonBattle1.Id,
                                    actionType!,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing EventDungeonBattle action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is EventConsumableItemCrafts eventConsumableItemCrafts)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _eventConsumableItemCraftsList.Add(EventConsumableItemCraftsData.GetEventConsumableItemCraftsInfo(eventConsumableItemCrafts, ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, ae.InputContext.BlockIndex, _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing EventConsumableItemCrafts action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is HackAndSlashSweep hasSweep)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfo(ae.OutputState, ae.InputContext.Signer, hasSweep.avatarAddress, hasSweep.runeInfos, _blockTimeOffset));
                                _hackAndSlashSweepList.Add(HackAndSlashSweepData.GetHackAndSlashSweepInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    hasSweep.avatarAddress,
                                    hasSweep.stageId,
                                    hasSweep.worldId,
                                    hasSweep.apStoneCount,
                                    hasSweep.actionPoint,
                                    hasSweep.costumes.Count,
                                    hasSweep.equipments.Count,
                                    hasSweep.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing HackAndSlashSweep action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is HackAndSlashSweep8 hasSweep8)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfo(ae.OutputState, ae.InputContext.Signer, hasSweep8.avatarAddress, hasSweep8.runeInfos, _blockTimeOffset));
                                _hackAndSlashSweepList.Add(HackAndSlashSweepData.GetHackAndSlashSweepInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    hasSweep8.avatarAddress,
                                    hasSweep8.stageId,
                                    hasSweep8.worldId,
                                    hasSweep8.apStoneCount,
                                    hasSweep8.actionPoint,
                                    hasSweep8.costumes.Count,
                                    hasSweep8.equipments.Count,
                                    hasSweep8.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing HackAndSlashSweep action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is HackAndSlashSweep7 hasSweep7)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasSweep7.avatarAddress, _blockTimeOffset));
                                _hackAndSlashSweepList.Add(HackAndSlashSweepData.GetHackAndSlashSweepInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    hasSweep7.avatarAddress,
                                    hasSweep7.stageId,
                                    hasSweep7.worldId,
                                    hasSweep7.apStoneCount,
                                    hasSweep7.actionPoint,
                                    hasSweep7.costumes.Count,
                                    hasSweep7.equipments.Count,
                                    hasSweep7.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing HackAndSlashSweep action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is HackAndSlashSweep6 hasSweep6)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasSweep6.avatarAddress, _blockTimeOffset));
                                _hackAndSlashSweepList.Add(HackAndSlashSweepData.GetHackAndSlashSweepInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    hasSweep6.avatarAddress,
                                    hasSweep6.stageId,
                                    hasSweep6.worldId,
                                    hasSweep6.apStoneCount,
                                    hasSweep6.actionPoint,
                                    hasSweep6.costumes.Count,
                                    hasSweep6.equipments.Count,
                                    hasSweep6.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing HackAndSlashSweep action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is HackAndSlashSweep5 hasSweep5)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasSweep5.avatarAddress, _blockTimeOffset));
                                _hackAndSlashSweepList.Add(HackAndSlashSweepData.GetHackAndSlashSweepInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    hasSweep5.avatarAddress,
                                    hasSweep5.stageId,
                                    hasSweep5.worldId,
                                    hasSweep5.apStoneCount,
                                    hasSweep5.actionPoint,
                                    hasSweep5.costumes.Count,
                                    hasSweep5.equipments.Count,
                                    hasSweep5.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing HackAndSlashSweep action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is HackAndSlashSweep4 hasSweep4)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasSweep4.avatarAddress, _blockTimeOffset));
                                _hackAndSlashSweepList.Add(HackAndSlashSweepData.GetHackAndSlashSweepInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    hasSweep4.avatarAddress,
                                    hasSweep4.stageId,
                                    hasSweep4.worldId,
                                    hasSweep4.apStoneCount,
                                    hasSweep4.actionPoint,
                                    hasSweep4.costumes.Count,
                                    hasSweep4.equipments.Count,
                                    hasSweep4.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing HackAndSlashSweep action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is HackAndSlashSweep3 hasSweep3)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasSweep3.avatarAddress, _blockTimeOffset));
                                _hackAndSlashSweepList.Add(HackAndSlashSweepData.GetHackAndSlashSweepInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    hasSweep3.avatarAddress,
                                    hasSweep3.stageId,
                                    hasSweep3.worldId,
                                    hasSweep3.apStoneCount,
                                    hasSweep3.actionPoint,
                                    hasSweep3.costumes.Count,
                                    hasSweep3.equipments.Count,
                                    hasSweep3.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing HackAndSlashSweep action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is HackAndSlashSweep2 hasSweep2)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasSweep2.avatarAddress, _blockTimeOffset));
                                _hackAndSlashSweepList.Add(HackAndSlashSweepData.GetHackAndSlashSweepInfoV1(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    hasSweep2.avatarAddress,
                                    hasSweep2.stageId,
                                    hasSweep2.worldId,
                                    hasSweep2.apStoneCount,
                                    hasSweep2.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing HackAndSlashSweep action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is HackAndSlashSweep1 hasSweep1)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, hasSweep1.avatarAddress, _blockTimeOffset));
                                _hackAndSlashSweepList.Add(HackAndSlashSweepData.GetHackAndSlashSweepInfoV1(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    hasSweep1.avatarAddress,
                                    hasSweep1.stageId,
                                    hasSweep1.worldId,
                                    hasSweep1.apStoneCount,
                                    hasSweep1.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing HackAndSlashSweep action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is CombinationConsumable combinationConsumable)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationConsumableList.Add(CombinationConsumableData.GetCombinationConsumableInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationConsumable.avatarAddress,
                                    combinationConsumable.recipeId,
                                    combinationConsumable.slotIndex,
                                    combinationConsumable.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing CombinationConsumable action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is CombinationConsumable7 combinationConsumable7)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationConsumableList.Add(CombinationConsumableData.GetCombinationConsumableInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationConsumable7.AvatarAddress,
                                    combinationConsumable7.recipeId,
                                    combinationConsumable7.slotIndex,
                                    combinationConsumable7.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing CombinationConsumable action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is CombinationConsumable6 combinationConsumable6)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationConsumableList.Add(CombinationConsumableData.GetCombinationConsumableInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationConsumable6.AvatarAddress,
                                    combinationConsumable6.recipeId,
                                    combinationConsumable6.slotIndex,
                                    combinationConsumable6.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing CombinationConsumable action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is CombinationConsumable5 combinationConsumable5)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationConsumableList.Add(CombinationConsumableData.GetCombinationConsumableInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationConsumable5.AvatarAddress,
                                    combinationConsumable5.recipeId,
                                    combinationConsumable5.slotIndex,
                                    combinationConsumable5.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing CombinationConsumable action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is CombinationConsumable4 combinationConsumable4)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationConsumableList.Add(CombinationConsumableData.GetCombinationConsumableInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationConsumable4.AvatarAddress,
                                    combinationConsumable4.recipeId,
                                    combinationConsumable4.slotIndex,
                                    combinationConsumable4.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing CombinationConsumable action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is CombinationConsumable3 combinationConsumable3)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationConsumableList.Add(CombinationConsumableData.GetCombinationConsumableInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationConsumable3.AvatarAddress,
                                    combinationConsumable3.recipeId,
                                    combinationConsumable3.slotIndex,
                                    combinationConsumable3.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing CombinationConsumable action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is CombinationConsumable2 combinationConsumable2)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationConsumableList.Add(CombinationConsumableData.GetCombinationConsumableInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationConsumable2.AvatarAddress,
                                    combinationConsumable2.recipeId,
                                    combinationConsumable2.slotIndex,
                                    combinationConsumable2.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing CombinationConsumable action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is CombinationConsumable0 combinationConsumable0)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationConsumableList.Add(CombinationConsumableData.GetCombinationConsumableInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationConsumable0.AvatarAddress,
                                    combinationConsumable0.recipeId,
                                    combinationConsumable0.slotIndex,
                                    combinationConsumable0.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing CombinationConsumable action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment combinationEquipment)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment.avatarAddress,
                                    combinationEquipment.recipeId,
                                    combinationEquipment.slotIndex,
                                    combinationEquipment.subRecipeId,
                                    combinationEquipment.Id,
                                    ae.InputContext.BlockIndex));
                                if (combinationEquipment.payByCrystal)
                                {
                                    var replaceCombinationEquipmentMaterialList = ReplaceCombinationEquipmentMaterialData
                                        .GetReplaceCombinationEquipmentMaterialInfo(
                                            ae.InputContext.PreviousState,
                                            ae.OutputState,
                                            ae.InputContext.Signer,
                                            combinationEquipment.avatarAddress,
                                            combinationEquipment.recipeId,
                                            combinationEquipment.subRecipeId,
                                            combinationEquipment.payByCrystal,
                                            combinationEquipment.Id,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset);
                                    foreach (var replaceCombinationEquipmentMaterial in replaceCombinationEquipmentMaterialList)
                                    {
                                        _replaceCombinationEquipmentMaterialList.Add(replaceCombinationEquipmentMaterial);
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment.avatarAddress,
                                    combinationEquipment.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment15 combinationEquipment15)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment15.avatarAddress,
                                    combinationEquipment15.recipeId,
                                    combinationEquipment15.slotIndex,
                                    combinationEquipment15.subRecipeId,
                                    combinationEquipment15.Id,
                                    ae.InputContext.BlockIndex));
                                if (combinationEquipment15.payByCrystal)
                                {
                                    var replaceCombinationEquipmentMaterialList = ReplaceCombinationEquipmentMaterialData
                                        .GetReplaceCombinationEquipmentMaterialInfo(
                                            ae.InputContext.PreviousState,
                                            ae.OutputState,
                                            ae.InputContext.Signer,
                                            combinationEquipment15.avatarAddress,
                                            combinationEquipment15.recipeId,
                                            combinationEquipment15.subRecipeId,
                                            combinationEquipment15.payByCrystal,
                                            combinationEquipment15.Id,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset);
                                    foreach (var replaceCombinationEquipmentMaterial in replaceCombinationEquipmentMaterialList)
                                    {
                                        _replaceCombinationEquipmentMaterialList.Add(replaceCombinationEquipmentMaterial);
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment15.avatarAddress,
                                    combinationEquipment15.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment15.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment15.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment14 combinationEquipment14)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment14.avatarAddress,
                                    combinationEquipment14.recipeId,
                                    combinationEquipment14.slotIndex,
                                    combinationEquipment14.subRecipeId,
                                    combinationEquipment14.Id,
                                    ae.InputContext.BlockIndex));
                                if (combinationEquipment14.payByCrystal)
                                {
                                    var replaceCombinationEquipmentMaterialList = ReplaceCombinationEquipmentMaterialData
                                        .GetReplaceCombinationEquipmentMaterialInfo(
                                            ae.InputContext.PreviousState,
                                            ae.OutputState,
                                            ae.InputContext.Signer,
                                            combinationEquipment14.avatarAddress,
                                            combinationEquipment14.recipeId,
                                            combinationEquipment14.subRecipeId,
                                            combinationEquipment14.payByCrystal,
                                            combinationEquipment14.Id,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset);
                                    foreach (var replaceCombinationEquipmentMaterial in replaceCombinationEquipmentMaterialList)
                                    {
                                        _replaceCombinationEquipmentMaterialList.Add(replaceCombinationEquipmentMaterial);
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment14.avatarAddress,
                                    combinationEquipment14.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment14.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment14.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment13 combinationEquipment13)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment13.avatarAddress,
                                    combinationEquipment13.recipeId,
                                    combinationEquipment13.slotIndex,
                                    combinationEquipment13.subRecipeId,
                                    combinationEquipment13.Id,
                                    ae.InputContext.BlockIndex));
                                if (combinationEquipment13.payByCrystal)
                                {
                                    var replaceCombinationEquipmentMaterialList = ReplaceCombinationEquipmentMaterialData
                                        .GetReplaceCombinationEquipmentMaterialInfo(
                                            ae.InputContext.PreviousState,
                                            ae.OutputState,
                                            ae.InputContext.Signer,
                                            combinationEquipment13.avatarAddress,
                                            combinationEquipment13.recipeId,
                                            combinationEquipment13.subRecipeId,
                                            combinationEquipment13.payByCrystal,
                                            combinationEquipment13.Id,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset);
                                    foreach (var replaceCombinationEquipmentMaterial in replaceCombinationEquipmentMaterialList)
                                    {
                                        _replaceCombinationEquipmentMaterialList.Add(replaceCombinationEquipmentMaterial);
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment13.avatarAddress,
                                    combinationEquipment13.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment13.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment13.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment12 combinationEquipment12)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment12.avatarAddress,
                                    combinationEquipment12.recipeId,
                                    combinationEquipment12.slotIndex,
                                    combinationEquipment12.subRecipeId,
                                    combinationEquipment12.Id,
                                    ae.InputContext.BlockIndex));
                                if (combinationEquipment12.payByCrystal)
                                {
                                    var replaceCombinationEquipmentMaterialList = ReplaceCombinationEquipmentMaterialData
                                        .GetReplaceCombinationEquipmentMaterialInfo(
                                            ae.InputContext.PreviousState,
                                            ae.OutputState,
                                            ae.InputContext.Signer,
                                            combinationEquipment12.avatarAddress,
                                            combinationEquipment12.recipeId,
                                            combinationEquipment12.subRecipeId,
                                            combinationEquipment12.payByCrystal,
                                            combinationEquipment12.Id,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset);
                                    foreach (var replaceCombinationEquipmentMaterial in replaceCombinationEquipmentMaterialList)
                                    {
                                        _replaceCombinationEquipmentMaterialList.Add(replaceCombinationEquipmentMaterial);
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment12.avatarAddress,
                                    combinationEquipment12.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment12.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment12.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment11 combinationEquipment11)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment11.avatarAddress,
                                    combinationEquipment11.recipeId,
                                    combinationEquipment11.slotIndex,
                                    combinationEquipment11.subRecipeId,
                                    combinationEquipment11.Id,
                                    ae.InputContext.BlockIndex));

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment11.avatarAddress,
                                    combinationEquipment11.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment11.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment11.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment10 combinationEquipment10)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment10.avatarAddress,
                                    combinationEquipment10.recipeId,
                                    combinationEquipment10.slotIndex,
                                    combinationEquipment10.subRecipeId,
                                    combinationEquipment10.Id,
                                    ae.InputContext.BlockIndex));

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment10.avatarAddress,
                                    combinationEquipment10.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment10.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment10.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment9 combinationEquipment9)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment9.avatarAddress,
                                    combinationEquipment9.recipeId,
                                    combinationEquipment9.slotIndex,
                                    combinationEquipment9.subRecipeId,
                                    combinationEquipment9.Id,
                                    ae.InputContext.BlockIndex));

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment9.avatarAddress,
                                    combinationEquipment9.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment9.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment9.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment8 combinationEquipment8)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment8.avatarAddress,
                                    combinationEquipment8.recipeId,
                                    combinationEquipment8.slotIndex,
                                    combinationEquipment8.subRecipeId,
                                    combinationEquipment8.Id,
                                    ae.InputContext.BlockIndex));

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment8.avatarAddress,
                                    combinationEquipment8.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment8.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment8.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment7 combinationEquipment7)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment7.AvatarAddress,
                                    combinationEquipment7.RecipeId,
                                    combinationEquipment7.SlotIndex,
                                    combinationEquipment7.SubRecipeId,
                                    combinationEquipment7.Id,
                                    ae.InputContext.BlockIndex));

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment7.AvatarAddress,
                                    combinationEquipment7.SlotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment7.AvatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment7.AvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment6 combinationEquipment6)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment6.AvatarAddress,
                                    combinationEquipment6.RecipeId,
                                    combinationEquipment6.SlotIndex,
                                    combinationEquipment6.SubRecipeId,
                                    combinationEquipment6.Id,
                                    ae.InputContext.BlockIndex));

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment6.AvatarAddress,
                                    combinationEquipment6.SlotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment6.AvatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment6.AvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment5 combinationEquipment5)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment5.AvatarAddress,
                                    combinationEquipment5.RecipeId,
                                    combinationEquipment5.SlotIndex,
                                    combinationEquipment5.SubRecipeId,
                                    combinationEquipment5.Id,
                                    ae.InputContext.BlockIndex));

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment5.AvatarAddress,
                                    combinationEquipment5.SlotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment5.AvatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment5.AvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment4 combinationEquipment4)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment4.AvatarAddress,
                                    combinationEquipment4.RecipeId,
                                    combinationEquipment4.SlotIndex,
                                    combinationEquipment4.SubRecipeId,
                                    combinationEquipment4.Id,
                                    ae.InputContext.BlockIndex));

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment4.AvatarAddress,
                                    combinationEquipment4.SlotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment4.AvatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment4.AvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment3 combinationEquipment3)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment3.AvatarAddress,
                                    combinationEquipment3.RecipeId,
                                    combinationEquipment3.SlotIndex,
                                    combinationEquipment3.SubRecipeId,
                                    combinationEquipment3.Id,
                                    ae.InputContext.BlockIndex));

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment3.AvatarAddress,
                                    combinationEquipment3.SlotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment3.AvatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment3.AvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment2 combinationEquipment2)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment2.AvatarAddress,
                                    combinationEquipment2.RecipeId,
                                    combinationEquipment2.SlotIndex,
                                    combinationEquipment2.SubRecipeId,
                                    combinationEquipment2.Id,
                                    ae.InputContext.BlockIndex));

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment2.AvatarAddress,
                                    combinationEquipment2.SlotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment2.AvatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment2.AvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment0 combinationEquipment0)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment0.AvatarAddress,
                                    combinationEquipment0.RecipeId,
                                    combinationEquipment0.SlotIndex,
                                    combinationEquipment0.SubRecipeId,
                                    combinationEquipment0.Id,
                                    ae.InputContext.BlockIndex));

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    combinationEquipment0.AvatarAddress,
                                    combinationEquipment0.SlotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment0.AvatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment0.AvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is ItemEnhancement itemEnhancement)
                            {
                                var start = DateTimeOffset.UtcNow;
                                if (ItemEnhancementFailData.GetItemEnhancementFailInfo(
                                        ae.InputContext.PreviousState,
                                        ae.OutputState,
                                        ae.InputContext.Signer,
                                        itemEnhancement.avatarAddress,
                                        Guid.Empty,
                                        itemEnhancement.materialIds,
                                        itemEnhancement.itemId,
                                        itemEnhancement.Id,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset) is { } itemEnhancementFailModel)
                                {
                                    _itemEnhancementFailList.Add(itemEnhancementFailModel);
                                }

                                _itemEnhancementList.Add(ItemEnhancementData.GetItemEnhancementInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    itemEnhancement.avatarAddress,
                                    itemEnhancement.slotIndex,
                                    Guid.Empty,
                                    itemEnhancement.materialIds,
                                    itemEnhancement.itemId,
                                    itemEnhancement.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ItemEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    itemEnhancement.avatarAddress,
                                    itemEnhancement.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        itemEnhancement.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    itemEnhancement.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is ItemEnhancement10 itemEnhancement10)
                            {
                                var start = DateTimeOffset.UtcNow;
                                if (ItemEnhancementFailData.GetItemEnhancementFailInfo(
                                        ae.InputContext.PreviousState,
                                        ae.OutputState,
                                        ae.InputContext.Signer,
                                        itemEnhancement10.avatarAddress,
                                        itemEnhancement10.materialId,
                                        new List<Guid>(),
                                        itemEnhancement10.itemId,
                                        itemEnhancement10.Id,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset) is { } itemEnhancementFailModel)
                                {
                                    _itemEnhancementFailList.Add(itemEnhancementFailModel);
                                }

                                _itemEnhancementList.Add(ItemEnhancementData.GetItemEnhancementInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    itemEnhancement10.avatarAddress,
                                    itemEnhancement10.slotIndex,
                                    itemEnhancement10.materialId,
                                    new List<Guid>(),
                                    itemEnhancement10.itemId,
                                    itemEnhancement10.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ItemEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    itemEnhancement10.avatarAddress,
                                    itemEnhancement10.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        itemEnhancement10.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    itemEnhancement10.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is ItemEnhancement9 itemEnhancement9)
                            {
                                var start = DateTimeOffset.UtcNow;
                                if (ItemEnhancementFailData.GetItemEnhancementFailInfo(
                                        ae.InputContext.PreviousState,
                                        ae.OutputState,
                                        ae.InputContext.Signer,
                                        itemEnhancement9.avatarAddress,
                                        itemEnhancement9.materialId,
                                        new List<Guid>(),
                                        itemEnhancement9.itemId,
                                        itemEnhancement9.Id,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset) is { } itemEnhancementFailModel)
                                {
                                    _itemEnhancementFailList.Add(itemEnhancementFailModel);
                                }

                                _itemEnhancementList.Add(ItemEnhancementData.GetItemEnhancementInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    itemEnhancement9.avatarAddress,
                                    itemEnhancement9.slotIndex,
                                    itemEnhancement9.materialId,
                                    new List<Guid>(),
                                    itemEnhancement9.itemId,
                                    itemEnhancement9.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ItemEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    itemEnhancement9.avatarAddress,
                                    itemEnhancement9.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        itemEnhancement9.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    itemEnhancement9.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is ItemEnhancement8 itemEnhancement8)
                            {
                                var start = DateTimeOffset.UtcNow;
                                if (ItemEnhancementFailData.GetItemEnhancementFailInfo(
                                        ae.InputContext.PreviousState,
                                        ae.OutputState,
                                        ae.InputContext.Signer,
                                        itemEnhancement8.avatarAddress,
                                        itemEnhancement8.materialId,
                                        new List<Guid>(),
                                        itemEnhancement8.itemId,
                                        itemEnhancement8.Id,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset) is { } itemEnhancementFailModel)
                                {
                                    _itemEnhancementFailList.Add(itemEnhancementFailModel);
                                }

                                _itemEnhancementList.Add(ItemEnhancementData.GetItemEnhancementInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    itemEnhancement8.avatarAddress,
                                    itemEnhancement8.slotIndex,
                                    itemEnhancement8.materialId,
                                    new List<Guid>(),
                                    itemEnhancement8.itemId,
                                    itemEnhancement8.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ItemEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    itemEnhancement8.avatarAddress,
                                    itemEnhancement8.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        itemEnhancement8.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    itemEnhancement8.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is ItemEnhancement7 itemEnhancement7)
                            {
                                var start = DateTimeOffset.UtcNow;
                                if (ItemEnhancementFailData.GetItemEnhancementFailInfo(
                                        ae.InputContext.PreviousState,
                                        ae.OutputState,
                                        ae.InputContext.Signer,
                                        itemEnhancement7.avatarAddress,
                                        itemEnhancement7.materialId,
                                        new List<Guid>(),
                                        itemEnhancement7.itemId,
                                        itemEnhancement7.Id,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset) is { } itemEnhancementFailModel)
                                {
                                    _itemEnhancementFailList.Add(itemEnhancementFailModel);
                                }

                                _itemEnhancementList.Add(ItemEnhancementData.GetItemEnhancementInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    itemEnhancement7.avatarAddress,
                                    itemEnhancement7.slotIndex,
                                    itemEnhancement7.materialId,
                                    new List<Guid>(),
                                    itemEnhancement7.itemId,
                                    itemEnhancement7.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ItemEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    itemEnhancement7.avatarAddress,
                                    itemEnhancement7.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        itemEnhancement7.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    itemEnhancement7.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is ItemEnhancement6 itemEnhancement6)
                            {
                                var start = DateTimeOffset.UtcNow;
                                if (ItemEnhancementFailData.GetItemEnhancementFailInfo(
                                        ae.InputContext.PreviousState,
                                        ae.OutputState,
                                        ae.InputContext.Signer,
                                        itemEnhancement6.avatarAddress,
                                        itemEnhancement6.materialId,
                                        new List<Guid>(),
                                        itemEnhancement6.itemId,
                                        itemEnhancement6.Id,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset) is { } itemEnhancementFailModel)
                                {
                                    _itemEnhancementFailList.Add(itemEnhancementFailModel);
                                }

                                _itemEnhancementList.Add(ItemEnhancementData.GetItemEnhancementInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    itemEnhancement6.avatarAddress,
                                    itemEnhancement6.slotIndex,
                                    itemEnhancement6.materialId,
                                    new List<Guid>(),
                                    itemEnhancement6.itemId,
                                    itemEnhancement6.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ItemEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    itemEnhancement6.avatarAddress,
                                    itemEnhancement6.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        itemEnhancement6.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    itemEnhancement6.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is ItemEnhancement5 itemEnhancement5)
                            {
                                var start = DateTimeOffset.UtcNow;
                                if (ItemEnhancementFailData.GetItemEnhancementFailInfo(
                                        ae.InputContext.PreviousState,
                                        ae.OutputState,
                                        ae.InputContext.Signer,
                                        itemEnhancement5.avatarAddress,
                                        itemEnhancement5.materialId,
                                        new List<Guid>(),
                                        itemEnhancement5.itemId,
                                        itemEnhancement5.Id,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset) is { } itemEnhancementFailModel)
                                {
                                    _itemEnhancementFailList.Add(itemEnhancementFailModel);
                                }

                                _itemEnhancementList.Add(ItemEnhancementData.GetItemEnhancementInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    itemEnhancement5.avatarAddress,
                                    itemEnhancement5.slotIndex,
                                    itemEnhancement5.materialId,
                                    new List<Guid>(),
                                    itemEnhancement5.itemId,
                                    itemEnhancement5.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ItemEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    itemEnhancement5.avatarAddress,
                                    itemEnhancement5.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        itemEnhancement5.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    itemEnhancement5.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is ItemEnhancement4 itemEnhancement4)
                            {
                                var start = DateTimeOffset.UtcNow;
                                if (ItemEnhancementFailData.GetItemEnhancementFailInfo(
                                        ae.InputContext.PreviousState,
                                        ae.OutputState,
                                        ae.InputContext.Signer,
                                        itemEnhancement4.avatarAddress,
                                        itemEnhancement4.materialId,
                                        new List<Guid>(),
                                        itemEnhancement4.itemId,
                                        itemEnhancement4.Id,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset) is { } itemEnhancementFailModel)
                                {
                                    _itemEnhancementFailList.Add(itemEnhancementFailModel);
                                }

                                _itemEnhancementList.Add(ItemEnhancementData.GetItemEnhancementInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    itemEnhancement4.avatarAddress,
                                    itemEnhancement4.slotIndex,
                                    itemEnhancement4.materialId,
                                    new List<Guid>(),
                                    itemEnhancement4.itemId,
                                    itemEnhancement4.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ItemEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    itemEnhancement4.avatarAddress,
                                    itemEnhancement4.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        itemEnhancement4.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    itemEnhancement4.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is ItemEnhancement3 itemEnhancement3)
                            {
                                var start = DateTimeOffset.UtcNow;
                                if (ItemEnhancementFailData.GetItemEnhancementFailInfo(
                                        ae.InputContext.PreviousState,
                                        ae.OutputState,
                                        ae.InputContext.Signer,
                                        itemEnhancement3.avatarAddress,
                                        itemEnhancement3.materialId,
                                        new List<Guid>(),
                                        itemEnhancement3.itemId,
                                        itemEnhancement3.Id,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset) is { } itemEnhancementFailModel)
                                {
                                    _itemEnhancementFailList.Add(itemEnhancementFailModel);
                                }

                                _itemEnhancementList.Add(ItemEnhancementData.GetItemEnhancementInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    itemEnhancement3.avatarAddress,
                                    itemEnhancement3.slotIndex,
                                    itemEnhancement3.materialId,
                                    new List<Guid>(),
                                    itemEnhancement3.itemId,
                                    itemEnhancement3.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ItemEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    itemEnhancement3.avatarAddress,
                                    itemEnhancement3.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        itemEnhancement3.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    itemEnhancement3.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is ItemEnhancement2 itemEnhancement2)
                            {
                                var start = DateTimeOffset.UtcNow;
                                if (ItemEnhancementFailData.GetItemEnhancementFailInfo(
                                        ae.InputContext.PreviousState,
                                        ae.OutputState,
                                        ae.InputContext.Signer,
                                        itemEnhancement2.avatarAddress,
                                        itemEnhancement2.materialId,
                                        new List<Guid>(),
                                        itemEnhancement2.itemId,
                                        itemEnhancement2.Id,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset) is { } itemEnhancementFailModel)
                                {
                                    _itemEnhancementFailList.Add(itemEnhancementFailModel);
                                }

                                _itemEnhancementList.Add(ItemEnhancementData.GetItemEnhancementInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    itemEnhancement2.avatarAddress,
                                    itemEnhancement2.slotIndex,
                                    itemEnhancement2.materialId,
                                    new List<Guid>(),
                                    itemEnhancement2.itemId,
                                    itemEnhancement2.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ItemEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    itemEnhancement2.avatarAddress,
                                    itemEnhancement2.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        itemEnhancement2.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    itemEnhancement2.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is ItemEnhancement0 itemEnhancement0)
                            {
                                var start = DateTimeOffset.UtcNow;
                                if (ItemEnhancementFailData.GetItemEnhancementFailInfo(
                                        ae.InputContext.PreviousState,
                                        ae.OutputState,
                                        ae.InputContext.Signer,
                                        itemEnhancement0.avatarAddress,
                                        itemEnhancement0.materialIds.FirstOrDefault(),
                                        new List<Guid>(),
                                        itemEnhancement0.itemId,
                                        itemEnhancement0.Id,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset) is { } itemEnhancementFailModel)
                                {
                                    _itemEnhancementFailList.Add(itemEnhancementFailModel);
                                }

                                _itemEnhancementList.Add(ItemEnhancementData.GetItemEnhancementInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    itemEnhancement0.avatarAddress,
                                    itemEnhancement0.slotIndex,
                                    itemEnhancement0.materialIds.FirstOrDefault(),
                                    new List<Guid>(),
                                    itemEnhancement0.itemId,
                                    itemEnhancement0.Id,
                                    ae.InputContext.BlockIndex));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ItemEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = ae.OutputState.GetCombinationSlotState(
                                    itemEnhancement0.avatarAddress,
                                    itemEnhancement0.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        itemEnhancement0.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    itemEnhancement0.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is Buy buy)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ae.OutputState.GetAvatarStateV2(buy.buyerAvatarAddress);
                                var buyerInventory = avatarState.inventory;
                                foreach (var purchaseInfo in buy.purchaseInfos)
                                {
                                    var state = ae.OutputState.GetState(
                                    Addresses.GetItemAddress(purchaseInfo.TradableId));
                                    ITradableItem orderItem =
                                        (ITradableItem)ItemFactory.Deserialize((Dictionary)state!);
                                    Order order =
                                        OrderFactory.Deserialize(
                                            (Dictionary)ae.OutputState.GetState(
                                                Order.DeriveAddress(purchaseInfo.OrderId))!);
                                    int itemCount = order is FungibleOrder fungibleOrder
                                        ? fungibleOrder.ItemCount
                                        : 1;
                                    if (orderItem.ItemType == ItemType.Equipment)
                                    {
                                        Equipment equipment = (Equipment)orderItem;
                                        _buyShopEquipmentsList.Add(ShopHistoryEquipmentData.GetShopHistoryEquipmentInfo(
                                            buy.buyerAvatarAddress,
                                            purchaseInfo,
                                            equipment,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Costume)
                                    {
                                        Costume costume = (Costume)orderItem;
                                        _buyShopCostumesList.Add(ShopHistoryCostumeData.GetShopHistoryCostumeInfo(
                                            buy.buyerAvatarAddress,
                                            purchaseInfo,
                                            costume,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Material)
                                    {
                                        Material material = (Material)orderItem;
                                        _buyShopMaterialsList.Add(ShopHistoryMaterialData.GetShopHistoryMaterialInfo(
                                            buy.buyerAvatarAddress,
                                            purchaseInfo,
                                            material,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Consumable)
                                    {
                                        Consumable consumable = (Consumable)orderItem;
                                        _buyShopConsumablesList.Add(ShopHistoryConsumableData.GetShopHistoryConsumableInfo(
                                            buy.buyerAvatarAddress,
                                            purchaseInfo,
                                            consumable,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (purchaseInfo.ItemSubType == ItemSubType.Armor
                                        || purchaseInfo.ItemSubType == ItemSubType.Belt
                                        || purchaseInfo.ItemSubType == ItemSubType.Necklace
                                        || purchaseInfo.ItemSubType == ItemSubType.Ring
                                        || purchaseInfo.ItemSubType == ItemSubType.Weapon)
                                    {
                                        var sellerState = ae.OutputState.GetAvatarStateV2(purchaseInfo.SellerAvatarAddress);
                                        var sellerInventory = sellerState.inventory;

                                        if (buyerInventory.Equipments == null || sellerInventory.Equipments == null)
                                        {
                                            continue;
                                        }

                                        Equipment equipment = buyerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.TradableId) ?? sellerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.TradableId);

                                        if (equipment is { } equipmentNotNull)
                                        {
                                            _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                                ae.InputContext.Signer,
                                                buy.buyerAvatarAddress,
                                                equipmentNotNull));
                                        }
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    buy.buyerAvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is Buy11 buy11)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ae.OutputState.GetAvatarStateV2(buy11.buyerAvatarAddress);
                                var buyerInventory = avatarState.inventory;
                                foreach (var purchaseInfo in buy11.purchaseInfos)
                                {
                                    var state = ae.OutputState.GetState(
                                    Addresses.GetItemAddress(purchaseInfo.TradableId));
                                    ITradableItem orderItem =
                                        (ITradableItem)ItemFactory.Deserialize((Dictionary)state!);
                                    Order order =
                                        OrderFactory.Deserialize(
                                            (Dictionary)ae.OutputState.GetState(
                                                Order.DeriveAddress(purchaseInfo.OrderId))!);
                                    int itemCount = order is FungibleOrder fungibleOrder
                                        ? fungibleOrder.ItemCount
                                        : 1;
                                    if (orderItem.ItemType == ItemType.Equipment)
                                    {
                                        Equipment equipment = (Equipment)orderItem;
                                        _buyShopEquipmentsList.Add(ShopHistoryEquipmentData.GetShopHistoryEquipmentInfo(
                                            buy11.buyerAvatarAddress,
                                            purchaseInfo,
                                            equipment,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Costume)
                                    {
                                        Costume costume = (Costume)orderItem;
                                        _buyShopCostumesList.Add(ShopHistoryCostumeData.GetShopHistoryCostumeInfo(
                                            buy11.buyerAvatarAddress,
                                            purchaseInfo,
                                            costume,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Material)
                                    {
                                        Material material = (Material)orderItem;
                                        _buyShopMaterialsList.Add(ShopHistoryMaterialData.GetShopHistoryMaterialInfo(
                                            buy11.buyerAvatarAddress,
                                            purchaseInfo,
                                            material,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Consumable)
                                    {
                                        Consumable consumable = (Consumable)orderItem;
                                        _buyShopConsumablesList.Add(ShopHistoryConsumableData.GetShopHistoryConsumableInfo(
                                            buy11.buyerAvatarAddress,
                                            purchaseInfo,
                                            consumable,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (purchaseInfo.ItemSubType == ItemSubType.Armor
                                        || purchaseInfo.ItemSubType == ItemSubType.Belt
                                        || purchaseInfo.ItemSubType == ItemSubType.Necklace
                                        || purchaseInfo.ItemSubType == ItemSubType.Ring
                                        || purchaseInfo.ItemSubType == ItemSubType.Weapon)
                                    {
                                        var sellerState = ae.OutputState.GetAvatarStateV2(purchaseInfo.SellerAvatarAddress);
                                        var sellerInventory = sellerState.inventory;

                                        if (buyerInventory.Equipments == null || sellerInventory.Equipments == null)
                                        {
                                            continue;
                                        }

                                        Equipment equipment = buyerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.TradableId) ?? sellerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.TradableId);

                                        if (equipment is { } equipmentNotNull)
                                        {
                                            _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                                ae.InputContext.Signer,
                                                buy11.buyerAvatarAddress,
                                                equipmentNotNull));
                                        }
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    buy11.buyerAvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is Buy10 buy10)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ae.OutputState.GetAvatarStateV2(buy10.buyerAvatarAddress);
                                var buyerInventory = avatarState.inventory;
                                foreach (var purchaseInfo in buy10.purchaseInfos)
                                {
                                    var state = ae.OutputState.GetState(
                                    Addresses.GetItemAddress(purchaseInfo.TradableId));
                                    ITradableItem orderItem =
                                        (ITradableItem)ItemFactory.Deserialize((Dictionary)state!);
                                    Order order =
                                        OrderFactory.Deserialize(
                                            (Dictionary)ae.OutputState.GetState(
                                                Order.DeriveAddress(purchaseInfo.OrderId))!);
                                    int itemCount = order is FungibleOrder fungibleOrder
                                        ? fungibleOrder.ItemCount
                                        : 1;
                                    if (orderItem.ItemType == ItemType.Equipment)
                                    {
                                        Equipment equipment = (Equipment)orderItem;
                                        _buyShopEquipmentsList.Add(ShopHistoryEquipmentData.GetShopHistoryEquipmentInfo(
                                            buy10.buyerAvatarAddress,
                                            purchaseInfo,
                                            equipment,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Costume)
                                    {
                                        Costume costume = (Costume)orderItem;
                                        _buyShopCostumesList.Add(ShopHistoryCostumeData.GetShopHistoryCostumeInfo(
                                            buy10.buyerAvatarAddress,
                                            purchaseInfo,
                                            costume,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Material)
                                    {
                                        Material material = (Material)orderItem;
                                        _buyShopMaterialsList.Add(ShopHistoryMaterialData.GetShopHistoryMaterialInfo(
                                            buy10.buyerAvatarAddress,
                                            purchaseInfo,
                                            material,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Consumable)
                                    {
                                        Consumable consumable = (Consumable)orderItem;
                                        _buyShopConsumablesList.Add(ShopHistoryConsumableData.GetShopHistoryConsumableInfo(
                                            buy10.buyerAvatarAddress,
                                            purchaseInfo,
                                            consumable,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (purchaseInfo.ItemSubType == ItemSubType.Armor
                                        || purchaseInfo.ItemSubType == ItemSubType.Belt
                                        || purchaseInfo.ItemSubType == ItemSubType.Necklace
                                        || purchaseInfo.ItemSubType == ItemSubType.Ring
                                        || purchaseInfo.ItemSubType == ItemSubType.Weapon)
                                    {
                                        var sellerState = ae.OutputState.GetAvatarStateV2(purchaseInfo.SellerAvatarAddress);
                                        var sellerInventory = sellerState.inventory;

                                        if (buyerInventory.Equipments == null || sellerInventory.Equipments == null)
                                        {
                                            continue;
                                        }

                                        Equipment equipment = buyerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.TradableId) ?? sellerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.TradableId);

                                        if (equipment is { } equipmentNotNull)
                                        {
                                            _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                                ae.InputContext.Signer,
                                                buy10.buyerAvatarAddress,
                                                equipmentNotNull));
                                        }
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    buy10.buyerAvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is Buy9 buy9)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ae.OutputState.GetAvatarStateV2(buy9.buyerAvatarAddress);
                                var buyerInventory = avatarState.inventory;
                                foreach (var purchaseInfo in buy9.purchaseInfos)
                                {
                                    var state = ae.OutputState.GetState(
                                    Addresses.GetItemAddress(purchaseInfo.TradableId));
                                    ITradableItem orderItem =
                                        (ITradableItem)ItemFactory.Deserialize((Dictionary)state!);
                                    Order order =
                                        OrderFactory.Deserialize(
                                            (Dictionary)ae.OutputState.GetState(
                                                Order.DeriveAddress(purchaseInfo.OrderId))!);
                                    int itemCount = order is FungibleOrder fungibleOrder
                                        ? fungibleOrder.ItemCount
                                        : 1;
                                    if (orderItem.ItemType == ItemType.Equipment)
                                    {
                                        Equipment equipment = (Equipment)orderItem;
                                        _buyShopEquipmentsList.Add(ShopHistoryEquipmentData.GetShopHistoryEquipmentInfo(
                                            buy9.buyerAvatarAddress,
                                            purchaseInfo,
                                            equipment,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Costume)
                                    {
                                        Costume costume = (Costume)orderItem;
                                        _buyShopCostumesList.Add(ShopHistoryCostumeData.GetShopHistoryCostumeInfo(
                                            buy9.buyerAvatarAddress,
                                            purchaseInfo,
                                            costume,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Material)
                                    {
                                        Material material = (Material)orderItem;
                                        _buyShopMaterialsList.Add(ShopHistoryMaterialData.GetShopHistoryMaterialInfo(
                                            buy9.buyerAvatarAddress,
                                            purchaseInfo,
                                            material,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Consumable)
                                    {
                                        Consumable consumable = (Consumable)orderItem;
                                        _buyShopConsumablesList.Add(ShopHistoryConsumableData.GetShopHistoryConsumableInfo(
                                            buy9.buyerAvatarAddress,
                                            purchaseInfo,
                                            consumable,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (purchaseInfo.ItemSubType == ItemSubType.Armor
                                        || purchaseInfo.ItemSubType == ItemSubType.Belt
                                        || purchaseInfo.ItemSubType == ItemSubType.Necklace
                                        || purchaseInfo.ItemSubType == ItemSubType.Ring
                                        || purchaseInfo.ItemSubType == ItemSubType.Weapon)
                                    {
                                        var sellerState = ae.OutputState.GetAvatarStateV2(purchaseInfo.SellerAvatarAddress);
                                        var sellerInventory = sellerState.inventory;

                                        if (buyerInventory.Equipments == null || sellerInventory.Equipments == null)
                                        {
                                            continue;
                                        }

                                        Equipment equipment = buyerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.TradableId) ?? sellerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.TradableId);

                                        if (equipment is { } equipmentNotNull)
                                        {
                                            _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                                ae.InputContext.Signer,
                                                buy9.buyerAvatarAddress,
                                                equipmentNotNull));
                                        }
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    buy9.buyerAvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is Buy8 buy8)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ae.OutputState.GetAvatarStateV2(buy8.buyerAvatarAddress);
                                var buyerInventory = avatarState.inventory;
                                foreach (var purchaseInfo in buy8.purchaseInfos)
                                {
                                    var state = ae.OutputState.GetState(
                                    Addresses.GetItemAddress(purchaseInfo.TradableId));
                                    ITradableItem orderItem =
                                        (ITradableItem)ItemFactory.Deserialize((Dictionary)state!);
                                    Order order =
                                        OrderFactory.Deserialize(
                                            (Dictionary)ae.OutputState.GetState(
                                                Order.DeriveAddress(purchaseInfo.OrderId))!);
                                    int itemCount = order is FungibleOrder fungibleOrder
                                        ? fungibleOrder.ItemCount
                                        : 1;
                                    if (orderItem.ItemType == ItemType.Equipment)
                                    {
                                        Equipment equipment = (Equipment)orderItem;
                                        _buyShopEquipmentsList.Add(ShopHistoryEquipmentData.GetShopHistoryEquipmentInfo(
                                            buy8.buyerAvatarAddress,
                                            purchaseInfo,
                                            equipment,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Costume)
                                    {
                                        Costume costume = (Costume)orderItem;
                                        _buyShopCostumesList.Add(ShopHistoryCostumeData.GetShopHistoryCostumeInfo(
                                            buy8.buyerAvatarAddress,
                                            purchaseInfo,
                                            costume,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Material)
                                    {
                                        Material material = (Material)orderItem;
                                        _buyShopMaterialsList.Add(ShopHistoryMaterialData.GetShopHistoryMaterialInfo(
                                            buy8.buyerAvatarAddress,
                                            purchaseInfo,
                                            material,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Consumable)
                                    {
                                        Consumable consumable = (Consumable)orderItem;
                                        _buyShopConsumablesList.Add(ShopHistoryConsumableData.GetShopHistoryConsumableInfo(
                                            buy8.buyerAvatarAddress,
                                            purchaseInfo,
                                            consumable,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (purchaseInfo.ItemSubType == ItemSubType.Armor
                                        || purchaseInfo.ItemSubType == ItemSubType.Belt
                                        || purchaseInfo.ItemSubType == ItemSubType.Necklace
                                        || purchaseInfo.ItemSubType == ItemSubType.Ring
                                        || purchaseInfo.ItemSubType == ItemSubType.Weapon)
                                    {
                                        var sellerState = ae.OutputState.GetAvatarStateV2(purchaseInfo.SellerAvatarAddress);
                                        var sellerInventory = sellerState.inventory;

                                        if (buyerInventory.Equipments == null || sellerInventory.Equipments == null)
                                        {
                                            continue;
                                        }

                                        Equipment equipment = buyerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.TradableId) ?? sellerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.TradableId);

                                        if (equipment is { } equipmentNotNull)
                                        {
                                            _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                                ae.InputContext.Signer,
                                                buy8.buyerAvatarAddress,
                                                equipmentNotNull));
                                        }
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    buy8.buyerAvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is Buy7 buy7)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ae.OutputState.GetAvatarStateV2(buy7.buyerAvatarAddress);
                                var buyerInventory = avatarState.inventory;
                                foreach (var purchaseInfo in buy7.purchaseInfos)
                                {
                                    var state = ae.OutputState.GetState(
                                    Addresses.GetItemAddress(purchaseInfo.productId));
                                    ITradableItem orderItem =
                                        (ITradableItem)ItemFactory.Deserialize((Dictionary)state!);
                                    Order order =
                                        OrderFactory.Deserialize(
                                            (Dictionary)ae.OutputState.GetState(
                                                Order.DeriveAddress(purchaseInfo.productId))!);
                                    int itemCount = order is FungibleOrder fungibleOrder
                                        ? fungibleOrder.ItemCount
                                        : 1;
                                    if (orderItem.ItemType == ItemType.Equipment)
                                    {
                                        Equipment equipment = (Equipment)orderItem;
                                        _buyShopEquipmentsList.Add(ShopHistoryEquipmentData.GetShopHistoryEquipmentInfoV2(
                                            buy7.buyerAvatarAddress,
                                            purchaseInfo,
                                            equipment,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Costume)
                                    {
                                        Costume costume = (Costume)orderItem;
                                        _buyShopCostumesList.Add(ShopHistoryCostumeData.GetShopHistoryCostumeInfoV2(
                                            buy7.buyerAvatarAddress,
                                            purchaseInfo,
                                            costume,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Material)
                                    {
                                        Material material = (Material)orderItem;
                                        _buyShopMaterialsList.Add(ShopHistoryMaterialData.GetShopHistoryMaterialInfoV2(
                                            buy7.buyerAvatarAddress,
                                            purchaseInfo,
                                            material,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Consumable)
                                    {
                                        Consumable consumable = (Consumable)orderItem;
                                        _buyShopConsumablesList.Add(ShopHistoryConsumableData.GetShopHistoryConsumableInfoV2(
                                            buy7.buyerAvatarAddress,
                                            purchaseInfo,
                                            consumable,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (purchaseInfo.itemSubType == ItemSubType.Armor
                                        || purchaseInfo.itemSubType == ItemSubType.Belt
                                        || purchaseInfo.itemSubType == ItemSubType.Necklace
                                        || purchaseInfo.itemSubType == ItemSubType.Ring
                                        || purchaseInfo.itemSubType == ItemSubType.Weapon)
                                    {
                                        var sellerState = ae.OutputState.GetAvatarStateV2(purchaseInfo.sellerAvatarAddress);
                                        var sellerInventory = sellerState.inventory;

                                        if (buyerInventory.Equipments == null || sellerInventory.Equipments == null)
                                        {
                                            continue;
                                        }

                                        Equipment equipment = buyerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.productId) ?? sellerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.productId);

                                        if (equipment is { } equipmentNotNull)
                                        {
                                            _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                                ae.InputContext.Signer,
                                                buy7.buyerAvatarAddress,
                                                equipmentNotNull));
                                        }
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    buy7.buyerAvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is Buy6 buy6)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ae.OutputState.GetAvatarStateV2(buy6.buyerAvatarAddress);
                                var buyerInventory = avatarState.inventory;
                                foreach (var purchaseInfo in buy6.purchaseInfos)
                                {
                                    var state = ae.OutputState.GetState(
                                    Addresses.GetItemAddress(purchaseInfo.productId));
                                    ITradableItem orderItem =
                                        (ITradableItem)ItemFactory.Deserialize((Dictionary)state!);
                                    Order order =
                                        OrderFactory.Deserialize(
                                            (Dictionary)ae.OutputState.GetState(
                                                Order.DeriveAddress(purchaseInfo.productId))!);
                                    int itemCount = order is FungibleOrder fungibleOrder
                                        ? fungibleOrder.ItemCount
                                        : 1;
                                    if (orderItem.ItemType == ItemType.Equipment)
                                    {
                                        Equipment equipment = (Equipment)orderItem;
                                        _buyShopEquipmentsList.Add(ShopHistoryEquipmentData.GetShopHistoryEquipmentInfoV2(
                                            buy6.buyerAvatarAddress,
                                            purchaseInfo,
                                            equipment,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Costume)
                                    {
                                        Costume costume = (Costume)orderItem;
                                        _buyShopCostumesList.Add(ShopHistoryCostumeData.GetShopHistoryCostumeInfoV2(
                                            buy6.buyerAvatarAddress,
                                            purchaseInfo,
                                            costume,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Material)
                                    {
                                        Material material = (Material)orderItem;
                                        _buyShopMaterialsList.Add(ShopHistoryMaterialData.GetShopHistoryMaterialInfoV2(
                                            buy6.buyerAvatarAddress,
                                            purchaseInfo,
                                            material,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Consumable)
                                    {
                                        Consumable consumable = (Consumable)orderItem;
                                        _buyShopConsumablesList.Add(ShopHistoryConsumableData.GetShopHistoryConsumableInfoV2(
                                            buy6.buyerAvatarAddress,
                                            purchaseInfo,
                                            consumable,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (purchaseInfo.itemSubType == ItemSubType.Armor
                                        || purchaseInfo.itemSubType == ItemSubType.Belt
                                        || purchaseInfo.itemSubType == ItemSubType.Necklace
                                        || purchaseInfo.itemSubType == ItemSubType.Ring
                                        || purchaseInfo.itemSubType == ItemSubType.Weapon)
                                    {
                                        var sellerState = ae.OutputState.GetAvatarStateV2(purchaseInfo.sellerAvatarAddress);
                                        var sellerInventory = sellerState.inventory;

                                        if (buyerInventory.Equipments == null || sellerInventory.Equipments == null)
                                        {
                                            continue;
                                        }

                                        Equipment equipment = buyerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.productId) ?? sellerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.productId);

                                        if (equipment is { } equipmentNotNull)
                                        {
                                            _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                                ae.InputContext.Signer,
                                                buy6.buyerAvatarAddress,
                                                equipmentNotNull));
                                        }
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    buy6.buyerAvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is Buy5 buy5)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ae.OutputState.GetAvatarStateV2(buy5.buyerAvatarAddress);
                                var buyerInventory = avatarState.inventory;
                                foreach (var purchaseInfo in buy5.purchaseInfos)
                                {
                                    var state = ae.OutputState.GetState(
                                    Addresses.GetItemAddress(purchaseInfo.productId));
                                    ITradableItem orderItem =
                                        (ITradableItem)ItemFactory.Deserialize((Dictionary)state!);
                                    Order order =
                                        OrderFactory.Deserialize(
                                            (Dictionary)ae.OutputState.GetState(
                                                Order.DeriveAddress(purchaseInfo.productId))!);
                                    int itemCount = order is FungibleOrder fungibleOrder
                                        ? fungibleOrder.ItemCount
                                        : 1;
                                    if (orderItem.ItemType == ItemType.Equipment)
                                    {
                                        Equipment equipment = (Equipment)orderItem;
                                        _buyShopEquipmentsList.Add(ShopHistoryEquipmentData.GetShopHistoryEquipmentInfoV2(
                                            buy5.buyerAvatarAddress,
                                            purchaseInfo,
                                            equipment,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Costume)
                                    {
                                        Costume costume = (Costume)orderItem;
                                        _buyShopCostumesList.Add(ShopHistoryCostumeData.GetShopHistoryCostumeInfoV2(
                                            buy5.buyerAvatarAddress,
                                            purchaseInfo,
                                            costume,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Material)
                                    {
                                        Material material = (Material)orderItem;
                                        _buyShopMaterialsList.Add(ShopHistoryMaterialData.GetShopHistoryMaterialInfoV2(
                                            buy5.buyerAvatarAddress,
                                            purchaseInfo,
                                            material,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (orderItem.ItemType == ItemType.Consumable)
                                    {
                                        Consumable consumable = (Consumable)orderItem;
                                        _buyShopConsumablesList.Add(ShopHistoryConsumableData.GetShopHistoryConsumableInfoV2(
                                            buy5.buyerAvatarAddress,
                                            purchaseInfo,
                                            consumable,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                    }

                                    if (purchaseInfo.itemSubType == ItemSubType.Armor
                                        || purchaseInfo.itemSubType == ItemSubType.Belt
                                        || purchaseInfo.itemSubType == ItemSubType.Necklace
                                        || purchaseInfo.itemSubType == ItemSubType.Ring
                                        || purchaseInfo.itemSubType == ItemSubType.Weapon)
                                    {
                                        var sellerState = ae.OutputState.GetAvatarStateV2(purchaseInfo.sellerAvatarAddress);
                                        var sellerInventory = sellerState.inventory;

                                        if (buyerInventory.Equipments == null || sellerInventory.Equipments == null)
                                        {
                                            continue;
                                        }

                                        Equipment equipment = buyerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.productId) ?? sellerInventory.Equipments.SingleOrDefault(i =>
                                            i.ItemId == purchaseInfo.productId);

                                        if (equipment is { } equipmentNotNull)
                                        {
                                            _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                                ae.InputContext.Signer,
                                                buy5.buyerAvatarAddress,
                                                equipmentNotNull));
                                        }
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    buy5.buyerAvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is Buy4 buy4)
                            {
                                var start = DateTimeOffset.UtcNow;
                                ShopItem shopItem = buy4.buyerResult.shopItem;

                                int itemCount = shopItem.TradableFungibleItemCount == 0 ? 1 : shopItem.TradableFungibleItemCount;
                                if (shopItem.ItemUsable.ItemType == ItemType.Equipment)
                                {
                                    var equipment = (Equipment)shopItem.ItemUsable;
                                    _buyShopEquipmentsList.Add(ShopHistoryEquipmentData.GetShopHistoryEquipmentInfoV1(
                                        buy4.buyerAvatarAddress,
                                        shopItem.SellerAvatarAddress,
                                        buy4.buyerResult,
                                        shopItem.Price,
                                        equipment,
                                        itemCount,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset));
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        buy4.buyerAvatarAddress,
                                        equipment));
                                }

                                if (shopItem.ItemUsable.ItemType == ItemType.Costume)
                                {
                                    _buyShopCostumesList.Add(ShopHistoryCostumeData.GetShopHistoryCostumeInfoV1(
                                        buy4.buyerAvatarAddress,
                                        shopItem.SellerAvatarAddress,
                                        buy4.buyerResult,
                                        shopItem.Price,
                                        shopItem.Costume,
                                        itemCount,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset));
                                }

                                if (shopItem.ItemUsable.ItemType == ItemType.Material)
                                {
                                    Material material = (Material)shopItem.TradableFungibleItem;
                                    _buyShopMaterialsList.Add(ShopHistoryMaterialData.GetShopHistoryMaterialInfoV1(
                                        buy4.buyerAvatarAddress,
                                        shopItem.SellerAvatarAddress,
                                        buy4.buyerResult,
                                        shopItem.Price,
                                        material,
                                        itemCount,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset));
                                }

                                if (shopItem.ItemUsable.ItemType == ItemType.Consumable)
                                {
                                    Consumable consumable = (Consumable)shopItem.ItemUsable;
                                    _buyShopConsumablesList.Add(
                                        ShopHistoryConsumableData.GetShopHistoryConsumableInfoV1(
                                            buy4.buyerAvatarAddress,
                                            shopItem.SellerAvatarAddress,
                                            buy4.buyerResult,
                                            shopItem.Price,
                                            consumable,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    buy4.buyerAvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is Buy3 buy3)
                            {
                                var start = DateTimeOffset.UtcNow;
                                ShopItem shopItem = buy3.buyerResult.shopItem;

                                int itemCount = shopItem.TradableFungibleItemCount == 0 ? 1 : shopItem.TradableFungibleItemCount;
                                if (shopItem.ItemUsable.ItemType == ItemType.Equipment)
                                {
                                    var equipment = (Equipment)shopItem.ItemUsable;
                                    _buyShopEquipmentsList.Add(ShopHistoryEquipmentData.GetShopHistoryEquipmentInfoV1(
                                        buy3.buyerAvatarAddress,
                                        shopItem.SellerAvatarAddress,
                                        buy3.buyerResult,
                                        shopItem.Price,
                                        equipment,
                                        itemCount,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset));
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        buy3.buyerAvatarAddress,
                                        equipment));
                                }

                                if (shopItem.ItemUsable.ItemType == ItemType.Costume)
                                {
                                    _buyShopCostumesList.Add(ShopHistoryCostumeData.GetShopHistoryCostumeInfoV1(
                                        buy3.buyerAvatarAddress,
                                        shopItem.SellerAvatarAddress,
                                        buy3.buyerResult,
                                        shopItem.Price,
                                        shopItem.Costume,
                                        itemCount,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset));
                                }

                                if (shopItem.ItemUsable.ItemType == ItemType.Material)
                                {
                                    Material material = (Material)shopItem.TradableFungibleItem;
                                    _buyShopMaterialsList.Add(ShopHistoryMaterialData.GetShopHistoryMaterialInfoV1(
                                        buy3.buyerAvatarAddress,
                                        shopItem.SellerAvatarAddress,
                                        buy3.buyerResult,
                                        shopItem.Price,
                                        material,
                                        itemCount,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset));
                                }

                                if (shopItem.ItemUsable.ItemType == ItemType.Consumable)
                                {
                                    Consumable consumable = (Consumable)shopItem.ItemUsable;
                                    _buyShopConsumablesList.Add(
                                        ShopHistoryConsumableData.GetShopHistoryConsumableInfoV1(
                                            buy3.buyerAvatarAddress,
                                            shopItem.SellerAvatarAddress,
                                            buy3.buyerResult,
                                            shopItem.Price,
                                            consumable,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    buy3.buyerAvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is Buy2 buy2)
                            {
                                var start = DateTimeOffset.UtcNow;
                                ShopItem shopItem = buy2.buyerResult.shopItem;

                                int itemCount = shopItem.TradableFungibleItemCount == 0 ? 1 : shopItem.TradableFungibleItemCount;
                                if (shopItem.ItemUsable.ItemType == ItemType.Equipment)
                                {
                                    var equipment = (Equipment)shopItem.ItemUsable;
                                    _buyShopEquipmentsList.Add(ShopHistoryEquipmentData.GetShopHistoryEquipmentInfoV1(
                                        buy2.buyerAvatarAddress,
                                        shopItem.SellerAvatarAddress,
                                        buy2.buyerResult,
                                        shopItem.Price,
                                        equipment,
                                        itemCount,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset));
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        buy2.buyerAvatarAddress,
                                        equipment));
                                }

                                if (shopItem.ItemUsable.ItemType == ItemType.Costume)
                                {
                                    _buyShopCostumesList.Add(ShopHistoryCostumeData.GetShopHistoryCostumeInfoV1(
                                        buy2.buyerAvatarAddress,
                                        shopItem.SellerAvatarAddress,
                                        buy2.buyerResult,
                                        shopItem.Price,
                                        shopItem.Costume,
                                        itemCount,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset));
                                }

                                if (shopItem.ItemUsable.ItemType == ItemType.Material)
                                {
                                    Material material = (Material)shopItem.TradableFungibleItem;
                                    _buyShopMaterialsList.Add(ShopHistoryMaterialData.GetShopHistoryMaterialInfoV1(
                                        buy2.buyerAvatarAddress,
                                        shopItem.SellerAvatarAddress,
                                        buy2.buyerResult,
                                        shopItem.Price,
                                        material,
                                        itemCount,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset));
                                }

                                if (shopItem.ItemUsable.ItemType == ItemType.Consumable)
                                {
                                    Consumable consumable = (Consumable)shopItem.ItemUsable;
                                    _buyShopConsumablesList.Add(
                                        ShopHistoryConsumableData.GetShopHistoryConsumableInfoV1(
                                            buy2.buyerAvatarAddress,
                                            shopItem.SellerAvatarAddress,
                                            buy2.buyerResult,
                                            shopItem.Price,
                                            consumable,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    buy2.buyerAvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is Buy0 buy0)
                            {
                                var start = DateTimeOffset.UtcNow;
                                ShopItem shopItem = buy0.buyerResult.shopItem;

                                int itemCount = shopItem.TradableFungibleItemCount == 0 ? 1 : shopItem.TradableFungibleItemCount;
                                if (shopItem.ItemUsable.ItemType == ItemType.Equipment)
                                {
                                    var equipment = (Equipment)shopItem.ItemUsable;
                                    _buyShopEquipmentsList.Add(ShopHistoryEquipmentData.GetShopHistoryEquipmentInfoV1(
                                        buy0.buyerAvatarAddress,
                                        shopItem.SellerAvatarAddress,
                                        buy0.buyerResult,
                                        shopItem.Price,
                                        equipment,
                                        itemCount,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset));
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        buy0.buyerAvatarAddress,
                                        equipment));
                                }

                                if (shopItem.ItemUsable.ItemType == ItemType.Costume)
                                {
                                    _buyShopCostumesList.Add(ShopHistoryCostumeData.GetShopHistoryCostumeInfoV1(
                                        buy0.buyerAvatarAddress,
                                        shopItem.SellerAvatarAddress,
                                        buy0.buyerResult,
                                        shopItem.Price,
                                        shopItem.Costume,
                                        itemCount,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset));
                                }

                                if (shopItem.ItemUsable.ItemType == ItemType.Material)
                                {
                                    Material material = (Material)shopItem.TradableFungibleItem;
                                    _buyShopMaterialsList.Add(ShopHistoryMaterialData.GetShopHistoryMaterialInfoV1(
                                        buy0.buyerAvatarAddress,
                                        shopItem.SellerAvatarAddress,
                                        buy0.buyerResult,
                                        shopItem.Price,
                                        material,
                                        itemCount,
                                        ae.InputContext.BlockIndex,
                                        _blockTimeOffset));
                                }

                                if (shopItem.ItemUsable.ItemType == ItemType.Consumable)
                                {
                                    Consumable consumable = (Consumable)shopItem.ItemUsable;
                                    _buyShopConsumablesList.Add(
                                        ShopHistoryConsumableData.GetShopHistoryConsumableInfoV1(
                                            buy0.buyerAvatarAddress,
                                            shopItem.SellerAvatarAddress,
                                            buy0.buyerResult,
                                            shopItem.Price,
                                            consumable,
                                            itemCount,
                                            ae.InputContext.BlockIndex,
                                            _blockTimeOffset));
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    buy0.buyerAvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is Stake stake)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _stakeList.Add(StakeData.GetStakeInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, ae.InputContext.BlockIndex, _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing Stake action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is Stake0 stake0)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _stakeList.Add(StakeData.GetStakeInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, ae.InputContext.BlockIndex, _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing Stake action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is MigrateMonsterCollection migrateMonsterCollection)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _migrateMonsterCollectionList.Add(MigrateMonsterCollectionData.GetMigrateMonsterCollectionInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, ae.InputContext.BlockIndex, _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing MigrateMonsterCollection action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is Grinding grinding)
                            {
                                var start = DateTimeOffset.UtcNow;

                                var grindList = GrindingData.GetGrindingInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, grinding.AvatarAddress, grinding.EquipmentIds, grinding.Id, ae.InputContext.BlockIndex, _blockTimeOffset);

                                foreach (var grind in grindList)
                                {
                                    _grindList.Add(grind);
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing Grinding action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is UnlockEquipmentRecipe unlockEquipmentRecipe)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var unlockEquipmentRecipeList = UnlockEquipmentRecipeData.GetUnlockEquipmentRecipeInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, unlockEquipmentRecipe.AvatarAddress, unlockEquipmentRecipe.RecipeIds, unlockEquipmentRecipe.Id, ae.InputContext.BlockIndex, _blockTimeOffset);
                                foreach (var unlockEquipmentRecipeData in unlockEquipmentRecipeList)
                                {
                                    _unlockEquipmentRecipeList.Add(unlockEquipmentRecipeData);
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing UnlockEquipmentRecipe action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is UnlockEquipmentRecipe1 unlockEquipmentRecipe1)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var unlockEquipmentRecipeList = UnlockEquipmentRecipeData.GetUnlockEquipmentRecipeInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, unlockEquipmentRecipe1.AvatarAddress, unlockEquipmentRecipe1.RecipeIds, unlockEquipmentRecipe1.Id, ae.InputContext.BlockIndex, _blockTimeOffset);
                                foreach (var unlockEquipmentRecipeData in unlockEquipmentRecipeList)
                                {
                                    _unlockEquipmentRecipeList.Add(unlockEquipmentRecipeData);
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing UnlockEquipmentRecipe action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is UnlockWorld unlockWorld)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var unlockWorldList = UnlockWorldData.GetUnlockWorldInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, unlockWorld.AvatarAddress, unlockWorld.WorldIds, unlockWorld.Id, ae.InputContext.BlockIndex, _blockTimeOffset);
                                foreach (var unlockWorldData in unlockWorldList)
                                {
                                    _unlockWorldList.Add(unlockWorldData);
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing UnlockWorld action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is UnlockWorld1 unlockWorld1)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var unlockWorldList = UnlockWorldData.GetUnlockWorldInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, unlockWorld1.AvatarAddress, unlockWorld1.WorldIds, unlockWorld1.Id, ae.InputContext.BlockIndex, _blockTimeOffset);
                                foreach (var unlockWorldData in unlockWorldList)
                                {
                                    _unlockWorldList.Add(unlockWorldData);
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing UnlockWorld action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is HackAndSlashRandomBuff hasRandomBuff)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _hasRandomBuffList.Add(HackAndSlashRandomBuffData.GetHasRandomBuffInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, hasRandomBuff.AvatarAddress, hasRandomBuff.AdvancedGacha, hasRandomBuff.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing HasRandomBuff action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is JoinArena joinArena)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _joinArenaList.Add(JoinArenaData.GetJoinArenaInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, joinArena.avatarAddress, joinArena.round, joinArena.championshipId, joinArena.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing JoinArena action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is JoinArena2 joinArena2)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _joinArenaList.Add(JoinArenaData.GetJoinArenaInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, joinArena2.avatarAddress, joinArena2.round, joinArena2.championshipId, joinArena2.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing JoinArena action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is JoinArena1 joinArena1)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _joinArenaList.Add(JoinArenaData.GetJoinArenaInfo(ae.InputContext.PreviousState, ae.OutputState, ae.InputContext.Signer, joinArena1.avatarAddress, joinArena1.round, joinArena1.championshipId, joinArena1.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing JoinArena action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is BattleArena battleArena)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfo(ae.OutputState, ae.InputContext.Signer, battleArena.myAvatarAddress, battleArena.runeInfos, _blockTimeOffset));
                                _battleArenaList.Add(BattleArenaData.GetBattleArenaInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    battleArena.myAvatarAddress,
                                    battleArena.enemyAvatarAddress,
                                    battleArena.round,
                                    battleArena.championshipId,
                                    battleArena.ticket,
                                    battleArena.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing BattleArena action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is BattleArena8 battleArena8)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfo(ae.OutputState, ae.InputContext.Signer, battleArena8.myAvatarAddress, battleArena8.runeInfos, _blockTimeOffset));
                                _battleArenaList.Add(BattleArenaData.GetBattleArenaInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    battleArena8.myAvatarAddress,
                                    battleArena8.enemyAvatarAddress,
                                    battleArena8.round,
                                    battleArena8.championshipId,
                                    battleArena8.ticket,
                                    battleArena8.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing BattleArena action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is BattleArena7 battleArena7)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfo(ae.OutputState, ae.InputContext.Signer, battleArena7.myAvatarAddress, battleArena7.runeInfos, _blockTimeOffset));
                                _battleArenaList.Add(BattleArenaData.GetBattleArenaInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    battleArena7.myAvatarAddress,
                                    battleArena7.enemyAvatarAddress,
                                    battleArena7.round,
                                    battleArena7.championshipId,
                                    battleArena7.ticket,
                                    battleArena7.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing BattleArena action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is BattleArena6 battleArena6)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, battleArena6.myAvatarAddress, _blockTimeOffset));
                                _battleArenaList.Add(BattleArenaData.GetBattleArenaInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    battleArena6.myAvatarAddress,
                                    battleArena6.enemyAvatarAddress,
                                    battleArena6.round,
                                    battleArena6.championshipId,
                                    battleArena6.ticket,
                                    battleArena6.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing BattleArena action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is BattleArena5 battleArena5)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, battleArena5.myAvatarAddress, _blockTimeOffset));
                                _battleArenaList.Add(BattleArenaData.GetBattleArenaInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    battleArena5.myAvatarAddress,
                                    battleArena5.enemyAvatarAddress,
                                    battleArena5.round,
                                    battleArena5.championshipId,
                                    battleArena5.ticket,
                                    battleArena5.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing BattleArena action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is BattleArena4 battleArena4)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, battleArena4.myAvatarAddress, _blockTimeOffset));
                                _battleArenaList.Add(BattleArenaData.GetBattleArenaInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    battleArena4.myAvatarAddress,
                                    battleArena4.enemyAvatarAddress,
                                    battleArena4.round,
                                    battleArena4.championshipId,
                                    battleArena4.ticket,
                                    battleArena4.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing BattleArena action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is BattleArena3 battleArena3)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, battleArena3.myAvatarAddress, _blockTimeOffset));
                                _battleArenaList.Add(BattleArenaData.GetBattleArenaInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    battleArena3.myAvatarAddress,
                                    battleArena3.enemyAvatarAddress,
                                    battleArena3.round,
                                    battleArena3.championshipId,
                                    battleArena3.ticket,
                                    battleArena3.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing BattleArena action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is BattleArena2 battleArena2)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, battleArena2.myAvatarAddress, _blockTimeOffset));
                                _battleArenaList.Add(BattleArenaData.GetBattleArenaInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    battleArena2.myAvatarAddress,
                                    battleArena2.enemyAvatarAddress,
                                    battleArena2.round,
                                    battleArena2.championshipId,
                                    battleArena2.ticket,
                                    battleArena2.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing BattleArena action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is BattleArena1 battleArena1)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, battleArena1.myAvatarAddress, _blockTimeOffset));
                                _battleArenaList.Add(BattleArenaData.GetBattleArenaInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    battleArena1.myAvatarAddress,
                                    battleArena1.enemyAvatarAddress,
                                    battleArena1.round,
                                    battleArena1.championshipId,
                                    battleArena1.ticket,
                                    battleArena1.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing BattleArena action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is BattleGrandFinale battleGrandFinale)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _battleGrandFinaleList.Add(BattleGrandFinaleData.GetBattleGrandFinaleInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    battleGrandFinale.myAvatarAddress,
                                    battleGrandFinale.enemyAvatarAddress,
                                    battleGrandFinale.grandFinaleId,
                                    battleGrandFinale.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing BattleGrandFinale action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is BattleGrandFinale1 battleGrandFinale1)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _battleGrandFinaleList.Add(BattleGrandFinaleData.GetBattleGrandFinaleInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    battleGrandFinale1.myAvatarAddress,
                                    battleGrandFinale1.enemyAvatarAddress,
                                    battleGrandFinale1.grandFinaleId,
                                    battleGrandFinale1.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing BattleGrandFinale action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is EventMaterialItemCrafts eventMaterialItemCrafts)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _eventMaterialItemCraftsList.Add(EventMaterialItemCraftsData.GetEventMaterialItemCraftsInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    eventMaterialItemCrafts.AvatarAddress,
                                    eventMaterialItemCrafts.MaterialsToUse,
                                    eventMaterialItemCrafts.EventScheduleId,
                                    eventMaterialItemCrafts.EventMaterialItemRecipeId,
                                    eventMaterialItemCrafts.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing EventMaterialItemCrafts action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is RuneEnhancement runeEnhancement)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _runeEnhancementList.Add(RuneEnhancementData.GetRuneEnhancementInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    runeEnhancement.AvatarAddress,
                                    runeEnhancement.RuneId,
                                    runeEnhancement.TryCount,
                                    runeEnhancement.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing RuneEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is RuneEnhancement0 runeEnhancement0)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _runeEnhancementList.Add(RuneEnhancementData.GetRuneEnhancementInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    runeEnhancement0.AvatarAddress,
                                    runeEnhancement0.RuneId,
                                    runeEnhancement0.TryCount,
                                    runeEnhancement0.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing RuneEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is TransferAssets transferAssets)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var count = 0;
                                foreach (var recipient in transferAssets.Recipients)
                                {
                                    var actionString = count + ae.InputContext.TxId.ToString();
                                    var actionByteArray = Encoding.UTF8.GetBytes(actionString).Take(16).ToArray();
                                    var id = new Guid(actionByteArray);
                                    var avatarAddress = recipient.recipient;
                                    var actionType = transferAssets.ToString()!.Split('.').LastOrDefault()
                                        ?.Replace(">", string.Empty);
                                    _transferAssetList.Add(TransferAssetData.GetTransferAssetInfo(
                                        id,
                                        (TxId)ae.InputContext.TxId!,
                                        ae.InputContext.BlockIndex,
                                        _blockHash!.ToString(),
                                        transferAssets.Sender,
                                        recipient.recipient,
                                        recipient.amount.Currency.Ticker,
                                        recipient.amount,
                                        _blockTimeOffset));
                                    _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                        id,
                                        ae.InputContext.Signer,
                                        avatarAddress,
                                        ae.InputContext.BlockIndex,
                                        actionType!,
                                        recipient.amount.Currency.Ticker,
                                        recipient.amount,
                                        _blockTimeOffset));
                                    count++;
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing TransferAssets action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is DailyReward dailyReward)
                            {
                                var start = DateTimeOffset.UtcNow;
#pragma warning disable CS0618
                                var runeCurrency = Currency.Legacy(RuneHelper.DailyRewardRune.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                var prevRuneBalance = ae.InputContext.PreviousState.GetBalance(
                                    dailyReward.avatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = ae.OutputState.GetBalance(
                                    dailyReward.avatarAddress,
                                    runeCurrency);
                                var acquiredRune = outputRuneBalance - prevRuneBalance;
                                var actionType = dailyReward.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                    dailyReward.Id,
                                    ae.InputContext.Signer,
                                    dailyReward.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    actionType!,
                                    runeCurrency.Ticker,
                                    acquiredRune,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing DailyReward action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is DailyReward6 dailyReward6)
                            {
                                var start = DateTimeOffset.UtcNow;
#pragma warning disable CS0618
                                var runeCurrency = Currency.Legacy(RuneHelper.DailyRewardRune.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                var prevRuneBalance = ae.InputContext.PreviousState.GetBalance(
                                    dailyReward6.avatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = ae.OutputState.GetBalance(
                                    dailyReward6.avatarAddress,
                                    runeCurrency);
                                var acquiredRune = outputRuneBalance - prevRuneBalance;
                                var actionType = dailyReward6.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                    dailyReward6.Id,
                                    ae.InputContext.Signer,
                                    dailyReward6.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    actionType!,
                                    runeCurrency.Ticker,
                                    acquiredRune,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing DailyReward action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is DailyReward5 dailyReward5)
                            {
                                var start = DateTimeOffset.UtcNow;
#pragma warning disable CS0618
                                var runeCurrency = Currency.Legacy(RuneHelper.DailyRewardRune.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                var prevRuneBalance = ae.InputContext.PreviousState.GetBalance(
                                    dailyReward5.avatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = ae.OutputState.GetBalance(
                                    dailyReward5.avatarAddress,
                                    runeCurrency);
                                var acquiredRune = outputRuneBalance - prevRuneBalance;
                                var actionType = dailyReward5.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                    dailyReward5.Id,
                                    ae.InputContext.Signer,
                                    dailyReward5.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    actionType!,
                                    runeCurrency.Ticker,
                                    acquiredRune,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing DailyReward action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is DailyReward4 dailyReward4)
                            {
                                var start = DateTimeOffset.UtcNow;
#pragma warning disable CS0618
                                var runeCurrency = Currency.Legacy(RuneHelper.DailyRewardRune.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                var prevRuneBalance = ae.InputContext.PreviousState.GetBalance(
                                    dailyReward4.avatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = ae.OutputState.GetBalance(
                                    dailyReward4.avatarAddress,
                                    runeCurrency);
                                var acquiredRune = outputRuneBalance - prevRuneBalance;
                                var actionType = dailyReward4.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                    dailyReward4.Id,
                                    ae.InputContext.Signer,
                                    dailyReward4.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    actionType!,
                                    runeCurrency.Ticker,
                                    acquiredRune,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing DailyReward action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is DailyReward3 dailyReward3)
                            {
                                var start = DateTimeOffset.UtcNow;
#pragma warning disable CS0618
                                var runeCurrency = Currency.Legacy(RuneHelper.DailyRewardRune.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                var prevRuneBalance = ae.InputContext.PreviousState.GetBalance(
                                    dailyReward3.avatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = ae.OutputState.GetBalance(
                                    dailyReward3.avatarAddress,
                                    runeCurrency);
                                var acquiredRune = outputRuneBalance - prevRuneBalance;
                                var actionType = dailyReward3.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                    dailyReward3.Id,
                                    ae.InputContext.Signer,
                                    dailyReward3.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    actionType!,
                                    runeCurrency.Ticker,
                                    acquiredRune,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing DailyReward action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is DailyReward2 dailyReward2)
                            {
                                var start = DateTimeOffset.UtcNow;
#pragma warning disable CS0618
                                var runeCurrency = Currency.Legacy(RuneHelper.DailyRewardRune.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                var prevRuneBalance = ae.InputContext.PreviousState.GetBalance(
                                    dailyReward2.avatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = ae.OutputState.GetBalance(
                                    dailyReward2.avatarAddress,
                                    runeCurrency);
                                var acquiredRune = outputRuneBalance - prevRuneBalance;
                                var actionType = dailyReward2.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                    dailyReward2.Id,
                                    ae.InputContext.Signer,
                                    dailyReward2.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    actionType!,
                                    runeCurrency.Ticker,
                                    acquiredRune,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing DailyReward action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is DailyReward0 dailyReward0)
                            {
                                var start = DateTimeOffset.UtcNow;
#pragma warning disable CS0618
                                var runeCurrency = Currency.Legacy(RuneHelper.DailyRewardRune.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                var prevRuneBalance = ae.InputContext.PreviousState.GetBalance(
                                    dailyReward0.avatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = ae.OutputState.GetBalance(
                                    dailyReward0.avatarAddress,
                                    runeCurrency);
                                var acquiredRune = outputRuneBalance - prevRuneBalance;
                                var actionType = dailyReward0.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                    dailyReward0.Id,
                                    ae.InputContext.Signer,
                                    dailyReward0.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    actionType!,
                                    runeCurrency.Ticker,
                                    acquiredRune,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing DailyReward action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is ClaimRaidReward claimRaidReward)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var sheets = ae.OutputState.GetSheets(
                                    sheetTypes: new[]
                                    {
                                        typeof(RuneSheet),
                                    });
                                var runeSheet = sheets.GetSheet<RuneSheet>();
                                foreach (var runeType in runeSheet.Values)
                                {
#pragma warning disable CS0618
                                    var runeCurrency = Currency.Legacy(runeType.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                    var prevRuneBalance = ae.InputContext.PreviousState.GetBalance(
                                        claimRaidReward.AvatarAddress,
                                        runeCurrency);
                                    var outputRuneBalance = ae.OutputState.GetBalance(
                                        claimRaidReward.AvatarAddress,
                                        runeCurrency);
                                    var acquiredRune = outputRuneBalance - prevRuneBalance;
                                    var actionType = claimRaidReward.ToString()!.Split('.').LastOrDefault()
                                        ?.Replace(">", string.Empty);
                                    if (Convert.ToDecimal(acquiredRune.GetQuantityString()) > 0)
                                    {
                                        _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                            claimRaidReward.Id,
                                            ae.InputContext.Signer,
                                            claimRaidReward.AvatarAddress,
                                            ae.InputContext.BlockIndex,
                                            actionType!,
                                            runeCurrency.Ticker,
                                            acquiredRune,
                                            _blockTimeOffset));
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ClaimRaidReward action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is UnlockRuneSlot unlockRuneSlot)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _unlockRuneSlotList.Add(UnlockRuneSlotData.GetUnlockRuneSlotInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    unlockRuneSlot.AvatarAddress,
                                    unlockRuneSlot.SlotIndex,
                                    unlockRuneSlot.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing UnlockRuneSlot action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is RapidCombination rapidCombination)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _rapidCombinationList.Add(RapidCombinationData.GetRapidCombinationInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    rapidCombination.avatarAddress,
                                    rapidCombination.slotIndex,
                                    rapidCombination.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing RapidCombination action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is RapidCombination8 rapidCombination8)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _rapidCombinationList.Add(RapidCombinationData.GetRapidCombinationInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    rapidCombination8.avatarAddress,
                                    rapidCombination8.slotIndex,
                                    rapidCombination8.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing RapidCombination action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is RapidCombination7 rapidCombination7)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _rapidCombinationList.Add(RapidCombinationData.GetRapidCombinationInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    rapidCombination7.avatarAddress,
                                    rapidCombination7.slotIndex,
                                    rapidCombination7.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing RapidCombination action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is RapidCombination6 rapidCombination6)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _rapidCombinationList.Add(RapidCombinationData.GetRapidCombinationInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    rapidCombination6.avatarAddress,
                                    rapidCombination6.slotIndex,
                                    rapidCombination6.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing RapidCombination action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is RapidCombination5 rapidCombination5)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _rapidCombinationList.Add(RapidCombinationData.GetRapidCombinationInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    rapidCombination5.avatarAddress,
                                    rapidCombination5.slotIndex,
                                    rapidCombination5.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing RapidCombination action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is RapidCombination4 rapidCombination4)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _rapidCombinationList.Add(RapidCombinationData.GetRapidCombinationInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    rapidCombination4.avatarAddress,
                                    rapidCombination4.slotIndex,
                                    rapidCombination4.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing RapidCombination action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is RapidCombination3 rapidCombination3)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _rapidCombinationList.Add(RapidCombinationData.GetRapidCombinationInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    rapidCombination3.avatarAddress,
                                    rapidCombination3.slotIndex,
                                    rapidCombination3.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing RapidCombination action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is RapidCombination2 rapidCombination2)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _rapidCombinationList.Add(RapidCombinationData.GetRapidCombinationInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    rapidCombination2.avatarAddress,
                                    rapidCombination2.slotIndex,
                                    rapidCombination2.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing RapidCombination action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is RapidCombination0 rapidCombination0)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _rapidCombinationList.Add(RapidCombinationData.GetRapidCombinationInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    rapidCombination0.avatarAddress,
                                    rapidCombination0.slotIndex,
                                    rapidCombination0.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing RapidCombination action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is Raid raid)
                            {
                                var sheets = ae.OutputState.GetSheets(
                                    sheetTypes: new[]
                                    {
                                        typeof(CharacterSheet),
                                        typeof(CostumeStatSheet),
                                        typeof(RuneSheet),
                                        typeof(RuneListSheet),
                                        typeof(RuneOptionSheet),
                                    });

                                var runeSheet = sheets.GetSheet<RuneSheet>();
                                foreach (var runeType in runeSheet.Values)
                                {
#pragma warning disable CS0618
                                    var runeCurrency = Currency.Legacy(runeType.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                    var prevRuneBalance = ae.InputContext.PreviousState.GetBalance(
                                        raid.AvatarAddress,
                                        runeCurrency);
                                    var outputRuneBalance = ae.OutputState.GetBalance(
                                        raid.AvatarAddress,
                                        runeCurrency);
                                    var acquiredRune = outputRuneBalance - prevRuneBalance;
                                    var actionType = raid.ToString()!.Split('.').LastOrDefault()
                                        ?.Replace(">", string.Empty);
                                    if (Convert.ToDecimal(acquiredRune.GetQuantityString()) > 0)
                                    {
                                        _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                            raid.Id,
                                            ae.InputContext.Signer,
                                            raid.AvatarAddress,
                                            ae.InputContext.BlockIndex,
                                            actionType!,
                                            runeCurrency.Ticker,
                                            acquiredRune,
                                            _blockTimeOffset));
                                    }
                                }

                                _avatarList.Add(AvatarData.GetAvatarInfo(ae.OutputState, ae.InputContext.Signer, raid.AvatarAddress, raid.RuneInfos, _blockTimeOffset));

                                int raidId = 0;
                                bool found = false;
                                for (int i = 0; i < 99; i++)
                                {
                                    if (ae.OutputState.Delta.UpdatedAddresses.Contains(
                                            Addresses.GetRaiderAddress(raid.AvatarAddress, i)))
                                    {
                                        raidId = i;
                                        found = true;
                                        break;
                                    }
                                }

                                if (found)
                                {
                                    RaiderState raiderState =
                                        ae.OutputState.GetRaiderState(raid.AvatarAddress, raidId);
                                    _raiderList.Add(RaidData.GetRaidInfo(raidId, raiderState));
                                }
                                else
                                {
                                    Log.Error("can't find raidId.");
                                }
                            }

                            if (action is Raid4 raid4)
                            {
                                var sheets = ae.OutputState.GetSheets(
                                    sheetTypes: new[]
                                    {
                                        typeof(CharacterSheet),
                                        typeof(CostumeStatSheet),
                                        typeof(RuneSheet),
                                        typeof(RuneListSheet),
                                        typeof(RuneOptionSheet),
                                    });

                                var runeSheet = sheets.GetSheet<RuneSheet>();
                                foreach (var runeType in runeSheet.Values)
                                {
#pragma warning disable CS0618
                                    var runeCurrency = Currency.Legacy(runeType.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                    var prevRuneBalance = ae.InputContext.PreviousState.GetBalance(
                                        raid4.AvatarAddress,
                                        runeCurrency);
                                    var outputRuneBalance = ae.OutputState.GetBalance(
                                        raid4.AvatarAddress,
                                        runeCurrency);
                                    var acquiredRune = outputRuneBalance - prevRuneBalance;
                                    var actionType = raid4.ToString()!.Split('.').LastOrDefault()
                                        ?.Replace(">", string.Empty);
                                    if (Convert.ToDecimal(acquiredRune.GetQuantityString()) > 0)
                                    {
                                        _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                            raid4.Id,
                                            ae.InputContext.Signer,
                                            raid4.AvatarAddress,
                                            ae.InputContext.BlockIndex,
                                            actionType!,
                                            runeCurrency.Ticker,
                                            acquiredRune,
                                            _blockTimeOffset));
                                    }
                                }

                                _avatarList.Add(AvatarData.GetAvatarInfo(ae.OutputState, ae.InputContext.Signer, raid4.AvatarAddress, raid4.RuneInfos, _blockTimeOffset));

                                int raidId = 0;
                                bool found = false;
                                for (int i = 0; i < 99; i++)
                                {
                                    if (ae.OutputState.Delta.UpdatedAddresses.Contains(
                                            Addresses.GetRaiderAddress(raid4.AvatarAddress, i)))
                                    {
                                        raidId = i;
                                        found = true;
                                        break;
                                    }
                                }

                                if (found)
                                {
                                    RaiderState raiderState =
                                        ae.OutputState.GetRaiderState(raid4.AvatarAddress, raidId);
                                    _raiderList.Add(RaidData.GetRaidInfo(raidId, raiderState));
                                }
                                else
                                {
                                    Log.Error("can't find raidId.");
                                }
                            }

                            if (action is Raid3 raid3)
                            {
                                var sheets = ae.OutputState.GetSheets(
                                    sheetTypes: new[]
                                    {
                                        typeof(CharacterSheet),
                                        typeof(CostumeStatSheet),
                                        typeof(RuneSheet),
                                        typeof(RuneListSheet),
                                        typeof(RuneOptionSheet),
                                    });

                                var runeSheet = sheets.GetSheet<RuneSheet>();
                                foreach (var runeType in runeSheet.Values)
                                {
#pragma warning disable CS0618
                                    var runeCurrency = Currency.Legacy(runeType.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                    var prevRuneBalance = ae.InputContext.PreviousState.GetBalance(
                                        raid3.AvatarAddress,
                                        runeCurrency);
                                    var outputRuneBalance = ae.OutputState.GetBalance(
                                        raid3.AvatarAddress,
                                        runeCurrency);
                                    var acquiredRune = outputRuneBalance - prevRuneBalance;
                                    var actionType = raid3.ToString()!.Split('.').LastOrDefault()
                                        ?.Replace(">", string.Empty);
                                    if (Convert.ToDecimal(acquiredRune.GetQuantityString()) > 0)
                                    {
                                        _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                            raid3.Id,
                                            ae.InputContext.Signer,
                                            raid3.AvatarAddress,
                                            ae.InputContext.BlockIndex,
                                            actionType!,
                                            runeCurrency.Ticker,
                                            acquiredRune,
                                            _blockTimeOffset));
                                    }
                                }

                                _avatarList.Add(AvatarData.GetAvatarInfo(ae.OutputState, ae.InputContext.Signer, raid3.AvatarAddress, raid3.RuneInfos, _blockTimeOffset));

                                int raidId = 0;
                                bool found = false;
                                for (int i = 0; i < 99; i++)
                                {
                                    if (ae.OutputState.Delta.UpdatedAddresses.Contains(
                                            Addresses.GetRaiderAddress(raid3.AvatarAddress, i)))
                                    {
                                        raidId = i;
                                        found = true;
                                        break;
                                    }
                                }

                                if (found)
                                {
                                    RaiderState raiderState =
                                        ae.OutputState.GetRaiderState(raid3.AvatarAddress, raidId);
                                    _raiderList.Add(RaidData.GetRaidInfo(raidId, raiderState));
                                }
                                else
                                {
                                    Log.Error("can't find raidId.");
                                }
                            }

                            if (action is Raid2 raid2)
                            {
                                var sheets = ae.OutputState.GetSheets(
                                    sheetTypes: new[]
                                    {
                                        typeof(CharacterSheet),
                                        typeof(CostumeStatSheet),
                                        typeof(RuneSheet),
                                        typeof(RuneListSheet),
                                        typeof(RuneOptionSheet),
                                    });

                                var runeSheet = sheets.GetSheet<RuneSheet>();
                                foreach (var runeType in runeSheet.Values)
                                {
#pragma warning disable CS0618
                                    var runeCurrency = Currency.Legacy(runeType.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                    var prevRuneBalance = ae.InputContext.PreviousState.GetBalance(
                                        raid2.AvatarAddress,
                                        runeCurrency);
                                    var outputRuneBalance = ae.OutputState.GetBalance(
                                        raid2.AvatarAddress,
                                        runeCurrency);
                                    var acquiredRune = outputRuneBalance - prevRuneBalance;
                                    var actionType = raid2.ToString()!.Split('.').LastOrDefault()
                                        ?.Replace(">", string.Empty);
                                    if (Convert.ToDecimal(acquiredRune.GetQuantityString()) > 0)
                                    {
                                        _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                            raid2.Id,
                                            ae.InputContext.Signer,
                                            raid2.AvatarAddress,
                                            ae.InputContext.BlockIndex,
                                            actionType!,
                                            runeCurrency.Ticker,
                                            acquiredRune,
                                            _blockTimeOffset));
                                    }
                                }

                                _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, raid2.AvatarAddress, _blockTimeOffset));

                                int raidId = 0;
                                bool found = false;
                                for (int i = 0; i < 99; i++)
                                {
                                    if (ae.OutputState.Delta.UpdatedAddresses.Contains(
                                            Addresses.GetRaiderAddress(raid2.AvatarAddress, i)))
                                    {
                                        raidId = i;
                                        found = true;
                                        break;
                                    }
                                }

                                if (found)
                                {
                                    RaiderState raiderState =
                                        ae.OutputState.GetRaiderState(raid2.AvatarAddress, raidId);
                                    _raiderList.Add(RaidData.GetRaidInfo(raidId, raiderState));
                                }
                                else
                                {
                                    Log.Error("can't find raidId.");
                                }
                            }

                            if (action is Raid1 raid1)
                            {
                                var sheets = ae.OutputState.GetSheets(
                                    sheetTypes: new[]
                                    {
                                        typeof(CharacterSheet),
                                        typeof(CostumeStatSheet),
                                        typeof(RuneSheet),
                                        typeof(RuneListSheet),
                                        typeof(RuneOptionSheet),
                                    });

                                var runeSheet = sheets.GetSheet<RuneSheet>();
                                foreach (var runeType in runeSheet.Values)
                                {
#pragma warning disable CS0618
                                    var runeCurrency = Currency.Legacy(runeType.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                    var prevRuneBalance = ae.InputContext.PreviousState.GetBalance(
                                        raid1.AvatarAddress,
                                        runeCurrency);
                                    var outputRuneBalance = ae.OutputState.GetBalance(
                                        raid1.AvatarAddress,
                                        runeCurrency);
                                    var acquiredRune = outputRuneBalance - prevRuneBalance;
                                    var actionType = raid1.ToString()!.Split('.').LastOrDefault()
                                        ?.Replace(">", string.Empty);
                                    if (Convert.ToDecimal(acquiredRune.GetQuantityString()) > 0)
                                    {
                                        _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                            raid1.Id,
                                            ae.InputContext.Signer,
                                            raid1.AvatarAddress,
                                            ae.InputContext.BlockIndex,
                                            actionType!,
                                            runeCurrency.Ticker,
                                            acquiredRune,
                                            _blockTimeOffset));
                                    }
                                }

                                _avatarList.Add(AvatarData.GetAvatarInfoV1(ae.OutputState, ae.InputContext.Signer, raid1.AvatarAddress, _blockTimeOffset));

                                int raidId = 0;
                                bool found = false;
                                for (int i = 0; i < 99; i++)
                                {
                                    if (ae.OutputState.Delta.UpdatedAddresses.Contains(
                                            Addresses.GetRaiderAddress(raid1.AvatarAddress, i)))
                                    {
                                        raidId = i;
                                        found = true;
                                        break;
                                    }
                                }

                                if (found)
                                {
                                    RaiderState raiderState =
                                        ae.OutputState.GetRaiderState(raid1.AvatarAddress, raidId);
                                    _raiderList.Add(RaidData.GetRaidInfo(raidId, raiderState));
                                }
                                else
                                {
                                    Log.Error("can't find raidId.");
                                }
                            }

                            if (action is PetEnhancement petEnhancement)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _petEnhancementList.Add(PetEnhancementData.GetPetEnhancementInfo(
                                    ae.InputContext.PreviousState,
                                    ae.OutputState,
                                    ae.InputContext.Signer,
                                    petEnhancement.AvatarAddress,
                                    petEnhancement.PetId,
                                    petEnhancement.TargetLevel,
                                    petEnhancement.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset
                                ));
                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored PetEnhancement action in block #{BlockIndex}. Time taken: {Time} ms", ae.InputContext.BlockIndex, end - start);
                            }

                            if (action is TransferAsset transferAsset)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var actionString = ae.InputContext.TxId.ToString();
                                var actionByteArray = Encoding.UTF8.GetBytes(actionString!).Take(16).ToArray();
                                var id = new Guid(actionByteArray);
                                _transferAssetList.Add(TransferAssetData.GetTransferAssetInfo(
                                    id,
                                    (TxId)ae.InputContext.TxId!,
                                    ae.InputContext.BlockIndex,
                                    _blockHash!.ToString(),
                                    transferAsset.Sender,
                                    transferAsset.Recipient,
                                    transferAsset.Amount.Currency.Ticker,
                                    transferAsset.Amount,
                                    _blockTimeOffset));

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored TransferAsset action in block #{index}. Time Taken: {time} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is TransferAsset2 transferAsset2)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var actionString = ae.InputContext.TxId.ToString();
                                var actionByteArray = Encoding.UTF8.GetBytes(actionString!).Take(16).ToArray();
                                var id = new Guid(actionByteArray);
                                _transferAssetList.Add(TransferAssetData.GetTransferAssetInfo(
                                    id,
                                    (TxId)ae.InputContext.TxId!,
                                    ae.InputContext.BlockIndex,
                                    _blockHash!.ToString(),
                                    transferAsset2.Sender,
                                    transferAsset2.Recipient,
                                    transferAsset2.Amount.Currency.Ticker,
                                    transferAsset2.Amount,
                                    _blockTimeOffset));

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored TransferAsset action in block #{index}. Time Taken: {time} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is TransferAsset0 transferAsset0)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var actionString = ae.InputContext.TxId.ToString();
                                var actionByteArray = Encoding.UTF8.GetBytes(actionString!).Take(16).ToArray();
                                var id = new Guid(actionByteArray);
                                _transferAssetList.Add(TransferAssetData.GetTransferAssetInfo(
                                    id,
                                    (TxId)ae.InputContext.TxId!,
                                    ae.InputContext.BlockIndex,
                                    _blockHash!.ToString(),
                                    transferAsset0.Sender,
                                    transferAsset0.Recipient,
                                    transferAsset0.Amount.Currency.Ticker,
                                    transferAsset0.Amount,
                                    _blockTimeOffset));

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored TransferAsset action in block #{index}. Time Taken: {time} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is RequestPledge requestPledge)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _requestPledgeList.Add(RequestPledgeData.GetRequestPledgeInfo(
                                    ae.InputContext.TxId!.ToString()!,
                                    ae.InputContext.BlockIndex,
                                    _blockHash!.ToString(),
                                    ae.InputContext.Signer,
                                    requestPledge.AgentAddress,
                                    requestPledge.RefillMead,
                                    _blockTimeOffset));

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug(
                                    "Stored RequestPledge action in block #{index}. Time Taken: {time} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }
                        }
                    }
                }
            }
        }

        private List<IActionEvaluation> EvaluateBlock(Block block)
        {
            var evList = _baseChain.EvaluateBlock(block).ToList();
            return evList;
        }
    }
}
