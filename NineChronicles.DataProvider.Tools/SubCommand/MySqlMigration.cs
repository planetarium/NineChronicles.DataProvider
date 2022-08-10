using Nekoyume.Model.State;

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
        private const string HSWDbName = "HackAndSlashSweeps";
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain<NCAction> _baseChain;
        private StreamWriter _agentBulkFile;
        private StreamWriter _avatarBulkFile;
        private StreamWriter _hswBulkFile;
        private List<string> _agentList;
        private List<string> _avatarList;
        private List<string> _agentFiles;
        private List<string> _avatarFiles;
        private List<string> _hswFiles;

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
            _agentFiles = new List<string>();
            _avatarFiles = new List<string>();
            _hswFiles = new List<string>();

            // lists to keep track of inserted addresses to minimize duplicates
            _agentList = new List<string>();
            _avatarList = new List<string>();

            CreateBulkFiles();
            try
            {
                int totalCount = limit ?? (int)_baseStore.CountBlocks();
                int remainingCount = totalCount;
                int offsetIdx = 0;
                var tipHash = _baseStore.IndexBlockHash(_baseChain.Id, _baseChain.Tip.Index);
                var tip = _baseStore.GetBlock<NCAction>(blockPolicy.GetHashAlgorithm, (BlockHash)tipHash);
                var exec = _baseChain.ExecuteActions(tip);
                var ev = exec.First();
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
                    IReadOnlyList<ActionEvaluation> aes = null;

                    foreach (var item in
                            _baseStore.IterateIndexes(_baseChain.Id, offset + idx ?? 0 + idx, limitInterval)
                                .Select((value, i) => new { i, value }))
                    {
                        var block = _baseStore.GetBlock<NCAction>(blockPolicy.GetHashAlgorithm, item.value);
                        Console.WriteLine("Migrating {0}/{1} #{2}", item.i, count, block.Index);

                        foreach (var tx in block.Transactions)
                        {
                            if (tx.Actions.FirstOrDefault()?.InnerAction is HackAndSlashSweep hasSweep)
                            {
                                try
                                {
                                    AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(hasSweep.avatarAddress);
                                    bool isClear = avatarState.stageMap.ContainsKey(hasSweep.stageId);
                                    _hswBulkFile.WriteLine(
                                        $"{hasSweep.Id.ToString()};" +
                                        $"{tx.Signer.ToString()};" +
                                        $"{hasSweep.avatarAddress.ToString()};" +
                                        $"{hasSweep.worldId};" +
                                        $"{hasSweep.stageId};" +
                                        $"{hasSweep.apStoneCount};" +
                                        $"{hasSweep.actionPoint};" +
                                        $"{hasSweep.costumes.Count};" +
                                        $"{hasSweep.equipments.Count};" +
                                        $"{isClear};" +
                                        $"{hasSweep.stageId > 10000000};" +
                                        $"{block.Index};" +
                                        $"{tx.Timestamp.UtcDateTime:o}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.Actions.FirstOrDefault()?.InnerAction is HackAndSlashSweep1 hasSweep1)
                            {
                                try
                                {
                                    AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(hasSweep1.avatarAddress);
                                    bool isClear = avatarState.stageMap.ContainsKey(hasSweep1.stageId);
                                    _hswBulkFile.WriteLine(
                                        $"{hasSweep1.Id.ToString()};" +
                                        $"{tx.Signer.ToString()};" +
                                        $"{hasSweep1.avatarAddress.ToString()};" +
                                        $"{hasSweep1.worldId};" +
                                        $"{hasSweep1.stageId};" +
                                        $"{hasSweep1.apStoneCount};" +
                                        $"{0};" +
                                        $"{0};" +
                                        $"{0};" +
                                        $"{isClear};" +
                                        $"{hasSweep1.stageId > 10000000};" +
                                        $"{block.Index};" +
                                        $"{tx.Timestamp.UtcDateTime:o}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.Actions.FirstOrDefault()?.InnerAction is HackAndSlashSweep2 hasSweep2)
                            {
                                try
                                {
                                    AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(hasSweep2.avatarAddress);
                                    bool isClear = avatarState.stageMap.ContainsKey(hasSweep2.stageId);
                                    _hswBulkFile.WriteLine(
                                        $"{hasSweep2.Id.ToString()};" +
                                        $"{tx.Signer.ToString()};" +
                                        $"{hasSweep2.avatarAddress.ToString()};" +
                                        $"{hasSweep2.worldId};" +
                                        $"{hasSweep2.stageId};" +
                                        $"{hasSweep2.apStoneCount};" +
                                        $"{0};" +
                                        $"{0};" +
                                        $"{0};" +
                                        $"{isClear};" +
                                        $"{hasSweep2.stageId > 10000000};" +
                                        $"{block.Index};" +
                                        $"{tx.Timestamp.UtcDateTime:o}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.Actions.FirstOrDefault()?.InnerAction is HackAndSlashSweep3 hasSweep3)
                            {
                                try
                                {
                                    AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(hasSweep3.avatarAddress);
                                    bool isClear = avatarState.stageMap.ContainsKey(hasSweep3.stageId);
                                    _hswBulkFile.WriteLine(
                                        $"{hasSweep3.Id.ToString()};" +
                                        $"{tx.Signer.ToString()};" +
                                        $"{hasSweep3.avatarAddress.ToString()};" +
                                        $"{hasSweep3.worldId};" +
                                        $"{hasSweep3.stageId};" +
                                        $"{hasSweep3.apStoneCount};" +
                                        $"{hasSweep3.actionPoint};" +
                                        $"{hasSweep3.costumes.Count};" +
                                        $"{hasSweep3.equipments.Count};" +
                                        $"{isClear};" +
                                        $"{hasSweep3.stageId > 10000000};" +
                                        $"{block.Index};" +
                                        $"{tx.Timestamp.UtcDateTime:o}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.Actions.FirstOrDefault()?.InnerAction is HackAndSlashSweep4 hasSweep4)
                            {
                                try
                                {
                                    AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(hasSweep4.avatarAddress);
                                    bool isClear = avatarState.stageMap.ContainsKey(hasSweep4.stageId);
                                    _hswBulkFile.WriteLine(
                                        $"{hasSweep4.Id.ToString()};" +
                                        $"{tx.Signer.ToString()};" +
                                        $"{hasSweep4.avatarAddress.ToString()};" +
                                        $"{hasSweep4.worldId};" +
                                        $"{hasSweep4.stageId};" +
                                        $"{hasSweep4.apStoneCount};" +
                                        $"{hasSweep4.actionPoint};" +
                                        $"{hasSweep4.costumes.Count};" +
                                        $"{hasSweep4.equipments.Count};" +
                                        $"{isClear};" +
                                        $"{hasSweep4.stageId > 10000000};" +
                                        $"{block.Index};" +
                                        $"{tx.Timestamp.UtcDateTime:o}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }
                        }

                        Console.WriteLine("Migrating Done {0}/{1} #{2}", item.i, count, block.Index);
                    }

                    if (interval < remainingCount)
                    {
                        remainingCount -= interval;
                        offsetIdx += interval;
                        FlushBulkFiles();

                        foreach (var path in _hswFiles)
                        {
                            BulkInsert(HSWDbName, path);
                        }

                        _hswFiles.RemoveAt(0);
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

                foreach (var path in _hswFiles)
                {
                    BulkInsert(HSWDbName, path);
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

            _hswBulkFile.Flush();
            _hswBulkFile.Close();
        }

        private void CreateBulkFiles()
        {
            string agentFilePath = Path.GetTempFileName();
            _agentBulkFile = new StreamWriter(agentFilePath);

            string avatarFilePath = Path.GetTempFileName();
            _avatarBulkFile = new StreamWriter(avatarFilePath);

            string hswFilePath = Path.GetTempFileName();
            _hswBulkFile = new StreamWriter(hswFilePath);

            _agentFiles.Add(agentFilePath);
            _avatarFiles.Add(avatarFilePath);
            _hswFiles.Add(hswFilePath);
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
