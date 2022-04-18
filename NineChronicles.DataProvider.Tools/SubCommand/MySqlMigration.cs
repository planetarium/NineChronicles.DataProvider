using System.Net;
using Bencodex.Types;
using Lib9c.Model.Order;
using Nekoyume;
using Nekoyume.Battle;
using Nekoyume.BlockChain.Policy;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace NineChronicles.DataProvider.Tools.SubCommand
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Cocona;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.RocksDBStore;
    using Libplanet.Store;
    using MySqlConnector;
    using Nekoyume.Action;
    using Nekoyume.BlockChain;
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
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain<NCAction> _baseChain;
        private StreamWriter _ccBulkFile;
        private StreamWriter _ceBulkFile;
        private StreamWriter _ieBulkFile;
        private StreamWriter _agentBulkFile;
        private StreamWriter _avatarBulkFile;
        private List<string> _agentList;
        private List<string> _avatarList;
        private List<string> _ccFiles;
        private List<string> _ceFiles;
        private List<string> _ieFiles;
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
            _agentFiles = new List<string>();
            _avatarFiles = new List<string>();

            // lists to keep track of inserted addresses to minimize duplicates
            _agentList = new List<string>();
            _avatarList = new List<string>();
            var eqCount = 0;
            var mtCount = 0;
            var csCount = 0;
            var ctCount = 0;
            var buy0Count = 0;

            CreateBulkFiles();
            try
            {
                int totalCount = limit ?? (int)_baseStore.CountBlocks();
                int remainingCount = totalCount;
                int offsetIdx = 0;
                // var tasks = new List<Task>();
                

                while (remainingCount > 0)
                {
                    int interval = 10000;
                    int limitInterval;
                    if (interval < remainingCount)
                    {
                        limitInterval = interval;
                    }
                    else
                    {
                        limitInterval = remainingCount;
                    }

                    var tipHash = _baseStore.IndexBlockHash(_baseChain.Id, _baseChain.Tip.Index);
                    var tip = _baseStore.GetBlock<NCAction>(blockPolicy.GetHashAlgorithm, (BlockHash)tipHash);
                    var exec = _baseChain.ExecuteActions(tip);
                    var ev = exec.First();

                    var count = remainingCount;
                    var idx = offsetIdx;
                    // tasks.Add(Task.Run(() =>
                    // {
                        foreach (var item in
                            _baseStore.IterateIndexes(_baseChain.Id, offset + idx ?? 0 + idx, limitInterval)
                                .Select((value, i) => new { i, value }))
                        {
                            var block = _baseStore.GetBlock<NCAction>(blockPolicy.GetHashAlgorithm, item.value);
                            Console.WriteLine("Migrating {0}/{1} #{2}", item.i, count, block.Index);

                            foreach (var tx in block.Transactions)
                            {
                                // if (tx.Actions.FirstOrDefault()?.InnerAction is Sell0 sell0)
                                // {
                                //     try
                                //     {
                                //         Console.WriteLine(sell0);
                                //     }
                                //     catch (Exception ex)
                                //     {
                                //         Console.WriteLine(ex.Message);
                                //     }
                                // }

                                // if (tx.Actions.FirstOrDefault()?.InnerAction is Buy0 buy0)
                                // {
                                //     try
                                //     {
                                //         buy0Count++;
                                //         var i = _baseChain.ExecuteActions(block);
                                //         Console.WriteLine($"Buy0: {buy0Count}");
                                //         Console.WriteLine(buy0.buyerResult);
                                //         var buyerAvatarState = ev.OutputStates.GetAvatarState(buy0.buyerAvatarAddress);
                                //         var sellerAvatarState = ev.OutputStates.GetAvatarState(buy0
                                //                 .sellerAvatarAddress);
                                //         var itemSheet = ev.OutputStates.GetItemSheet();
                                //         var shopState = ev.OutputStates.GetShopState();
                                //         var (productId, shopItem1) = shopState.Products.FirstOrDefault();
                                //         var state = ev.OutputStates.GetState(
                                //             Addresses.GetItemAddress(buy0.productId));
                                //         ITradableItem orderItem =
                                //             (ITradableItem) ItemFactory.Deserialize((Dictionary) state);
                                //         if (orderItem.ItemType == ItemType.Material)
                                //         {
                                //             mtCount++;
                                //             Console.WriteLine("0. Material");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Equipment)
                                //         {
                                //             eqCount++;
                                //             Console.WriteLine("0. Equipment");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Consumable)
                                //         {
                                //             csCount++;
                                //             Console.WriteLine("0. Consumable");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Costume)
                                //         {
                                //             ctCount++;
                                //             Console.WriteLine("0. Costume");
                                //         }
                                //
                                //         var shopItem = buy0.buyerResult.shopItem;
                                //         int itemCount = shopItem.TradableFungibleItemCount == 0 ? 1 : shopItem.TradableFungibleItemCount;
                                //     }
                                //     catch (Exception ex)
                                //     {
                                //         Console.WriteLine(ex.Message);
                                //     }
                                // }

                                // if (tx.Actions.FirstOrDefault()?.InnerAction is Buy2 buy2)
                                // {
                                //     try
                                //     {
                                //         Console.WriteLine(buy2.buyerResult);
                                //         // var buyerAvatarState = ev.OutputStates.GetAvatarStateV2(buy7.buyerAvatarAddress);
                                //         // var sellerAvatarState = ev.OutputStates.GetAvatarStateV2((Address) buy7
                                //         //     .purchaseInfos.FirstOrDefault()
                                //         //     ?.sellerAvatarAddress);
                                //         // var itemSheet = ev.OutputStates.GetItemSheet();
                                //         // var state = ev.OutputStates.GetState(
                                //         //     Addresses.GetItemAddress(buy7.purchaseInfos.First().productId));
                                //         ITradableItem orderItem = buy2.buyerResult.shopItem.ItemUsable;
                                //         if (orderItem.ItemType == ItemType.Material)
                                //         {
                                //             mtCount++;
                                //             Console.WriteLine("2. Material");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Equipment)
                                //         {
                                //             eqCount++;
                                //             Console.WriteLine("2. Equipment");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Consumable)
                                //         {
                                //             csCount++;
                                //             Console.WriteLine("2. Consumable");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Costume)
                                //         {
                                //             ctCount++;
                                //             Console.WriteLine("2. Costume");
                                //         }
                                //
                                //         var shopItem = buy2.buyerResult.shopItem;
                                //         int itemCount = shopItem.TradableFungibleItemCount == 0 ? 1 : shopItem.TradableFungibleItemCount;
                                //     }
                                //     catch (Exception ex)
                                //     {
                                //         Console.WriteLine(ex.Message);
                                //     }
                                // }
                                //
                                // if (tx.Actions.FirstOrDefault()?.InnerAction is Buy3 buy3)
                                // {
                                //     try
                                //     {
                                //         Console.WriteLine(buy3.buyerResult);
                                //         // var buyerAvatarState = ev.OutputStates.GetAvatarStateV2(buy7.buyerAvatarAddress);
                                //         // var sellerAvatarState = ev.OutputStates.GetAvatarStateV2((Address) buy7
                                //         //     .purchaseInfos.FirstOrDefault()
                                //         //     ?.sellerAvatarAddress);
                                //         // var itemSheet = ev.OutputStates.GetItemSheet();
                                //         // var state = ev.OutputStates.GetState(
                                //         //     Addresses.GetItemAddress(buy7.purchaseInfos.First().productId));
                                //         ITradableItem orderItem = buy3.buyerResult.shopItem.ItemUsable;
                                //         if (orderItem.ItemType == ItemType.Material)
                                //         {
                                //             mtCount++;
                                //             Console.WriteLine("3. Material");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Equipment)
                                //         {
                                //             eqCount++;
                                //             Console.WriteLine("3. Equipment");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Consumable)
                                //         {
                                //             csCount++;
                                //             Console.WriteLine("3. Consumable");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Costume)
                                //         {
                                //             ctCount++;
                                //             Console.WriteLine("3. Costume");
                                //         }
                                //
                                //         var shopItem = buy3.buyerResult.shopItem;
                                //         int itemCount = shopItem.TradableFungibleItemCount == 0 ? 1 : shopItem.TradableFungibleItemCount;
                                //     }
                                //     catch (Exception ex)
                                //     {
                                //         Console.WriteLine(ex.Message);
                                //     }
                                // }
                                //
                                // if (tx.Actions.FirstOrDefault()?.InnerAction is Buy4 buy4)
                                // {
                                //     try
                                //     {
                                //         Console.WriteLine(buy4.buyerResult);
                                //         // var buyerAvatarState = ev.OutputStates.GetAvatarStateV2(buy7.buyerAvatarAddress);
                                //         // var sellerAvatarState = ev.OutputStates.GetAvatarStateV2((Address) buy7
                                //         //     .purchaseInfos.FirstOrDefault()
                                //         //     ?.sellerAvatarAddress);
                                //         // var itemSheet = ev.OutputStates.GetItemSheet();
                                //         // var state = ev.OutputStates.GetState(
                                //         //     Addresses.GetItemAddress(buy7.purchaseInfos.First().productId));
                                //         ITradableItem orderItem = buy4.buyerResult.shopItem.ItemUsable;
                                //         if (orderItem.ItemType == ItemType.Material)
                                //         {
                                //             mtCount++;
                                //             Console.WriteLine("4. Material");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Equipment)
                                //         {
                                //             eqCount++;
                                //             Console.WriteLine("4. Equipment");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Consumable)
                                //         {
                                //             csCount++;
                                //             Console.WriteLine("4. Consumable");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Costume)
                                //         {
                                //             ctCount++;
                                //             Console.WriteLine("4. Costume");
                                //         }
                                //
                                //         var shopItem = buy4.buyerResult.shopItem;
                                //         int itemCount = shopItem.TradableFungibleItemCount == 0 ? 1 : shopItem.TradableFungibleItemCount;
                                //     }
                                //     catch (Exception ex)
                                //     {
                                //         Console.WriteLine(ex.Message);
                                //     }
                                // }

                                // if (tx.Actions.FirstOrDefault()?.InnerAction is Buy5 buy5)
                                // {
                                //     try
                                //     {
                                //         Console.WriteLine(buy5.purchaseInfos.Count());
                                //         var i = _baseChain.ExecuteActions(block);
                                //         var buyerAvatarState = ev.OutputStates.GetAvatarStateV2(buy5.buyerAvatarAddress) ?? ev.OutputStates.GetAvatarState(buy5.buyerAvatarAddress);
                                //         var sellerAvatarState = ev.OutputStates.GetAvatarStateV2((Address)buy5
                                //             .purchaseInfos.FirstOrDefault()
                                //             ?.sellerAvatarAddress) ?? ev.OutputStates.GetAvatarState((Address)buy5
                                //             .purchaseInfos.FirstOrDefault()
                                //             ?.sellerAvatarAddress);
                                //         var itemSheet = ev.OutputStates.GetItemSheet();
                                //         var state = ev.OutputStates.GetState(
                                //             Addresses.GetItemAddress(buy5.purchaseInfos.First().productId));
                                //         ITradableItem orderItem =
                                //             (ITradableItem)ItemFactory.Deserialize((Dictionary)state);
                                //         if (orderItem.ItemType == ItemType.Material)
                                //         {
                                //             mtCount++;
                                //             Console.WriteLine("5. Material");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Equipment)
                                //         {
                                //             eqCount++;
                                //             Console.WriteLine("5. Equipment");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Consumable)
                                //         {
                                //             csCount++;
                                //             Console.WriteLine("5. Consumable");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Costume)
                                //         {
                                //             ctCount++;
                                //             Console.WriteLine("5. Costume");
                                //         }
                                //
                                //         var pr = buy5.buyerMultipleResult.purchaseResults.FirstOrDefault();
                                //         var shopItem1 = pr.shopItem;
                                //         int itemCount = shopItem1.TradableFungibleItemCount == 0 ? 1 : shopItem1.TradableFungibleItemCount;
                                //     }
                                //     catch (Exception ex)
                                //     {
                                //         Console.WriteLine(ex.Message);
                                //     }
                                // }

                                if (tx.Actions.FirstOrDefault()?.InnerAction is Buy6 buy6)
                                {
                                    try
                                    {
                                        Console.WriteLine(buy6.purchaseInfos.Count());
                                        var buyerAvatarState = ev.OutputStates.GetAvatarStateV2(buy6.buyerAvatarAddress);
                                        var sellerAvatarState = ev.OutputStates.GetAvatarStateV2((Address) buy6
                                            .purchaseInfos.FirstOrDefault()
                                            ?.sellerAvatarAddress);
                                        var itemSheet = ev.OutputStates.GetItemSheet();
                                        var state = ev.OutputStates.GetState(
                                            Addresses.GetItemAddress(buy6.purchaseInfos.First().productId));
                                        ITradableItem orderItem =
                                            (ITradableItem) ItemFactory.Deserialize((Dictionary) state);
                                        if (orderItem.ItemType == ItemType.Material)
                                        {
                                            mtCount++;
                                            Console.WriteLine("6. Material");
                                        }
                                
                                        if (orderItem.ItemType == ItemType.Equipment)
                                        {
                                            eqCount++;
                                            Console.WriteLine("6. Equipment");
                                        }
                                
                                        if (orderItem.ItemType == ItemType.Consumable)
                                        {
                                            csCount++;
                                            Console.WriteLine("6. Consumable");
                                        }
                                
                                        if (orderItem.ItemType == ItemType.Costume)
                                        {
                                            ctCount++;
                                            Console.WriteLine("6. Costume");
                                        }
                                
                                        var pr = buy6.buyerMultipleResult.purchaseResults.FirstOrDefault();
                                        var shopItem = pr.shopItem;
                                        int itemCount = shopItem.TradableFungibleItemCount == 0 ? 1 : shopItem.TradableFungibleItemCount;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }
                                
                                // if (tx.Actions.FirstOrDefault()?.InnerAction is Buy7 buy7)
                                // {
                                //     try
                                //     {
                                //         Console.WriteLine(buy7.purchaseInfos.Count());
                                //         var buyerAvatarState = ev.OutputStates.GetAvatarStateV2(buy7.buyerAvatarAddress);
                                //         var sellerAvatarState = ev.OutputStates.GetAvatarStateV2((Address) buy7
                                //             .purchaseInfos.FirstOrDefault()
                                //             ?.sellerAvatarAddress);
                                //         var itemSheet = ev.OutputStates.GetItemSheet();
                                //         var state = ev.OutputStates.GetState(
                                //             Addresses.GetItemAddress(buy7.purchaseInfos.First().productId));
                                //         Address shardedShopAddress =
                                //             ShardedShopState.DeriveAddress(buy7.purchaseInfos.FirstOrDefault().itemSubType, buy7.purchaseInfos.FirstOrDefault().productId);
                                //         var nextShopState = new ShardedShopState((Dictionary)ev.OutputStates.GetState(shardedShopAddress));
                                //         ITradableItem orderItem =
                                //             (ITradableItem) ItemFactory.Deserialize((Dictionary) state);
                                //         if (orderItem.ItemType == ItemType.Material)
                                //         {
                                //             mtCount++;
                                //             Console.WriteLine("7. Material");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Equipment)
                                //         {
                                //             eqCount++;
                                //             Console.WriteLine("7. Equipment");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Consumable)
                                //         {
                                //             csCount++;
                                //             Console.WriteLine("7. Consumable");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Costume)
                                //         {
                                //             ctCount++;
                                //             Console.WriteLine("7. Costume");
                                //         }
                                //
                                //         var pr = buy7.buyerMultipleResult.purchaseResults.FirstOrDefault();
                                //         var shopItem = pr.shopItem;
                                //         int itemCount = shopItem.TradableFungibleItemCount == 0 ? 1 : shopItem.TradableFungibleItemCount;
                                //     }
                                //     catch (Exception ex)
                                //     {
                                //         Console.WriteLine(ex.Message);
                                //     }
                                // }
                                //
                                // if (tx.Actions.FirstOrDefault()?.InnerAction is Buy8 buy8)
                                // {
                                //     try
                                //     {
                                //         Console.WriteLine(buy8.purchaseInfos.Count());
                                //         var buyerAvatarState = ev.OutputStates.GetAvatarStateV2(buy8.buyerAvatarAddress);
                                //         var sellerAvatarState = ev.OutputStates.GetAvatarStateV2((Address) buy8
                                //             .purchaseInfos.FirstOrDefault()
                                //             ?.SellerAvatarAddress);
                                //         var itemSheet = ev.OutputStates.GetItemSheet();
                                //         var state = ev.OutputStates.GetState(
                                //             Addresses.GetItemAddress(buy8.purchaseInfos.First().TradableId));
                                //         ITradableItem orderItem =
                                //             (ITradableItem) ItemFactory.Deserialize((Dictionary) state);
                                //         if (orderItem.ItemType == ItemType.Material)
                                //         {
                                //             mtCount++;
                                //             Console.WriteLine("8. Material");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Equipment)
                                //         {
                                //             eqCount++;
                                //             Console.WriteLine("8. Equipment");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Consumable)
                                //         {
                                //             csCount++;
                                //             Console.WriteLine("8. Consumable");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Costume)
                                //         {
                                //             ctCount++;
                                //             Console.WriteLine("8. Costume");
                                //         }
                                //
                                //         Order order =
                                //             OrderFactory.Deserialize(
                                //                 (Dictionary) ev.OutputStates.GetState(
                                //                     Order.DeriveAddress(buy8.purchaseInfos.FirstOrDefault().OrderId)));
                                //         var orderReceipt = new OrderReceipt(
                                //             (Dictionary) ev.OutputStates.GetState(
                                //                 OrderReceipt.DeriveAddress(buy8.purchaseInfos.FirstOrDefault()
                                //                     .OrderId)));
                                //         int itemCount = order is FungibleOrder fungibleOrder
                                //             ? fungibleOrder.ItemCount
                                //             : 1;
                                //     }
                                //     catch (Exception ex)
                                //     {
                                //         Console.WriteLine(ex.Message);
                                //     }
                                // }
                                //
                                // if (tx.Actions.FirstOrDefault()?.InnerAction is Buy9 buy9)
                                // {
                                //     try
                                //     {
                                //         Console.WriteLine(buy9.purchaseInfos.Count());
                                //         var buyerAvatarState = ev.OutputStates.GetAvatarStateV2(buy9.buyerAvatarAddress);
                                //         var sellerAvatarState = ev.OutputStates.GetAvatarStateV2((Address) buy9
                                //             .purchaseInfos.FirstOrDefault()
                                //             ?.SellerAvatarAddress);
                                //         var itemSheet = ev.OutputStates.GetItemSheet();
                                //         var state = ev.OutputStates.GetState(
                                //             Addresses.GetItemAddress(buy9.purchaseInfos.First().TradableId));
                                //         ITradableItem orderItem =
                                //             (ITradableItem) ItemFactory.Deserialize((Dictionary) state);
                                //         if (orderItem.ItemType == ItemType.Material)
                                //         {
                                //             mtCount++;
                                //             Console.WriteLine("9. Material");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Equipment)
                                //         {
                                //             eqCount++;
                                //             Console.WriteLine("9. Equipment");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Consumable)
                                //         {
                                //             csCount++;
                                //             Console.WriteLine("9. Consumable");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Costume)
                                //         {
                                //             ctCount++;
                                //             Console.WriteLine("9. Costume");
                                //         }
                                //
                                //         Order order =
                                //             OrderFactory.Deserialize(
                                //                 (Dictionary) ev.OutputStates.GetState(
                                //                     Order.DeriveAddress(buy9.purchaseInfos.FirstOrDefault().OrderId)));
                                //         var orderReceipt = new OrderReceipt(
                                //             (Dictionary) ev.OutputStates.GetState(
                                //                 OrderReceipt.DeriveAddress(buy9.purchaseInfos.FirstOrDefault()
                                //                     .OrderId)));
                                //         int itemCount = order is FungibleOrder fungibleOrder
                                //             ? fungibleOrder.ItemCount
                                //             : 1;
                                //     }
                                //     catch (Exception ex)
                                //     {
                                //         Console.WriteLine(ex.Message);
                                //     }
                                // }
                                //
                                // if (tx.Actions.FirstOrDefault()?.InnerAction is Buy buy)
                                // {
                                //     try
                                //     {
                                //         Console.WriteLine(buy.purchaseInfos.Count());
                                //         var buyerAvatarState = ev.OutputStates.GetAvatarStateV2(buy.buyerAvatarAddress);
                                //         var sellerAvatarState = ev.OutputStates.GetAvatarStateV2((Address) buy
                                //             .purchaseInfos.FirstOrDefault()
                                //             ?.SellerAvatarAddress);
                                //         var itemSheet = ev.OutputStates.GetItemSheet();
                                //         var state = ev.OutputStates.GetState(
                                //             Addresses.GetItemAddress(buy.purchaseInfos.First().TradableId));
                                //         ITradableItem orderItem =
                                //             (ITradableItem) ItemFactory.Deserialize((Dictionary) state);
                                //         if (orderItem.ItemType == ItemType.Material)
                                //         {
                                //             mtCount++;
                                //             Console.WriteLine("Material");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Equipment)
                                //         {
                                //             eqCount++;
                                //             Console.WriteLine("Equipment");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Consumable)
                                //         {
                                //             csCount++;
                                //             Console.WriteLine("Consumable");
                                //         }
                                //
                                //         if (orderItem.ItemType == ItemType.Costume)
                                //         {
                                //             ctCount++;
                                //             Console.WriteLine("Costume");
                                //         }
                                //
                                //         Order order =
                                //             OrderFactory.Deserialize(
                                //                 (Dictionary) ev.OutputStates.GetState(
                                //                     Order.DeriveAddress(buy.purchaseInfos.FirstOrDefault().OrderId)));
                                //         var orderReceipt = new OrderReceipt(
                                //             (Dictionary) ev.OutputStates.GetState(
                                //                 OrderReceipt.DeriveAddress(buy.purchaseInfos.FirstOrDefault()
                                //                     .OrderId)));
                                //         int itemCount = order is FungibleOrder fungibleOrder
                                //             ? fungibleOrder.ItemCount
                                //             : 1;
                                //     }
                                //     catch (Exception ex)
                                //     {
                                //         Console.WriteLine(ex.Message);
                                //     }
                                // }

                                // if (!_agentList.Contains(tx.Signer.ToString()))
                                // {
                                //     WriteAgent(tx.Signer);
                                //     if (ev.OutputStates.GetAgentState(tx.Signer) is { } agentState)
                                //     {
                                //         var avatarAddresses = agentState.avatarAddresses;
                                //         foreach (var avatarAddress in avatarAddresses)
                                //         {
                                //             try
                                //             {
                                //                 if (ev.OutputStates.GetAvatarStateV2(avatarAddress.Value) is
                                //                     { } avatarState)
                                //                 {
                                //                     var characterSheet = ev.OutputStates.GetSheet<CharacterSheet>();
                                //                     var avatarLevel = avatarState.level;
                                //                     var avatarArmorId = avatarState.GetArmorId();
                                //                     var avatarTitleCostume =
                                //                         avatarState.inventory.Costumes.FirstOrDefault(costume =>
                                //                             costume.ItemSubType == ItemSubType.Title &&
                                //                             costume.equipped);
                                //                     int? avatarTitleId = null;
                                //                     if (avatarTitleCostume != null)
                                //                     {
                                //                         avatarTitleId = avatarTitleCostume.Id;
                                //                     }
                                //
                                //                     var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                //                     string avatarName = avatarState.name;
                                //
                                //                     Log.Debug(
                                //                         "AvatarName: {0}, AvatarLevel: {1}, ArmorId: {2}, TitleId: {3}, CP: {4}",
                                //                         avatarName,
                                //                         avatarLevel,
                                //                         avatarArmorId,
                                //                         avatarTitleId,
                                //                         avatarCp);
                                //                     WriteAvatar(
                                //                         tx.Signer,
                                //                         avatarAddress.Value,
                                //                         avatarName,
                                //                         avatarLevel,
                                //                         avatarTitleId ?? 0,
                                //                         avatarArmorId,
                                //                         avatarCp);
                                //                 }
                                //                 else
                                //                 {
                                //                     Console.WriteLine("Hi");
                                //                 }
                                //             }
                                //             catch (Exception e)
                                //             {
                                //                 Console.WriteLine("Hi");
                                //             }
                                //         }
                                //     }
                                    // WriteAgent(tx.Signer);
                                    // tasks.Add(Task.Run(() =>
                                    // {
                                    //     WriteAgent(tx.Signer);
                                    // }));
                                // }
                                // if (tx.Actions.FirstOrDefault()?.InnerAction is BuyMultiple buyMultiple)
                                // {
                                //     try
                                //     {
                                //         var info = buyMultiple.buyerResult;
                                //         Console.WriteLine(info.purchaseResults);
                                //     }
                                //     catch (Exception ex)
                                //     {
                                //         Console.WriteLine(ex.Message);
                                //     }
                                // }
                            }

                            Console.WriteLine("Migrating Done {0}/{1} #{2}", item.i, count, block.Index);
                        }
                    // }));

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

                // Task.WaitAll(tasks.ToArray());
                FlushBulkFiles();
                DateTimeOffset postDataPrep = DateTimeOffset.Now;
                Console.WriteLine("Data Preparation Complete! Time Elapsed: {0}", postDataPrep - start);
                Console.WriteLine($"Equipment: {eqCount}, Costume: {ctCount}, Material: {mtCount}, Consumable: {csCount} Buy0: {buy0Count}");

                // foreach (var path in _agentFiles)
                // {
                //     BulkInsert(AgentDbName, path);
                // }
                //
                // foreach (var path in _avatarFiles)
                // {
                //     BulkInsert(AvatarDbName, path);
                // }
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

            _agentFiles.Add(agentFilePath);
            _avatarFiles.Add(avatarFilePath);
            _ccFiles.Add(ccFilePath);
            _ceFiles.Add(ceFilePath);
            _ieFiles.Add(ieFilePath);
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
