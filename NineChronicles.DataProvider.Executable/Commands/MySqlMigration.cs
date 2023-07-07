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
    using Libplanet.Assets;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.RocksDBStore;
    using Libplanet.Store;
    using Libplanet.Tx;
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
                new NCActionLoader(),
                null);
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
                        if (actionLoader.LoadAction(_blockIndex, ae.Action) is TransferAsset3 transferAsset3)
                        {
                            var actionString = ae.InputContext.TxId.ToString();
                            var actionByteArray = Encoding.UTF8.GetBytes(actionString!).Take(16).ToArray();
                            var id = new Guid(actionByteArray);
                            _transferAssetList.Add(TransferAssetData.GetTransferAssetInfo(
                                id,
                                (TxId)ae.InputContext.TxId!,
                                ae.InputContext.BlockIndex,
                                _blockHash!.ToString(),
                                transferAsset3.Sender,
                                transferAsset3.Recipient,
                                transferAsset3.Amount.Currency.Ticker,
                                transferAsset3.Amount,
                                _blockTimeOffset));

                            Log.Debug("Stored TransferAsset action in block #{index}. TxId: {txId} Sender: {sender} Recipient: {recipient}, Ticker: {ticker}, Amount: {amount}.", ae.InputContext.BlockIndex, ae.InputContext.TxId!, transferAsset3.Sender, transferAsset3.Recipient, transferAsset3.Amount.Currency.Ticker, transferAsset3.Amount);
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
