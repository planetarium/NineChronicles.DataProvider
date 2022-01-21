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
        private const string AgentDbName = "agent";
        private const string AvatarDbName = "avatar";
        private const string HasDbName = "hack_and_slash";
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain<NCAction> _baseChain;
        private StreamWriter _agentBulkFile;
        private StreamWriter _avatarBulkFile;
        private StreamWriter _hasBulkFile;
        private List<string> _agentList;
        private List<string> _avatarList;
        private List<string> _agentFiles;
        private List<string> _avatarFiles;
        private List<string> _hasFiles;

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
            else if (rocksdbStoreType == "mono")
            {
                _baseStore = new MonoRocksDBStore(storePath);
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
            RocksDBKeyValueStore baseStateRootKeyValueStore = new RocksDBKeyValueStore(Path.Combine(storePath, "state_hashes"));
            RocksDBKeyValueStore baseStateKeyValueStore = new RocksDBKeyValueStore(Path.Combine(storePath, "states"));
            TrieStateStore baseStateStore =
                new TrieStateStore(baseStateKeyValueStore, baseStateRootKeyValueStore);

            // Setup block policy
            const int minimumDifficulty = 5000000, maximumTransactions = 100;
            IStagePolicy<NCAction> stagePolicy = new VolatileStagePolicy<NCAction>();
            LogEventLevel logLevel = LogEventLevel.Debug;
            var blockPolicySource = new BlockPolicySource(Log.Logger, logLevel);
            IBlockPolicy<NCAction> blockPolicy = blockPolicySource.GetPolicy(minimumDifficulty, maximumTransactions);

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

            BlockHash[] blockHashes = limit < 0
                ? _baseChain.BlockHashes.SkipWhile((_, i) => i < height + limit).ToArray()
                : _baseChain.BlockHashes.Skip(offset ?? 0).Take(limit ?? (int)height).ToArray();
            Block<NCAction>[] blocks = blockHashes.Select(h => _baseStore.GetBlock<NCAction>(h)).ToArray();
            Console.WriteLine(
                "Finished setting up RocksDBStore. Migrating {0} blocks: {1}-{2} (inclusive).",
                blockHashes.Length,
                blockHashes[0],
                blockHashes.Last()
            );

            Console.WriteLine("Start migration.");

            // files to store bulk file paths (new file created every 10000 blocks for bulk load performance)
            _agentFiles = new List<string>();
            _avatarFiles = new List<string>();
            _hasFiles = new List<string>();

            // lists to keep track of inserted addresses to minimize duplicates
            _agentList = new List<string>();
            _avatarList = new List<string>();

            CreateBulkFiles();
            try
            {
                Task<List<ActionEvaluation>>[] taskArray = new Task<List<ActionEvaluation>>[blocks.Length];
                for (int i = 0; i < taskArray.Length; i++)
                {
                    var block = blocks[i];
                    taskArray[i] = Task.Factory.StartNew(() => EvaluateBlock(block));
                }

                Task.WaitAll(taskArray);
                var count = 0;
                foreach (var task in taskArray)
                {
                    count += 1;
                    Console.WriteLine("Preparing block {0}/{1}", count, taskArray.Length);
                    if (task.Result is { } data)
                    {
                        foreach (var ae in data)
                        {
                            if (ae.Action is PolymorphicAction<ActionBase> action)
                            {
                                // avatarNames will be stored as "N/A" for optimzation
                                if (action.InnerAction is HackAndSlash2 hasAction2)
                                {
                                    Address signer = ae.InputContext.Signer;
                                    WriteHackAndSlash(
                                        ae.InputContext.BlockIndex,
                                        signer,
                                        hasAction2.avatarAddress,
                                        "N/A",
                                        hasAction2.stageId,
                                        hasAction2.Result is { IsClear: true });
                                }

                                if (ae.Action is HackAndSlash3 hasAction3)
                                {
                                    Address signer = ae.InputContext.Signer;
                                    WriteHackAndSlash(
                                        ae.InputContext.BlockIndex,
                                        signer,
                                        hasAction3.avatarAddress,
                                        "N/A",
                                        hasAction3.stageId,
                                        hasAction3.Result is { IsClear: true });
                                }

                                if (ae.Action is HackAndSlash4 hasAction4)
                                {
                                    Address signer = ae.InputContext.Signer;
                                    WriteHackAndSlash(
                                        ae.InputContext.BlockIndex,
                                        signer,
                                        hasAction4.avatarAddress,
                                        "N/A",
                                        hasAction4.stageId,
                                        hasAction4.Result is { IsClear: true });
                                }
                            }
                        }
                    }

                    // create new bulk files for every 10000 blocks
                    if (count > 0 && count % 10000 == 0)
                    {
                        FlushBulkFiles();
                        CreateBulkFiles();
                        Console.WriteLine("Finished block data preparation at {0}/{1} blocks.", count, taskArray.Length);
                    }

                    // flush final bulk files when prep is finished
                    if (count == taskArray.Length)
                    {
                        FlushBulkFiles();
                        Console.WriteLine("Finished block data preparation at {0}/{1} blocks.", count, taskArray.Length);
                    }
                }

                DateTimeOffset postDataPrep = DateTimeOffset.Now;
                Console.WriteLine("Data Preparation Complete! Time Elapsed: {0}", postDataPrep - start);

                foreach (var path in _agentFiles)
                {
                    BulkInsert(AgentDbName, path);
                }

                foreach (var path in _avatarFiles)
                {
                    BulkInsert(AvatarDbName, path);
                }

                foreach (var path in _hasFiles)
                {
                    BulkInsert(HasDbName, path);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            DateTimeOffset end = DateTimeOffset.UtcNow;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
        }

        private List<ActionEvaluation> EvaluateBlock(Block<NCAction> block)
        {
            Console.WriteLine("Evaluating block #{0}", block.Index);
            var evList = block.Evaluate(
                DateTimeOffset.Now,
                address => _baseChain.GetState(address, block.Hash),
                (address, currency) =>
                    _baseChain.GetBalance(address, currency, block.Hash)).ToList();
            return evList;
        }

        private void FlushBulkFiles()
        {
            _agentBulkFile.Flush();
            _agentBulkFile.Close();

            _avatarBulkFile.Flush();
            _avatarBulkFile.Close();

            _hasBulkFile.Flush();
            _hasBulkFile.Close();
        }

        private void CreateBulkFiles()
        {
            string agentFilePath = Path.GetTempFileName();
            _agentBulkFile = new StreamWriter(agentFilePath);

            string avatarFilePath = Path.GetTempFileName();
            _avatarBulkFile = new StreamWriter(avatarFilePath);

            string hasFilePath = Path.GetTempFileName();
            _hasBulkFile = new StreamWriter(hasFilePath);

            _agentFiles.Add(agentFilePath);
            _avatarFiles.Add(avatarFilePath);
            _hasFiles.Add(hasFilePath);
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
                    LineTerminator = "\n",
                    FieldTerminator = ";",
                    Local = true,
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

        private void WriteHackAndSlash(
            long blockIndex,
            Address agentAddress,
            Address avatarAddress,
            string avatarName,
            int stageId,
            bool isClear)
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
                    $"{avatarName ?? "N/A"}");
                _avatarList.Add(avatarAddress.ToString());
            }

            _hasBulkFile.WriteLine(
                $"{avatarAddress.ToString()};" +
                $"{agentAddress.ToString()};" +
                $"{stageId};" +
                $"{isClear};" +
                $"{stageId > 10000000}");
            Console.WriteLine("Writing HackAndSlash action in block #{0}", blockIndex);
        }
    }
}
