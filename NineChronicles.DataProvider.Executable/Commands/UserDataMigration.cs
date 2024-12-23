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
            [Option('o', Description = "Rocksdb path to migrate.")] string storePath,
            [Option("rocksdb-storetype", Description = "Store type of RocksDb (new or mono).")] string rocksdbStoreType,
            [Option("mysql-server", Description = "A hostname of MySQL server.")] string mysqlServer,
            [Option("mysql-port", Description = "A port of MySQL server.")] uint mysqlPort,
            [Option("mysql-username", Description = "The name of MySQL user.")] string mysqlUsername,
            [Option("mysql-password", Description = "The password of MySQL user.")] string mysqlPassword,
            [Option("mysql-database", Description = "The name of MySQL database to use.")] string mysqlDatabase,
            [Option("slack-token", Description = "Slack token to send the migration data.")] string slackToken,
            [Option("slack-channel", Description = "Slack channel that receives the migration data.")] string slackChannel,
            [Option("network", Description = "Name of network (e.g., Odin or Heimdall)")] string network,
            [Option("data-folder", Description = "Folder path to store bulk files. Defaults to a temporary path.")] string dataFolder = null
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

            dataFolder ??= Path.GetTempPath();
            Directory.CreateDirectory(dataFolder);

            Console.WriteLine($"Using data folder: {dataFolder}");

            CreateBulkFiles();

            Console.WriteLine("0-1");
            using MySqlConnection connection = new MySqlConnection(_connectionString);
            connection.Open();

            Console.WriteLine("0-2");
            var stm = "SELECT `Address` from Avatars";
            var cmd = new MySqlCommand(stm, connection);

            Console.WriteLine("0-3");
            var rdr = cmd.ExecuteReader();
            List<string> avatars = new List<string>();

            Console.WriteLine("0-4");
            while (rdr.Read())
            {
                Console.WriteLine("{0}", rdr.GetString(0));
                avatars.Add(rdr.GetString(0).Replace("0x", string.Empty));
            }

            Console.WriteLine("0-5");
            connection.Close();

            int shopOrderCount = 0;
            bool finalizeBaranking = true;
            Console.WriteLine("1");

            try
            {
                Console.WriteLine("1-1");
                var tipHash = _baseStore.IndexBlockHash(_baseChain.Id, 12803380);
                Console.WriteLine("1-2");
                var tip = _baseStore.GetBlock((BlockHash)tipHash);
                Console.WriteLine("1-3");
                var exec = _baseChain.EvaluateBlock(tip);
                Console.WriteLine("1-4");
                var ev = exec.Last();
                Console.WriteLine("1-5");
                var outputState = new World(blockChainStates.GetWorldState(ev.OutputState));
                Console.WriteLine("1-6");
                var arenaSheet = outputState.GetSheet<ArenaSheet>();
                Console.WriteLine("1-7");
                var arenaData = arenaSheet.GetRoundByBlockIndex(tip.Index);
                Console.WriteLine("1-8");

                Console.WriteLine("2");

                try
                {
                    Console.WriteLine("2-1");
                    var prevArenaEndIndex = arenaData.StartBlockIndex - 1;
                    Console.WriteLine("2-2");
                    var prevArenaData = arenaSheet.GetRoundByBlockIndex(prevArenaEndIndex);
                    Console.WriteLine("2-3");
                    var finalizeBarankingTip = prevArenaEndIndex;
                    Console.WriteLine("2-4");
                    fbBARDbName = $"{fbBARDbName}_{prevArenaData.ChampionshipId}_{prevArenaData.Round}";
                    Console.WriteLine("2-5");

                    connection.Open();
                    Console.WriteLine("2-6");
                    var preBarQuery = $"SELECT `BlockIndex` FROM {fbBARDbName} limit 1";
                    Console.WriteLine("2-7");
                    var preBarCmd = new MySqlCommand(preBarQuery, connection);
                    Console.WriteLine("2-8");

                    var dataReader = preBarCmd.ExecuteReader();
                    Console.WriteLine("2-9");
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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            DateTimeOffset end = DateTimeOffset.UtcNow;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
            Console.WriteLine("Shop Count for {0} avatars: {1}", avatars.Count, shopOrderCount);

            // Delete files after migration
            DeleteAllFiles();
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
            string dataFolder = "/data";
            Directory.CreateDirectory(dataFolder);

            _agentBulkFile = new StreamWriter(Path.Combine(dataFolder, "agentBulkFile.csv"));
            _avatarBulkFile = new StreamWriter(Path.Combine(dataFolder, "avatarBulkFile.csv"));
            _ccBulkFile = new StreamWriter(Path.Combine(dataFolder, "ccBulkFile.csv"));
            _ceBulkFile = new StreamWriter(Path.Combine(dataFolder, "ceBulkFile.csv"));
            _ieBulkFile = new StreamWriter(Path.Combine(dataFolder, "ieBulkFile.csv"));
            _ueBulkFile = new StreamWriter(Path.Combine(dataFolder, "ueBulkFile.csv"));
            _uctBulkFile = new StreamWriter(Path.Combine(dataFolder, "uctBulkFile.csv"));
            _uiBulkFile = new StreamWriter(Path.Combine(dataFolder, "uiBulkFile.csv"));
            _umBulkFile = new StreamWriter(Path.Combine(dataFolder, "umBulkFile.csv"));
            _ucBulkFile = new StreamWriter(Path.Combine(dataFolder, "ucBulkFile.csv"));
            _eBulkFile = new StreamWriter(Path.Combine(dataFolder, "eBulkFile.csv"));
            _usBulkFile = new StreamWriter(Path.Combine(dataFolder, "usBulkFile.csv"));
            _umcBulkFile = new StreamWriter(Path.Combine(dataFolder, "umcBulkFile.csv"));
            _uncgBulkFile = new StreamWriter(Path.Combine(dataFolder, "uncgBulkFile.csv"));
            _ucyBulkFile = new StreamWriter(Path.Combine(dataFolder, "ucyBulkFile.csv"));
            _scBulkFile = new StreamWriter(Path.Combine(dataFolder, "scBulkFile.csv"));
            _seBulkFile = new StreamWriter(Path.Combine(dataFolder, "seBulkFile.csv"));
            _sctBulkFile = new StreamWriter(Path.Combine(dataFolder, "sctBulkFile.csv"));
            _smBulkFile = new StreamWriter(Path.Combine(dataFolder, "smBulkFile.csv"));
            _barBulkFile = new StreamWriter(Path.Combine(dataFolder, "barBulkFile.csv"));
            _urBulkFile = new StreamWriter(Path.Combine(dataFolder, "urBulkFile.csv"));
            _fbBarBulkFile = new StreamWriter(Path.Combine(dataFolder, "fbBarBulkFile.csv"));
            _fbUsBulkFile = new StreamWriter(Path.Combine(dataFolder, "fbUsBulkFile.csv"));

            // Add files to list for deletion
            _agentFiles.Add(Path.Combine(dataFolder, "agentBulkFile.csv"));
            _avatarFiles.Add(Path.Combine(dataFolder, "avatarBulkFile.csv"));
            _ccFiles.Add(Path.Combine(dataFolder, "ccBulkFile.csv"));
            _ceFiles.Add(Path.Combine(dataFolder, "ceBulkFile.csv"));
            _ieFiles.Add(Path.Combine(dataFolder, "ieBulkFile.csv"));
            _ueFiles.Add(Path.Combine(dataFolder, "ueBulkFile.csv"));
            _uctFiles.Add(Path.Combine(dataFolder, "uctBulkFile.csv"));
            _uiFiles.Add(Path.Combine(dataFolder, "uiBulkFile.csv"));
            _umFiles.Add(Path.Combine(dataFolder, "umBulkFile.csv"));
            _ucFiles.Add(Path.Combine(dataFolder, "ucBulkFile.csv"));
            _eFiles.Add(Path.Combine(dataFolder, "eBulkFile.csv"));
            _usFiles.Add(Path.Combine(dataFolder, "usBulkFile.csv"));
            _umcFiles.Add(Path.Combine(dataFolder, "umcBulkFile.csv"));
            _uncgFiles.Add(Path.Combine(dataFolder, "uncgBulkFile.csv"));
            _ucyFiles.Add(Path.Combine(dataFolder, "ucyBulkFile.csv"));
            _scFiles.Add(Path.Combine(dataFolder, "scBulkFile.csv"));
            _seFiles.Add(Path.Combine(dataFolder, "seBulkFile.csv"));
            _sctFiles.Add(Path.Combine(dataFolder, "sctBulkFile.csv"));
            _smFiles.Add(Path.Combine(dataFolder, "smBulkFile.csv"));
            _barFiles.Add(Path.Combine(dataFolder, "barBulkFile.csv"));
            _urFiles.Add(Path.Combine(dataFolder, "urBulkFile.csv"));
            _fbBarFiles.Add(Path.Combine(dataFolder, "fbBarBulkFile.csv"));
            _fbUsFiles.Add(Path.Combine(dataFolder, "fbUsBulkFile.csv"));
        }

        private void DeleteAllFiles()
        {
            List<string> allFiles = _agentFiles
                .Concat(_avatarFiles)
                .Concat(_ccFiles)
                .Concat(_ceFiles)
                .Concat(_ieFiles)
                .Concat(_ueFiles)
                .Concat(_uctFiles)
                .Concat(_uiFiles)
                .Concat(_umFiles)
                .Concat(_ucFiles)
                .Concat(_eFiles)
                .Concat(_usFiles)
                .Concat(_umcFiles)
                .Concat(_uncgFiles)
                .Concat(_ucyFiles)
                .Concat(_scFiles)
                .Concat(_seFiles)
                .Concat(_sctFiles)
                .Concat(_smFiles)
                .Concat(_barFiles)
                .Concat(_urFiles)
                .Concat(_fbBarFiles)
                .Concat(_fbUsFiles)
                .ToList();

            foreach (var file in allFiles)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                        Console.WriteLine($"Deleted file: {file}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file {file}: {ex.Message}");
                }
            }
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
