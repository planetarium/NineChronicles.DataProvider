using Nekoyume.Action.AdventureBoss;
using Nekoyume.Model.EnumType;
using NineChronicles.DataProvider.DataRendering.AdventureBoss;
using NineChronicles.DataProvider.Store.Models.AdventureBoss;

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
    using Nekoyume.TableData.Rune;
    using NineChronicles.DataProvider.DataRendering;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.DataProvider.Store.Models;
    using Serilog;
    using static Lib9c.SerializeKeys;

    public class MySqlMigration
    {
        private readonly Dictionary<long, AdventureBossSeasonModel> _adventureBossSeasonDict = new ();
        private readonly List<AdventureBossWantedModel> _adventureBossWantedList = new ();
        private readonly List<AdventureBossChallengeModel> _adventureBossChallengeList = new ();
        private readonly List<AdventureBossRushModel> _adventureBossRushList = new ();
        private readonly List<AdventureBossUnlockFloorModel> _adventureBossUnlockFloorList = new ();
        private readonly List<AdventureBossClaimRewardModel> _adventureBossClaimRewardList = new ();
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
        public async Task Migration(
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
                blockPolicy.PolicyActionsRegistry,
                baseStateStore,
                new NCActionLoader());
            _baseChain = new BlockChain(
                blockPolicy, stagePolicy, _baseStore, baseStateStore, genesis, blockChainStates, actionEvaluator
            );

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
                             _baseStore.IterateIndexes(
                                 _baseChain.Id,
                                 offset + offsetIdx ?? 0 + offsetIdx,
                                 limitInterval
                             ).Select((value, i) => new { i, value }))
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

                        try
                        {
                            taskArray[item.i] = Task.Factory.StartNew(() =>
                            {
                                List<ICommittedActionEvaluation> actionEvaluations = EvaluateBlock(block);
                                Console.WriteLine($"Block progress: #{block.Index}/{remainingCount}");
                                return actionEvaluations;
                            });
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
                        }
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
                await Task.Run(async () =>
                {
                    Console.WriteLine($"[Adventure Boss] {_adventureBossSeasonDict.Count} Season");
                    await _mySqlStore.StoreAdventureBossSeasonList(_adventureBossSeasonDict.Values.ToList());
                });

                await Task.Run(async () =>
                {
                    Console.WriteLine($"[Adventure Boss] {_adventureBossWantedList.Count} Wanted");
                    await _mySqlStore.StoreAdventureBossWantedList(_adventureBossWantedList);
                });

                await Task.Run(async () =>
                {
                    Console.WriteLine($"[Adventure Boss] {_adventureBossChallengeList.Count} Challenge");
                    await _mySqlStore.StoreAdventureBossChallengeList(_adventureBossChallengeList);
                });

                await Task.Run(async () =>
                {
                    Console.WriteLine($"[Adventure Boss] {_adventureBossRushList.Count} Rush");
                    await _mySqlStore.StoreAdventureBossRushList(_adventureBossRushList);
                });

                await Task.Run(async () =>
                {
                    Console.WriteLine($"[Adventure Boss] {_adventureBossUnlockFloorList.Count} Unlock");
                    await _mySqlStore.StoreAdventureBossUnlockFloorList(_adventureBossUnlockFloorList);
                });

                await Task.Run(async () =>
                {
                    Console.WriteLine($"[Adventure Boss] {_adventureBossClaimRewardList.Count} claim");
                    await _mySqlStore.StoreAdventureBossClaimRewardList(_adventureBossClaimRewardList);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
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

                        try
                        {
                            if (actionLoader.LoadAction(_blockIndex, ae.Action) is ActionBase and EventDungeonBattle eventDungeonBattle)
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
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
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
