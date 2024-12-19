using System.Net.Http;
using System.Threading.Tasks;

namespace NineChronicles.DataProvider.Executable.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Bencodex.Types;
    using Cocona;
    using Lib9c.Model.Order;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Crypto;
    using Libplanet.RocksDBStore;
    using Libplanet.Store;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using MySqlConnector;
    using Nekoyume;
    using Nekoyume.Action.Loader;
    using Nekoyume.Battle;
    using Nekoyume.Blockchain.Policy;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;

    public class UserDataMigration
    {
        private const string AgentDbName = "Agents";
        private const string AvatarDbName = "Avatars";
        private const string UEDbName = "UserEquipments";
        private const string UCTDbName = "UserCostumes";
        private const string UMDbName = "UserMaterials";
        private const string UCDbName = "UserConsumables";
        private const string USDbName = "UserStakings";
        private const string UMCDbName = "UserMonsterCollections";
        private const string UNCGDbName = "UserNCGs";
        private const string UCYDbName = "UserCrystals";
        private const string EDbName = "Equipments";
        private const string SCDbName = "ShopConsumables";
        private const string SEDbName = "ShopEquipments";
        private const string SCTDbName = "ShopCostumes";
        private const string SMDbName = "ShopMaterials";
        private readonly string uRDbName = "UserRunes";
        private readonly string dailyMetricDbName = "DailyMetrics";
        private string bARDbName = "BattleArenaRanking";
        private string fbBARDbName = "BattleArenaRanking";
        private string fbUSDbName = "UserStakings";
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain _baseChain;
        private StreamWriter _ccBulkFile;
        private StreamWriter _ceBulkFile;
        private StreamWriter _ieBulkFile;
        private StreamWriter _ueBulkFile;
        private StreamWriter _uctBulkFile;
        private StreamWriter _uiBulkFile;
        private StreamWriter _umBulkFile;
        private StreamWriter _ucBulkFile;
        private StreamWriter _usBulkFile;
        private StreamWriter _fbUsBulkFile;
        private StreamWriter _umcBulkFile;
        private StreamWriter _uncgBulkFile;
        private StreamWriter _ucyBulkFile;
        private StreamWriter _eBulkFile;
        private StreamWriter _scBulkFile;
        private StreamWriter _seBulkFile;
        private StreamWriter _sctBulkFile;
        private StreamWriter _smBulkFile;
        private StreamWriter _barBulkFile;
        private StreamWriter _fbBarBulkFile;
        private StreamWriter _urBulkFile;
        private StreamWriter _agentBulkFile;
        private StreamWriter _avatarBulkFile;
        private StreamWriter _dailyMetricsBulkFile;
        private List<string> _hourGlassAgentList;
        private List<string> _apStoneAgentList;
        private List<string> _ccFiles;
        private List<string> _ceFiles;
        private List<string> _ieFiles;
        private List<string> _ueFiles;
        private List<string> _uctFiles;
        private List<string> _uiFiles;
        private List<string> _umFiles;
        private List<string> _ucFiles;
        private List<string> _usFiles;
        private List<string> _fbUsFiles;
        private List<string> _umcFiles;
        private List<string> _uncgFiles;
        private List<string> _ucyFiles;
        private List<string> _eFiles;
        private List<string> _scFiles;
        private List<string> _seFiles;
        private List<string> _sctFiles;
        private List<string> _smFiles;
        private List<string> _barFiles;
        private List<string> _fbBarFiles;
        private List<string> _urFiles;
        private List<string> _agentFiles;
        private List<string> _avatarFiles;
        private List<string> _dailyMetricFiles;

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
                "slack-token",
                Description = "slack token to send the migration data.")]
            string slackToken,
            [Option(
                "slack-channel",
                Description = "slack channel that receives the migration data.")]
            string slackChannel,
            [Option(
                "network",
                Description = "Name of network(e.g., Odin or Heimdall)")]
            string network
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
            var blockPolicySource = new BlockPolicySource();
            IBlockPolicy blockPolicy = blockPolicySource.GetPolicy();

            // Setup base chain & new chain
            Block genesis = _baseStore.GetBlock(gHash);
            var blockChainStates = new BlockChainStates(_baseStore, baseStateStore);
            var actionEvaluator = new ActionEvaluator(
                blockPolicy.PolicyActionsRegistry,
                baseStateStore,
                new NCActionLoader());
            _baseChain = new BlockChain(blockPolicy, stagePolicy, _baseStore, baseStateStore, genesis, blockChainStates, actionEvaluator);

            Console.WriteLine("Start migration.");

            // files to store bulk file paths (new file created every 10000 blocks for bulk load performance)
            _ccFiles = new List<string>();
            _ceFiles = new List<string>();
            _ieFiles = new List<string>();
            _ueFiles = new List<string>();
            _uctFiles = new List<string>();
            _uiFiles = new List<string>();
            _umFiles = new List<string>();
            _ucFiles = new List<string>();
            _usFiles = new List<string>();
            _fbUsFiles = new List<string>();
            _umcFiles = new List<string>();
            _uncgFiles = new List<string>();
            _ucyFiles = new List<string>();
            _eFiles = new List<string>();
            _scFiles = new List<string>();
            _seFiles = new List<string>();
            _sctFiles = new List<string>();
            _smFiles = new List<string>();
            _barFiles = new List<string>();
            _fbBarFiles = new List<string>();
            _urFiles = new List<string>();
            _agentFiles = new List<string>();
            _avatarFiles = new List<string>();
            _dailyMetricFiles = new List<string>();

            // lists to keep track of inserted addresses to minimize duplicates
            _hourGlassAgentList = new List<string>();
            _apStoneAgentList = new List<string>();

            CreateBulkFiles();

            using MySqlConnection connection = new MySqlConnection(_connectionString);
            connection.Open();

            var stm = "SELECT `Address` from Avatars";
            var cmd = new MySqlCommand(stm, connection);

            var rdr = cmd.ExecuteReader();
            List<string> avatars = new List<string>();
            List<string> agents = new List<string>();

            while (rdr.Read())
            {
                Console.WriteLine("{0}", rdr.GetString(0));
                avatars.Add(rdr.GetString(0).Replace("0x", string.Empty));
            }

            connection.Close();

            int shopOrderCount = 0;
            bool finalizeBaranking = false;

            try
            {
                var tipHash = _baseStore.IndexBlockHash(_baseChain.Id, _baseChain.Tip.Index);
                var tip = _baseStore.GetBlock((BlockHash)tipHash);
                var exec = _baseChain.EvaluateBlock(tip);
                var ev = exec.Last();
                var outputState = new World(blockChainStates.GetWorldState(ev.OutputState));
                var avatarCount = 0;
                AvatarState avatarState;
                int interval = 10000000;
                int intervalCount = 0;
                var sheets = outputState.GetSheets(
                    sheetTypes: new[]
                    {
                        typeof(RuneSheet),
                    });
                var arenaSheet = outputState.GetSheet<ArenaSheet>();
                var arenaData = arenaSheet.GetRoundByBlockIndex(tip.Index);

                Console.WriteLine("2");

                var date = DateOnly.FromDateTime(tip.Timestamp.Date.AddDays(-1)).ToString("yyyy-MM-dd");
                Console.WriteLine(date);

                var dau = 0;
                var dauQuery = $"SELECT COUNT(DISTINCT Signer) as 'Unique Address' FROM Transactions WHERE Date = '{date}'";
                connection.Open();
                var dauCommand = new MySqlCommand(dauQuery, connection);
                dauCommand.CommandTimeout = 600;
                var dauReader = dauCommand.ExecuteReader();
                while (dauReader.Read())
                {
                    if (!dauReader.IsDBNull(0))
                    {
                        Console.WriteLine("dau: {0}", dauReader.GetInt32(0));
                        dau = dauReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("dau is null");
                    }
                }

                connection.Close();

                var txCount = 0;
                var txCountQuery = $"SELECT COUNT(TxId) as 'Transactions' FROM Transactions WHERE Date = '{date}'";
                connection.Open();
                var txCountCommand = new MySqlCommand(txCountQuery, connection);
                txCountCommand.CommandTimeout = 600;
                var txCountReader = txCountCommand.ExecuteReader();
                while (txCountReader.Read())
                {
                    if (!txCountReader.IsDBNull(0))
                    {
                        Console.WriteLine("txCount: {0}", txCountReader.GetInt32(0));
                        txCount = txCountReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("txCount is null");
                    }
                }

                connection.Close();

                var newDau = 0;
                var newDauQuery = $"select count(Signer) from Transactions WHERE ActionType = 'ApprovePledge' AND Date = '{date}'";
                connection.Open();
                var newDauCommand = new MySqlCommand(newDauQuery, connection);
                newDauCommand.CommandTimeout = 600;
                var newDauReader = newDauCommand.ExecuteReader();
                while (newDauReader.Read())
                {
                    if (!newDauReader.IsDBNull(0))
                    {
                        Console.WriteLine("newDau: {0}", newDauReader.GetInt32(0));
                        newDau = newDauReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("newDau is null");
                    }
                }

                connection.Close();

                var hasCount = 0;
                var hasCountQuery = $"select count(Id) as 'Count' from HackAndSlashes where Date = '{date}'";
                connection.Open();
                var hasCountCommand = new MySqlCommand(hasCountQuery, connection);
                hasCountCommand.CommandTimeout = 600;
                var hasCountReader = hasCountCommand.ExecuteReader();
                while (hasCountReader.Read())
                {
                    if (!hasCountReader.IsDBNull(0))
                    {
                        Console.WriteLine("hasCount: {0}", hasCountReader.GetInt32(0));
                        hasCount = hasCountReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("hasCount is null");
                    }
                }

                connection.Close();

                var hasUsers = 0;
                var hasUsersQuery = $"select COUNT(DISTINCT AgentAddress) as 'Player Count' from HackAndSlashes where Date = '{date}'";
                connection.Open();
                var hasUsersCommand = new MySqlCommand(hasUsersQuery, connection);
                hasUsersCommand.CommandTimeout = 600;
                var hasUsersReader = hasUsersCommand.ExecuteReader();
                while (hasUsersReader.Read())
                {
                    if (!hasUsersReader.IsDBNull(0))
                    {
                        Console.WriteLine("hasUsers: {0}", hasUsersReader.GetInt32(0));
                        hasUsers = hasUsersReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("hasUsers is null");
                    }
                }

                connection.Close();

                var sweepCount = 0;
                var sweepCountQuery = $"select count(Id) as 'Count' from HackAndSlashSweeps where Date = '{date}'";
                connection.Open();
                var sweepCountCommand = new MySqlCommand(sweepCountQuery, connection);
                sweepCountCommand.CommandTimeout = 600;
                var sweepCountReader = sweepCountCommand.ExecuteReader();
                while (sweepCountReader.Read())
                {
                    if (!sweepCountReader.IsDBNull(0))
                    {
                        Console.WriteLine("sweepCount: {0}", sweepCountReader.GetInt32(0));
                        sweepCount = sweepCountReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("sweepCount is null");
                    }
                }

                connection.Close();

                var sweepUsers = 0;
                var sweepUsersQuery = $"select COUNT(DISTINCT AgentAddress) as 'Player Count' from HackAndSlashSweeps where Date =  '{date}'";
                connection.Open();
                var sweepUsersCommand = new MySqlCommand(sweepUsersQuery, connection);
                sweepUsersCommand.CommandTimeout = 600;
                var sweepUsersReader = sweepUsersCommand.ExecuteReader();
                while (sweepUsersReader.Read())
                {
                    if (!sweepUsersReader.IsDBNull(0))
                    {
                        Console.WriteLine("sweepUsers: {0}", sweepUsersReader.GetInt32(0));
                        sweepUsers = sweepUsersReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("sweepUsers is null");
                    }
                }

                connection.Close();

                var combinationEquipmentCount = 0;
                var combinationEquipmentCountQuery = $"select count(Id) as 'Count' from CombinationEquipments where Date = '{date}'";
                connection.Open();
                var combinationEquipmentCountCommand = new MySqlCommand(combinationEquipmentCountQuery, connection);
                combinationEquipmentCountCommand.CommandTimeout = 600;
                var combinationEquipmentCountReader = combinationEquipmentCountCommand.ExecuteReader();
                while (combinationEquipmentCountReader.Read())
                {
                    if (!combinationEquipmentCountReader.IsDBNull(0))
                    {
                        Console.WriteLine("combinationEquipmentCount: {0}", combinationEquipmentCountReader.GetInt32(0));
                        combinationEquipmentCount = combinationEquipmentCountReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("combinationEquipmentCount is null");
                    }
                }

                connection.Close();

                var combinationEquipmentUsers = 0;
                var combinationEquipmentUsersQuery = $"select COUNT(DISTINCT AgentAddress) as 'Player Count' from CombinationConsumables where Date = '{date}'";
                connection.Open();
                var combinationEquipmentUsersCommand = new MySqlCommand(combinationEquipmentUsersQuery, connection);
                combinationEquipmentUsersCommand.CommandTimeout = 600;
                var combinationEquipmentUsersReader = combinationEquipmentUsersCommand.ExecuteReader();
                while (combinationEquipmentUsersReader.Read())
                {
                    if (!combinationEquipmentUsersReader.IsDBNull(0))
                    {
                        Console.WriteLine("combinationEquipmentUsers: {0}", combinationEquipmentUsersReader.GetInt32(0));
                        combinationEquipmentUsers = combinationEquipmentUsersReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("combinationEquipmentUsers is null");
                    }
                }

                connection.Close();

                var combinationConsumableCount = 0;
                var combinationConsumableCountQuery = $"select count(Id) as 'Count' from CombinationConsumables where Date = '{date}'";
                connection.Open();
                var combinationConsumableCountCommand = new MySqlCommand(combinationConsumableCountQuery, connection);
                combinationConsumableCountCommand.CommandTimeout = 600;
                var combinationConsumableCountReader = combinationConsumableCountCommand.ExecuteReader();
                while (combinationConsumableCountReader.Read())
                {
                    if (!combinationConsumableCountReader.IsDBNull(0))
                    {
                        Console.WriteLine("combinationConsumableCount: {0}", combinationConsumableCountReader.GetInt32(0));
                        combinationConsumableCount = combinationConsumableCountReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("combinationConsumableCount is null");
                    }
                }

                connection.Close();

                var combinationConsumableUsers = 0;
                var combinationConsumableUsersQuery = $"select COUNT(DISTINCT AgentAddress) as 'Player Count' from CombinationEquipments where Date = '{date}'";
                connection.Open();
                var combinationConsumableUsersCommand = new MySqlCommand(combinationConsumableUsersQuery, connection);
                combinationConsumableUsersCommand.CommandTimeout = 600;
                var combinationConsumableUsersReader = combinationConsumableUsersCommand.ExecuteReader();
                while (combinationConsumableUsersReader.Read())
                {
                    if (!combinationConsumableUsersReader.IsDBNull(0))
                    {
                        Console.WriteLine("combinationConsumableUsers: {0}", combinationConsumableUsersReader.GetInt32(0));
                        combinationConsumableUsers = combinationConsumableUsersReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("combinationConsumableUsers is null");
                    }
                }

                connection.Close();

                var itemEnhancementCount = 0;
                var itemEnhancementCountQuery = $"select count(Id) as 'Count' from ItemEnhancements where Date = '{date}'";
                connection.Open();
                var itemEnhancementCountCommand = new MySqlCommand(itemEnhancementCountQuery, connection);
                itemEnhancementCountCommand.CommandTimeout = 600;
                var itemEnhancementCountReader = itemEnhancementCountCommand.ExecuteReader();
                while (itemEnhancementCountReader.Read())
                {
                    if (!itemEnhancementCountReader.IsDBNull(0))
                    {
                        Console.WriteLine("itemEnhancementCount: {0}", itemEnhancementCountReader.GetInt32(0));
                        itemEnhancementCount = itemEnhancementCountReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("itemEnhancementCount is null");
                    }
                }

                connection.Close();

                var itemEnhancementUsers = 0;
                var itemEnhancementUsersQuery = $"select COUNT(DISTINCT AgentAddress) as 'Player Count' from ItemEnhancements where Date = '{date}'";
                connection.Open();
                var itemEnhancementUsersCommand = new MySqlCommand(itemEnhancementUsersQuery, connection);
                itemEnhancementUsersCommand.CommandTimeout = 600;
                var itemEnhancementUsersReader = itemEnhancementUsersCommand.ExecuteReader();
                while (itemEnhancementUsersReader.Read())
                {
                    if (!itemEnhancementUsersReader.IsDBNull(0))
                    {
                        Console.WriteLine("itemEnhancementUsers: {0}", itemEnhancementUsersReader.GetInt32(0));
                        itemEnhancementUsers = itemEnhancementUsersReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("itemEnhancementUsers is null");
                    }
                }

                connection.Close();

                var auraSummon = 0;
                var auraSummonQuery =
                    $"select SUM(SummonCount) from AuraSummons where GroupId = '10002' AND Date = '{date}'";
                connection.Open();
                var auraSummonCommand = new MySqlCommand(auraSummonQuery, connection);
                auraSummonCommand.CommandTimeout = 600;
                var auraSummonReader = auraSummonCommand.ExecuteReader();
                while (auraSummonReader.Read())
                {
                    if (!auraSummonReader.IsDBNull(0))
                    {
                        Console.WriteLine("auraSummon: {0}", auraSummonReader.GetInt32(0));
                        auraSummon = auraSummonReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("auraSummon is null");
                    }
                }

                connection.Close();

                var runeSummon = 0;
                var runeSummonQuery = $"select SUM(SummonCount) from RuneSummons where Date = '{date}'";
                connection.Open();
                var runeSummonCommand = new MySqlCommand(runeSummonQuery, connection);
                runeSummonCommand.CommandTimeout = 600;
                var runeSummonReader = runeSummonCommand.ExecuteReader();
                while (runeSummonReader.Read())
                {
                    if (!runeSummonReader.IsDBNull(0))
                    {
                        Console.WriteLine("runeSummon: {0}", runeSummonReader.GetInt32(0));
                        runeSummon = runeSummonReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("runeSummon is null");
                    }
                }

                connection.Close();

                var apUsage = 0;
                var apUsageQuery = $"select SUM(ApStoneCount) from HackAndSlashSweeps where Date = '{date}'";
                connection.Open();
                var apUsageCommand = new MySqlCommand(apUsageQuery, connection);
                apUsageCommand.CommandTimeout = 600;
                var apUsageReader = apUsageCommand.ExecuteReader();
                while (apUsageReader.Read())
                {
                    if (!apUsageReader.IsDBNull(0))
                    {
                        Console.WriteLine("apUsage: {0}", apUsageReader.GetInt32(0));
                        apUsage = apUsageReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("apUsage is null");
                    }
                }

                connection.Close();

                var hourglassUsage = 0;
                var hourglassUsageQuery = $"SELECT SUM(HourglassCount) FROM RapidCombinations WHERE Date = '{date}'";
                connection.Open();
                var hourglassUsageCommand = new MySqlCommand(hourglassUsageQuery, connection);
                hourglassUsageCommand.CommandTimeout = 600;
                var hourglassUsageReader = hourglassUsageCommand.ExecuteReader();
                while (hourglassUsageReader.Read())
                {
                    if (!hourglassUsageReader.IsDBNull(0))
                    {
                        Console.WriteLine("hourglassUsage: {0}", hourglassUsageReader.GetInt32(0));
                        hourglassUsage = hourglassUsageReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("hourglassUsage is null");
                    }
                }

                connection.Close();

                var ncgTrade = 0m;
                var ncgTradeQuery = @$"select (SUM(price)) as 'Trade NCG(Amount)' from 
                    (
                        (select Price from ShopHistoryConsumables WHERE Date = '{date}') Union all 
                        (select Price from ShopHistoryCostumes WHERE Date = '{date}' ) Union all 
                        (select Price from ShopHistoryEquipments WHERE Date = '{date}' ) Union all 
                        (select Price from ShopHistoryMaterials WHERE Date = '{date}' )
                    ) a";
                connection.Open();
                var ncgTradeCommand = new MySqlCommand(ncgTradeQuery, connection);
                ncgTradeCommand.CommandTimeout = 600;
                var ncgTradeReader = ncgTradeCommand.ExecuteReader();
                while (ncgTradeReader.Read())
                {
                    if (!ncgTradeReader.IsDBNull(0))
                    {
                        Console.WriteLine("ncgTrade: {0}", ncgTradeReader.GetDecimal(0));
                        ncgTrade = ncgTradeReader.GetDecimal(0);
                    }
                    else
                    {
                        Console.WriteLine("ncgTrade is null");
                    }
                }

                connection.Close();

                var enhanceNcg = 0m;
                var enhanceNcgQuery = $"SELECT sum(BurntNCG) as 'Enhance NCG(Amount)' from ItemEnhancements  where Date = '{date}'";
                connection.Open();
                var enhanceNcgCommand = new MySqlCommand(enhanceNcgQuery, connection);
                enhanceNcgCommand.CommandTimeout = 600;
                var enhanceNcgReader = enhanceNcgCommand.ExecuteReader();
                while (enhanceNcgReader.Read())
                {
                    if (!enhanceNcgReader.IsDBNull(0))
                    {
                        Console.WriteLine("enhanceNcg: {0}", enhanceNcgReader.GetDecimal(0));
                        enhanceNcg = enhanceNcgReader.GetDecimal(0);
                    }
                    else
                    {
                        Console.WriteLine("enhanceNcg is null");
                    }
                }

                connection.Close();

                var runeNcg = 0m;
                var runeNcgQuery = $"SELECT sum(BurntNCG) as 'Rune NCG(Amount)' from RuneEnhancements   where Date = '{date}'";
                connection.Open();
                var runeNcgCommand = new MySqlCommand(runeNcgQuery, connection);
                runeNcgCommand.CommandTimeout = 600;
                var runeNcgReader = runeNcgCommand.ExecuteReader();
                while (runeNcgReader.Read())
                {
                    if (!runeNcgReader.IsDBNull(0))
                    {
                        Console.WriteLine("runeNcg: {0}", runeNcgReader.GetDecimal(0));
                        runeNcg = runeNcgReader.GetDecimal(0);
                    }
                    else
                    {
                        Console.WriteLine("runeNcg is null");
                    }
                }

                connection.Close();

                var runeSlotNcg = 0m;
                var runeSlotNcgQuery = $"SELECT sum(BurntNCG) as 'RuneSlot NCG(Amount)' from UnlockRuneSlots  where Date = '{date}'";
                connection.Open();
                var runeSlotNcgCommand = new MySqlCommand(runeSlotNcgQuery, connection);
                runeSlotNcgCommand.CommandTimeout = 600;
                var runeSlotNcgReader = runeSlotNcgCommand.ExecuteReader();
                while (runeSlotNcgReader.Read())
                {
                    if (!runeSlotNcgReader.IsDBNull(0))
                    {
                        Console.WriteLine("runeSlotNcg: {0}", runeSlotNcgReader.GetDecimal(0));
                        runeSlotNcg = runeSlotNcgReader.GetDecimal(0);
                    }
                    else
                    {
                        Console.WriteLine("runeSlotNcg is null");
                    }
                }

                connection.Close();

                var arenaNcg = 0m;
                var arenaNcgQuery = $"SELECT sum(BurntNCG) as 'Arena NCG(Amount)' from BattleArenas where Date = '{date}'";
                connection.Open();
                var arenaNcgCommand = new MySqlCommand(arenaNcgQuery, connection);
                arenaNcgCommand.CommandTimeout = 600;
                var arenaNcgReader = arenaNcgCommand.ExecuteReader();
                while (arenaNcgReader.Read())
                {
                    if (!arenaNcgReader.IsDBNull(0))
                    {
                        Console.WriteLine("arenaNcg: {0}", arenaNcgReader.GetDecimal(0));
                        arenaNcg = arenaNcgReader.GetDecimal(0);
                    }
                    else
                    {
                        Console.WriteLine("arenaNcg is null");
                    }
                }

                connection.Close();

                var eventTicketNcg = 0m;
                var eventTicketNcgQuery = $"SELECT sum(BurntNCG) as 'EventTicket NCG' from EventDungeonBattles where Date = '{date}'";
                connection.Open();
                var eventTicketNcgCommand = new MySqlCommand(eventTicketNcgQuery, connection);
                eventTicketNcgCommand.CommandTimeout = 600;
                var eventTicketNcgReader = eventTicketNcgCommand.ExecuteReader();
                while (eventTicketNcgReader.Read())
                {
                    if (!eventTicketNcgReader.IsDBNull(0))
                    {
                        Console.WriteLine("eventTicketNcg: {0}", eventTicketNcgReader.GetDecimal(0));
                        eventTicketNcg = eventTicketNcgReader.GetDecimal(0);
                    }
                    else
                    {
                        Console.WriteLine("eventTicketNcg is null");
                    }
                }

                connection.Close();

                _dailyMetricsBulkFile.WriteLine(
                    $"{date};" +
                    $"{dau};" +
                    $"{txCount};" +
                    $"{newDau};" +
                    $"{hasCount};" +
                    $"{hasUsers};" +
                    $"{sweepCount};" +
                    $"{sweepUsers};" +
                    $"{combinationEquipmentCount};" +
                    $"{combinationEquipmentUsers};" +
                    $"{combinationConsumableCount};" +
                    $"{combinationConsumableUsers};" +
                    $"{itemEnhancementCount};" +
                    $"{itemEnhancementUsers};" +
                    $"{auraSummon};" +
                    $"{runeSummon};" +
                    $"{apUsage};" +
                    $"{hourglassUsage};" +
                    $"{ncgTrade};" +
                    $"{enhanceNcg};" +
                    $"{runeNcg};" +
                    $"{runeSlotNcg};" +
                    $"{arenaNcg};" +
                    $"{eventTicketNcg}"
                );

                _dailyMetricsBulkFile.Flush();
                _dailyMetricsBulkFile.Close();

                foreach (var path in _dailyMetricFiles)
                {
                    BulkInsert(dailyMetricDbName, path);
                }

                try
                {
                    var prevArenaEndIndex = arenaData.StartBlockIndex - 1;
                    var prevArenaData = arenaSheet.GetRoundByBlockIndex(prevArenaEndIndex);
                    var finalizeBarankingTip = prevArenaEndIndex;
                    fbBARDbName = $"{fbBARDbName}_{prevArenaData.ChampionshipId}_{prevArenaData.Round}";

                    connection.Open();
                    var preBarQuery = $"SELECT `BlockIndex` FROM {fbBARDbName} limit 1";
                    var preBarCmd = new MySqlCommand(preBarQuery, connection);

                    var dataReader = preBarCmd.ExecuteReader();
                    long prevBarDbTip = 0;
                    Console.WriteLine("3");
                    while (dataReader.Read())
                    {
                        Console.WriteLine("{0}", dataReader.GetInt64(0));
                        prevBarDbTip = dataReader.GetInt64(0);
                    }

                    connection.Close();
                    Console.WriteLine("4");
                    if (prevBarDbTip != 0 && prevBarDbTip < finalizeBarankingTip)
                    {
                        finalizeBaranking = true;
                    }

                    if (finalizeBaranking)
                    {
                        try
                        {
                            Console.WriteLine($"Finalize {fbBARDbName} Table!");
                            var fbTipHash = _baseStore.IndexBlockHash(_baseChain.Id, finalizeBarankingTip);
                            var fbTip = _baseStore.GetBlock((BlockHash)fbTipHash!);
                            var fbExec = _baseChain.EvaluateBlock(fbTip);
                            var fbEv = fbExec.Last();
                            var fbOutputState = new World(blockChainStates.GetWorldState(fbEv.OutputState));
                            var fbArenaSheet = fbOutputState.GetSheet<ArenaSheet>();
                            var fbArenaData = fbArenaSheet.GetRoundByBlockIndex(fbTip.Index);
                            List<string> fbAgents = new List<string>();
                            var fbavatarCount = 0;

                            fbUSDbName = $"{fbUSDbName}_{fbTip.Index}";
                            Console.WriteLine("5");

                            foreach (var fbAvatar in avatars)
                            {
                                try
                                {
                                    fbavatarCount++;
                                    Console.WriteLine("Migrating {0}/{1}", fbavatarCount, avatars.Count);
                                    AvatarState fbAvatarState;
                                    var fbAvatarAddress = new Address(fbAvatar);
                                    fbAvatarState = fbOutputState.GetAvatarState(fbAvatarAddress);

                                    var fbAvatarLevel = fbAvatarState.level;

                                    var fbArenaScoreAdr =
                                        ArenaScore.DeriveAddress(fbAvatarAddress, fbArenaData.ChampionshipId, fbArenaData.Round);
                                    var fbArenaInformationAdr =
                                        ArenaInformation.DeriveAddress(fbAvatarAddress, fbArenaData.ChampionshipId, fbArenaData.Round);
                                    fbOutputState.TryGetArenaInformation(fbArenaInformationAdr,
                                        out var fbCurrentArenaInformation);
                                    fbOutputState.TryGetArenaScore(fbArenaScoreAdr, out var fbOutputArenaScore);
                                    if (fbCurrentArenaInformation != null && fbOutputArenaScore != null)
                                    {
                                        _fbBarBulkFile.WriteLine(
                                            $"{fbTip.Index};" +
                                            $"{fbAvatarState.agentAddress.ToString()};" +
                                            $"{fbAvatarAddress.ToString()};" +
                                            $"{fbAvatarLevel};" +
                                            $"{fbArenaData.ChampionshipId};" +
                                            $"{fbArenaData.Round};" +
                                            $"{fbArenaData.ArenaType.ToString()};" +
                                            $"{fbOutputArenaScore.Score};" +
                                            $"{fbCurrentArenaInformation.Win};" +
                                            $"{fbCurrentArenaInformation.Win};" +
                                            $"{fbCurrentArenaInformation.Lose};" +
                                            $"{fbCurrentArenaInformation.Ticket};" +
                                            $"{fbCurrentArenaInformation.PurchasedTicketCount};" +
                                            $"{fbCurrentArenaInformation.TicketResetCount};" +
                                            $"{fbArenaData.EntranceFee};" +
                                            $"{fbArenaData.TicketPrice};" +
                                            $"{fbArenaData.AdditionalTicketPrice};" +
                                            $"{fbArenaData.RequiredMedalCount};" +
                                            $"{fbArenaData.StartBlockIndex};" +
                                            $"{fbArenaData.EndBlockIndex};" +
                                            $"{0};" +
                                            $"{fbTip.Timestamp.UtcDateTime:yyyy-MM-dd}"
                                        );
                                    }

                                    if (!fbAgents.Contains(fbAvatarState.agentAddress.ToString()))
                                    {
                                        fbAgents.Add(fbAvatarState.agentAddress.ToString());

                                        if (fbOutputState.TryGetStakeState(fbAvatarState.agentAddress,
                                                out StakeState fbStakeState2))
                                        {
                                            var fbStakeStateAddress =
                                                StakeState.DeriveAddress(fbAvatarState.agentAddress);
                                            var fbCurrency = fbOutputState.GetGoldCurrency();
                                            var fbStakedBalance =
                                                fbOutputState.GetBalance(fbStakeStateAddress, fbCurrency);
                                            _fbUsBulkFile.WriteLine(
                                                $"{fbTip.Index};" +
                                                "V3;" +
                                                $"{fbAvatarState.agentAddress.ToString()};" +
                                                $"{Convert.ToDecimal(fbStakedBalance.GetQuantityString())};" +
                                                $"{fbStakeState2.StartedBlockIndex};" +
                                                $"{fbStakeState2.ReceivedBlockIndex};" +
                                                $"{fbStakeState2.CancellableBlockIndex}"
                                            );
                                        }

                                        var fbAgentState = fbOutputState.GetAgentState(fbAvatarState.agentAddress);
                                        Address fbMonsterCollectionAddress = MonsterCollectionState.DeriveAddress(
                                            fbAvatarState.agentAddress,
                                            fbAgentState.MonsterCollectionRound
                                        );
                                        if (fbOutputState.TryGetLegacyState(fbMonsterCollectionAddress,
                                                out Dictionary fbStateDict))
                                        {
                                            var fbMonsterCollectionStates = new MonsterCollectionState(fbStateDict);
                                            var fbCurrency = fbOutputState.GetGoldCurrency();
                                            FungibleAssetValue fbMonsterCollectionBalance =
                                                fbOutputState.GetBalance(fbMonsterCollectionAddress, fbCurrency);
                                            _fbUsBulkFile.WriteLine(
                                                $"{fbTip.Index};" +
                                                "V1;" +
                                                $"{fbAvatarState.agentAddress.ToString()};" +
                                                $"{Convert.ToDecimal(fbMonsterCollectionBalance.GetQuantityString())};" +
                                                $"{fbMonsterCollectionStates.StartedBlockIndex};" +
                                                $"{fbMonsterCollectionStates.ReceivedBlockIndex};" +
                                                $"{fbMonsterCollectionStates.ExpiredBlockIndex}"
                                            );
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(ex.StackTrace);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
                        }

                        _fbUsBulkFile.Flush();
                        _fbUsBulkFile.Close();

                        _fbBarBulkFile.Flush();
                        _fbBarBulkFile.Close();

                        connection.Open();
                        var s =
                            $@"CREATE TABLE IF NOT EXISTS `{fbUSDbName}` (
                                      `BlockIndex` bigint NOT NULL,
                                      `StakeVersion` varchar(100) NOT NULL,
                                      `AgentAddress` varchar(100) NOT NULL,
                                      `StakingAmount` decimal(13,2) NOT NULL,
                                      `StartedBlockIndex` bigint NOT NULL,
                                      `ReceivedBlockIndex` bigint NOT NULL,
                                      `CancellableBlockIndex` bigint NOT NULL,
                                      `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP
                                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";
                        var c = new MySqlCommand(s, connection);
                        c.CommandTimeout = 300;
                        c.ExecuteScalar();
                        connection.Close();

                        Console.WriteLine("6");

                        var fbstm23 =
                            $"RENAME TABLE {fbBARDbName} TO {fbBARDbName}_Dump; CREATE TABLE {fbBARDbName} LIKE {fbBARDbName}_Dump;";
                        var fbcmd23 = new MySqlCommand(fbstm23, connection);
                        connection.Open();
                        fbcmd23.CommandTimeout = 300;
                        fbcmd23.ExecuteScalar();
                        connection.Close();
                        Console.WriteLine($"Move {fbBARDbName} Complete!");

                        foreach (var path in _fbUsFiles)
                        {
                            BulkInsert(fbUSDbName, path);
                        }

                        foreach (var path in _fbBarFiles)
                        {
                            BulkInsert(fbBARDbName, path);
                        }

                        var fbstm34 = $"DROP TABLE {fbBARDbName}_Dump;";
                        var fbcmd34 = new MySqlCommand(fbstm34, connection);
                        connection.Open();
                        fbcmd34.CommandTimeout = 300;
                        fbcmd34.ExecuteScalar();
                        connection.Close();
                        Console.WriteLine($"Delete {fbBARDbName}_Dump Complete!");
                        Console.WriteLine($"Finalize {fbBARDbName} & {fbUSDbName} Tables Complete!");

                        if (slackToken is not null && slackChannel is not null)
                        {
                            var slackMessage =
                                $"@here {network} arena season(Championship Id: {prevArenaData.ChampionshipId}/Round: {prevArenaData.Round}) ranking finalized! Check tables {fbBARDbName} & {fbUSDbName}.";
                            SendMessageAsync(
                                slackToken,
                                slackChannel,
                                slackMessage
                            ).Wait();
                        }
                    }

                    bARDbName = $"{bARDbName}_{arenaData.ChampionshipId}_{arenaData.Round}";
                    Console.WriteLine("1");
                    connection.Open();
                    var stm33 =
                        $@"CREATE TABLE IF NOT EXISTS `{bARDbName}` (
                            `BlockIndex` bigint NOT NULL,
                            `AgentAddress` varchar(100) NOT NULL,
                            `AvatarAddress` varchar(100) NOT NULL,
                            `AvatarLevel` int NOT NULL,
                            `ChampionshipId` int NOT NULL,
                            `Round` int NOT NULL,
                            `ArenaType` varchar(100) NOT NULL,
                            `Score` int NOT NULL,
                            `WinCount` int NOT NULL,
                            `MedalCount` int NOT NULL,
                            `LossCount` int NOT NULL,
                            `Ticket` int NOT NULL,
                            `PurchasedTicketCount` int NOT NULL,
                            `TicketResetCount` int NOT NULL,
                            `EntranceFee` bigint NOT NULL,
                            `TicketPrice` bigint NOT NULL,
                            `AdditionalTicketPrice` bigint NOT NULL,
                            `RequiredMedalCount` int NOT NULL,
                            `StartBlockIndex` bigint NOT NULL,
                            `EndBlockIndex` bigint NOT NULL,
                            `Ranking` int NOT NULL,
                            `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            KEY `fk_BattleArenaRanking_Agent1_idx` (`AgentAddress`),
                            KEY `fk_BattleArenaRanking_AvatarAddress1_idx` (`AvatarAddress`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";

                    var cmd33 = new MySqlCommand(stm33, connection);
                    cmd33.CommandTimeout = 300;
                    cmd33.ExecuteScalar();
                    connection.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }

                foreach (var avatar in avatars)
                {
                    try
                    {
                        intervalCount++;
                        avatarCount++;
                        Console.WriteLine("Interval Count {0}", intervalCount);
                        Console.WriteLine("Migrating {0}/{1}", avatarCount, avatars.Count);
                        var avatarAddress = new Address(avatar);
                        avatarState = outputState.GetAvatarState(avatarAddress);

                        var avatarLevel = avatarState.level;

                        var runeSheet = sheets.GetSheet<RuneSheet>();
                        foreach (var ticker in runeSheet.Values.Select(x => x.Ticker))
                        {
#pragma warning disable CS0618
                            var runeCurrency = Currency.Legacy(ticker, 0, minters: null);
#pragma warning restore CS0618
                            var outputRuneBalance = outputState.GetBalance(
                                avatarAddress,
                                runeCurrency);
                            if (Convert.ToDecimal(outputRuneBalance.GetQuantityString()) > 0)
                            {
                                _urBulkFile.WriteLine(
                                    $"{tip.Index};" +
                                    $"{avatarState.agentAddress.ToString()};" +
                                    $"{avatarAddress.ToString()};" +
                                    $"{ticker};" +
                                    $"{Convert.ToDecimal(outputRuneBalance.GetQuantityString())};" +
                                    $"{tip.Timestamp.UtcDateTime:yyyy-MM-dd}"
                                );
                            }
                        }

                        var arenaScoreAdr =
                            ArenaScore.DeriveAddress(avatarAddress, arenaData.ChampionshipId, arenaData.Round);
                        var arenaInformationAdr =
                            ArenaInformation.DeriveAddress(avatarAddress, arenaData.ChampionshipId, arenaData.Round);
                        outputState.TryGetArenaInformation(arenaInformationAdr, out var currentArenaInformation);
                        outputState.TryGetArenaScore(arenaScoreAdr, out var outputArenaScore);
                        if (currentArenaInformation != null && outputArenaScore != null)
                        {
                            _barBulkFile.WriteLine(
                                $"{tip.Index};" +
                                $"{avatarState.agentAddress.ToString()};" +
                                $"{avatarAddress.ToString()};" +
                                $"{avatarLevel};" +
                                $"{arenaData.ChampionshipId};" +
                                $"{arenaData.Round};" +
                                $"{arenaData.ArenaType.ToString()};" +
                                $"{outputArenaScore.Score};" +
                                $"{currentArenaInformation.Win};" +
                                $"{currentArenaInformation.Win};" +
                                $"{currentArenaInformation.Lose};" +
                                $"{currentArenaInformation.Ticket};" +
                                $"{currentArenaInformation.PurchasedTicketCount};" +
                                $"{currentArenaInformation.TicketResetCount};" +
                                $"{arenaData.EntranceFee};" +
                                $"{arenaData.TicketPrice};" +
                                $"{arenaData.AdditionalTicketPrice};" +
                                $"{arenaData.RequiredMedalCount};" +
                                $"{arenaData.StartBlockIndex};" +
                                $"{arenaData.EndBlockIndex};" +
                                $"{0};" +
                                $"{tip.Timestamp.UtcDateTime:yyyy-MM-dd}"
                            );
                        }

                        Address orderReceiptAddress = OrderDigestListState.DeriveAddress(avatarAddress);
                        var orderReceiptList = outputState.TryGetLegacyState(orderReceiptAddress, out Dictionary receiptDict)
                            ? new OrderDigestListState(receiptDict)
                            : new OrderDigestListState(orderReceiptAddress);
                        foreach (var orderReceipt in orderReceiptList.OrderDigestList)
                        {
                            if (orderReceipt.ExpiredBlockIndex >= tip.Index)
                            {
                                var state = outputState.GetLegacyState(
                                    Addresses.GetItemAddress(orderReceipt.TradableId));
                                ITradableItem orderItem =
                                    (ITradableItem)ItemFactory.Deserialize((Dictionary)state);
                                if (orderItem.ItemType == ItemType.Equipment)
                                {
                                    var equipment = (Equipment)orderItem;
                                    Console.WriteLine(equipment.ItemId);
                                    _seBulkFile.WriteLine(
                                        $"{equipment.ItemId.ToString()};" +
                                        $"{tip.Index};" +
                                        $"{orderReceipt.SellerAgentAddress.ToString()};" +
                                        $"{avatarAddress.ToString()};" +
                                        $"{equipment.ItemType.ToString()};" +
                                        $"{equipment.ItemSubType.ToString()};" +
                                        $"{equipment.Id};" +
                                        $"{equipment.BuffSkills.Count};" +
                                        $"{equipment.ElementalType.ToString()};" +
                                        $"{equipment.Grade};" +
                                        $"{equipment.level};" +
                                        $"{equipment.SetId};" +
                                        $"{equipment.Skills.Count};" +
                                        $"{equipment.SpineResourcePath};" +
                                        $"{equipment.RequiredBlockIndex};" +
                                        $"{equipment.NonFungibleId.ToString()};" +
                                        $"{equipment.NonFungibleId.ToString()};" +
                                        $"{equipment.UniqueStatType.ToString()};" +
                                        $"{Convert.ToDecimal(orderReceipt.Price.GetQuantityString())};" +
                                        $"{orderReceipt.OrderId};" +
                                        $"{orderReceipt.CombatPoint};" +
                                        $"{orderReceipt.ItemCount};" +
                                        $"{orderReceipt.StartedBlockIndex};" +
                                        $"{orderReceipt.ExpiredBlockIndex}"
                                    );
                                    shopOrderCount += 1;
                                }

                                if (orderItem.ItemType == ItemType.Costume)
                                {
                                    var costume = (Costume)orderItem;
                                    Console.WriteLine(costume.ItemId);
                                    _sctBulkFile.WriteLine(
                                        $"{costume.ItemId.ToString()};" +
                                        $"{tip.Index};" +
                                        $"{orderReceipt.SellerAgentAddress.ToString()};" +
                                        $"{avatarAddress.ToString()};" +
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
                                        $"{Convert.ToDecimal(orderReceipt.Price.GetQuantityString())};" +
                                        $"{orderReceipt.OrderId};" +
                                        $"{orderReceipt.CombatPoint};" +
                                        $"{orderReceipt.ItemCount};" +
                                        $"{orderReceipt.StartedBlockIndex};" +
                                        $"{orderReceipt.ExpiredBlockIndex}"
                                    );
                                    shopOrderCount += 1;
                                }

                                if (orderItem.ItemType == ItemType.Material)
                                {
                                    var material = (Material)orderItem;
                                    Console.WriteLine(material.ItemId);
                                    _smBulkFile.WriteLine(
                                        $"{material.ItemId.ToString()};" +
                                        $"{tip.Index};" +
                                        $"{orderReceipt.SellerAgentAddress.ToString()};" +
                                        $"{avatarAddress.ToString()};" +
                                        $"{material.ItemType.ToString()};" +
                                        $"{material.ItemSubType.ToString()};" +
                                        $"{material.Id};" +
                                        $"{material.ElementalType.ToString()};" +
                                        $"{material.Grade};" +
                                        $"{orderReceipt.TradableId};" +
                                        $"{Convert.ToDecimal(orderReceipt.Price.GetQuantityString())};" +
                                        $"{orderReceipt.OrderId};" +
                                        $"{orderReceipt.CombatPoint};" +
                                        $"{orderReceipt.ItemCount};" +
                                        $"{orderReceipt.StartedBlockIndex};" +
                                        $"{orderReceipt.ExpiredBlockIndex}"
                                    );
                                    shopOrderCount += 1;
                                }

                                if (orderItem.ItemType == ItemType.Consumable)
                                {
                                    var consumable = (Consumable)orderItem;
                                    Console.WriteLine(consumable.ItemId);
                                    _scBulkFile.WriteLine(
                                        $"{consumable.ItemId.ToString()};" +
                                        $"{tip.Index};" +
                                        $"{orderReceipt.SellerAgentAddress.ToString()};" +
                                        $"{avatarAddress.ToString()};" +
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
                                        $"{Convert.ToDecimal(orderReceipt.Price.GetQuantityString())};" +
                                        $"{orderReceipt.OrderId};" +
                                        $"{orderReceipt.CombatPoint};" +
                                        $"{orderReceipt.ItemCount};" +
                                        $"{orderReceipt.StartedBlockIndex};" +
                                        $"{orderReceipt.ExpiredBlockIndex}"
                                    );
                                    shopOrderCount += 1;
                                }

                                Console.WriteLine(orderReceipt.OrderId);
                                Console.WriteLine(orderItem.ItemType);
                            }
                        }

                        var userEquipments = avatarState.inventory.Equipments;
                        var userCostumes = avatarState.inventory.Costumes;
                        var userMaterials = avatarState.inventory.Materials;
                        var materialItemSheet = outputState.GetSheet<MaterialItemSheet>();
                        var hourglassRow = materialItemSheet
                            .First(pair => pair.Value.ItemSubType == ItemSubType.Hourglass)
                            .Value;
                        var apStoneRow = materialItemSheet
                            .First(pair => pair.Value.ItemSubType == ItemSubType.ApStone)
                            .Value;
                        var userConsumables = avatarState.inventory.Consumables;

                        foreach (var equipment in userEquipments)
                        {
                            var equipmentCp = CPHelper.GetCP(equipment);
                            WriteEquipment(tip.Index, equipment, avatarState.agentAddress, avatarAddress);
                            WriteRankingEquipment(equipment, avatarState.agentAddress, avatarAddress, equipmentCp);
                        }

                        foreach (var costume in userCostumes)
                        {
                            WriteCostume(tip.Index, costume, avatarState.agentAddress, avatarAddress);
                        }

                        foreach (var material in userMaterials)
                        {
                            if (material.ItemId.ToString() == hourglassRow.ItemId.ToString())
                            {
                                if (!_hourGlassAgentList.Contains(avatarState.agentAddress.ToString()))
                                {
                                     var inventoryState = new Inventory((List)avatarState.inventory.Serialize());
                                     inventoryState.TryGetFungibleItems(hourglassRow.ItemId, out var hourglasses);
                                     var hourglassesCount = hourglasses.Sum(e => e.count);
                                     WriteMaterial(tip.Index, material, hourglassesCount, avatarState.agentAddress, avatarAddress);
                                     _hourGlassAgentList.Add(avatarState.agentAddress.ToString());
                                }
                            }
                            else if (material.ItemId.ToString() == apStoneRow.ItemId.ToString())
                            {
                                if (!_apStoneAgentList.Contains(avatarState.agentAddress.ToString()))
                                {
                                    var inventoryState = new Inventory((List)avatarState.inventory.Serialize());
                                    inventoryState.TryGetFungibleItems(apStoneRow.ItemId, out var apStones);
                                    var apStonesCount = apStones.Sum(e => e.count);
                                    WriteMaterial(tip.Index, material, apStonesCount, avatarState.agentAddress, avatarAddress);
                                    _apStoneAgentList.Add(avatarState.agentAddress.ToString());
                                }
                            }
                            else
                            {
                                var inventoryState = new Inventory((List)avatarState.inventory.Serialize());
                                inventoryState.TryGetFungibleItems(material.ItemId, out var materialItem);
                                var materialCount = materialItem.Sum(e => e.count);
                                WriteMaterial(tip.Index, material, materialCount, avatarState.agentAddress, avatarAddress);
                            }
                        }

                        foreach (var consumable in userConsumables)
                        {
                            WriteConsumable(tip.Index, consumable, avatarState.agentAddress, avatarAddress);
                        }

                        if (!agents.Contains(avatarState.agentAddress.ToString()))
                        {
                            agents.Add(avatarState.agentAddress.ToString());
                            Currency ncgCurrency = outputState.GetGoldCurrency();
                            var ncgBalance = outputState.GetBalance(
                                avatarState.agentAddress,
                                ncgCurrency);
                            _uncgBulkFile.WriteLine(
                                $"{tip.Index};" +
                                $"{avatarState.agentAddress.ToString()};" +
                                $"{Convert.ToDecimal(ncgBalance.GetQuantityString())}"
                            );
                            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
                            var crystalBalance = outputState.GetBalance(
                                avatarState.agentAddress,
                                crystalCurrency);
                            _ucyBulkFile.WriteLine(
                                $"{tip.Index};" +
                                $"{avatarState.agentAddress.ToString()};" +
                                $"{Convert.ToDecimal(crystalBalance.GetQuantityString())}"
                            );
                            var agentState = outputState.GetAgentState(avatarState.agentAddress);
                            Address monsterCollectionAddress = MonsterCollectionState.DeriveAddress(
                                avatarState.agentAddress,
                                agentState.MonsterCollectionRound
                            );
                            if (outputState.TryGetLegacyState(monsterCollectionAddress, out Dictionary stateDict))
                            {
                                var mcStates = new MonsterCollectionState(stateDict);
                                var currency = outputState.GetGoldCurrency();
                                FungibleAssetValue mcBalance = outputState.GetBalance(monsterCollectionAddress, currency);
                                _umcBulkFile.WriteLine(
                                    $"{tip.Index};" +
                                    $"{avatarState.agentAddress.ToString()};" +
                                    $"{Convert.ToDecimal(mcBalance.GetQuantityString())};" +
                                    $"{mcStates.Level};" +
                                    $"{mcStates.RewardLevel};" +
                                    $"{mcStates.StartedBlockIndex};" +
                                    $"{mcStates.ReceivedBlockIndex};" +
                                    $"{mcStates.ExpiredBlockIndex}"
                                );
                            }

                            if (outputState.TryGetStakeState(avatarState.agentAddress, out StakeState stakeState))
                            {
                                var stakeStateAddress = StakeState.DeriveAddress(avatarState.agentAddress);
                                var currency = outputState.GetGoldCurrency();
                                var stakedBalance = outputState.GetBalance(stakeStateAddress, currency);
                                _usBulkFile.WriteLine(
                                    $"{tip.Index};" +
                                    $"{avatarState.agentAddress.ToString()};" +
                                    $"{Convert.ToDecimal(stakedBalance.GetQuantityString())};" +
                                    $"{stakeState.StartedBlockIndex};" +
                                    $"{stakeState.ReceivedBlockIndex};" +
                                    $"{stakeState.CancellableBlockIndex}"
                                );
                            }
                            else
                            {
                                if (outputState.TryGetStakeState(avatarState.agentAddress, out StakeState stakeState2))
                                {
                                    var stakeStateAddress = StakeState.DeriveAddress(avatarState.agentAddress);
                                    var currency = outputState.GetGoldCurrency();
                                    var stakedBalance = outputState.GetBalance(stakeStateAddress, currency);
                                    _usBulkFile.WriteLine(
                                        $"{tip.Index};" +
                                        $"{avatarState.agentAddress.ToString()};" +
                                        $"{Convert.ToDecimal(stakedBalance.GetQuantityString())};" +
                                        $"{stakeState2.StartedBlockIndex};" +
                                        $"{stakeState2.ReceivedBlockIndex};" +
                                        $"{stakeState2.CancellableBlockIndex}"
                                    );
                                }
                            }
                        }

                        Console.WriteLine("Migrating Complete {0}/{1}", avatarCount, avatars.Count);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }

                    if (intervalCount == interval)
                    {
                        FlushBulkFiles();
                        foreach (var path in _agentFiles)
                        {
                            BulkInsert(AgentDbName, path);
                        }

                        foreach (var path in _avatarFiles)
                        {
                            BulkInsert(AvatarDbName, path);
                        }

                        foreach (var path in _ueFiles)
                        {
                            BulkInsert(UEDbName, path);
                        }

                        foreach (var path in _uctFiles)
                        {
                            BulkInsert(UCTDbName, path);
                        }

                        foreach (var path in _umFiles)
                        {
                            BulkInsert(UMDbName, path);
                        }

                        foreach (var path in _ucFiles)
                        {
                            BulkInsert(UCDbName, path);
                        }

                        foreach (var path in _eFiles)
                        {
                            BulkInsert(EDbName, path);
                        }

                        foreach (var path in _usFiles)
                        {
                            BulkInsert(USDbName, path);
                        }

                        foreach (var path in _umcFiles)
                        {
                            BulkInsert(UMCDbName, path);
                        }

                        foreach (var path in _uncgFiles)
                        {
                            BulkInsert(UNCGDbName, path);
                        }

                        foreach (var path in _ucyFiles)
                        {
                            BulkInsert(UCYDbName, path);
                        }

                        foreach (var path in _scFiles)
                        {
                            BulkInsert(SCDbName, path);
                        }

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

                        foreach (var path in _barFiles)
                        {
                            BulkInsert(bARDbName, path);
                        }

                        foreach (var path in _urFiles)
                        {
                            BulkInsert(uRDbName, path);
                        }

                        _agentFiles.RemoveAt(0);
                        _avatarFiles.RemoveAt(0);
                        _ueFiles.RemoveAt(0);
                        _uctFiles.RemoveAt(0);
                        _umFiles.RemoveAt(0);
                        _ucFiles.RemoveAt(0);
                        _eFiles.RemoveAt(0);
                        _usFiles.RemoveAt(0);
                        _umcFiles.RemoveAt(0);
                        _uncgFiles.RemoveAt(0);
                        _ucyFiles.RemoveAt(0);
                        _scFiles.RemoveAt(0);
                        _seFiles.RemoveAt(0);
                        _sctFiles.RemoveAt(0);
                        _smFiles.RemoveAt(0);
                        _barFiles.RemoveAt(0);
                        _urFiles.RemoveAt(0);
                        CreateBulkFiles();
                        intervalCount = 0;
                    }
                }

                FlushBulkFiles();
                DateTimeOffset postDataPrep = DateTimeOffset.Now;
                Console.WriteLine("Data Preparation Complete! Time Elapsed: {0}", postDataPrep - start);
                var stm6 = $"RENAME TABLE {EDbName} TO {EDbName}_Dump; CREATE TABLE {EDbName} LIKE {EDbName}_Dump;";
                var stm23 = $"RENAME TABLE {bARDbName} TO {bARDbName}_Dump; CREATE TABLE {bARDbName} LIKE {bARDbName}_Dump;";
                var cmd6 = new MySqlCommand(stm6, connection);
                var cmd23 = new MySqlCommand(stm23, connection);
                foreach (var path in _agentFiles)
                {
                    BulkInsert(AgentDbName, path);
                }

                foreach (var path in _avatarFiles)
                {
                    BulkInsert(AvatarDbName, path);
                }

                DateTimeOffset startMove;
                DateTimeOffset endMove;
                foreach (var path in _ueFiles)
                {
                    BulkInsert(UEDbName, path);
                }

                foreach (var path in _uctFiles)
                {
                    BulkInsert(UCTDbName, path);
                }

                foreach (var path in _umFiles)
                {
                    BulkInsert(UMDbName, path);
                }

                foreach (var path in _ucFiles)
                {
                    BulkInsert(UCDbName, path);
                }

                startMove = DateTimeOffset.Now;
                connection.Open();
                cmd6.CommandTimeout = 300;
                cmd6.ExecuteScalar();
                connection.Close();
                endMove = DateTimeOffset.Now;
                Console.WriteLine("Move Equipments Complete! Time Elapsed: {0}", endMove - startMove);
                foreach (var path in _eFiles)
                {
                    BulkInsert(EDbName, path);
                }

                foreach (var path in _usFiles)
                {
                    BulkInsert(USDbName, path);
                }

                foreach (var path in _uncgFiles)
                {
                    BulkInsert(UNCGDbName, path);
                }

                foreach (var path in _ucyFiles)
                {
                    BulkInsert(UCYDbName, path);
                }

                foreach (var path in _umcFiles)
                {
                    BulkInsert(UMCDbName, path);
                }

                foreach (var path in _scFiles)
                {
                    BulkInsert(SCDbName, path);
                }

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

                startMove = DateTimeOffset.Now;
                connection.Open();
                cmd23.CommandTimeout = 300;
                cmd23.ExecuteScalar();
                connection.Close();
                endMove = DateTimeOffset.Now;
                Console.WriteLine("Move BattleArenaRanking Complete! Time Elapsed: {0}", endMove - startMove);
                foreach (var path in _barFiles)
                {
                    BulkInsert(bARDbName, path);
                }

                foreach (var path in _urFiles)
                {
                    BulkInsert(uRDbName, path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            var stm11 = $"DROP TABLE {EDbName}_Dump;";
            var stm34 = $"DROP TABLE {bARDbName}_Dump;";
            var cmd11 = new MySqlCommand(stm11, connection);
            var cmd34 = new MySqlCommand(stm34, connection);
            DateTimeOffset startDelete;
            DateTimeOffset endDelete;
            startDelete = DateTimeOffset.Now;
            connection.Open();
            cmd11.CommandTimeout = 300;
            cmd11.ExecuteScalar();
            connection.Close();
            endDelete = DateTimeOffset.Now;
            Console.WriteLine("Delete Equipments_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            startDelete = DateTimeOffset.Now;
            connection.Open();
            cmd34.CommandTimeout = 300;
            cmd34.ExecuteScalar();
            connection.Close();
            endDelete = DateTimeOffset.Now;
            Console.WriteLine("Delete BattleArenaRanking_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            DateTimeOffset end = DateTimeOffset.UtcNow;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
            Console.WriteLine("Shop Count for {0} avatars: {1}", avatars.Count, shopOrderCount);
        }

        private async Task SendMessageAsync(string token, string channel, string message)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("channel", channel),
                    new KeyValuePair<string, string>("text", message)
                });

                var url = "https://slack.com/api/chat.postMessage";
                var response = await client.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseJson);
            }
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

            _ueBulkFile.Flush();
            _ueBulkFile.Close();

            _uctBulkFile.Flush();
            _uctBulkFile.Close();

            _uiBulkFile.Flush();
            _uiBulkFile.Close();

            _umBulkFile.Flush();
            _umBulkFile.Close();

            _ucBulkFile.Flush();
            _ucBulkFile.Close();

            _eBulkFile.Flush();
            _eBulkFile.Close();

            _usBulkFile.Flush();
            _usBulkFile.Close();

            _umcBulkFile.Flush();
            _umcBulkFile.Close();

            _uncgBulkFile.Flush();
            _uncgBulkFile.Close();

            _ucyBulkFile.Flush();
            _ucyBulkFile.Close();

            _scBulkFile.Flush();
            _scBulkFile.Close();

            _seBulkFile.Flush();
            _seBulkFile.Close();

            _sctBulkFile.Flush();
            _sctBulkFile.Close();

            _smBulkFile.Flush();
            _smBulkFile.Close();

            _barBulkFile.Flush();
            _barBulkFile.Close();

            _urBulkFile.Flush();
            _urBulkFile.Close();
        }

        private void CreateBulkFiles()
        {
            string agentFilePath = Path.GetRandomFileName();
            _agentBulkFile = new StreamWriter(agentFilePath);

            string avatarFilePath = Path.GetRandomFileName();
            _avatarBulkFile = new StreamWriter(avatarFilePath);

            string ccFilePath = Path.GetRandomFileName();
            _ccBulkFile = new StreamWriter(ccFilePath);

            string ceFilePath = Path.GetRandomFileName();
            _ceBulkFile = new StreamWriter(ceFilePath);

            string ieFilePath = Path.GetRandomFileName();
            _ieBulkFile = new StreamWriter(ieFilePath);

            string ueFilePath = Path.GetRandomFileName();
            _ueBulkFile = new StreamWriter(ueFilePath);

            string uctFilePath = Path.GetRandomFileName();
            _uctBulkFile = new StreamWriter(uctFilePath);

            string uiFilePath = Path.GetRandomFileName();
            _uiBulkFile = new StreamWriter(uiFilePath);

            string umFilePath = Path.GetRandomFileName();
            _umBulkFile = new StreamWriter(umFilePath);

            string ucFilePath = Path.GetRandomFileName();
            _ucBulkFile = new StreamWriter(ucFilePath);

            string eFilePath = Path.GetRandomFileName();
            _eBulkFile = new StreamWriter(eFilePath);

            string usFilePath = Path.GetRandomFileName();
            _usBulkFile = new StreamWriter(usFilePath);

            string umcFilePath = Path.GetRandomFileName();
            _umcBulkFile = new StreamWriter(umcFilePath);

            string uncgFilePath = Path.GetRandomFileName();
            _uncgBulkFile = new StreamWriter(uncgFilePath);

            string ucyFilePath = Path.GetRandomFileName();
            _ucyBulkFile = new StreamWriter(ucyFilePath);

            string scFilePath = Path.GetRandomFileName();
            _scBulkFile = new StreamWriter(scFilePath);

            string seFilePath = Path.GetRandomFileName();
            _seBulkFile = new StreamWriter(seFilePath);

            string sctFilePath = Path.GetRandomFileName();
            _sctBulkFile = new StreamWriter(sctFilePath);

            string smFilePath = Path.GetRandomFileName();
            _smBulkFile = new StreamWriter(smFilePath);

            string barFilePath = Path.GetRandomFileName();
            _barBulkFile = new StreamWriter(barFilePath);

            string urFilePath = Path.GetRandomFileName();
            _urBulkFile = new StreamWriter(urFilePath);

            string fbBarFilePath = Path.GetRandomFileName();
            _fbBarBulkFile = new StreamWriter(fbBarFilePath);

            string fbUsFilePath = Path.GetRandomFileName();
            _fbUsBulkFile = new StreamWriter(fbUsFilePath);

            string dailyMetricsFilePath = Path.GetRandomFileName();
            _dailyMetricsBulkFile = new StreamWriter(dailyMetricsFilePath);

            _agentFiles.Add(agentFilePath);
            _avatarFiles.Add(avatarFilePath);
            _ccFiles.Add(ccFilePath);
            _ceFiles.Add(ceFilePath);
            _ieFiles.Add(ieFilePath);
            _ueFiles.Add(ueFilePath);
            _uctFiles.Add(uctFilePath);
            _uiFiles.Add(uiFilePath);
            _umFiles.Add(umFilePath);
            _ucFiles.Add(ucFilePath);
            _eFiles.Add(eFilePath);
            _usFiles.Add(usFilePath);
            _umcFiles.Add(umcFilePath);
            _uncgFiles.Add(uncgFilePath);
            _ucyFiles.Add(ucyFilePath);
            _scFiles.Add(scFilePath);
            _seFiles.Add(seFilePath);
            _sctFiles.Add(sctFilePath);
            _smFiles.Add(smFilePath);
            _barFiles.Add(barFilePath);
            _urFiles.Add(urFilePath);
            _fbBarFiles.Add(fbBarFilePath);
            _fbUsFiles.Add(fbUsFilePath);
            _dailyMetricFiles.Add(dailyMetricsFilePath);
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
                Console.WriteLine($"Bulk load to {tableName} failed. Retry bulk insert");
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
                    ConflictOption = MySqlBulkLoaderConflictOption.Ignore,
                };

                loader.Load();
                Console.WriteLine($"Bulk load to {tableName} complete.");
                DateTimeOffset end = DateTimeOffset.Now;
                Console.WriteLine("Time elapsed: {0}", end - start);
            }
        }

        private void WriteEquipment(
            long tipIndex,
            Equipment equipment,
            Address agentAddress,
            Address avatarAddress)
        {
            try
            {
                _ueBulkFile.WriteLine(
                    $"{tipIndex};" +
                    $"{equipment.ItemId.ToString()};" +
                    $"{agentAddress.ToString()};" +
                    $"{avatarAddress.ToString()};" +
                    $"{equipment.ItemType.ToString()};" +
                    $"{equipment.ItemSubType.ToString()};" +
                    $"{equipment.Id};" +
                    $"{equipment.BuffSkills.Count};" +
                    $"{equipment.ElementalType.ToString()};" +
                    $"{equipment.Grade};" +
                    $"{equipment.level};" +
                    $"{equipment.SetId};" +
                    $"{equipment.Skills.Count};" +
                    $"{equipment.SpineResourcePath};" +
                    $"{equipment.RequiredBlockIndex};" +
                    $"{equipment.NonFungibleId.ToString()};" +
                    $"{equipment.NonFungibleId.ToString()};" +
                    $"{equipment.UniqueStatType.ToString()}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void WriteRankingEquipment(
            Equipment equipment,
            Address agentAddress,
            Address avatarAddress,
            int equipmentCp)
        {
            try
            {
                _eBulkFile.WriteLine(
                    $"{equipment.ItemId.ToString()};" +
                    $"{agentAddress.ToString()};" +
                    $"{avatarAddress.ToString()};" +
                    $"{equipment.Id};" +
                    $"{equipmentCp};" +
                    $"{equipment.level};" +
                    $"{equipment.ItemSubType.ToString()}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void WriteCostume(
            long tipIndex,
            Costume costume,
            Address agentAddress,
            Address avatarAddress)
        {
            try
            {
                _uctBulkFile.WriteLine(
                    $"{tipIndex};" +
                    $"{costume.ItemId.ToString()};" +
                    $"{agentAddress.ToString()};" +
                    $"{avatarAddress.ToString()};" +
                    $"{costume.ItemType.ToString()};" +
                    $"{costume.ItemSubType.ToString()};" +
                    $"{costume.Id};" +
                    $"{costume.ElementalType.ToString()};" +
                    $"{costume.Grade};" +
                    $"{costume.Equipped};" +
                    $"{costume.SpineResourcePath};" +
                    $"{costume.RequiredBlockIndex};" +
                    $"{costume.NonFungibleId.ToString()};" +
                    $"{costume.TradableId.ToString()}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void WriteMaterial(
            long tipIndex,
            Material material,
            int materialCount,
            Address agentAddress,
            Address avatarAddress)
        {
            try
            {
                _umBulkFile.WriteLine(
                    $"{tipIndex};" +
                    $"{material.ItemId.ToString()};" +
                    $"{agentAddress.ToString()};" +
                    $"{avatarAddress.ToString()};" +
                    $"{material.ItemType.ToString()};" +
                    $"{material.ItemSubType.ToString()};" +
                    $"{materialCount};" +
                    $"{material.Id};" +
                    $"{material.ElementalType.ToString()};" +
                    $"{material.Grade}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void WriteConsumable(
            long tipIndex,
            Consumable consumable,
            Address agentAddress,
            Address avatarAddress)
        {
            try
            {
                _ucBulkFile.WriteLine(
                    $"{tipIndex};" +
                    $"{consumable.ItemId.ToString()};" +
                    $"{agentAddress.ToString()};" +
                    $"{avatarAddress.ToString()};" +
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
                    $"{consumable.MainStat.ToString()}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
