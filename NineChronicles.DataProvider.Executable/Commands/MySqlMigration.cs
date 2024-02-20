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
    using Libplanet.Action.State;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
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
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.DataRendering;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.DataProvider.Store.Models;
    using Serilog;
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
            var blockPolicySource = new BlockPolicySource();
            IBlockPolicy blockPolicy = blockPolicySource.GetPolicy();

            // Setup base chain & new chain
            Block genesis = _baseStore.GetBlock(gHash);
            var blockChainStates = new BlockChainStates(_baseStore, baseStateStore);
            var actionEvaluator = new ActionEvaluator(
                _ => blockPolicy.BlockAction,
                baseStateStore,
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
                    Task<List<ICommittedActionEvaluation>>[] taskArray;
                    if (interval < remainingCount)
                    {
                        taskArray = new Task<List<ICommittedActionEvaluation>>[interval];
                        limitInterval = interval;
                    }
                    else
                    {
                        taskArray = new Task<List<ICommittedActionEvaluation>>[remainingCount];
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
                            List<ICommittedActionEvaluation> actionEvaluations = EvaluateBlock(block);
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
                    ProcessTasks(taskArray, blockChainStates);
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

        private void ProcessTasks(Task<List<ICommittedActionEvaluation>>[] taskArray, IBlockChainStates blockChainStates)
        {
            foreach (var task in taskArray)
            {
                if (task.Result is { } data)
                {
                    var actionLoader = new NCActionLoader();

                    foreach (var ae in data)
                    {
                        var inputState = new World(blockChainStates.GetWorldState(ae.InputContext.PreviousState));
                        var outputState = new World(blockChainStates.GetWorldState(ae.OutputState));

                        if (actionLoader.LoadAction(_blockIndex, ae.Action) is ActionBase action)
                        {
                            // avatarNames will be stored as "N/A" for optimization
                            if (action is HackAndSlash hasAction)
                            {
                                var avatarAddress = hasAction.AvatarAddress;

                                // check if address is already in _avatarCheck
                                if (!_avatarCheck.Contains(avatarAddress.ToString()))
                                {
                                    _avatarList.Add(AvatarData.GetAvatarInfo(outputState, ae.InputContext.Signer, hasAction.AvatarAddress, hasAction.RuneInfos, _blockTimeOffset));
                                    _avatarCheck.Add(avatarAddress.ToString());
                                }

                                Console.WriteLine("Writing {0} action {1} in block #{2}", nameof(HackAndSlash), ae.InputContext.TxId, ae.InputContext.BlockIndex);
                                _hackAndSlashList.Add(HackAndSlashData.GetHackAndSlashInfo(inputState, outputState, ae.InputContext.Signer, hasAction.AvatarAddress, hasAction.StageId, hasAction.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                if (hasAction.StageBuffId.HasValue)
                                {
                                    _hasWithRandomBuffList.Add(HasWithRandomBuffData.GetHasWithRandomBuffInfo(inputState, outputState, ae.InputContext.Signer, hasAction.AvatarAddress, hasAction.StageId, hasAction.StageBuffId, hasAction.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                }
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
                                var prevRuneBalance = inputState.GetBalance(
                                    avatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = outputState.GetBalance(
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
                                _claimStakeRewardList.Add(ClaimStakeRewardData.GetClaimStakeRewardInfo(claimStakeReward, inputState, outputState, ae.InputContext.Signer, ae.InputContext.BlockIndex, _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ClaimStakeReward action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is EventDungeonBattle eventDungeonBattle)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var actionType = eventDungeonBattle.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                _eventDungeonBattleList.Add(EventDungeonBattleData.GetEventDungeonBattleInfo(
                                    inputState,
                                    outputState,
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

                            if (action is EventConsumableItemCrafts eventConsumableItemCrafts)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _eventConsumableItemCraftsList.Add(EventConsumableItemCraftsData.GetEventConsumableItemCraftsInfo(eventConsumableItemCrafts, inputState, outputState, ae.InputContext.Signer, ae.InputContext.BlockIndex, _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing EventConsumableItemCrafts action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is HackAndSlashSweep hasSweep)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfo(outputState, ae.InputContext.Signer, hasSweep.avatarAddress, hasSweep.runeInfos, _blockTimeOffset));
                                _hackAndSlashSweepList.Add(HackAndSlashSweepData.GetHackAndSlashSweepInfo(
                                    inputState,
                                    outputState,
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

                            if (action is CombinationConsumable combinationConsumable)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationConsumableList.Add(CombinationConsumableData.GetCombinationConsumableInfo(
                                    inputState,
                                    outputState,
                                    ae.InputContext.Signer,
                                    combinationConsumable.avatarAddress,
                                    combinationConsumable.recipeId,
                                    combinationConsumable.slotIndex,
                                    combinationConsumable.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing CombinationConsumable action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is CombinationConsumable5 combinationConsumable5)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationConsumableList.Add(CombinationConsumableData.GetCombinationConsumableInfo(
                                    inputState,
                                    outputState,
                                    ae.InputContext.Signer,
                                    combinationConsumable5.AvatarAddress,
                                    combinationConsumable5.recipeId,
                                    combinationConsumable5.slotIndex,
                                    combinationConsumable5.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing CombinationConsumable action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment combinationEquipment)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    inputState,
                                    outputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment.avatarAddress,
                                    combinationEquipment.recipeId,
                                    combinationEquipment.slotIndex,
                                    combinationEquipment.subRecipeId,
                                    combinationEquipment.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                if (combinationEquipment.payByCrystal)
                                {
                                    var replaceCombinationEquipmentMaterialList = ReplaceCombinationEquipmentMaterialData
                                        .GetReplaceCombinationEquipmentMaterialInfo(
                                            inputState,
                                            outputState,
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

                                var slotState = outputState.GetCombinationSlotState(
                                    combinationEquipment.avatarAddress,
                                    combinationEquipment.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable,
                                        _blockTimeOffset));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is CombinationEquipment5 combinationEquipment5)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _combinationEquipmentList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                    inputState,
                                    outputState,
                                    ae.InputContext.Signer,
                                    combinationEquipment5.AvatarAddress,
                                    combinationEquipment5.RecipeId,
                                    combinationEquipment5.SlotIndex,
                                    combinationEquipment5.SubRecipeId,
                                    combinationEquipment5.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));

                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing CombinationEquipment action in block #{0}. Time Taken: {1} ms.",
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = outputState.GetCombinationSlotState(
                                    combinationEquipment5.AvatarAddress,
                                    combinationEquipment5.SlotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        combinationEquipment5.AvatarAddress,
                                        (Equipment)slotState.Result.itemUsable,
                                        _blockTimeOffset));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    combinationEquipment5.AvatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is ItemEnhancement itemEnhancement)
                            {
                                var start = DateTimeOffset.UtcNow;
                                if (ItemEnhancementFailData.GetItemEnhancementFailInfo(
                                        inputState,
                                        outputState,
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
                                    inputState,
                                    outputState,
                                    ae.InputContext.Signer,
                                    itemEnhancement.avatarAddress,
                                    itemEnhancement.slotIndex,
                                    Guid.Empty,
                                    itemEnhancement.materialIds,
                                    itemEnhancement.itemId,
                                    itemEnhancement.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ItemEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = outputState.GetCombinationSlotState(
                                    itemEnhancement.avatarAddress,
                                    itemEnhancement.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        itemEnhancement.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable,
                                        _blockTimeOffset));
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
                                        inputState,
                                        outputState,
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
                                    inputState,
                                    outputState,
                                    ae.InputContext.Signer,
                                    itemEnhancement10.avatarAddress,
                                    itemEnhancement10.slotIndex,
                                    itemEnhancement10.materialId,
                                    new List<Guid>(),
                                    itemEnhancement10.itemId,
                                    itemEnhancement10.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ItemEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = outputState.GetCombinationSlotState(
                                    itemEnhancement10.avatarAddress,
                                    itemEnhancement10.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        itemEnhancement10.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable,
                                        _blockTimeOffset));
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
                                        inputState,
                                        outputState,
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
                                    inputState,
                                    outputState,
                                    ae.InputContext.Signer,
                                    itemEnhancement9.avatarAddress,
                                    itemEnhancement9.slotIndex,
                                    itemEnhancement9.materialId,
                                    new List<Guid>(),
                                    itemEnhancement9.itemId,
                                    itemEnhancement9.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ItemEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = outputState.GetCombinationSlotState(
                                    itemEnhancement9.avatarAddress,
                                    itemEnhancement9.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        itemEnhancement9.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable,
                                        _blockTimeOffset));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    itemEnhancement9.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is ItemEnhancement7 itemEnhancement7)
                            {
                                var start = DateTimeOffset.UtcNow;
                                if (ItemEnhancementFailData.GetItemEnhancementFailInfo(
                                        inputState,
                                        outputState,
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
                                    inputState,
                                    outputState,
                                    ae.InputContext.Signer,
                                    itemEnhancement7.avatarAddress,
                                    itemEnhancement7.slotIndex,
                                    itemEnhancement7.materialId,
                                    new List<Guid>(),
                                    itemEnhancement7.itemId,
                                    itemEnhancement7.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing ItemEnhancement action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

                                var slotState = outputState.GetCombinationSlotState(
                                    itemEnhancement7.avatarAddress,
                                    itemEnhancement7.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    _equipmentList.Add(EquipmentData.GetEquipmentInfo(
                                        ae.InputContext.Signer,
                                        itemEnhancement7.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable,
                                        _blockTimeOffset));
                                }

                                end = DateTimeOffset.UtcNow;
                                Console.WriteLine(
                                    "Writing avatar {0}'s equipment in block #{1}. Time Taken: {2} ms.",
                                    itemEnhancement7.avatarAddress,
                                    ae.InputContext.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (action is Buy buy)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = outputState.GetAvatarState(buy.buyerAvatarAddress);
                                var buyerInventory = avatarState.inventory;
                                foreach (var purchaseInfo in buy.purchaseInfos)
                                {
                                    var state = outputState.GetLegacyState(
                                    Addresses.GetItemAddress(purchaseInfo.TradableId));
                                    ITradableItem orderItem =
                                        (ITradableItem)ItemFactory.Deserialize((Dictionary)state!);
                                    Order order =
                                        OrderFactory.Deserialize(
                                            (Dictionary)outputState.GetLegacyState(
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
                                        var sellerState = outputState.GetAvatarState(purchaseInfo.SellerAvatarAddress);
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
                                                equipmentNotNull,
                                                _blockTimeOffset));
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

                            if (action is Buy7 buy7)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = outputState.GetAvatarState(buy7.buyerAvatarAddress);
                                var buyerInventory = avatarState.inventory;
                                foreach (var purchaseInfo in buy7.purchaseInfos)
                                {
                                    var state = outputState.GetLegacyState(
                                    Addresses.GetItemAddress(purchaseInfo.productId));
                                    ITradableItem orderItem =
                                        (ITradableItem)ItemFactory.Deserialize((Dictionary)state!);
                                    Order order =
                                        OrderFactory.Deserialize(
                                            (Dictionary)outputState.GetLegacyState(
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
                                        var sellerState = outputState.GetAvatarState(purchaseInfo.sellerAvatarAddress);
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
                                                equipmentNotNull,
                                                _blockTimeOffset));
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

                            if (action is Stake stake)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _stakeList.Add(StakeData.GetStakeInfo(inputState, outputState, ae.InputContext.Signer, ae.InputContext.BlockIndex, _blockTimeOffset, stake.Id));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing Stake action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is Stake0 stake0)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _stakeList.Add(StakeData.GetStakeInfo(inputState, outputState, ae.InputContext.Signer, ae.InputContext.BlockIndex, _blockTimeOffset, Guid.Empty));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing Stake action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is MigrateMonsterCollection migrateMonsterCollection)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _migrateMonsterCollectionList.Add(MigrateMonsterCollectionData.GetMigrateMonsterCollectionInfo(inputState, outputState, ae.InputContext.Signer, ae.InputContext.BlockIndex, _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing MigrateMonsterCollection action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is Grinding grinding)
                            {
                                var start = DateTimeOffset.UtcNow;

                                var grindList = GrindingData.GetGrindingInfo(inputState, outputState, ae.InputContext.Signer, grinding.AvatarAddress, grinding.EquipmentIds, grinding.Id, ae.InputContext.BlockIndex, _blockTimeOffset);

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
                                var unlockEquipmentRecipeList = UnlockEquipmentRecipeData.GetUnlockEquipmentRecipeInfo(inputState, outputState, ae.InputContext.Signer, unlockEquipmentRecipe.AvatarAddress, unlockEquipmentRecipe.RecipeIds, unlockEquipmentRecipe.Id, ae.InputContext.BlockIndex, _blockTimeOffset);
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
                                var unlockEquipmentRecipeList = UnlockEquipmentRecipeData.GetUnlockEquipmentRecipeInfo(inputState, outputState, ae.InputContext.Signer, unlockEquipmentRecipe1.AvatarAddress, unlockEquipmentRecipe1.RecipeIds, unlockEquipmentRecipe1.Id, ae.InputContext.BlockIndex, _blockTimeOffset);
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
                                var unlockWorldList = UnlockWorldData.GetUnlockWorldInfo(inputState, outputState, ae.InputContext.Signer, unlockWorld.AvatarAddress, unlockWorld.WorldIds, unlockWorld.Id, ae.InputContext.BlockIndex, _blockTimeOffset);
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
                                var unlockWorldList = UnlockWorldData.GetUnlockWorldInfo(inputState, outputState, ae.InputContext.Signer, unlockWorld1.AvatarAddress, unlockWorld1.WorldIds, unlockWorld1.Id, ae.InputContext.BlockIndex, _blockTimeOffset);
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
                                _hasRandomBuffList.Add(HackAndSlashRandomBuffData.GetHasRandomBuffInfo(inputState, outputState, ae.InputContext.Signer, hasRandomBuff.AvatarAddress, hasRandomBuff.AdvancedGacha, hasRandomBuff.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing HasRandomBuff action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is JoinArena joinArena)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _joinArenaList.Add(JoinArenaData.GetJoinArenaInfo(inputState, outputState, ae.InputContext.Signer, joinArena.avatarAddress, joinArena.round, joinArena.championshipId, joinArena.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing JoinArena action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is JoinArena1 joinArena1)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _joinArenaList.Add(JoinArenaData.GetJoinArenaInfo(inputState, outputState, ae.InputContext.Signer, joinArena1.avatarAddress, joinArena1.round, joinArena1.championshipId, joinArena1.Id, ae.InputContext.BlockIndex, _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing JoinArena action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is BattleArena battleArena)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _avatarList.Add(AvatarData.GetAvatarInfo(outputState, ae.InputContext.Signer, battleArena.myAvatarAddress, battleArena.runeInfos, _blockTimeOffset));
                                _battleArenaList.Add(BattleArenaData.GetBattleArenaInfo(
                                    inputState,
                                    outputState,
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

                            if (action is EventMaterialItemCrafts eventMaterialItemCrafts)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _eventMaterialItemCraftsList.Add(EventMaterialItemCraftsData.GetEventMaterialItemCraftsInfo(
                                    inputState,
                                    outputState,
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
                                    inputState,
                                    outputState,
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
                                    inputState,
                                    outputState,
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
                                var prevRuneBalance = inputState.GetBalance(
                                    dailyReward.avatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = outputState.GetBalance(
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

                            if (action is DailyReward2 dailyReward2)
                            {
                                var start = DateTimeOffset.UtcNow;
#pragma warning disable CS0618
                                var runeCurrency = Currency.Legacy(RuneHelper.DailyRewardRune.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                var prevRuneBalance = inputState.GetBalance(
                                    dailyReward2.avatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = outputState.GetBalance(
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

                            if (action is ClaimRaidReward claimRaidReward)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var sheets = outputState.GetSheets(
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
                                    var prevRuneBalance = inputState.GetBalance(
                                        claimRaidReward.AvatarAddress,
                                        runeCurrency);
                                    var outputRuneBalance = outputState.GetBalance(
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
                                    inputState,
                                    outputState,
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
                                    inputState,
                                    outputState,
                                    ae.InputContext.Signer,
                                    rapidCombination.avatarAddress,
                                    rapidCombination.slotIndex,
                                    rapidCombination.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing RapidCombination action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is RapidCombination5 rapidCombination5)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _rapidCombinationList.Add(RapidCombinationData.GetRapidCombinationInfo(
                                    inputState,
                                    outputState,
                                    ae.InputContext.Signer,
                                    rapidCombination5.avatarAddress,
                                    rapidCombination5.slotIndex,
                                    rapidCombination5.Id,
                                    ae.InputContext.BlockIndex,
                                    _blockTimeOffset));
                                var end = DateTimeOffset.UtcNow;
                                Console.WriteLine("Writing RapidCombination action in block #{0}. Time Taken: {1} ms.", ae.InputContext.BlockIndex, (end - start).Milliseconds);
                            }

                            if (action is RapidCombination0 rapidCombination0)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _rapidCombinationList.Add(RapidCombinationData.GetRapidCombinationInfo(
                                    inputState,
                                    outputState,
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
                                var sheets = outputState.GetSheets(
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
                                    var prevRuneBalance = inputState.GetBalance(
                                        raid.AvatarAddress,
                                        runeCurrency);
                                    var outputRuneBalance = outputState.GetBalance(
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

                                _avatarList.Add(AvatarData.GetAvatarInfo(outputState, ae.InputContext.Signer, raid.AvatarAddress, raid.RuneInfos, _blockTimeOffset));

                                int raidId = 0;
                                bool found = false;
                                var legacyAccountDiff = AccountDiff.Create(
                                    inputState.GetAccount(ReservedAddresses.LegacyAccount),
                                    outputState.GetAccount(ReservedAddresses.LegacyAccount));
                                var updatedAddresses = legacyAccountDiff.StateDiffs.Keys
                                    .Union(legacyAccountDiff.FungibleAssetValueDiffs.Select(kv => kv.Key.Item1))
                                    .ToHashSet();
                                for (int i = 0; i < 99; i++)
                                {
                                    if (updatedAddresses.Contains(Addresses.GetRaiderAddress(raid.AvatarAddress, i)))
                                    {
                                        raidId = i;
                                        found = true;
                                        break;
                                    }
                                }

                                if (found)
                                {
                                    RaiderState raiderState =
                                        outputState.GetRaiderState(raid.AvatarAddress, raidId);
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
                                    inputState,
                                    outputState,
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

        private List<ICommittedActionEvaluation> EvaluateBlock(Block block)
        {
            var evList = _baseChain.EvaluateBlock(block).ToList();
            return evList;
        }
    }
}
