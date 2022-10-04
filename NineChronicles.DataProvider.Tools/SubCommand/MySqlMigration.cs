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
        private const string BlockDbName = "Blocks";
        private const string TxDbName = "Transactions";
        private string _connectionString;
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
            var avatarList = new List<string>();
            try
            {
                int totalCount = limit ?? (int)_baseStore.CountBlocks();
                int remainingCount = totalCount;
                int offsetIdx = 0;
                while (remainingCount > 0)
                {
                    int interval = 100000;
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
                        var block = _baseStore.GetBlock<NCAction>(item.value);
                        Console.WriteLine("Migrating {0}/{1} #{2}", item.i, count, block.Index);
                        foreach (var tx in block.Transactions)
                        {
                            string actionType = null;
                            if (tx.CustomActions!.Count == 0)
                            {
                                actionType = string.Empty;
                            }
                            else
                            {
                                actionType = tx.CustomActions[0].InnerAction.ToString()!.Split(".").Last();
                                if (tx.CustomActions[0].InnerAction is Raid raid)
                                {
                                    if (!avatarList.Contains(raid.AvatarAddress.ToHex()))
                                    {
                                        avatarList.Add(raid.AvatarAddress.ToHex());
                                    }
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
                foreach (var avatar in avatarList)
                {
                    Console.WriteLine(avatar);
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
