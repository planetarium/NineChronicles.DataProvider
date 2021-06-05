namespace NineChronicles.DataProvider.Tools.SubCommand
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Bencodex.Types;
    using Cocona;
    using Libplanet;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.RocksDBStore;
    using Libplanet.Store;
    using MySqlConnector;
    using Nekoyume.Action;
    using Nekoyume.BlockChain;
    using Nekoyume.Model.State;
    using Serilog;
    using Serilog.Events;
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

    public class MySqlMigration
    {
        private const string AgentDbName = "Agents";
        private const string AvatarDbName = "Avatars";
        private const string HasDbName = "HackAndSlashes";
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain<NCAction> _baseChain;
        private BlockChain<NCAction> _newChain;
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
                _baseStore = new MonoRocksDBStore(storePath, 100000, 100000);
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

            // Setup new store
            var newPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            RocksDBStore newStore = new RocksDBStore(newPath);
            RocksDBKeyValueStore newStateRootKeyValueStore = new RocksDBKeyValueStore(Path.Combine(newPath, "state_hashes"));
            RocksDBKeyValueStore newStateKeyValueStore = new RocksDBKeyValueStore(Path.Combine(newPath, "states"));
            TrieStateStore newStateStore =
                new TrieStateStore(newStateKeyValueStore, newStateRootKeyValueStore);

            // Setup block policy
            const int minimumDifficulty = 5000000, maximumTransactions = 100;
            IStagePolicy<NCAction> stagePolicy = new VolatileStagePolicy<NCAction>();
            LogEventLevel logLevel = LogEventLevel.Debug;
            var blockPolicySource = new BlockPolicySource(Log.Logger, logLevel);
            IBlockPolicy<NCAction> blockPolicy = blockPolicySource.GetPolicy(minimumDifficulty, maximumTransactions);

            // Setup base chain & new chain
            Block<NCAction> genesis = _baseStore.GetBlock<NCAction>(gHash);
            _baseChain = new BlockChain<NCAction>(blockPolicy, stagePolicy, _baseStore, baseStateStore, genesis);
            if (blockPolicy is BlockPolicy bp && _baseChain.GetState(AuthorizedMinersState.Address) is Dictionary ams)
            {
                bp.AuthorizedMinersState = new AuthorizedMinersState(ams);
            }

            _newChain = new BlockChain<NCAction>(blockPolicy, stagePolicy, newStore, newStateStore, genesis);

            Console.WriteLine("Start migration.");

            _agentFiles = new List<string>();
            _avatarFiles = new List<string>();
            _hasFiles = new List<string>();

            _agentList = new List<string>();
            _avatarList = new List<string>();

            CreateBulkFiles();

            try
            {
                int totalCount = limit ?? (int)totalLength;
                foreach (var item in
                    _baseStore.IterateIndexes(_baseChain.Id, 0, limit).Select((value, i) => new { i, value }))
                {
                    Console.WriteLine($"Block progress: {item.i}/{totalCount}");
                    var block = _baseStore.GetBlock<NCAction>(item.value);
                    if (block.Index == 0)
                    {
                        continue;
                    }

                    Console.WriteLine("Migrating block #{0}", block.Index);
                    _newChain.Append(block);
                    if (block.Index < (offset ?? 0))
                    {
                        continue;
                    }

                    if (block.Transactions.Count > 0)
                    {
                        foreach (var tx in block.Transactions)
                        {
                            if (tx.Actions[0].InnerAction is HackAndSlash2 hasAction2)
                            {
                                WriteHackAndSlash(
                                    hasAction2.Id,
                                    block.Index,
                                    tx.Signer,
                                    hasAction2.avatarAddress,
                                    "N/A",
                                    hasAction2.stageId,
                                    hasAction2.Result is { IsClear: true });
                            }

                            if (tx.Actions[0].InnerAction is HackAndSlash3 hasAction3)
                            {
                                WriteHackAndSlash(
                                    hasAction3.Id,
                                    block.Index,
                                    tx.Signer,
                                    hasAction3.avatarAddress,
                                    "N/A",
                                    hasAction3.stageId,
                                    hasAction3.Result is { IsClear: true });
                            }

                            if (tx.Actions[0].InnerAction is HackAndSlash4 hasAction4)
                            {
                                WriteHackAndSlash(
                                    hasAction4.Id,
                                    block.Index,
                                    tx.Signer,
                                    hasAction4.avatarAddress,
                                    "N/A",
                                    hasAction4.stageId,
                                    hasAction4.Result is { IsClear: true });
                            }
                        }
                    }

                    if ((block.Index > 0 && block.Index % 10000 == 0) || block.Index == limit - 1)
                    {
                        FlushBulkFiles();
                        CreateBulkFiles();
                        Console.WriteLine("Finished block data preparation at block {0}.", block.Index);
                    }

                    if (block.Index > 0 && block.Index % 5000 == 0)
                    {
                        Thread.Sleep(1000);

                        // ReSharper disable once S1215
                        GC.Collect();
                    }
                }

                FlushBulkFiles();

                Console.WriteLine("Data Preparation Complete! Time Elapsed: {0}", DateTimeOffset.Now - start);

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
                Console.WriteLine("Error: {0}", e.Message);
            }

            Console.WriteLine("Migration Complete! Time Elapsed: {0}", DateTimeOffset.UtcNow - start);
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
            string agentFilePath = Path.GetTempPath() + "agents-" + Guid.NewGuid().ToString() + ".tmp";
            _agentBulkFile = new StreamWriter(agentFilePath);

            string avatarFilePath = Path.GetTempPath() + "avatars-" + Guid.NewGuid().ToString() + ".tmp";
            _avatarBulkFile = new StreamWriter(avatarFilePath);

            string hasFilePath = Path.GetTempPath() + "has-" + Guid.NewGuid().ToString() + ".tmp";
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
                Console.WriteLine("Time elapsed: {0}", DateTimeOffset.Now - start);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine($"Bulk load to {tableName} failed.");
            }
        }

        private void WriteHackAndSlash(
            Guid id,
            long blockIndex,
            Address agentAddress,
            Address avatarAddress,
            string avatarName,
            int stageId,
            bool isClear)
        {
            if (!_agentList.Contains(agentAddress.ToString()))
            {
                _agentBulkFile.WriteLine(
                    $"{agentAddress.ToString()};");
                _agentList.Add(agentAddress.ToString());
            }

            if (!_avatarList.Contains(avatarAddress.ToString()))
            {
                _avatarBulkFile.WriteLine(
                    $"{avatarAddress.ToString()};" +
                    $"{agentAddress.ToString()};" +
                    $"{avatarName ?? "N/A"}");
                _avatarList.Add(avatarAddress.ToString());
            }

            _hasBulkFile.WriteLine(
                $"{id.ToString()};" +
                $"{avatarAddress.ToString()};" +
                $"{agentAddress.ToString()};" +
                $"{stageId};" +
                $"{(isClear ? 1 : 0)};" +
                $"{(stageId > 10000000 ? 1 : 0)};" +
                $"{blockIndex.ToString()};");
            Console.WriteLine("Write HackAndSlash action in block #{0}", blockIndex);
        }
    }
}
