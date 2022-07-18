namespace NineChronicles.DataProvider.Tools.SubCommand
{
#nullable enable
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Bencodex.Types;
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
    using Nekoyume.Battle;
    using Nekoyume.BlockChain.Policy;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Serilog;
    using Serilog.Events;
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

    public class MySqlMigration
    {
        private const string AgentDbName = "Agents";
        private const string AvatarDbName = "Avatars";
        private const string CCDbName = "CombinationConsumables";
        private string CEDbName = "CombinationEquipmentsData";
        private string IEDbName = "ItemEnhancementsData";
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain<NCAction> _baseChain;
        private StreamWriter _ccBulkFile;
        private StreamWriter _ceBulkFile;
        private StreamWriter _ieBulkFile;
        private StreamWriter _agentBulkFile;
        private StreamWriter _avatarBulkFile;
        private List<string> _agentList;
        private List<string> _avatarList;
        private List<string> _ccFiles;
        private List<string> _ceFiles;
        private List<string> _ieFiles;
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
            _ccFiles = new List<string>();
            _ceFiles = new List<string>();
            _ieFiles = new List<string>();
            _agentFiles = new List<string>();
            _avatarFiles = new List<string>();

            // lists to keep track of inserted addresses to minimize duplicates
            _agentList = new List<string>();
            _avatarList = new List<string>();

            CreateBulkFiles();
            using MySqlConnection connection = new MySqlConnection(_connectionString);
            var stm33 =
                $@"CREATE TABLE IF NOT EXISTS `data_provider`.`{IEDbName}` (
                  `BlockIndex` bigint NOT NULL,
                  `TxId` varchar(100) NOT NULL,
                  `ActionType` varchar(100) NOT NULL,
                  `ItemId` varchar(100) NOT NULL,
                  `PreviousCp` int NOT NULL,
                  `OutputCp` int NOT NULL,
                  `PrevLevel` int NOT NULL,
                  `OutputLevel` int NOT NULL,
                  `EnhancementResult` varchar(100) NOT NULL,
                  `AgentAddress` varchar(100) NOT NULL,
                  `AvatarAddress` varchar(100) NOT NULL,
                  `Name` varchar(100) NOT NULL,
                  `AvatarLevel` int NOT NULL,
                  `ItemType` varchar(100) NOT NULL,
                  `ItemSubType` varchar(100) NOT NULL,
                  `Id` int NOT NULL,
                  `BuffSkillCount` int NOT NULL,
                  `ElementalType` varchar(100) NOT NULL,
                  `Grade` int NOT NULL,
                  `SetId` int NOT NULL,
                  `SkillsCount` int NOT NULL,
                  `SpineResourcePath` varchar(100) NOT NULL,
                  `RequiredBlockIndex` bigint NOT NULL,
                  `NonFungibleId` varchar(100) NOT NULL,
                  `TradableId` varchar(100) NOT NULL,
                  `UniqueStatType` varchar(100) NOT NULL,
                  `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  PRIMARY KEY (`TxId`),
                  INDEX (`BlockIndex`, `Timestamp`),
                  KEY `fk_ItemEnhancementData_Avatar1_idx` (`AvatarAddress`),
                  KEY `fk_ItemEnhancementData_Agent1_idx` (`AgentAddress`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";
            var cmd33 = new MySqlCommand(stm33, connection);
            connection.Open();
            cmd33.CommandTimeout = 300;
            cmd33.ExecuteScalar();
            connection.Close();
            var stm34 =
                $@"CREATE TABLE IF NOT EXISTS `data_provider`.`{CEDbName}` (
                  `BlockIndex` bigint NOT NULL,
                  `TxId` varchar(100) NOT NULL,
                  `ActionType` varchar(100) NOT NULL,
                  `ItemId` varchar(100) NOT NULL,
                  `OutputCp` int NOT NULL,
                  `OutputLevel` int NOT NULL,
                  `AgentAddress` varchar(100) NOT NULL,
                  `AvatarAddress` varchar(100) NOT NULL,
                  `Name` varchar(100) NOT NULL,
                  `AvatarLevel` int NOT NULL,
                  `ItemType` varchar(100) NOT NULL,
                  `ItemSubType` varchar(100) NOT NULL,
                  `Id` int NOT NULL,
                  `BuffSkillCount` int NOT NULL,
                  `ElementalType` varchar(100) NOT NULL,
                  `Grade` int NOT NULL,
                  `SetId` int NOT NULL,
                  `SkillsCount` int NOT NULL,
                  `SpineResourcePath` varchar(100) NOT NULL,
                  `RequiredBlockIndex` bigint NOT NULL,
                  `NonFungibleId` varchar(100) NOT NULL,
                  `TradableId` varchar(100) NOT NULL,
                  `UniqueStatType` varchar(100) NOT NULL,
                  `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  PRIMARY KEY (`TxId`),
                  INDEX (`BlockIndex`, `Timestamp`),
                  KEY `fk_CombinationEquipmentData_Avatar1_idx` (`AvatarAddress`),
                  KEY `fk_CombinationEquipmentData_Agent1_idx` (`AgentAddress`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";
            var cmd34 = new MySqlCommand(stm34, connection);
            connection.Open();
            cmd34.CommandTimeout = 300;
            cmd34.ExecuteScalar();
            connection.Close();

            try
            {
                int totalCount = limit ?? (int)_baseStore.CountBlocks();
                int remainingCount = totalCount;
                int offsetIdx = 0;

                while (remainingCount > 0)
                {
                    int interval = 1000;
                    int limitInterval;
                    if (interval < remainingCount)
                    {
                        limitInterval = interval;
                    }
                    else
                    {
                        limitInterval = remainingCount;
                    }

                    IReadOnlyList<ActionEvaluation> aes = null;
                    foreach (var item in
                        _baseStore.IterateIndexes(_baseChain.Id, offset + offsetIdx ?? 0 + offsetIdx, limitInterval).Select((value, i) => new { i, value }))
                    {
                        var block = _baseStore.GetBlock<NCAction>(blockPolicy.GetHashAlgorithm, item.value);
                        List<string> ceList = new List<string>();
                        Console.WriteLine($"Checking Block #{block.Index}");
                        foreach (var tx in block.Transactions)
                        {
                            if (tx.Actions.FirstOrDefault()?.InnerAction is ItemEnhancement ie)
                            {
                                try
                                {
                                    Console.WriteLine($"Found IE11 action in #{block.Index}");
                                    var outputState = _baseChain.GetState(ie.avatarAddress, block.Hash);
                                    AvatarState avatarState = new AvatarState((Dictionary)outputState);
                                    foreach (var mail in avatarState.mailBox)
                                    {
                                        if (mail.ToString() == "Nekoyume.Model.Mail.ItemEnhanceMail" && mail.blockIndex == block.Index)
                                        {
                                            var itemEnhanceMail = (ItemEnhanceMail)mail;
                                            if (itemEnhanceMail.attachment.itemUsable.ItemId == ie.itemId)
                                            {
                                                Console.WriteLine($"ItemEnhancement11 Action in Block #{block.Index}, TxId: {tx.Id}");
                                                var data = (ItemEnhancement.ResultModel)itemEnhanceMail.attachment;
                                                var prevEnhancementEquipment = (Equipment)data.preItemUsable;
                                                var outputEnhancementEquipment = (Equipment)data.itemUsable;
                                                var avatarLevel = avatarState.level;
                                                string avatarName = avatarState.name;
                                                _ieBulkFile.WriteLine(
                                                    $"{block.Index};" +
                                                    $"{tx.Id};" +
                                                    "Item_Enhancement11;" +
                                                    $"{outputEnhancementEquipment.ItemId.ToString()};" +
                                                    $"{CPHelper.GetCP(prevEnhancementEquipment)};" +
                                                    $"{CPHelper.GetCP(outputEnhancementEquipment)};" +
                                                    $"{prevEnhancementEquipment.level};" +
                                                    $"{outputEnhancementEquipment.level};" +
                                                    $"{data.enhancementResult.ToString()};" +
                                                    $"{tx.Signer.ToString()};" +
                                                    $"{ie.avatarAddress.ToString()};" +
                                                    $"{avatarName};" +
                                                    $"{avatarLevel};" +
                                                    $"{outputEnhancementEquipment.ItemType.ToString()};" +
                                                    $"{outputEnhancementEquipment.ItemSubType.ToString()};" +
                                                    $"{outputEnhancementEquipment.Id};" +
                                                    $"{outputEnhancementEquipment.BuffSkills.Count};" +
                                                    $"{outputEnhancementEquipment.ElementalType.ToString()};" +
                                                    $"{outputEnhancementEquipment.Grade};" +
                                                    $"{outputEnhancementEquipment.SetId};" +
                                                    $"{outputEnhancementEquipment.Skills.Count};" +
                                                    $"{outputEnhancementEquipment.SpineResourcePath};" +
                                                    $"{outputEnhancementEquipment.RequiredBlockIndex};" +
                                                    $"{outputEnhancementEquipment.NonFungibleId.ToString()};" +
                                                    $"{outputEnhancementEquipment.TradableId.ToString()};" +
                                                    $"{outputEnhancementEquipment.UniqueStatType.ToString()};" +
                                                    $"{block.Timestamp:o}"
                                                );
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.Actions.FirstOrDefault()?.InnerAction is ItemEnhancement10 ie10)
                            {
                                try
                                {
                                    Console.WriteLine($"Found IE10 action in #{block.Index}");
                                    var outputState = _baseChain.GetState(ie10.avatarAddress, block.Hash);
                                    AvatarState avatarState = new AvatarState((Dictionary)outputState);
                                    foreach (var mail in avatarState.mailBox)
                                    {
                                        if (mail.ToString() == "Nekoyume.Model.Mail.ItemEnhanceMail" && mail.blockIndex == block.Index)
                                        {
                                            var itemEnhanceMail = (ItemEnhanceMail)mail;
                                            if (itemEnhanceMail.attachment.itemUsable.ItemId == ie10.itemId)
                                            {
                                                Console.WriteLine($"ItemEnhancement10 Action in Block #{block.Index}, TxId: {tx.Id}");
                                                var data = (ItemEnhancement10.ResultModel)itemEnhanceMail.attachment;
                                                var prevEnhancementEquipment = (Equipment)data.preItemUsable;
                                                var outputEnhancementEquipment = (Equipment)data.itemUsable;
                                                var avatarLevel = avatarState.level;
                                                string avatarName = avatarState.name;
                                                _ieBulkFile.WriteLine(
                                                    $"{block.Index};" +
                                                    $"{tx.Id};" +
                                                    "Item_Enhancement10;" +
                                                    $"{outputEnhancementEquipment.ItemId.ToString()};" +
                                                    $"{CPHelper.GetCP(prevEnhancementEquipment)};" +
                                                    $"{CPHelper.GetCP(outputEnhancementEquipment)};" +
                                                    $"{prevEnhancementEquipment.level};" +
                                                    $"{outputEnhancementEquipment.level};" +
                                                    $"{data.enhancementResult.ToString()};" +
                                                    $"{tx.Signer.ToString()};" +
                                                    $"{ie10.avatarAddress.ToString()};" +
                                                    $"{avatarName};" +
                                                    $"{avatarLevel};" +
                                                    $"{outputEnhancementEquipment.ItemType.ToString()};" +
                                                    $"{outputEnhancementEquipment.ItemSubType.ToString()};" +
                                                    $"{outputEnhancementEquipment.Id};" +
                                                    $"{outputEnhancementEquipment.BuffSkills.Count};" +
                                                    $"{outputEnhancementEquipment.ElementalType.ToString()};" +
                                                    $"{outputEnhancementEquipment.Grade};" +
                                                    $"{outputEnhancementEquipment.SetId};" +
                                                    $"{outputEnhancementEquipment.Skills.Count};" +
                                                    $"{outputEnhancementEquipment.SpineResourcePath};" +
                                                    $"{outputEnhancementEquipment.RequiredBlockIndex};" +
                                                    $"{outputEnhancementEquipment.NonFungibleId.ToString()};" +
                                                    $"{outputEnhancementEquipment.TradableId.ToString()};" +
                                                    $"{outputEnhancementEquipment.UniqueStatType.ToString()};" +
                                                    $"{block.Timestamp:o}"
                                                );
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.Actions.FirstOrDefault()?.InnerAction is ItemEnhancement9 ie9)
                            {
                                try
                                {
                                    Console.WriteLine($"Found IE9 action in #{block.Index}");
                                    var outputState = _baseChain.GetState(ie9.avatarAddress, block.Hash);
                                    AvatarState avatarState = new AvatarState((Dictionary)outputState);
                                    foreach (var mail in avatarState.mailBox)
                                    {
                                        if (mail.ToString() == "Nekoyume.Model.Mail.ItemEnhanceMail" && mail.blockIndex == block.Index)
                                        {
                                            var itemEnhanceMail = (ItemEnhanceMail)mail;
                                            if (itemEnhanceMail.attachment.itemUsable.ItemId == ie9.itemId)
                                            {
                                                Console.WriteLine($"ItemEnhancement9 Action in Block #{block.Index}, TxId: {tx.Id}");
                                                var data = (ItemEnhancement9.ResultModel)itemEnhanceMail.attachment;
                                                var prevEnhancementEquipment = (Equipment)data.preItemUsable;
                                                var outputEnhancementEquipment = (Equipment)data.itemUsable;
                                                var avatarLevel = avatarState.level;
                                                string avatarName = avatarState.name;
                                                _ieBulkFile.WriteLine(
                                                    $"{block.Index};" +
                                                    $"{tx.Id};" +
                                                    "Item_Enhancement9;" +
                                                    $"{outputEnhancementEquipment.ItemId.ToString()};" +
                                                    $"{CPHelper.GetCP(prevEnhancementEquipment)};" +
                                                    $"{CPHelper.GetCP(outputEnhancementEquipment)};" +
                                                    $"{prevEnhancementEquipment.level};" +
                                                    $"{outputEnhancementEquipment.level};" +
                                                    $"{data.enhancementResult.ToString()};" +
                                                    $"{tx.Signer.ToString()};" +
                                                    $"{ie9.avatarAddress.ToString()};" +
                                                    $"{avatarName};" +
                                                    $"{avatarLevel};" +
                                                    $"{outputEnhancementEquipment.ItemType.ToString()};" +
                                                    $"{outputEnhancementEquipment.ItemSubType.ToString()};" +
                                                    $"{outputEnhancementEquipment.Id};" +
                                                    $"{outputEnhancementEquipment.BuffSkills.Count};" +
                                                    $"{outputEnhancementEquipment.ElementalType.ToString()};" +
                                                    $"{outputEnhancementEquipment.Grade};" +
                                                    $"{outputEnhancementEquipment.SetId};" +
                                                    $"{outputEnhancementEquipment.Skills.Count};" +
                                                    $"{outputEnhancementEquipment.SpineResourcePath};" +
                                                    $"{outputEnhancementEquipment.RequiredBlockIndex};" +
                                                    $"{outputEnhancementEquipment.NonFungibleId.ToString()};" +
                                                    $"{outputEnhancementEquipment.TradableId.ToString()};" +
                                                    $"{outputEnhancementEquipment.UniqueStatType.ToString()};" +
                                                    $"{block.Timestamp:o}"
                                                );
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.Actions.FirstOrDefault()?.InnerAction is CombinationEquipment ce)
                            {
                                try
                                {
                                    Console.WriteLine($"Found CE12 action in #{block.Index}");
                                    var outputState = _baseChain.GetState(ce.avatarAddress, block.Hash);
                                    AvatarState avatarState = new AvatarState((Dictionary)outputState);
                                    foreach (var mail in avatarState.mailBox)
                                    {
                                        if (mail.ToString() == "Nekoyume.Model.Mail.CombinationMail" && mail.blockIndex == block.Index)
                                        {
                                            var combinationMail = (CombinationMail)mail;
                                            if (!ceList.Contains(combinationMail.attachment.itemUsable.ItemId.ToString()))
                                            {
                                                Console.WriteLine($"CombinationEquipment12 Action in Block #{block.Index}, TxId: {tx.Id}");
                                                ceList.Add(combinationMail.attachment.itemUsable.ItemId.ToString());
                                                var data = (CombinationConsumable5.ResultModel)combinationMail.attachment;
                                                var equipment = (Equipment)data.itemUsable;
                                                var avatarLevel = avatarState.level;
                                                string avatarName = avatarState.name;
                                                _ceBulkFile.WriteLine(
                                                    $"{block.Index};" +
                                                    $"{tx.Id};" +
                                                    "Combination_Equipment12;" +
                                                    $"{equipment.ItemId.ToString()};" +
                                                    $"{CPHelper.GetCP(equipment)};" +
                                                    $"{equipment.level};" +
                                                    $"{tx.Signer.ToString()};" +
                                                    $"{ce.avatarAddress.ToString()};" +
                                                    $"{avatarName};" +
                                                    $"{avatarLevel};" +
                                                    $"{equipment.ItemType.ToString()};" +
                                                    $"{equipment.ItemSubType.ToString()};" +
                                                    $"{equipment.Id};" +
                                                    $"{equipment.BuffSkills.Count};" +
                                                    $"{equipment.ElementalType.ToString()};" +
                                                    $"{equipment.Grade};" +
                                                    $"{equipment.SetId};" +
                                                    $"{equipment.Skills.Count};" +
                                                    $"{equipment.SpineResourcePath};" +
                                                    $"{equipment.RequiredBlockIndex};" +
                                                    $"{equipment.NonFungibleId.ToString()};" +
                                                    $"{equipment.TradableId.ToString()};" +
                                                    $"{equipment.UniqueStatType.ToString()};" +
                                                    $"{block.Timestamp:o}"
                                                );
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.Actions.FirstOrDefault()?.InnerAction is CombinationEquipment11 ce11)
                            {
                                try
                                {
                                    Console.WriteLine($"Found CE11 action in #{block.Index}");
                                    var outputState = _baseChain.GetState(ce11.avatarAddress, block.Hash);
                                    AvatarState avatarState = new AvatarState((Dictionary)outputState);
                                    foreach (var mail in avatarState.mailBox)
                                    {
                                        if (mail.ToString() == "Nekoyume.Model.Mail.CombinationMail" && mail.blockIndex == block.Index)
                                        {
                                            var combinationMail = (CombinationMail)mail;
                                            if (!ceList.Contains(combinationMail.attachment.itemUsable.ItemId.ToString()))
                                            {
                                                Console.WriteLine($"CombinationEquipment11 Action in Block #{block.Index}, TxId: {tx.Id}");
                                                ceList.Add(combinationMail.attachment.itemUsable.ItemId.ToString());
                                                var data = (CombinationConsumable5.ResultModel)combinationMail.attachment;
                                                var equipment = (Equipment)data.itemUsable;
                                                var avatarLevel = avatarState.level;
                                                string avatarName = avatarState.name;
                                                _ceBulkFile.WriteLine(
                                                    $"{block.Index};" +
                                                    $"{tx.Id};" +
                                                    "Combination_Equipment11;" +
                                                    $"{equipment.ItemId.ToString()};" +
                                                    $"{CPHelper.GetCP(equipment)};" +
                                                    $"{equipment.level};" +
                                                    $"{tx.Signer.ToString()};" +
                                                    $"{ce11.avatarAddress.ToString()};" +
                                                    $"{avatarName};" +
                                                    $"{avatarLevel};" +
                                                    $"{equipment.ItemType.ToString()};" +
                                                    $"{equipment.ItemSubType.ToString()};" +
                                                    $"{equipment.Id};" +
                                                    $"{equipment.BuffSkills.Count};" +
                                                    $"{equipment.ElementalType.ToString()};" +
                                                    $"{equipment.Grade};" +
                                                    $"{equipment.SetId};" +
                                                    $"{equipment.Skills.Count};" +
                                                    $"{equipment.SpineResourcePath};" +
                                                    $"{equipment.RequiredBlockIndex};" +
                                                    $"{equipment.NonFungibleId.ToString()};" +
                                                    $"{equipment.TradableId.ToString()};" +
                                                    $"{equipment.UniqueStatType.ToString()};" +
                                                    $"{block.Timestamp:o}"
                                                );
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.Actions.FirstOrDefault()?.InnerAction is CombinationEquipment10 ce10)
                            {
                                try
                                {
                                    Console.WriteLine($"Found CE10 action in #{block.Index}");
                                    var outputState = _baseChain.GetState(ce10.avatarAddress, block.Hash);
                                    AvatarState avatarState = new AvatarState((Dictionary)outputState);
                                    foreach (var mail in avatarState.mailBox)
                                    {
                                        if (mail.ToString() == "Nekoyume.Model.Mail.CombinationMail" && mail.blockIndex == block.Index)
                                        {
                                            var combinationMail = (CombinationMail)mail;
                                            if (!ceList.Contains(combinationMail.attachment.itemUsable.ItemId.ToString()))
                                            {
                                                Console.WriteLine($"CombinationEquipment10 Action in Block #{block.Index}, TxId: {tx.Id}");
                                                ceList.Add(combinationMail.attachment.itemUsable.ItemId.ToString());
                                                var data = (CombinationConsumable5.ResultModel)combinationMail.attachment;
                                                var equipment = (Equipment)data.itemUsable;
                                                var avatarLevel = avatarState.level;
                                                string avatarName = avatarState.name;
                                                _ceBulkFile.WriteLine(
                                                    $"{block.Index};" +
                                                    $"{tx.Id};" +
                                                    "Combination_Equipment10;" +
                                                    $"{equipment.ItemId.ToString()};" +
                                                    $"{CPHelper.GetCP(equipment)};" +
                                                    $"{equipment.level};" +
                                                    $"{tx.Signer.ToString()};" +
                                                    $"{ce10.avatarAddress.ToString()};" +
                                                    $"{avatarName};" +
                                                    $"{avatarLevel};" +
                                                    $"{equipment.ItemType.ToString()};" +
                                                    $"{equipment.ItemSubType.ToString()};" +
                                                    $"{equipment.Id};" +
                                                    $"{equipment.BuffSkills.Count};" +
                                                    $"{equipment.ElementalType.ToString()};" +
                                                    $"{equipment.Grade};" +
                                                    $"{equipment.SetId};" +
                                                    $"{equipment.Skills.Count};" +
                                                    $"{equipment.SpineResourcePath};" +
                                                    $"{equipment.RequiredBlockIndex};" +
                                                    $"{equipment.NonFungibleId.ToString()};" +
                                                    $"{equipment.TradableId.ToString()};" +
                                                    $"{equipment.UniqueStatType.ToString()};" +
                                                    $"{block.Timestamp:o}"
                                                );
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            if (tx.Actions.FirstOrDefault()?.InnerAction is CombinationEquipment9 ce9)
                            {
                                try
                                {
                                    Console.WriteLine($"Found CE9 action in #{block.Index}");
                                    var outputState = _baseChain.GetState(ce9.avatarAddress, block.Hash);
                                    AvatarState avatarState = new AvatarState((Dictionary)outputState);
                                    foreach (var mail in avatarState.mailBox)
                                    {
                                        if (mail.ToString() == "Nekoyume.Model.Mail.CombinationMail" && mail.blockIndex == block.Index)
                                        {
                                            var combinationMail = (CombinationMail)mail;
                                            if (!ceList.Contains(combinationMail.attachment.itemUsable.ItemId.ToString()))
                                            {
                                                Console.WriteLine($"CombinationEquipment9 Action in Block #{block.Index}, TxId: {tx.Id}");
                                                ceList.Add(combinationMail.attachment.itemUsable.ItemId.ToString());
                                                var data = (CombinationConsumable5.ResultModel)combinationMail.attachment;
                                                var equipment = (Equipment)data.itemUsable;
                                                var avatarLevel = avatarState.level;
                                                string avatarName = avatarState.name;
                                                _ceBulkFile.WriteLine(
                                                    $"{block.Index};" +
                                                    $"{tx.Id};" +
                                                    "Combination_Equipment9;" +
                                                    $"{equipment.ItemId.ToString()};" +
                                                    $"{CPHelper.GetCP(equipment)};" +
                                                    $"{equipment.level};" +
                                                    $"{tx.Signer.ToString()};" +
                                                    $"{ce9.avatarAddress.ToString()};" +
                                                    $"{avatarName};" +
                                                    $"{avatarLevel};" +
                                                    $"{equipment.ItemType.ToString()};" +
                                                    $"{equipment.ItemSubType.ToString()};" +
                                                    $"{equipment.Id};" +
                                                    $"{equipment.BuffSkills.Count};" +
                                                    $"{equipment.ElementalType.ToString()};" +
                                                    $"{equipment.Grade};" +
                                                    $"{equipment.SetId};" +
                                                    $"{equipment.Skills.Count};" +
                                                    $"{equipment.SpineResourcePath};" +
                                                    $"{equipment.RequiredBlockIndex};" +
                                                    $"{equipment.NonFungibleId.ToString()};" +
                                                    $"{equipment.TradableId.ToString()};" +
                                                    $"{equipment.UniqueStatType.ToString()};" +
                                                    $"{block.Timestamp:o}"
                                                );
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }
                        }

                        ceList.Clear();
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

                foreach (var path in _ceFiles)
                {
                    BulkInsert(CEDbName, path);
                }

                foreach (var path in _ieFiles)
                {
                    BulkInsert(IEDbName, path);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            DateTimeOffset end = DateTimeOffset.UtcNow;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
        }

        private void ProcessTasks(Task<List<ActionEvaluation>>[] taskArray)
        {
            foreach (var task in taskArray)
            {
                if (task.Result is { } data)
                {
                    foreach (var ae in data)
                    {
                        if (ae.Action is PolymorphicAction<ActionBase> action)
                        {
                            if (action.InnerAction is CombinationConsumable cc)
                            {
                                WriteCC(
                                    cc.Id,
                                    ae.InputContext.Signer,
                                    cc.avatarAddress,
                                    cc.recipeId,
                                    cc.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationConsumable2 cc2)
                            {
                                WriteCC(
                                    cc2.Id,
                                    ae.InputContext.Signer,
                                    cc2.AvatarAddress,
                                    cc2.recipeId,
                                    cc2.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationConsumable3 cc3)
                            {
                                WriteCC(
                                    cc3.Id,
                                    ae.InputContext.Signer,
                                    cc3.AvatarAddress,
                                    cc3.recipeId,
                                    cc3.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationConsumable4 cc4)
                            {
                                WriteCC(
                                    cc4.Id,
                                    ae.InputContext.Signer,
                                    cc4.AvatarAddress,
                                    cc4.recipeId,
                                    cc4.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationConsumable5 cc5)
                            {
                                WriteCC(
                                    cc5.Id,
                                    ae.InputContext.Signer,
                                    cc5.AvatarAddress,
                                    cc5.recipeId,
                                    cc5.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationEquipment ce)
                            {
                                WriteCE(
                                    ce.Id,
                                    ae.InputContext.Signer,
                                    ce.avatarAddress,
                                    ce.recipeId,
                                    ce.slotIndex,
                                    ce.subRecipeId ?? 0,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationEquipment2 ce2)
                            {
                                WriteCE(
                                    ce2.Id,
                                    ae.InputContext.Signer,
                                    ce2.AvatarAddress,
                                    ce2.RecipeId,
                                    ce2.SlotIndex,
                                    ce2.SubRecipeId ?? 0,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationEquipment3 ce3)
                            {
                                WriteCE(
                                    ce3.Id,
                                    ae.InputContext.Signer,
                                    ce3.AvatarAddress,
                                    ce3.RecipeId,
                                    ce3.SlotIndex,
                                    ce3.SubRecipeId ?? 0,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationEquipment4 ce4)
                            {
                                WriteCE(
                                    ce4.Id,
                                    ae.InputContext.Signer,
                                    ce4.AvatarAddress,
                                    ce4.RecipeId,
                                    ce4.SlotIndex,
                                    ce4.SubRecipeId ?? 0,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is CombinationEquipment5 ce5)
                            {
                                WriteCE(
                                    ce5.Id,
                                    ae.InputContext.Signer,
                                    ce5.AvatarAddress,
                                    ce5.RecipeId,
                                    ce5.SlotIndex,
                                    ce5.SubRecipeId ?? 0,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is ItemEnhancement ie)
                            {
                                WriteIE(
                                    ie.Id,
                                    ae.InputContext.Signer,
                                    ie.avatarAddress,
                                    ie.itemId,
                                    ie.materialId,
                                    ie.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is ItemEnhancement2 ie2)
                            {
                                WriteIE(
                                    ie2.Id,
                                    ae.InputContext.Signer,
                                    ie2.avatarAddress,
                                    ie2.itemId,
                                    ie2.materialId,
                                    ie2.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is ItemEnhancement3 ie3)
                            {
                                WriteIE(
                                    ie3.Id,
                                    ae.InputContext.Signer,
                                    ie3.avatarAddress,
                                    ie3.itemId,
                                    ie3.materialId,
                                    ie3.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is ItemEnhancement4 ie4)
                            {
                                WriteIE(
                                    ie4.Id,
                                    ae.InputContext.Signer,
                                    ie4.avatarAddress,
                                    ie4.itemId,
                                    ie4.materialId,
                                    ie4.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is ItemEnhancement5 ie5)
                            {
                                WriteIE(
                                    ie5.Id,
                                    ae.InputContext.Signer,
                                    ie5.avatarAddress,
                                    ie5.itemId,
                                    ie5.materialId,
                                    ie5.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }

                            if (action.InnerAction is ItemEnhancement6 ie6)
                            {
                                WriteIE(
                                    ie6.Id,
                                    ae.InputContext.Signer,
                                    ie6.avatarAddress,
                                    ie6.itemId,
                                    ie6.materialId,
                                    ie6.slotIndex,
                                    ae.InputContext.BlockIndex);
                            }
                        }
                    }
                }
            }
        }

        private List<ActionEvaluation> EvaluateBlock(Block<NCAction> block)
        {
            var evList = _baseChain.ExecuteActions(block).ToList();
            return evList;
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

            _agentFiles.Add(agentFilePath);
            _avatarFiles.Add(avatarFilePath);
            _ccFiles.Add(ccFilePath);
            _ceFiles.Add(ceFilePath);
            _ieFiles.Add(ieFilePath);
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

        private void WriteCC(
            Guid id,
            Address agentAddress,
            Address avatarAddress,
            int recipeId,
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

            _ccBulkFile.WriteLine(
                $"{id.ToString()};" +
                $"{avatarAddress.ToString()};" +
                $"{agentAddress.ToString()};" +
                $"{recipeId};" +
                $"{slotIndex};" +
                $"{blockIndex.ToString()}");
            Console.WriteLine("Writing CC action in block #{0}", blockIndex);
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
