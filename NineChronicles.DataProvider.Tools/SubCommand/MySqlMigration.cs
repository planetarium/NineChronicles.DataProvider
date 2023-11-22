using Libplanet.Action.State;
using Nekoyume.Model.Stake;

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
    using Serilog;
    using Serilog.Events;

    public class MySqlMigration
    {
        private const string AgentDbName = "Agents";
        private const string AvatarDbName = "Avatars";
        private const string CCDbName = "CombinationConsumables";
        private const string CEDbName = "CombinationEquipments";
        private const string IEDbName = "ItemEnhancements";
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
        private string BARDbName = "BattleArenaRanking";
        private string fbBARDbName = "BattleArenaRanking";
        private string fbUSDbName = "UserStakings";
        private string URDbName = "UserRunes";
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
        private List<string> _agentList;
        private List<string> _hourGlassAgentList;
        private List<string> _apStoneAgentList;
        private List<string> _avatarList;
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
                baseStateStore,
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

            // lists to keep track of inserted addresses to minimize duplicates
            _agentList = new List<string>();
            _hourGlassAgentList = new List<string>();
            _apStoneAgentList = new List<string>();
            _avatarList = new List<string>();

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
                var inputState = new Account(blockChainStates.GetAccountState(ev.InputContext.PreviousState));
                var outputState = new Account(blockChainStates.GetAccountState(ev.OutputState));
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

                BARDbName = $"{BARDbName}_{arenaData.ChampionshipId}_{arenaData.Round}";
                Console.WriteLine("1");
                connection.Open();
                var stm33 =
                    $@"CREATE TABLE IF NOT EXISTS `data_provider`.`{BARDbName}` (
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

                foreach (var avatar in avatars)
                {
                    try
                    {
                        intervalCount++;
                        avatarCount++;
                        Console.WriteLine("Interval Count {0}", intervalCount);
                        Console.WriteLine("Migrating {0}/{1}", avatarCount, avatars.Count);
                        var avatarAddress = new Address(avatar);
                        try
                        {
                            avatarState = outputState.GetAvatarStateV2(avatarAddress);
                        }
                        catch (Exception ex)
                        {
                            avatarState = outputState.GetAvatarState(avatarAddress);
                        }

                        var avatarLevel = avatarState.level;

                        var runeSheet = sheets.GetSheet<RuneSheet>();
                        foreach (var runeType in runeSheet.Values)
                        {
#pragma warning disable CS0618
                            var runeCurrency = Currency.Legacy(runeType.Ticker, 0, minters: null);
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
                                    $"{runeType.Ticker};" +
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
                        var orderReceiptList = outputState.TryGetState(orderReceiptAddress, out Dictionary receiptDict)
                            ? new OrderDigestListState(receiptDict)
                            : new OrderDigestListState(orderReceiptAddress);
                        foreach (var orderReceipt in orderReceiptList.OrderDigestList)
                        {
                            if (orderReceipt.ExpiredBlockIndex >= tip.Index)
                            {
                                var state = outputState.GetState(
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
                            if (outputState.TryGetState(monsterCollectionAddress, out Dictionary stateDict))
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
                                if (outputState.TryGetStakeStateV2(avatarState.agentAddress, out StakeStateV2 stakeState2))
                                {
                                    var stakeStateAddress = StakeStateV2.DeriveAddress(avatarState.agentAddress);
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
                            BulkInsert(BARDbName, path);
                        }

                        foreach (var path in _urFiles)
                        {
                            BulkInsert(URDbName, path);
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

                // var stm2 = $"RENAME TABLE {UEDbName} TO {UEDbName}_Dump; CREATE TABLE {UEDbName} LIKE {UEDbName}_Dump;";
                // var stm3 = $"RENAME TABLE {UCTDbName} TO {UCTDbName}_Dump; CREATE TABLE {UCTDbName} LIKE {UCTDbName}_Dump;";
                // var stm4 = $"RENAME TABLE {UMDbName} TO {UMDbName}_Dump; CREATE TABLE {UMDbName} LIKE {UMDbName}_Dump;";
                // var stm5 = $"RENAME TABLE {UCDbName} TO {UCDbName}_Dump; CREATE TABLE {UCDbName} LIKE {UCDbName}_Dump;";
                var stm6 = $"RENAME TABLE {EDbName} TO {EDbName}_Dump; CREATE TABLE {EDbName} LIKE {EDbName}_Dump;";
                // var stm12 = $"RENAME TABLE {USDbName} TO {USDbName}_Dump; CREATE TABLE {USDbName} LIKE {USDbName}_Dump;";
                // var stm13 = $"RENAME TABLE {UNCGDbName} TO {UNCGDbName}_Dump; CREATE TABLE {UNCGDbName} LIKE {UNCGDbName}_Dump;";
                // var stm14 = $"RENAME TABLE {UCYDbName} TO {UCYDbName}_Dump; CREATE TABLE {UCYDbName} LIKE {UCYDbName}_Dump;";
                // var stm15 = $"RENAME TABLE {UMCDbName} TO {UMCDbName}_Dump; CREATE TABLE {UMCDbName} LIKE {UMCDbName}_Dump;";
                // var stm16 = $"RENAME TABLE {SCDbName} TO {SCDbName}_Dump; CREATE TABLE {SCDbName} LIKE {SCDbName}_Dump;";
                // var stm17 = $"RENAME TABLE {SEDbName} TO {SEDbName}_Dump; CREATE TABLE {SEDbName} LIKE {SEDbName}_Dump;";
                // var stm19 = $"RENAME TABLE {SCTDbName} TO {SCTDbName}_Dump; CREATE TABLE {SCTDbName} LIKE {SCTDbName}_Dump;";
                // var stm20 = $"RENAME TABLE {SMDbName} TO {SMDbName}_Dump; CREATE TABLE {SMDbName} LIKE {SMDbName}_Dump;";
                var stm23 = $"RENAME TABLE {BARDbName} TO {BARDbName}_Dump; CREATE TABLE {BARDbName} LIKE {BARDbName}_Dump;";
                // var stm35 = $"RENAME TABLE {URDbName} TO {URDbName}_Dump; CREATE TABLE {URDbName} LIKE {URDbName}_Dump;";
                // var cmd2 = new MySqlCommand(stm2, connection);
                // var cmd3 = new MySqlCommand(stm3, connection);
                // var cmd4 = new MySqlCommand(stm4, connection);
                // var cmd5 = new MySqlCommand(stm5, connection);
                var cmd6 = new MySqlCommand(stm6, connection);
                // var cmd12 = new MySqlCommand(stm12, connection);
                // var cmd13 = new MySqlCommand(stm13, connection);
                // var cmd14 = new MySqlCommand(stm14, connection);
                // var cmd15 = new MySqlCommand(stm15, connection);
                // var cmd16 = new MySqlCommand(stm16, connection);
                // var cmd17 = new MySqlCommand(stm17, connection);
                // var cmd19 = new MySqlCommand(stm19, connection);
                // var cmd20 = new MySqlCommand(stm20, connection);
                var cmd23 = new MySqlCommand(stm23, connection);
                // var cmd35 = new MySqlCommand(stm35, connection);
                foreach (var path in _agentFiles)
                {
                    BulkInsert(AgentDbName, path);
                }

                foreach (var path in _avatarFiles)
                {
                    BulkInsert(AvatarDbName, path);
                }

                var startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd2.CommandTimeout = 300;
                // cmd2.ExecuteScalar();
                // connection.Close();
                var endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move UserEquipments Complete! Time Elapsed: {0}", endMove - startMove);
                foreach (var path in _ueFiles)
                {
                    BulkInsert(UEDbName, path);
                }

                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd3.CommandTimeout = 300;
                // cmd3.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move UserCostumes Complete! Time Elapsed: {0}", endMove - startMove);
                foreach (var path in _uctFiles)
                {
                    BulkInsert(UCTDbName, path);
                }

                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd4.CommandTimeout = 300;
                // cmd4.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move UserMaterials Complete! Time Elapsed: {0}", endMove - startMove);
                foreach (var path in _umFiles)
                {
                    BulkInsert(UMDbName, path);
                }

                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd5.CommandTimeout = 300;
                // cmd5.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move UserConsumables Complete! Time Elapsed: {0}", endMove - startMove);
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

                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd12.CommandTimeout = 300;
                // cmd12.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move UserStakings Complete! Time Elapsed: {0}", endMove - startMove);
                foreach (var path in _usFiles)
                {
                    BulkInsert(USDbName, path);
                }

                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd13.CommandTimeout = 300;
                // cmd13.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move UserNCGs Complete! Time Elapsed: {0}", endMove - startMove);
                foreach (var path in _uncgFiles)
                {
                    BulkInsert(UNCGDbName, path);
                }

                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd14.CommandTimeout = 300;
                // cmd14.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move UserCrystals Complete! Time Elapsed: {0}", endMove - startMove);
                foreach (var path in _ucyFiles)
                {
                    BulkInsert(UCYDbName, path);
                }

                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd15.CommandTimeout = 300;
                // cmd15.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move UserMonsterCollections Complete! Time Elapsed: {0}", endMove - startMove);
                foreach (var path in _umcFiles)
                {
                    BulkInsert(UMCDbName, path);
                }

                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd16.CommandTimeout = 300;
                // cmd16.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move ShopConsumables Complete! Time Elapsed: {0}", endMove - startMove);
                foreach (var path in _scFiles)
                {
                    BulkInsert(SCDbName, path);
                }

                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd17.CommandTimeout = 300;
                // cmd17.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move ShopEquipments Complete! Time Elapsed: {0}", endMove - startMove);
                foreach (var path in _seFiles)
                {
                    BulkInsert(SEDbName, path);
                }

                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd19.CommandTimeout = 300;
                // cmd19.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move ShopCostumes Complete! Time Elapsed: {0}", endMove - startMove);
                foreach (var path in _sctFiles)
                {
                    BulkInsert(SCTDbName, path);
                }

                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd20.CommandTimeout = 300;
                // cmd20.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move ShopMaterials Complete! Time Elapsed: {0}", endMove - startMove);
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
                    BulkInsert(BARDbName, path);
                }

                // startMove = DateTimeOffset.Now;
                // connection.Open();
                // cmd35.CommandTimeout = 300;
                // cmd35.ExecuteScalar();
                // connection.Close();
                // endMove = DateTimeOffset.Now;
                // Console.WriteLine("Move UserRunes Complete! Time Elapsed: {0}", endMove - startMove);
                foreach (var path in _urFiles)
                {
                    BulkInsert(URDbName, path);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            // var stm7 = $"DROP TABLE {UEDbName}_Dump;";
            // var stm8 = $"DROP TABLE {UCTDbName}_Dump;";
            // var stm9 = $"DROP TABLE {UMDbName}_Dump;";
            // var stm10 = $"DROP TABLE {UCDbName}_Dump;";
            var stm11 = $"DROP TABLE {EDbName}_Dump;";
            // var stm18 = $"DROP TABLE {USDbName}_Dump;";
            // var stm21 = $"DROP TABLE {UNCGDbName}_Dump;";
            // var stm22 = $"DROP TABLE {UCYDbName}_Dump;";
            // var stm24 = $"DROP TABLE {UMCDbName}_Dump;";
            // var stm29 = $"DROP TABLE {SCDbName}_Dump;";
            // var stm30 = $"DROP TABLE {SEDbName}_Dump;";
            // var stm31 = $"DROP TABLE {SCTDbName}_Dump;";
            // var stm32 = $"DROP TABLE {SMDbName}_Dump;";
            var stm34 = $"DROP TABLE {BARDbName}_Dump;";
            // var stm37 = $"DROP TABLE {URDbName}_Dump;";
            // var cmd7 = new MySqlCommand(stm7, connection);
            // var cmd8 = new MySqlCommand(stm8, connection);
            // var cmd9 = new MySqlCommand(stm9, connection);
            // var cmd10 = new MySqlCommand(stm10, connection);
            var cmd11 = new MySqlCommand(stm11, connection);
            // var cmd18 = new MySqlCommand(stm18, connection);
            // var cmd21 = new MySqlCommand(stm21, connection);
            // var cmd22 = new MySqlCommand(stm22, connection);
            // var cmd24 = new MySqlCommand(stm24, connection);
            // var cmd29 = new MySqlCommand(stm29, connection);
            // var cmd30 = new MySqlCommand(stm30, connection);
            // var cmd31 = new MySqlCommand(stm31, connection);
            // var cmd32 = new MySqlCommand(stm32, connection);
            var cmd34 = new MySqlCommand(stm34, connection);
            // var cmd37 = new MySqlCommand(stm37, connection);
            var startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd7.CommandTimeout = 300;
            // cmd7.ExecuteScalar();
            // connection.Close();
            var endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete UserEquipments_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd8.CommandTimeout = 300;
            // cmd8.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete UserCostumes_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd9.CommandTimeout = 300;
            // cmd9.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete UserMaterials_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd10.CommandTimeout = 300;
            // cmd10.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete UserConsumables_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            startDelete = DateTimeOffset.Now;
            connection.Open();
            cmd11.CommandTimeout = 300;
            cmd11.ExecuteScalar();
            connection.Close();
            endDelete = DateTimeOffset.Now;
            Console.WriteLine("Delete Equipments_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd18.CommandTimeout = 300;
            // cmd18.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete UserStakings_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd21.CommandTimeout = 300;
            // cmd21.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete UserNCGs_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd22.CommandTimeout = 300;
            // cmd22.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete UserCrystals_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd24.CommandTimeout = 300;
            // cmd24.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete UserMonsterCollections_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd29.CommandTimeout = 300;
            // cmd29.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete ShopConsumables_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd30.CommandTimeout = 300;
            // cmd30.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete ShopEquipments_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd31.CommandTimeout = 300;
            // cmd31.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete ShopCostumes_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd32.CommandTimeout = 300;
            // cmd32.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete ShopMaterials_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            startDelete = DateTimeOffset.Now;
            connection.Open();
            cmd34.CommandTimeout = 300;
            cmd34.ExecuteScalar();
            connection.Close();
            endDelete = DateTimeOffset.Now;
            Console.WriteLine("Delete BattleArenaRanking_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);
            // startDelete = DateTimeOffset.Now;
            // connection.Open();
            // cmd37.CommandTimeout = 300;
            // cmd37.ExecuteScalar();
            // connection.Close();
            // endDelete = DateTimeOffset.Now;
            // Console.WriteLine("Delete UserRunes_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);

            DateTimeOffset end = DateTimeOffset.UtcNow;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
            Console.WriteLine("Shop Count for {0} avatars: {1}", avatars.Count, shopOrderCount);
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
            string agentFilePath = Path.GetTempFileName();
            _agentBulkFile = new StreamWriter(agentFilePath);

            string avatarFilePath = Path.GetTempFileName();
            _avatarBulkFile = new StreamWriter(avatarFilePath);

            string ccFilePath = Path.GetTempFileName();
            _ccBulkFile = new StreamWriter(ccFilePath);

            string ceFilePath = Path.GetTempFileName();
            _ceBulkFile = new StreamWriter(ceFilePath);

            string ieFilePath = Path.GetTempFileName();
            _ieBulkFile = new StreamWriter(ieFilePath);

            string ueFilePath = Path.GetTempFileName();
            _ueBulkFile = new StreamWriter(ueFilePath);

            string uctFilePath = Path.GetTempFileName();
            _uctBulkFile = new StreamWriter(uctFilePath);

            string uiFilePath = Path.GetTempFileName();
            _uiBulkFile = new StreamWriter(uiFilePath);

            string umFilePath = Path.GetTempFileName();
            _umBulkFile = new StreamWriter(umFilePath);

            string ucFilePath = Path.GetTempFileName();
            _ucBulkFile = new StreamWriter(ucFilePath);

            string eFilePath = Path.GetTempFileName();
            _eBulkFile = new StreamWriter(eFilePath);

            string usFilePath = Path.GetTempFileName();
            _usBulkFile = new StreamWriter(usFilePath);

            string umcFilePath = Path.GetTempFileName();
            _umcBulkFile = new StreamWriter(umcFilePath);

            string uncgFilePath = Path.GetTempFileName();
            _uncgBulkFile = new StreamWriter(uncgFilePath);

            string ucyFilePath = Path.GetTempFileName();
            _ucyBulkFile = new StreamWriter(ucyFilePath);

            string scFilePath = Path.GetTempFileName();
            _scBulkFile = new StreamWriter(scFilePath);

            string seFilePath = Path.GetTempFileName();
            _seBulkFile = new StreamWriter(seFilePath);

            string sctFilePath = Path.GetTempFileName();
            _sctBulkFile = new StreamWriter(sctFilePath);

            string smFilePath = Path.GetTempFileName();
            _smBulkFile = new StreamWriter(smFilePath);

            string barFilePath = Path.GetTempFileName();
            _barBulkFile = new StreamWriter(barFilePath);

            string urFilePath = Path.GetTempFileName();
            _urBulkFile = new StreamWriter(urFilePath);

            string fbBarFilePath = Path.GetTempFileName();
            _fbBarBulkFile = new StreamWriter(fbBarFilePath);

            string fbUsFilePath = Path.GetTempFileName();
            _fbUsBulkFile = new StreamWriter(fbUsFilePath);

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


        private void WriteAgent(
            Address? agentAddress)
        {
            if (agentAddress == null)
            {
                return;
            }

            // _agentBulkFile.WriteLine(
            //     $"{agentAddress.ToString()}");
            // _agentList.Add(agentAddress.ToString());
            // check if address is already in _agentList
            if (!_agentList.Contains(agentAddress.ToString()))
            {
                _agentBulkFile.WriteLine(
                    $"{agentAddress.ToString()}");
                _agentList.Add(agentAddress.ToString());
            }
        }

        private void WriteAvatar(
            Address? agentAddress,
            Address? avatarAddress,
            string avatarName,
            int avatarLevel,
            int? avatarTitleId,
            int avatarArmorId,
            int avatarCp)
        {
            if (agentAddress == null)
            {
                return;
            }

            if (avatarAddress == null)
            {
                return;
            }

            if (!_avatarList.Contains(avatarAddress.ToString()))
            {
                _avatarBulkFile.WriteLine(
                    $"{avatarAddress.ToString()};" +
                    $"{agentAddress.ToString()};" +
                    $"{avatarName};" +
                    $"{avatarLevel};" +
                    $"{avatarTitleId};" +
                    $"{avatarArmorId};" +
                    $"{avatarCp}");
                _avatarList.Add(avatarAddress.ToString());
            }
        }

        private void WriteCE(
            Guid id,
            Address agentAddress,
            Address avatarAddress,
            int recipeId,
            int slotIndex,
            int? subRecipeId,
            long blockIndex)
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
                    "N/A");
                _avatarList.Add(avatarAddress.ToString());
            }

            _ceBulkFile.WriteLine(
                $"{id.ToString()};" +
                $"{avatarAddress.ToString()};" +
                $"{agentAddress.ToString()};" +
                $"{recipeId};" +
                $"{slotIndex};" +
                $"{subRecipeId ?? 0};" +
                $"{blockIndex.ToString()}");
            Console.WriteLine("Writing CE action in block #{0}", blockIndex);
        }

        private void WriteIE(
            Guid id,
            Address agentAddress,
            Address avatarAddress,
            Guid itemId,
            Guid materialId,
            int slotIndex,
            long blockIndex)
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
                    "N/A");
                _avatarList.Add(avatarAddress.ToString());
            }

            _ieBulkFile.WriteLine(
                $"{id.ToString()};" +
                $"{avatarAddress.ToString()};" +
                $"{agentAddress.ToString()};" +
                $"{itemId.ToString()};" +
                $"{materialId.ToString()};" +
                $"{slotIndex};" +
                $"{blockIndex.ToString()}");
            Console.WriteLine("Writing IE action in block #{0}", blockIndex);
        }
    }
}
