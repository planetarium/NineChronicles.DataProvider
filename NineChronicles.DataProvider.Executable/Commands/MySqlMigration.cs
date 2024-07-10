namespace NineChronicles.DataProvider.Executable.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Cocona;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.RocksDBStore;
    using Libplanet.Store;
    using Libplanet.Types.Blocks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.Extensions.DependencyInjection;
    using MySqlConnector;
    using Nekoyume.Action;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Action.Loader;
    using Nekoyume.Blockchain.Policy;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.DataRendering;
    using NineChronicles.DataProvider.DataRendering.AdventureBoss;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.DataProvider.Store.Models;
    using NineChronicles.DataProvider.Store.Models.AdventureBoss;
    using Serilog;

    public class MySqlMigration
    {
        private readonly List<string> _avatarCheck = new ();
        private readonly List<string> _agentCheck = new ();
        private readonly List<BlockModel> _blockList = new ();
        private readonly List<TransactionModel> _txList = new ();
        private readonly List<AgentModel> _agentList = new ();
        private readonly List<AvatarModel> _avatarList = new ();
        private readonly List<AdventureBossSeasonModel> _adventureBossSeasonList = new ();
        private readonly List<AdventureBossWantedModel> _adventureBossWantedList = new ();
        private readonly List<AdventureBossChallengeModel> _adventureBossChallengeList = new ();
        private readonly List<AdventureBossRushModel> _adventureBossRushList = new ();
        private readonly List<AdventureBossUnlockFloorModel> _adventureBossUnlockFloorList = new ();
        private readonly List<AdventureBossClaimRewardModel> _adventureBossClaimRewardList = new ();
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain _baseChain;

        // lists to keep track of inserted addresses to minimize duplicates
        private MySqlStore _mySqlStore;
        private BlockHash _blockHash;
        private long _blockIndex;
        private DateTimeOffset _blockTimeOffset;

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
            var blockPolicySource = new BlockPolicySource();
            IBlockPolicy blockPolicy = blockPolicySource.GetPolicy();

            // Setup base chain & new chain
            Block genesis = _baseStore.GetBlock(gHash);
            var blockChainStates = new BlockChainStates(_baseStore, baseStateStore);
            var actionEvaluator = new ActionEvaluator(
                _ => blockPolicy.BlockAction,
                baseStateStore,
                new NCActionLoader());
            _baseChain = new BlockChain(
                blockPolicy, stagePolicy, _baseStore, baseStateStore, genesis, blockChainStates, actionEvaluator
            );

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

            try
            {
                int totalCount = limit ?? (int)_baseStore.CountBlocks();
                int remainingCount = totalCount;
                int offsetIdx = 0;

                while (remainingCount > 0)
                {
                    int interval = 100;
                    int limitInterval;
                    Task<List<ICommittedActionEvaluation>>[] taskArray;
                    if (interval < remainingCount)
                    {
                        taskArray = new Task<List<ICommittedActionEvaluation>>[interval];
                        limitInterval = interval;
                    }
                    else
                    {
                        taskArray = new Task<List<ICommittedActionEvaluation>>[remainingCount];
                        limitInterval = remainingCount;
                    }

                    foreach (var item in
                             _baseStore.IterateIndexes(
                                 _baseChain.Id,
                                 offset + offsetIdx ?? 0 + offsetIdx,
                                 limitInterval
                             ).Select((value, i) => new { i, value }))
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

                        try
                        {
                            taskArray[item.i] = Task.Factory.StartNew(() =>
                            {
                                List<ICommittedActionEvaluation> actionEvaluations = EvaluateBlock(block);
                                Console.WriteLine($"Block progress: #{block.Index}/{remainingCount}");
                                return actionEvaluations;
                            });
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
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

                    Task.WaitAll(taskArray);
                    ProcessTasks(taskArray, blockChainStates);
                }

                DateTimeOffset postDataPrep = _blockTimeOffset;
                Console.WriteLine("Data Preparation Complete! Time Elapsed: {0}", postDataPrep - start);
                Console.WriteLine("Start Data Migration...");
                _mySqlStore.StoreBlockList(_blockList);
                _mySqlStore.StoreTransactionList(_txList);
                _mySqlStore.StoreAgentList(_agentList);
                _mySqlStore.StoreAvatarList(_avatarList);
                Console.WriteLine($"[Adventure Boss] {_adventureBossSeasonList.Count} Season");
                _mySqlStore.StoreAdventureBossSeasonList(_adventureBossSeasonList);
                Console.WriteLine($"[Adventure Boss] {_adventureBossWantedList.Count} Wanted");
                _mySqlStore.StoreAdventureBossWantedList(_adventureBossWantedList);
                Console.WriteLine($"[Adventure Boss] {_adventureBossChallengeList.Count} Challenge");
                _mySqlStore.StoreAdventureBossChallengeList(_adventureBossChallengeList);
                Console.WriteLine($"[Adventure Boss] {_adventureBossRushList.Count} Rush");
                _mySqlStore.StoreAdventureBossRushList(_adventureBossRushList);
                Console.WriteLine($"[Adventure Boss] {_adventureBossUnlockFloorList.Count} Unlock");
                _mySqlStore.StoreAdventureBossUnlockFloorList(_adventureBossUnlockFloorList);
                Console.WriteLine($"[Adventure Boss] {_adventureBossClaimRewardList.Count} claim");
                _mySqlStore.StoreAdventureBossClaimRewardList(_adventureBossClaimRewardList);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            DateTimeOffset end = DateTimeOffset.UtcNow;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
        }

        private void ProcessTasks(
            Task<List<ICommittedActionEvaluation>>[] taskArray, IBlockChainStates blockChainStates
        )
        {
            foreach (var task in taskArray)
            {
                try
                {
                    if (task.Result is { } data)
                    {
                        var actionLoader = new NCActionLoader();

                        foreach (var ae in data)
                        {
                            var inputState = new World(blockChainStates.GetWorldState(ae.InputContext.PreviousState));
                            var outputState = new World(blockChainStates.GetWorldState(ae.OutputState));

                            if (actionLoader.LoadAction(_blockIndex, ae.Action) is ActionBase action)
                            {
                                switch (action)
                                {
                                    // avatarNames will be stored as "N/A" for optimization
                                    case Wanted wanted:
                                        _adventureBossWantedList.Add(AdventureBossWantedData.GetWantedInfo(
                                            outputState, _blockIndex, _blockTimeOffset, wanted
                                        ));
                                        Console.WriteLine($"[Adventure Boss] Wanted added : {_adventureBossWantedList.Count}");

                                        // Update season info
                                        _adventureBossSeasonList.Add(AdventureBossSeasonData.GetAdventureBossSeasonInfo(
                                            outputState, wanted.Season, _blockTimeOffset
                                        ));
                                        Console.WriteLine($"[Adventure Boss] Season added : {_adventureBossSeasonList.Count}");
                                        break;
                                    case ExploreAdventureBoss challenge:
                                        _adventureBossChallengeList.Add(AdventureBossChallengeData.GetChallengeInfo(
                                            inputState, outputState, _blockIndex, _blockTimeOffset, challenge
                                        ));
                                        Console.WriteLine(
                                            $"[Adventure Boss] Challenge added : {_adventureBossChallengeList.Count}");
                                        break;
                                    case SweepAdventureBoss rush:
                                        _adventureBossRushList.Add(AdventureBossRushData.GetRushInfo(
                                            inputState, outputState, _blockIndex, _blockTimeOffset, rush
                                        ));
                                        Console.WriteLine($"[Adventure Boss] Rush added : {_adventureBossRushList.Count}");
                                        break;
                                    case UnlockFloor unlock:
                                        _adventureBossUnlockFloorList.Add(AdventureBossUnlockFloorData.GetUnlockInfo(
                                            inputState, outputState, _blockIndex, _blockTimeOffset, unlock
                                        ));
                                        Console.WriteLine($"[Adventure Boss] Unlock added : {_adventureBossUnlockFloorList.Count}");
                                        break;
                                    case ClaimAdventureBossReward claim:
                                    {
                                        _adventureBossClaimRewardList.Add(AdventureBossClaimRewardData.GetClaimInfo(
                                            inputState, _blockIndex, _blockTimeOffset, claim
                                        ));
                                        Console.WriteLine($"[Adventure Boss] Claim added : {_adventureBossClaimRewardList.Count}");

                                        // Update season info
                                        var latestSeason = inputState.GetLatestAdventureBossSeason();
                                        var season = latestSeason.EndBlockIndex <= _blockIndex
                                            ? latestSeason.Season // New season not started
                                            : latestSeason.Season - 1; // New season started
                                        _adventureBossSeasonList.Add(AdventureBossSeasonData.GetAdventureBossSeasonInfo(
                                            outputState, season, _blockTimeOffset
                                        ));
                                        Console.WriteLine($"[Adventure Boss] Season updated : {_adventureBossSeasonList.Count}");
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }

        private List<ICommittedActionEvaluation> EvaluateBlock(Block block)
        {
            var evList = _baseChain.EvaluateBlock(block).ToList();
            return evList;
        }
    }
}
