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
    using Libplanet.Crypto;
    using Libplanet.RocksDBStore;
    using Libplanet.Store;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using MySqlConnector;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Loader;
    using Nekoyume.Battle;
    using Nekoyume.Blockchain.Policy;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using NineChronicles.Headless;
    using Serilog;
    using Serilog.Events;

    public class MySqlMigration
    {
        private const string BlockDbName = "Blocks";
        private const string TxDbName = "Transactions";
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain _baseChain;
        private StreamWriter _blockBulkFile;
        private StreamWriter _txBulkFile;
        private List<string> _blockFiles;
        private List<string> _txFiles;

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
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            LogEventLevel logLevel = LogEventLevel.Debug;
            var blockPolicySource = new BlockPolicySource();
            IBlockPolicy blockPolicy = blockPolicySource.GetPolicy();

            // Setup base chain & new chain
            Block genesis = _baseStore.GetBlock(gHash);
            var blockChainStates = new BlockChainStates(_baseStore, baseStateStore);
            var actionEvaluator = new ActionEvaluator(
                _ => blockPolicy.BlockAction,
                blockChainStates,
                new NCActionLoader());
            _baseChain = new BlockChain(blockPolicy, stagePolicy, _baseStore, baseStateStore, genesis, blockChainStates, actionEvaluator);

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
                        var block = _baseStore.GetBlock(item.value);
                        Console.WriteLine("Migrating {0}/{1} #{2}", item.i, count, block.Index);
                        _blockBulkFile.WriteLine(
                            $"{block.Index};" +
                            $"{block.Hash.ToString()};" +
                            $"{block.Miner.ToString()};" +
                            $"{0};" +
                            "Empty;" +
                            $"{block.PreviousHash.ToString()};" +
                            $"{block.ProtocolVersion};" +
                            $"{block.PublicKey!};" +
                            $"{block.StateRootHash.ToString()};" +
                            $"{0};" +
                            $"{block.Transactions.Count};" +
                            $"{block.TxHash.ToString()};" +
                            $"{block.Timestamp.UtcDateTime:o}");
                        foreach (var tx in block.Transactions)
                        {
                            string actionType = null;
                            if (tx.Actions.Actions.Count == 0)
                            {
                                actionType = string.Empty;
                            }
                            else
                            {
                                actionType = NCActionUtils.ToAction(tx.Actions.FirstOrDefault()!)
                                    .ToString()!.Split('.').LastOrDefault()!.Replace(">", string.Empty);
                            }

                            _txBulkFile.WriteLine(
                                $"{block.Index};" +
                                $"{block.Hash.ToString()};" +
                                $"{tx.Id.ToString()};" +
                                $"{tx.Signer.ToString()};" +
                                $"{actionType};" +
                                $"{tx.Nonce};" +
                                $"{tx.PublicKey};" +
                                $"{tx.UpdatedAddresses.Count};" +
                                $"{tx.Timestamp.UtcDateTime:yyyy-MM-dd};" +
                                $"{tx.Timestamp.UtcDateTime:o}");
                        }

                        Console.WriteLine("Migrating Done {0}/{1} #{2}", item.i, count, block.Index);
                    }

                    if (interval < remainingCount)
                    {
                        remainingCount -= interval;
                        offsetIdx += interval;
                        FlushBulkFiles();
                        foreach (var path in _blockFiles)
                        {
                            BulkInsert(BlockDbName, path);
                        }

                        foreach (var path in _txFiles)
                        {
                            BulkInsert(TxDbName, path);
                        }

                        _blockFiles.RemoveAt(0);
                        _txFiles.RemoveAt(0);
                        CreateBulkFiles();
                    }
                    else
                    {
                        remainingCount = 0;
                        offsetIdx += remainingCount;
                        FlushBulkFiles();
                        CreateBulkFiles();
                    }
                }

                FlushBulkFiles();
                DateTimeOffset postDataPrep = DateTimeOffset.Now;
                Console.WriteLine("Data Preparation Complete! Time Elapsed: {0}", postDataPrep - start);

                foreach (var path in _blockFiles)
                {
                    BulkInsert(BlockDbName, path);
                }

                foreach (var path in _txFiles)
                {
                    BulkInsert(TxDbName, path);
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
                    LineTerminator = Environment.OSVersion.VersionString.Contains("Win") ? "\r\n" : "\n",
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
    }
}
