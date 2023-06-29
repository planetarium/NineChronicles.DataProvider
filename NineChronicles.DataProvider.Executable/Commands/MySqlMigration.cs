namespace NineChronicles.DataProvider.Executable.Commands
{
    using System;
    using System.IO;
    using System.Linq;
    using Cocona;
    using Libplanet.Action;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.RocksDBStore;
    using Libplanet.Store;
    using Nekoyume.Action.Loader;
    using Nekoyume.Blockchain.Policy;
    using Serilog;
    using Serilog.Events;

    public class MySqlMigration
    {
        private IStore _baseStore;
        private BlockChain _baseChain;

        [Command(Description = "Migrate action data in rocksdb store to mysql db.")]
        public void Migration(
            [Option('o', Description = "Rocksdb path to migrate.")]
            string storePath,
            [Option('t', Description = "Rocksdb path to migrate.")]
            string targetStorePath,
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
            Console.WriteLine("Setting up RocksDBStore...");
            _baseStore = new RocksDBStore(
                storePath,
                dbConnectionCacheSize: 10000);
            var targetStore = new RocksDBStore(
                targetStorePath,
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
            RocksDBKeyValueStore targetStateKeyValueStore = new RocksDBKeyValueStore(Path.Combine(targetStorePath, "states"));
            TrieStateStore targetStateStore =
                new TrieStateStore(targetStateKeyValueStore);
            var targetBlockChainStates = new BlockChainStates(targetStore, targetStateStore);
            var targetActionEvaluator = new ActionEvaluator(
                _ => blockPolicy.BlockAction,
                targetBlockChainStates,
                new NCActionLoader(),
                null);
            var targetChain = new BlockChain(blockPolicy, stagePolicy, targetStore, targetStateStore, genesis, targetBlockChainStates, targetActionEvaluator);

            Console.WriteLine("Start migration.");

            try
            {
                int totalCount = limit ?? (int)_baseStore.CountBlocks();
                int remainingCount = totalCount;

                foreach (var item in
                        targetStore.IterateIndexes(targetChain.Id, offset ?? 0, limit).Select((value, i) => new { i, value }))
                    {
                        var block = targetStore.GetBlock(item.value);
                        _baseStore.PutBlock(block);
                        Console.WriteLine($"Evaluating Block: #{block.Index} Transaction Count: {block.Transactions.Count} {item.i}/{remainingCount}");
                        _baseChain.EvaluateBlock(block);
                    }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            DateTimeOffset end = DateTimeOffset.UtcNow;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
        }
    }
}
