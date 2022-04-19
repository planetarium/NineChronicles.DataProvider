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
    using Libplanet.Action;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.RocksDBStore;
    using Libplanet.Store;
    using MySqlConnector;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.BlockChain.Policy;
    using Nekoyume.Model.Item;
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
        private const string SEDbName = "ShopHistoryEquipments";
        private const string SCTDbName = "ShopHistoryCostumes";
        private const string SMDbName = "ShopHistoryMaterials";
        private const string SCDbName = "ShopHistoryConsumables";
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain<NCAction> _baseChain;
        private StreamWriter _ccBulkFile;
        private StreamWriter _ceBulkFile;
        private StreamWriter _ieBulkFile;
        private StreamWriter _agentBulkFile;
        private StreamWriter _avatarBulkFile;
        private StreamWriter _seBulkFile;
        private StreamWriter _sctBulkFile;
        private StreamWriter _smBulkFile;
        private StreamWriter _scBulkFile;
        private List<string> _agentList;
        private List<string> _avatarList;
        private List<string> _ccFiles;
        private List<string> _ceFiles;
        private List<string> _ieFiles;
        private List<string> _agentFiles;
        private List<string> _avatarFiles;
        private List<string> _seFiles;
        private List<string> _sctFiles;
        private List<string> _smFiles;
        private List<string> _scFiles;

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
            _seFiles = new List<string>();
            _sctFiles = new List<string>();
            _smFiles = new List<string>();
            _scFiles = new List<string>();

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
                    IReadOnlyList<ActionEvaluation> aes = null;

                    foreach (var item in
                            _baseStore.IterateIndexes(_baseChain.Id, offset + idx ?? 0 + idx, limitInterval)
                                .Select((value, i) => new { i, value }))
                        {
                            var block = _baseStore.GetBlock<NCAction>(blockPolicy.GetHashAlgorithm, item.value);
                            Console.WriteLine("Migrating {0}/{1} #{2}", item.i, count, block.Index);

                            foreach (var tx in block.Transactions)
                            {
                                if (tx.Actions.FirstOrDefault()?.InnerAction is Buy0 buy0)
                                {
                                    try
                                    {
                                        buy0Count++;
                                        if (aes == null || aes.FirstOrDefault().InputContext.BlockIndex != block.Index)
                                        {
                                            aes = _baseChain.ExecuteActions(block);
                                        }

                                        // foreach (var ae in aes)
                                        // {
                                        //     var action = (PolymorphicAction<ActionBase>)ae.Action;
                                        //     if (action.InnerAction is Buy0 buy0I)
                                        //     {
                                                Console.WriteLine($"Buy0: {buy0Count}");
                                                Console.WriteLine(buy0.buyerResult);
                                                if (buy0.buyerResult.itemUsable.ItemType == ItemType.Material)
                                                {
                                                    mtCount++;
                                                    Console.WriteLine("0. Material");
                                                }
                                                
                                                if (buy0.buyerResult.itemUsable.ItemType == ItemType.Equipment)
                                                {
                                                    eqCount++;
                                                    Console.WriteLine("0. Equipment");
                                                }
                                                
                                                if (buy0.buyerResult.itemUsable.ItemType == ItemType.Consumable)
                                                {
                                                    csCount++;
                                                    Console.WriteLine("0. Consumable");
                                                }
                                                
                                                if (buy0.buyerResult.itemUsable.ItemType == ItemType.Costume)
                                                {
                                                    ctCount++;
                                                    Console.WriteLine("0. Costume");
                                                }
                                                
                                                var shopItem = buy0.buyerResult.shopItem;
                                                int itemCount = shopItem.TradableFungibleItemCount == 0 ? 1 : shopItem.TradableFungibleItemCount;
                                        //     }
                                        // }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }

                                if (tx.Actions.FirstOrDefault()?.InnerAction is Buy2 buy2)
                                {
                                    try
                                    {
                                        Console.WriteLine(buy2.buyerResult);
                                        ITradableItem orderItem = buy2.buyerResult.shopItem.ItemUsable;
                                        if (orderItem.ItemType == ItemType.Material)
                                        {
                                            mtCount++;
                                            Console.WriteLine("2. Material");
                                        }
                                
                                        if (orderItem.ItemType == ItemType.Equipment)
                                        {
                                            eqCount++;
                                            Console.WriteLine("2. Equipment");
                                        }
                                
                                        if (orderItem.ItemType == ItemType.Consumable)
                                        {
                                            csCount++;
                                            Console.WriteLine("2. Consumable");
                                        }
                                
                                        if (orderItem.ItemType == ItemType.Costume)
                                        {
                                            ctCount++;
                                            Console.WriteLine("2. Costume");
                                        }
                                
                                        var shopItem = buy2.buyerResult.shopItem;
                                        int itemCount = shopItem.TradableFungibleItemCount == 0 ? 1 : shopItem.TradableFungibleItemCount;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }
                                
                                if (tx.Actions.FirstOrDefault()?.InnerAction is Buy3 buy3)
                                {
                                    try
                                    {
                                        Console.WriteLine(buy3.buyerResult);
                                        ITradableItem orderItem = buy3.buyerResult.shopItem.ItemUsable;
                                        if (orderItem.ItemType == ItemType.Material)
                                        {
                                            mtCount++;
                                            Console.WriteLine("3. Material");
                                        }
                                
                                        if (orderItem.ItemType == ItemType.Equipment)
                                        {
                                            eqCount++;
                                            Console.WriteLine("3. Equipment");
                                        }
                                
                                        if (orderItem.ItemType == ItemType.Consumable)
                                        {
                                            csCount++;
                                            Console.WriteLine("3. Consumable");
                                        }
                                
                                        if (orderItem.ItemType == ItemType.Costume)
                                        {
                                            ctCount++;
                                            Console.WriteLine("3. Costume");
                                        }
                                
                                        var shopItem = buy3.buyerResult.shopItem;
                                        int itemCount = shopItem.TradableFungibleItemCount == 0 ? 1 : shopItem.TradableFungibleItemCount;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }

                                if (tx.Actions.FirstOrDefault()?.InnerAction is Buy4 buy4)
                                {
                                    try
                                    {
                                        Console.WriteLine(buy4.buyerResult);
                                        ITradableItem orderItem = buy4.buyerResult.shopItem.ItemUsable;
                                        if (orderItem.ItemType == ItemType.Material)
                                        {
                                            mtCount++;
                                            Console.WriteLine("4. Material");
                                        }
                                
                                        if (orderItem.ItemType == ItemType.Equipment)
                                        {
                                            eqCount++;
                                            Console.WriteLine("4. Equipment");
                                        }
                                
                                        if (orderItem.ItemType == ItemType.Consumable)
                                        {
                                            csCount++;
                                            Console.WriteLine("4. Consumable");
                                        }
                                
                                        if (orderItem.ItemType == ItemType.Costume)
                                        {
                                            ctCount++;
                                            Console.WriteLine("4. Costume");
                                        }

                                        var shopItem = buy4.buyerResult.shopItem;
                                        int itemCount = shopItem.TradableFungibleItemCount == 0 ? 1 : shopItem.TradableFungibleItemCount;
                                        Console.WriteLine($"{itemCount}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }

                                if (tx.Actions.FirstOrDefault()?.InnerAction is Buy5 b5)
                                {
                                    try
                                    {
                                        if (aes == null || aes.FirstOrDefault().InputContext.BlockIndex != block.Index)
                                        {
                                            aes = _baseChain.ExecuteActions(block);
                                        }

                                        foreach (var buy5 in b5.buyerMultipleResult.purchaseResults)
                                        {
                                            int itemCount = buy5.shopItem.TradableFungibleItemCount == 0 ? 1 : buy5.shopItem.TradableFungibleItemCount;
                                            if (buy5.shopItem.ItemUsable.ItemType == ItemType.Equipment)
                                            {
                                                eqCount++;
                                                WriteSE(
                                                    (Equipment)buy5.shopItem.ItemUsable,
                                                    buy5.shopItem.SellerAvatarAddress,
                                                    b5.buyerAvatarAddress,
                                                    block.Index,
                                                    tx.Id.ToString(),
                                                    block.Hash.ToString(),
                                                    buy5.shopItem.Price.ToString().Split(" ").FirstOrDefault(),
                                                    buy5.id.ToString(),
                                                    tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                    itemCount);
                                                Console.WriteLine("5. Equipment");
                                            }

                                            if (buy5.shopItem.ItemUsable.ItemType == ItemType.Costume)
                                            {
                                                ctCount++;
                                                WriteSCT(
                                                    buy5.shopItem.Costume,
                                                    buy5.shopItem.SellerAvatarAddress,
                                                    b5.buyerAvatarAddress,
                                                    block.Index,
                                                    tx.Id.ToString(),
                                                    block.Hash.ToString(),
                                                    buy5.shopItem.Price.ToString().Split(" ").FirstOrDefault(),
                                                    buy5.id.ToString(),
                                                    tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                    itemCount);
                                                Console.WriteLine("5. Costume");
                                            }

                                            if (buy5.shopItem.ItemUsable.ItemType == ItemType.Material)
                                            {
                                                mtCount++;
                                                WriteSM(
                                                    (Material)buy5.shopItem.TradableFungibleItem,
                                                    buy5.shopItem.SellerAvatarAddress,
                                                    b5.buyerAvatarAddress,
                                                    block.Index,
                                                    tx.Id.ToString(),
                                                    block.Hash.ToString(),
                                                    buy5.shopItem.Price.ToString().Split(" ").FirstOrDefault(),
                                                    buy5.id.ToString(),
                                                    tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                    itemCount);
                                                Console.WriteLine("5. Material");
                                            }

                                            if (buy5.shopItem.ItemUsable.ItemType == ItemType.Consumable)
                                            {
                                                csCount++;
                                                WriteSC(
                                                    (Consumable)buy5.shopItem.ItemUsable,
                                                    buy5.shopItem.SellerAvatarAddress,
                                                    b5.buyerAvatarAddress,
                                                    block.Index,
                                                    tx.Id.ToString(),
                                                    block.Hash.ToString(),
                                                    buy5.shopItem.Price.ToString().Split(" ").FirstOrDefault(),
                                                    buy5.id.ToString(),
                                                    tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                    itemCount);
                                                Console.WriteLine("5. Consumable");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }

                                if (tx.Actions.FirstOrDefault()?.InnerAction is Buy6 b6)
                                {
                                    try
                                    {
                                        if (aes == null || aes.FirstOrDefault().InputContext.BlockIndex != block.Index)
                                        {
                                            aes = _baseChain.ExecuteActions(block);
                                        }

                                        // foreach (var ae in aes)
                                        // {
                                        //     var action = (PolymorphicAction<ActionBase>)ae.Action;
                                        //     if (action.InnerAction is Buy6 buy6i)
                                        //     {
                                        //         Console.WriteLine(buy6i.purchaseInfos.Count());
                                                foreach (var buy6 in b6.buyerMultipleResult.purchaseResults)
                                                {
                                                    int itemCount = buy6.shopItem.TradableFungibleItemCount == 0 ? 1 : buy6.shopItem.TradableFungibleItemCount;
                                                    if (buy6.shopItem.ItemUsable.ItemType == ItemType.Equipment)
                                                    {
                                                        eqCount++;
                                                        WriteSE(
                                                            (Equipment)buy6.shopItem.ItemUsable,
                                                            buy6.shopItem.SellerAvatarAddress,
                                                            b6.buyerAvatarAddress,
                                                            block.Index,
                                                            tx.Id.ToString(),
                                                            block.Hash.ToString(),
                                                            buy6.shopItem.Price.ToString().Split(" ").FirstOrDefault(),
                                                            buy6.id.ToString(),
                                                            tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                            itemCount);
                                                        Console.WriteLine("6. Equipment");
                                                    }

                                                    if (buy6.shopItem.ItemUsable.ItemType == ItemType.Costume)
                                                    {
                                                        ctCount++;
                                                        WriteSCT(
                                                            buy6.shopItem.Costume,
                                                            buy6.shopItem.SellerAvatarAddress,
                                                            b6.buyerAvatarAddress,
                                                            block.Index,
                                                            tx.Id.ToString(),
                                                            block.Hash.ToString(),
                                                            buy6.shopItem.Price.ToString().Split(" ").FirstOrDefault(),
                                                            buy6.id.ToString(),
                                                            tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                            itemCount);
                                                        Console.WriteLine("6. Costume");
                                                    }

                                                    if (buy6.shopItem.ItemUsable.ItemType == ItemType.Material)
                                                    {
                                                        mtCount++;
                                                        WriteSM(
                                                            (Material)buy6.shopItem.TradableFungibleItem,
                                                            buy6.shopItem.SellerAvatarAddress,
                                                            b6.buyerAvatarAddress,
                                                            block.Index,
                                                            tx.Id.ToString(),
                                                            block.Hash.ToString(),
                                                            buy6.shopItem.Price.ToString().Split(" ").FirstOrDefault(),
                                                            buy6.id.ToString(),
                                                            tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                            itemCount);
                                                        Console.WriteLine("6. Material");
                                                    }

                                                    if (buy6.shopItem.ItemUsable.ItemType == ItemType.Consumable)
                                                    {
                                                        csCount++;
                                                        WriteSC(
                                                            (Consumable)buy6.shopItem.ItemUsable,
                                                            buy6.shopItem.SellerAvatarAddress,
                                                            b6.buyerAvatarAddress,
                                                            block.Index,
                                                            tx.Id.ToString(),
                                                            block.Hash.ToString(),
                                                            buy6.shopItem.Price.ToString().Split(" ").FirstOrDefault(),
                                                            buy6.id.ToString(),
                                                            tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                            itemCount);
                                                        Console.WriteLine("6. Consumable");
                                                    }
                                                }
                                        //     }
                                        // }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }

                                if (tx.Actions.FirstOrDefault()?.InnerAction is Buy7 b7)
                                {
                                    try
                                    {
                                        if (aes == null || aes.FirstOrDefault().InputContext.BlockIndex != block.Index)
                                        {
                                            aes = _baseChain.ExecuteActions(block);
                                        }
                                                Console.WriteLine(b7.purchaseInfos.Count());
                                                foreach (var buy7 in b7.buyerMultipleResult.purchaseResults)
                                                {
                                                    int itemCount = buy7.shopItem.TradableFungibleItemCount == 0 ? 1 : buy7.shopItem.TradableFungibleItemCount;
                                                    if (buy7.shopItem.ItemUsable.ItemType == ItemType.Equipment)
                                                    {
                                                        eqCount++;
                                                        WriteSE(
                                                            (Equipment)buy7.shopItem.ItemUsable,
                                                            buy7.shopItem.SellerAvatarAddress,
                                                            b7.buyerAvatarAddress,
                                                            block.Index,
                                                            tx.Id.ToString(),
                                                            block.Hash.ToString(),
                                                            buy7.shopItem.Price.ToString().Split(" ").FirstOrDefault(),
                                                            buy7.id.ToString(),
                                                            tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                            itemCount);
                                                        Console.WriteLine("7. Equipment");
                                                    }

                                                    if (buy7.shopItem.ItemUsable.ItemType == ItemType.Costume)
                                                    {
                                                        ctCount++;
                                                        WriteSCT(
                                                            buy7.shopItem.Costume,
                                                            buy7.shopItem.SellerAvatarAddress,
                                                            b7.buyerAvatarAddress,
                                                            block.Index,
                                                            tx.Id.ToString(),
                                                            block.Hash.ToString(),
                                                            buy7.shopItem.Price.ToString().Split(" ").FirstOrDefault(),
                                                            buy7.id.ToString(),
                                                            tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                            itemCount);
                                                        Console.WriteLine("7. Costume");
                                                    }

                                                    if (buy7.shopItem.ItemUsable.ItemType == ItemType.Material)
                                                    {
                                                        mtCount++;
                                                        WriteSM(
                                                            (Material)buy7.shopItem.TradableFungibleItem,
                                                            buy7.shopItem.SellerAvatarAddress,
                                                            b7.buyerAvatarAddress,
                                                            block.Index,
                                                            tx.Id.ToString(),
                                                            block.Hash.ToString(),
                                                            buy7.shopItem.Price.ToString().Split(" ").FirstOrDefault(),
                                                            buy7.id.ToString(),
                                                            tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                            itemCount);
                                                        Console.WriteLine("7. Material");
                                                    }

                                                    if (buy7.shopItem.ItemUsable.ItemType == ItemType.Consumable)
                                                    {
                                                        csCount++;
                                                        WriteSC(
                                                            (Consumable)buy7.shopItem.ItemUsable,
                                                            buy7.shopItem.SellerAvatarAddress,
                                                            b7.buyerAvatarAddress,
                                                            block.Index,
                                                            tx.Id.ToString(),
                                                            block.Hash.ToString(),
                                                            buy7.shopItem.Price.ToString().Split(" ").FirstOrDefault(),
                                                            buy7.id.ToString(),
                                                            tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                            itemCount);
                                                        Console.WriteLine("7. Consumable");
                                                    }
                                                }
                                        //     }
                                        // }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }

                                if (tx.Actions.FirstOrDefault()?.InnerAction is Buy8 buy8i)
                                {
                                    try
                                    {
                                        // var aes = _baseChain.ExecuteActions(block);
                                        // foreach (var ae in aes)
                                        // {
                                        //     var action = (PolymorphicAction<ActionBase>)ae.Action;
                                        //     if (action.InnerAction is Buy8 buy8i)
                                        //     {
                                                foreach (var buy in buy8i.purchaseInfos)
                                                {
                                                    var state = ev.OutputStates.GetState(
                                                    Addresses.GetItemAddress(buy.TradableId));
                                                    ITradableItem orderItem =
                                                        (ITradableItem) ItemFactory.Deserialize((Dictionary)state);
                                                    Order order =
                                                        OrderFactory.Deserialize(
                                                            (Dictionary) ev.OutputStates.GetState(
                                                                Order.DeriveAddress(buy.OrderId)));
                                                    var orderReceipt = new OrderReceipt(
                                                        (Dictionary) ev.OutputStates.GetState(
                                                            OrderReceipt.DeriveAddress(buy.OrderId)));
                                                    int itemCount = order is FungibleOrder fungibleOrder
                                                        ? fungibleOrder.ItemCount
                                                        : 1;
                                                    if (orderItem.ItemType == ItemType.Equipment)
                                                    {
                                                        eqCount++;
                                                        WriteSE(
                                                            (Equipment)orderItem,
                                                            buy.SellerAvatarAddress,
                                                            buy8i.buyerAvatarAddress,
                                                            block.Index,
                                                            tx.Id.ToString(),
                                                            block.Hash.ToString(),
                                                            buy.Price.ToString().Split(" ").FirstOrDefault(),
                                                            buy.OrderId.ToString(),
                                                            tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                            itemCount);
                                                        Console.WriteLine("8. Equipment");
                                                    }

                                                    if (orderItem.ItemType == ItemType.Costume)
                                                    {
                                                        ctCount++;
                                                        WriteSCT(
                                                            (Costume)orderItem,
                                                            buy.SellerAvatarAddress,
                                                            buy8i.buyerAvatarAddress,
                                                            block.Index,
                                                            tx.Id.ToString(),
                                                            block.Hash.ToString(),
                                                            buy.Price.ToString().Split(" ").FirstOrDefault(),
                                                            buy.OrderId.ToString(),
                                                            tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                            itemCount);
                                                        Console.WriteLine("8. Costume");
                                                    }

                                                    if (orderItem.ItemType == ItemType.Material)
                                                    {
                                                        mtCount++;
                                                        WriteSM(
                                                            (Material)orderItem,
                                                            buy.SellerAvatarAddress,
                                                            buy8i.buyerAvatarAddress,
                                                            block.Index,
                                                            tx.Id.ToString(),
                                                            block.Hash.ToString(),
                                                            buy.Price.ToString().Split(" ").FirstOrDefault(),
                                                            buy.OrderId.ToString(),
                                                            tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                            itemCount);
                                                        Console.WriteLine("8. Material");
                                                    }

                                                    if (orderItem.ItemType == ItemType.Consumable)
                                                    {
                                                        csCount++;
                                                        WriteSC(
                                                            (Consumable)orderItem,
                                                            buy.SellerAvatarAddress,
                                                            buy8i.buyerAvatarAddress,
                                                            block.Index,
                                                            tx.Id.ToString(),
                                                            block.Hash.ToString(),
                                                            buy.Price.ToString().Split(" ").FirstOrDefault(),
                                                            buy.OrderId.ToString(),
                                                            tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                            itemCount);
                                                        Console.WriteLine("8. Consumable");
                                                    }
                                                }
                                        //     }
                                        // }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }

                                if (tx.Actions.FirstOrDefault()?.InnerAction is Buy9 buy9i)
                                {
                                    try
                                    {
                                        foreach (var buy in buy9i.purchaseInfos)
                                        {
                                            var state = ev.OutputStates.GetState(
                                            Addresses.GetItemAddress(buy.TradableId));
                                            ITradableItem orderItem =
                                                (ITradableItem) ItemFactory.Deserialize((Dictionary)state);
                                            Order order =
                                                OrderFactory.Deserialize(
                                                    (Dictionary) ev.OutputStates.GetState(
                                                        Order.DeriveAddress(buy.OrderId)));
                                            var orderReceipt = new OrderReceipt(
                                                (Dictionary) ev.OutputStates.GetState(
                                                    OrderReceipt.DeriveAddress(buy.OrderId)));
                                            int itemCount = order is FungibleOrder fungibleOrder
                                                ? fungibleOrder.ItemCount
                                                : 1;
                                            if (orderItem.ItemType == ItemType.Equipment)
                                            {
                                                eqCount++;
                                                WriteSE(
                                                    (Equipment)orderItem,
                                                    buy.SellerAvatarAddress,
                                                    buy9i.buyerAvatarAddress,
                                                    block.Index,
                                                    tx.Id.ToString(),
                                                    block.Hash.ToString(),
                                                    buy.Price.ToString().Split(" ").FirstOrDefault(),
                                                    buy.OrderId.ToString(),
                                                    tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                    itemCount);
                                                Console.WriteLine("9. Equipment");
                                            }

                                            if (orderItem.ItemType == ItemType.Costume)
                                            {
                                                ctCount++;
                                                WriteSCT(
                                                    (Costume)orderItem,
                                                    buy.SellerAvatarAddress,
                                                    buy9i.buyerAvatarAddress,
                                                    block.Index,
                                                    tx.Id.ToString(),
                                                    block.Hash.ToString(),
                                                    buy.Price.ToString().Split(" ").FirstOrDefault(),
                                                    buy.OrderId.ToString(),
                                                    tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                    itemCount);
                                                Console.WriteLine("9. Costume");
                                            }

                                            if (orderItem.ItemType == ItemType.Material)
                                            {
                                                mtCount++;
                                                WriteSM(
                                                    (Material)orderItem,
                                                    buy.SellerAvatarAddress,
                                                    buy9i.buyerAvatarAddress,
                                                    block.Index,
                                                    tx.Id.ToString(),
                                                    block.Hash.ToString(),
                                                    buy.Price.ToString().Split(" ").FirstOrDefault(),
                                                    buy.OrderId.ToString(),
                                                    tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                    itemCount);
                                                Console.WriteLine("9. Material");
                                            }

                                            if (orderItem.ItemType == ItemType.Consumable)
                                            {
                                                csCount++;
                                                WriteSC(
                                                    (Consumable)orderItem,
                                                    buy.SellerAvatarAddress,
                                                    buy9i.buyerAvatarAddress,
                                                    block.Index,
                                                    tx.Id.ToString(),
                                                    block.Hash.ToString(),
                                                    buy.Price.ToString().Split(" ").FirstOrDefault(),
                                                    buy.OrderId.ToString(),
                                                    tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                    itemCount);
                                                Console.WriteLine("9. Consumable");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }

                                if (tx.Actions.FirstOrDefault()?.InnerAction is Buy buyi)
                                {
                                    try
                                    {
                                        foreach (var buy in buyi.purchaseInfos)
                                        {
                                            var state = ev.OutputStates.GetState(
                                            Addresses.GetItemAddress(buy.TradableId));
                                            ITradableItem orderItem =
                                                (ITradableItem) ItemFactory.Deserialize((Dictionary)state);
                                            Order order =
                                                OrderFactory.Deserialize(
                                                    (Dictionary) ev.OutputStates.GetState(
                                                        Order.DeriveAddress(buy.OrderId)));
                                            var orderReceipt = new OrderReceipt(
                                                (Dictionary) ev.OutputStates.GetState(
                                                    OrderReceipt.DeriveAddress(buy.OrderId)));
                                            int itemCount = order is FungibleOrder fungibleOrder
                                                ? fungibleOrder.ItemCount
                                                : 1;
                                            if (orderItem.ItemType == ItemType.Equipment)
                                            {
                                                eqCount++;
                                                WriteSE(
                                                    (Equipment)orderItem,
                                                    buy.SellerAvatarAddress,
                                                    buyi.buyerAvatarAddress,
                                                    block.Index,
                                                    tx.Id.ToString(),
                                                    block.Hash.ToString(),
                                                    buy.Price.ToString().Split(" ").FirstOrDefault(),
                                                    buy.OrderId.ToString(),
                                                    tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                    itemCount);
                                                Console.WriteLine("A. Equipment");
                                            }

                                            if (orderItem.ItemType == ItemType.Costume)
                                            {
                                                ctCount++;
                                                WriteSCT(
                                                    (Costume)orderItem,
                                                    buy.SellerAvatarAddress,
                                                    buyi.buyerAvatarAddress,
                                                    block.Index,
                                                    tx.Id.ToString(),
                                                    block.Hash.ToString(),
                                                    buy.Price.ToString().Split(" ").FirstOrDefault(),
                                                    buy.OrderId.ToString(),
                                                    tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                    itemCount);
                                                Console.WriteLine("A. Costume");
                                            }

                                            if (orderItem.ItemType == ItemType.Material)
                                            {
                                                mtCount++;
                                                WriteSM(
                                                    (Material)orderItem,
                                                    buy.SellerAvatarAddress,
                                                    buyi.buyerAvatarAddress,
                                                    block.Index,
                                                    tx.Id.ToString(),
                                                    block.Hash.ToString(),
                                                    buy.Price.ToString().Split(" ").FirstOrDefault(),
                                                    buy.OrderId.ToString(),
                                                    tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                    itemCount);
                                                Console.WriteLine("A. Material");
                                            }

                                            if (orderItem.ItemType == ItemType.Consumable)
                                            {
                                                csCount++;
                                                WriteSC(
                                                    (Consumable)orderItem,
                                                    buy.SellerAvatarAddress,
                                                    buyi.buyerAvatarAddress,
                                                    block.Index,
                                                    tx.Id.ToString(),
                                                    block.Hash.ToString(),
                                                    buy.Price.ToString().Split(" ").FirstOrDefault(),
                                                    buy.OrderId.ToString(),
                                                    tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                                    itemCount);
                                                Console.WriteLine("A. Consumable");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }

                                if (tx.Actions.FirstOrDefault()?.InnerAction is BuyMultiple bm)
                                {
                                    try
                                    {
                                        if (aes == null || aes.FirstOrDefault().InputContext.BlockIndex != block.Index)
                                        {
                                            aes = _baseChain.ExecuteActions(block);
                                        }

                                        foreach (var ae in aes)
                                        {
                                            var action = (PolymorphicAction<ActionBase>)ae.Action;
                                            if (action.InnerAction is Buy bmi)
                                            {
                                                Console.WriteLine(bmi.purchaseInfos.Count());
                                                foreach (var buy in bmi.purchaseInfos)
                                                {
                                                    var state = ev.OutputStates.GetState(
                                                    Addresses.GetItemAddress(buy.TradableId));
                                                    ITradableItem orderItem =
                                                        (ITradableItem) ItemFactory.Deserialize((Dictionary)state);
                                                    if (orderItem.ItemType == ItemType.Material)
                                                    {
                                                        mtCount++;
                                                        Console.WriteLine("BM. Material");
                                                    }
                                            
                                                    if (orderItem.ItemType == ItemType.Equipment)
                                                    {
                                                        eqCount++;
                                                        Console.WriteLine("BM. Equipment");
                                                    }
                                            
                                                    if (orderItem.ItemType == ItemType.Consumable)
                                                    {
                                                        csCount++;
                                                        Console.WriteLine("BM. Consumable");
                                                    }
                                            
                                                    if (orderItem.ItemType == ItemType.Costume)
                                                    {
                                                        ctCount++;
                                                        Console.WriteLine("BM. Costume");
                                                    }
                                            
                                                    Order order =
                                                        OrderFactory.Deserialize(
                                                            (Dictionary) ev.OutputStates.GetState(
                                                                Order.DeriveAddress(buy.OrderId)));
                                                    var orderReceipt = new OrderReceipt(
                                                        (Dictionary) ev.OutputStates.GetState(
                                                            OrderReceipt.DeriveAddress(buy.OrderId)));
                                                    int itemCount = order is FungibleOrder fungibleOrder
                                                        ? fungibleOrder.ItemCount
                                                        : 1;
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }
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
                foreach (var path in _seFiles)
                {
                    BulkInsert(SEDbName, path);
                }

                foreach (var path in _sctFiles)
                {
                    BulkInsert(SCTDbName, path);
                }

                foreach (var path in _smFiles)
                {
                    BulkInsert(SMDbName, path);
                }

                foreach (var path in _scFiles)
                {
                    BulkInsert(SCDbName, path);
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

            _seBulkFile.Flush();
            _seBulkFile.Close();

            _sctBulkFile.Flush();
            _sctBulkFile.Close();

            _smBulkFile.Flush();
            _smBulkFile.Close();

            _scBulkFile.Flush();
            _scBulkFile.Close();
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

            string seFilePath = Path.GetTempFileName();
            _seBulkFile = new StreamWriter(seFilePath);

            string sctFilePath = Path.GetTempFileName();
            _sctBulkFile = new StreamWriter(sctFilePath);

            string smFilePath = Path.GetTempFileName();
            _smBulkFile = new StreamWriter(smFilePath);

            string scFilePath = Path.GetTempFileName();
            _scBulkFile = new StreamWriter(scFilePath);

            _agentFiles.Add(agentFilePath);
            _avatarFiles.Add(avatarFilePath);
            _ccFiles.Add(ccFilePath);
            _ceFiles.Add(ceFilePath);
            _ieFiles.Add(ieFilePath);
            _seFiles.Add(seFilePath);
            _sctFiles.Add(sctFilePath);
            _smFiles.Add(smFilePath);
            _scFiles.Add(scFilePath);
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

        private void WriteSE(
            Equipment equipment,
            Address sellerAvatarAddress,
            Address buyerAvatarAddress,
            long blockIndex,
            string txId,
            string blockHash,
            string price,
            string orderId,
            string timestamp,
            int itemCount)
        {
            try
            {
                _seBulkFile.WriteLine(
                    $"{orderId};" +
                    $"{txId};" +
                    $"{blockIndex};" +
                    $"{blockHash};" +
                    $"{equipment.ItemId.ToString()};" +
                    $"{sellerAvatarAddress.ToString()};" +
                    $"{buyerAvatarAddress.ToString()};" +
                    $"{price};" +
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
                    $"{equipment.UniqueStatType.ToString()};" +
                    $"{itemCount};" +
                    $"{timestamp}"
                );
                Console.WriteLine("Writing SE history in block #{0}", blockIndex);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void WriteSCT(
            Costume costume,
            Address sellerAvatarAddress,
            Address buyerAvatarAddress,
            long blockIndex,
            string txId,
            string blockHash,
            string price,
            string orderId,
            string timestamp,
            int itemCount)
        {
            _sctBulkFile.WriteLine(
                $"{orderId};" +
                $"{txId};" +
                $"{blockIndex};" +
                $"{blockHash};" +
                $"{costume.ItemId.ToString()};" +
                $"{sellerAvatarAddress.ToString()};" +
                $"{buyerAvatarAddress.ToString()};" +
                $"{price};" +
                $"{costume.ItemType.ToString()};" +
                $"{costume.ItemSubType.ToString()};" +
                $"{costume.Id};" +
                $"{costume.ElementalType.ToString()};" +
                $"{costume.Grade};" +
                $"{costume.Equipped};" +
                $"{costume.SpineResourcePath};" +
                $"{costume.RequiredBlockIndex};" +
                $"{costume.NonFungibleId.ToString()};" +
                $"{costume.TradableId.ToString()};" +
                $"{itemCount};" +
                $"{timestamp}"
            );
            Console.WriteLine("Writing SCT history in block #{0}", blockIndex);
        }

        private void WriteSM(
            Material material,
            Address sellerAvatarAddress,
            Address buyerAvatarAddress,
            long blockIndex,
            string txId,
            string blockHash,
            string price,
            string orderId,
            string timestamp,
            int itemCount)
        {
            _smBulkFile.WriteLine(
                $"{orderId};" +
                $"{txId};" +
                $"{blockIndex};" +
                $"{blockHash};" +
                $"{material.ItemId.ToString()};" +
                $"{sellerAvatarAddress.ToString()};" +
                $"{buyerAvatarAddress.ToString()};" +
                $"{price};" +
                $"{material.ItemType.ToString()};" +
                $"{material.ItemSubType.ToString()};" +
                $"{material.Id};" +
                $"{material.ElementalType.ToString()};" +
                $"{material.Grade};" +
                $"{itemCount};" +
                $"{timestamp}"
            );
            Console.WriteLine("Writing SM action in block #{0}", blockIndex);
        }

        private void WriteSC(
            Consumable consumable,
            Address sellerAvatarAddress,
            Address buyerAvatarAddress,
            long blockIndex,
            string txId,
            string blockHash,
            string price,
            string orderId,
            string timestamp,
            int itemCount)
        {
            _scBulkFile.WriteLine(
                $"{orderId};" +
                $"{txId};" +
                $"{blockIndex};" +
                $"{blockHash};" +
                $"{consumable.ItemId.ToString()};" +
                $"{sellerAvatarAddress.ToString()};" +
                $"{buyerAvatarAddress.ToString()};" +
                $"{price};" +
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
                $"{consumable.MainStat.ToString()};" +
                $"{itemCount};" +
                $"{timestamp}"
            );
            Console.WriteLine("Writing SC action in block #{0}", blockIndex);
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
