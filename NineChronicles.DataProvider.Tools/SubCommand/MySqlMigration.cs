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
        private IStore _baseStore;
        private BlockChain<NCAction> _baseChain;
        private StreamWriter _blockBulkFile;
        private StreamWriter _txBulkFile;
        private List<string> _blockFiles;
        private List<string> _txFiles;

        [Command(Description = "Migrate action data in rocksdb store to mysql db.")]
        public void Migration(
            [Option('o', Description = "Rocksdb path to migrate.")]
            string storePath,
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
            _blockFiles = new List<string>();
            _txFiles = new List<string>();

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

                    var count = remainingCount;
                    var idx = offsetIdx;

                    foreach (var item in
                            _baseStore.IterateIndexes(_baseChain.Id, offset + idx ?? 0 + idx, limitInterval)
                                .Select((value, i) => new { i, value }))
                    {
                        try
                        {
                            var block = _baseStore.GetBlock<NCAction>(item.value);
                            Console.WriteLine("Checking {0}/{1} #{2}", item.i, count, block.Index);
                            _blockBulkFile.WriteLine(
                                $"{block.Index};" +
                                $"{block.Hash.ToString()};" +
                                $"{block.Miner.ToString()};" +
                                $"{block.Difficulty};" +
                                $"{block.Nonce.ToString()};" +
                                $"{block.PreviousHash.ToString()};" +
                                $"{block.ProtocolVersion};" +
                                $"{block.PublicKey!};" +
                                $"{block.StateRootHash.ToString()};" +
                                $"{block.TotalDifficulty};" +
                                $"{block.Transactions.Count};" +
                                $"{block.TxHash.ToString()};" +
                                $"{block.Timestamp.UtcDateTime:o}");
                            Console.WriteLine("Checking Done {0}/{1} #{2}", item.i, count, block.Index);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error Checking {0}/{1} #{2}", item.i, count, item.value);
                            _txBulkFile.WriteLine(
                                $"{item.i};" +
                                $"{item.value}");
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
                }

                FlushBulkFiles();
                DateTimeOffset postDataPrep = DateTimeOffset.Now;
                Console.WriteLine("Data Preparation Complete! Time Elapsed: {0}", postDataPrep - start);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            DateTimeOffset end = DateTimeOffset.UtcNow;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
            foreach (var path in _blockFiles)
            {
                Console.WriteLine("Block File: {0}", path);
            }

            foreach (var path in _txFiles)
            {
                Console.WriteLine("Error File: {0}", path);
            }
        }

        private void FlushBulkFiles()
        {
            _blockBulkFile.Flush();
            _blockBulkFile.Close();

            _txBulkFile.Flush();
            _txBulkFile.Close();
        }

        private void CreateBulkFiles()
        {
            string blockFilePath = Path.GetTempFileName();
            _blockBulkFile = new StreamWriter(blockFilePath);

            string txFilePath = Path.GetTempFileName();
            _txBulkFile = new StreamWriter(txFilePath);

            _blockFiles.Add(blockFilePath);
            _txFiles.Add(txFilePath);
        }
    }
}
