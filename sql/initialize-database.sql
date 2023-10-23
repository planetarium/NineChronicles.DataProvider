CREATE TABLE IF NOT EXISTS `data_provider`.`Agents` (
    `Address` VARCHAR(100) NOT NULL,

    PRIMARY KEY (`Address`),
    UNIQUE INDEX `Address_UNIQUE` (`Address`)
);

CREATE TABLE IF NOT EXISTS `data_provider`.`Avatars` (
    `Address` VARCHAR(100) NOT NULL,
    `AgentAddress` VARCHAR(100) NOT NULL,
    `Name` VARCHAR(100) NOT NULL,
    `AvatarLevel` INT NOT NULL,
    `TitleId` INT,
    `ArmorId` INT,
    `Cp` INT,
    `Timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (`Address`),
    INDEX `fk_Avatars_Agent_idx` (`AgentAddress`),
    UNIQUE INDEX `Address_UNIQUE` (`Address`),
    CONSTRAINT `fk_Avatars_Agent`
    FOREIGN KEY (`AgentAddress`)
    REFERENCES `Agents` (`Address`)
);

CREATE TABLE IF NOT EXISTS `data_provider`.`HackAndSlashes` (
    `Id` VARCHAR(100) NOT NULL,
    `AvatarAddress` VARCHAR(100) NOT NULL,
    `AgentAddress` VARCHAR(100) NOT NULL,
    `StageId` INT NOT NULL,
    `Cleared` BOOLEAN NOT NULL,
    `Mimisbrunnr` BOOLEAN NOT NULL,
    `BlockIndex` BIGINT NOT NULL,
    `Timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (`Id`),
    INDEX `fk_HackAndSlashes_Avatar1_idx` (`AvatarAddress`),
    INDEX `fk_HackAndSlashes_Agent1_idx` (`AgentAddress`),
    CONSTRAINT `fk_HackAndSlashes_Avatar1`
    FOREIGN KEY (`AvatarAddress`)
    REFERENCES `Avatars` (`Address`),
    CONSTRAINT `fk_HackAndSlashes_Agent1`
    FOREIGN KEY (`AgentAddress`)
    REFERENCES `Agents` (`Address`)
);

CREATE TABLE IF NOT EXISTS `data_provider`.`CombinationConsumables` (
    `Id` VARCHAR(100) NOT NULL,
    `AvatarAddress` VARCHAR(100) NOT NULL,
    `AgentAddress` VARCHAR(100) NOT NULL,
    `RecipeId` INT NOT NULL,
    `SlotIndex` INT NOT NULL,
    `BlockIndex` BIGINT NOT NULL,
    `Timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (`Id`),
    INDEX `fk_CombinationConsumables_Avatar1_idx` (`AvatarAddress`),
    INDEX `fk_CombinationConsumables_Agent1_idx` (`AgentAddress`),
    CONSTRAINT `fk_CombinationConsumables_Avatar1`
    FOREIGN KEY (`AvatarAddress`)
    REFERENCES `Avatars` (`Address`),
    CONSTRAINT `fk_CombinationConsumables_Agent1`
    FOREIGN KEY (`AgentAddress`)
    REFERENCES `Agents` (`Address`)
);

CREATE TABLE IF NOT EXISTS `data_provider`.`CombinationEquipments` (
    `Id` VARCHAR(100) NOT NULL,
    `AvatarAddress` VARCHAR(100) NOT NULL,
    `AgentAddress` VARCHAR(100) NOT NULL,
    `RecipeId` INT NOT NULL,
    `SlotIndex` INT NOT NULL,
    `SubRecipeId` INT NOT NULL,
    `BlockIndex` BIGINT NOT NULL,
    `Timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (`Id`),
    INDEX `fk_CombinationEquipments_Avatar1_idx` (`AvatarAddress`),
    INDEX `fk_CombinationEquipments_Agent1_idx` (`AgentAddress`),
    CONSTRAINT `fk_CombinationEquipments_Avatar1`
    FOREIGN KEY (`AvatarAddress`)
    REFERENCES `Avatars` (`Address`),
    CONSTRAINT `fk_CombinationEquipments_Agent1`
    FOREIGN KEY (`AgentAddress`)
    REFERENCES `Agents` (`Address`)
);

CREATE TABLE IF NOT EXISTS `data_provider`.`ItemEnhancements` (
    `Id` VARCHAR(100) NOT NULL,
    `AvatarAddress` VARCHAR(100) NOT NULL,
    `AgentAddress` VARCHAR(100) NOT NULL,
    `ItemId` VARCHAR(100) NOT NULL,
    `MaterialId` VARCHAR(100) NOT NULL,
    `SlotIndex` INT NOT NULL,
    `BurntNCG` decimal(13,2) NOT NULL,
    `BlockIndex` BIGINT NOT NULL,
    `Timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (`Id`),
    INDEX `fk_ItemEnhancements_Avatar1_idx` (`AvatarAddress`),
    INDEX `fk_ItemEnhancements_Agent1_idx` (`AgentAddress`),
    CONSTRAINT `fk_ItemEnhancements_Avatar1`
    FOREIGN KEY (`AvatarAddress`)
    REFERENCES `Avatars` (`Address`),
    CONSTRAINT `fk_ItemEnhancements_Agent1`
    FOREIGN KEY (`AgentAddress`)
    REFERENCES `Agents` (`Address`)
);

CREATE TABLE IF NOT EXISTS `data_provider`.`CraftRankings` (
    `AvatarAddress` VARCHAR(100) NOT NULL,
    `AgentAddress` VARCHAR(100) NOT NULL,
    `CraftCount` INT NOT NULL,
    `BlockIndex` BIGINT NOT NULL,
    `Ranking` INT DEFAULT NULL,

    UNIQUE KEY `AvatarAddress_UNIQUE` (`AvatarAddress`),
    KEY `fk_CrafRankings_Avatar1_idx` (`AvatarAddress`),
    KEY `fk_CrafRankings_Agent1_idx` (`AgentAddress`),
    CONSTRAINT `fk_CrafRankings_Agent1`
    FOREIGN KEY (`AgentAddress`)
    REFERENCES `Agents` (`Address`),
    CONSTRAINT `fk_CrafRankings_Avatar1`
    FOREIGN KEY (`AvatarAddress`)
    REFERENCES `Avatars` (`Address`)
);

CREATE TABLE IF NOT EXISTS `data_provider`.`Equipments` (
    `ItemId` VARCHAR(100) NOT NULL,
    `AgentAddress` VARCHAR(100) NOT NULL,
    `AvatarAddress` VARCHAR(100) NOT NULL,
    `EquipmentId` INT NOT NULL,
    `Cp` INT NOT NULL,
    `Level` INT NOT NULL,
    `ItemSubType` VARCHAR(100) NOT NULL,
    `Timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (`ItemId`),
    INDEX `fk_Equipments_Avatar1_idx` (`AvatarAddress`),
    INDEX `fk_Equipments_Agent1_idx` (`AgentAddress`),
    CONSTRAINT `fk_Equipments_Avatar1`
    FOREIGN KEY (`AvatarAddress`)
    REFERENCES `Avatars` (`Address`),
    CONSTRAINT `fk_Equipments_Agent1`
    FOREIGN KEY (`AgentAddress`)
    REFERENCES `Agents` (`Address`)
);

CREATE TABLE IF NOT EXISTS `data_provider`.`ShopHistoryEquipments` (
    `OrderId` varchar(100) NOT NULL,
    `TxId` varchar(100) NOT NULL,
    `BlockIndex`bigint NOT NULL,
    `BlockHash` varchar(100) NOT NULL,
    `ItemId` varchar(100) NOT NULL,
    `SellerAvatarAddress` varchar(100) NOT NULL,
    `BuyerAvatarAddress` varchar(100) NOT NULL,
    `Price` decimal(13,2) NOT NULL,
    `ItemType` varchar(100) NOT NULL,
    `ItemSubType` varchar(100) NOT NULL,
    `Id` int NOT NULL,
    `BuffSkillCount` int NOT NULL,
    `ElementalType`varchar(100) NOT NULL,
    `Grade`int NOT NULL,
    `SetId`int NOT NULL,
    `SkillsCount`int NOT NULL,
    `SpineResourcePath`varchar(100) NOT NULL,
    `RequiredBlockIndex`bigint NOT NULL,
    `NonFungibleId`varchar(100) NOT NULL,
    `TradableId`varchar(100) NOT NULL,
    `UniqueStatType`varchar(100) NOT NULL,
    `ItemCount` int NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`OrderId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`ShopHistoryCostumes` (
    `OrderId` varchar(100) NOT NULL,
    `TxId` varchar(100) NOT NULL,
    `BlockIndex`bigint NOT NULL,
    `BlockHash` varchar(100) NOT NULL,
    `ItemId` varchar(100) NOT NULL,
    `SellerAvatarAddress` varchar(100) NOT NULL,
    `BuyerAvatarAddress` varchar(100) NOT NULL,
    `Price` decimal(13,2) NOT NULL,
    `ItemType` varchar(100) NOT NULL,
    `ItemSubType` varchar(100) NOT NULL,
    `Id` int NOT NULL,
    `ElementalType`varchar(100) NOT NULL,
    `Grade`int NOT NULL,
    `Equipped`bool NOT NULL,
    `SpineResourcePath`varchar(100) NOT NULL,
    `RequiredBlockIndex`bigint NOT NULL,
    `NonFungibleId`varchar(100) NOT NULL,
    `TradableId`varchar(100) NOT NULL,
    `ItemCount` int NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`OrderId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`ShopHistoryMaterials` (
    `OrderId` varchar(100) NOT NULL,
    `TxId` varchar(100) NOT NULL,
    `BlockIndex`bigint NOT NULL,
    `BlockHash` varchar(100) NOT NULL,
    `ItemId` varchar(100) NOT NULL,
    `SellerAvatarAddress` varchar(100) NOT NULL,
    `BuyerAvatarAddress` varchar(100) NOT NULL,
    `Price` decimal(13,2) NOT NULL,
    `ItemType` varchar(100) NOT NULL,
    `ItemSubType` varchar(100) NOT NULL,
    `Id` int NOT NULL,
    `ElementalType`varchar(100) NOT NULL,
    `Grade`int NOT NULL,
    `ItemCount` int NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`OrderId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`ShopHistoryConsumables` (
    `OrderId` varchar(100) NOT NULL,
    `TxId` varchar(100) NOT NULL,
    `BlockIndex`bigint NOT NULL,
    `BlockHash` varchar(100) NOT NULL,
    `ItemId` varchar(100) NOT NULL,
    `SellerAvatarAddress` varchar(100) NOT NULL,
    `BuyerAvatarAddress` varchar(100) NOT NULL,
    `Price` decimal(13,2) NOT NULL,
    `ItemType` varchar(100) NOT NULL,
    `ItemSubType` varchar(100) NOT NULL,
    `Id` int NOT NULL,
    `BuffSkillCount` int NOT NULL,
    `ElementalType`varchar(100) NOT NULL,
    `Grade`int NOT NULL,
    `SkillsCount`int NOT NULL,
    `RequiredBlockIndex`bigint NOT NULL,
    `NonFungibleId`varchar(100) NOT NULL,
    `TradableId`varchar(100) NOT NULL,
    `MainStat`varchar(100) NOT NULL,
    `ItemCount` int NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`OrderId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`Stakings` (
    `BlockIndex` bigint NOT NULL,
    `AgentAddress` varchar(100) NOT NULL,
    `PreviousAmount` decimal(13,2) NOT NULL,
    `NewAmount` decimal(13,2) NOT NULL,
    `RemainingNCG` decimal(13,2) NOT NULL,
    `PrevStakeStartBlockIndex` bigint NOT NULL,
    `NewStakeStartBlockIndex` bigint NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX (`BlockIndex`, `Timestamp`),
    KEY `fk_Stakings_Agent1_idx` (`AgentAddress`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`ClaimStakeRewards` (
    `Id` varchar(100) NOT NULL,
    `BlockIndex` bigint NOT NULL,
    `AgentAddress` varchar(100) NOT NULL,
    `ClaimRewardAvatarAddress` varchar(100) NOT NULL,
    `HourGlassCount` int NOT NULL,
    `ApPotionCount` int NOT NULL,
    `ClaimStakeStartBlockIndex` bigint NOT NULL,
    `ClaimStakeEndBlockIndex` bigint NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX (`Id`, `BlockIndex`, `Timestamp`),
    KEY `fk_ClaimStakeRewards_Agent1_idx` (`AgentAddress`),
    KEY `fk_ClaimStakeRewards_ClaimRewardAvatarAddress1_idx` (`ClaimRewardAvatarAddress`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`MigrateMonsterCollections` (
    `BlockIndex` bigint NOT NULL,
    `AgentAddress` varchar(100) NOT NULL,
    `MigrationAmount` decimal(13,2) NOT NULL,
    `MigrationStartBlockIndex` bigint NOT NULL,
    `StakeStartBlockIndex` bigint NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX (`BlockIndex`, `Timestamp`),
    KEY `fk_MigratMonsterCollections_Agent1_idx` (`AgentAddress`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`Grindings` (
    `Id` varchar(100) NOT NULL,
    `BlockIndex` bigint NOT NULL,
    `AgentAddress` varchar(100) NOT NULL,
    `AvatarAddress` varchar(100) NOT NULL,
    `EquipmentItemId` varchar(100) NOT NULL,
    `EquipmentId` int NOT NULL,
    `EquipmentLevel` int NOT NULL,
    `Crystal` decimal(13,2) NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX (`Id`, `BlockIndex`, `Timestamp`),
    KEY `fk_Grindings_Agent1_idx` (`AgentAddress`),
    KEY `fk_Grindings_AvatarAddress1_idx` (`AvatarAddress`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`ItemEnhancementFails` (
    `Id` varchar(100) NOT NULL,
    `BlockIndex` bigint NOT NULL,
    `AgentAddress` varchar(100) NOT NULL,
    `AvatarAddress` varchar(100) NOT NULL,
    `EquipmentItemId` varchar(100) NOT NULL,
    `MaterialItemId` varchar(100) NOT NULL,
    `EquipmentLevel` int NOT NULL,
    `GainedCrystal` decimal(13,2) NOT NULL,
    `BurntNCG` decimal(13,2) NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX (`Id`, `BlockIndex`, `Timestamp`),
    KEY `fk_ItemEnhancementFails_Agent1_idx` (`AgentAddress`),
    KEY `fk_ItemEnhancementFails_AvatarAddress1_idx` (`AvatarAddress`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`UnlockEquipmentRecipes` (
    `Id` varchar(100) NOT NULL,
    `BlockIndex` bigint NOT NULL,
    `AgentAddress` varchar(100) NOT NULL,
    `AvatarAddress` varchar(100) NOT NULL,
    `UnlockEquipmentRecipeId` int NOT NULL,
    `BurntCrystal` decimal(13,2) NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX (`Id`, `BlockIndex`, `Timestamp`),
    KEY `fk_UnlockEquipmentRecipes_Agent1_idx` (`AgentAddress`),
    KEY `fk_UnlockEquipmentRecipes_AvatarAddress1_idx` (`AvatarAddress`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`UnlockWorlds` (
    `Id` varchar(100) NOT NULL,
    `BlockIndex` bigint NOT NULL,
    `AgentAddress` varchar(100) NOT NULL,
    `AvatarAddress` varchar(100) NOT NULL,
    `UnlockWorldId` int NOT NULL,
    `BurntCrystal` decimal(13,2) NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX (`Id`, `BlockIndex`, `Timestamp`),
    KEY `fk_UnlockWorlds_Agent1_idx` (`AgentAddress`),
    KEY `fk_UnlockWorlds_AvatarAddress1_idx` (`AvatarAddress`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`ReplaceCombinationEquipmentMaterials` (
    `Id` varchar(100) NOT NULL,
    `BlockIndex` bigint NOT NULL,
    `AgentAddress` varchar(100) NOT NULL,
    `AvatarAddress` varchar(100) NOT NULL,
    `ReplacedMaterialId` int NOT NULL,
    `ReplacedMaterialCount` int NOT NULL,
    `BurntCrystal` decimal(13,2) NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX (`Id`, `BlockIndex`, `Timestamp`),
    KEY `fk_ReplaceCombinationEquipmentMaterials_Agent1_idx` (`AgentAddress`),
    KEY `fk_ReplaceCombinationEquipmentMaterials_AvatarAddress1_idx` (`AvatarAddress`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`HasRandomBuffs` (
    `Id` varchar(100) NOT NULL,
    `BlockIndex` bigint NOT NULL,
    `AgentAddress` varchar(100) NOT NULL,
    `AvatarAddress` varchar(100) NOT NULL,
    `HasStageId` int NOT NULL,
    `GachaCount` int NOT NULL,
    `BurntCrystal` decimal(13,2) NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX (`Id`, `BlockIndex`, `Timestamp`),
    KEY `fk_HasRandomBuffs_Agent1_idx` (`AgentAddress`),
    KEY `fk_HasRandomBuffs_AvatarAddress1_idx` (`AvatarAddress`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`HasWithRandomBuffs` (
    `Id` varchar(100) NOT NULL,
    `BlockIndex` bigint NOT NULL,
    `AgentAddress` varchar(100) NOT NULL,
    `AvatarAddress` varchar(100) NOT NULL,
    `StageId` int NOT NULL,
    `BuffId` int NOT NULL,
    `Cleared` boolean NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX (`Id`, `BlockIndex`, `Timestamp`),
    KEY `fk_HasWithRandomBuffs_Agent1_idx` (`AgentAddress`),
    KEY `fk_HasWithRandomBuffs_AvatarAddress1_idx` (`AvatarAddress`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`JoinArenas` (
    `Id` varchar(100) NOT NULL,
    `BlockIndex` bigint NOT NULL,
    `AgentAddress` varchar(100) NOT NULL,
    `AvatarAddress` varchar(100) NOT NULL,
    `AvatarLevel` int NOT NULL,
    `ArenaRound` int NOT NULL,
    `ChampionshipId` int NOT NULL,
    `BurntCrystal` decimal(13,2) NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX (`Id`, `BlockIndex`, `Timestamp`),
    KEY `fk_JoinArenas_Agent1_idx` (`AgentAddress`),
    KEY `fk_JoinArenas_AvatarAddress1_idx` (`AvatarAddress`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`BattleArenas` (
    `Id` varchar(100) NOT NULL,
    `BlockIndex` bigint NOT NULL,
    `AgentAddress` varchar(100) NOT NULL,
    `AvatarAddress` varchar(100) NOT NULL,
    `AvatarLevel` int NOT NULL,
    `EnemyAvatarAddress` varchar(100) NOT NULL,
    `ChampionshipId` int NOT NULL,
    `Round` int NOT NULL,
    `TicketCount` int NOT NULL,
    `BurntNCG` decimal(13,2) NOT NULL,
    `Victory` boolean NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX (`Id`, `BlockIndex`, `Timestamp`),
    KEY `fk_BattleArenas_Agent1_idx` (`AgentAddress`),
    KEY `fk_BattleArenas_AvatarAddress1_idx` (`AvatarAddress`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`Blocks` (
    `Index` bigint NOT NULL,
    `Hash` varchar(100) NOT NULL,
    `Miner` varchar(100) NOT NULL,
    `Difficulty` bigint NOT NULL,
    `Nonce` varchar(100) NOT NULL,
    `PreviousHash` varchar(100) NOT NULL,
    `ProtocolVersion` int NOT NULL,
    `PublicKey` varchar(100) NOT NULL,
    `StateRootHash` varchar(100) NOT NULL,
    `TotalDifficulty` bigint NOT NULL,
    `TxCount` int NOT NULL,
    `TxHash` varchar(100) NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`Hash`),
    INDEX (`Index`, `Timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`Transactions` (
    `BlockIndex` bigint NOT NULL,
    `BlockHash` varchar(100) NOT NULL,
    `TxId` varchar(100) NOT NULL,
    `Signer` varchar(100) NOT NULL,
    `ActionType` varchar(100) NOT NULL,
    `Nonce` bigint NOT NULL,
    `PublicKey` varchar(100) NOT NULL,
    `UpdatedAddressesCount` int NOT NULL,
    `Date` date NOT NULL,
    `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`TxId`),
    KEY `Date` (`Date`,`Signer`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`HackAndSlashSweeps` (
    `Id` VARCHAR(100) NOT NULL,
    `AgentAddress` VARCHAR(100) NOT NULL,
    `AvatarAddress` VARCHAR(100) NOT NULL,
    `WorldId` INT NOT NULL,
    `StageId` INT NOT NULL,
    `ApStoneCount` INT NOT NULL,
    `ActionPoint` INT NOT NULL,
    `CostumesCount` INT NOT NULL,
    `EquipmentsCount` INT NOT NULL,
    `Cleared` BOOLEAN NOT NULL,
    `Mimisbrunnr` BOOLEAN NOT NULL,
    `BlockIndex` BIGINT NOT NULL,
    `Timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (`Id`),
    INDEX (`BlockIndex`),
    INDEX (`Timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`EventDungeonBattles` (
    `Id` VARCHAR(100) NOT NULL,
    `AgentAddress` VARCHAR(100) NOT NULL,
    `AvatarAddress` VARCHAR(100) NOT NULL,
    `EventDungeonId` INT NOT NULL,
    `EventScheduleId` INT NOT NULL,
    `EventDungeonStageId` INT NOT NULL,
    `RemainingTickets` INT NOT NULL,
    `BurntNCG` decimal(13,2) NOT NULL,
    `Cleared` BOOLEAN NOT NULL,
    `FoodsCount` INT NOT NULL,
    `CostumesCount` INT NOT NULL,
    `EquipmentsCount` INT NOT NULL,
    `RewardItem1Id` INT NOT NULL,
    `RewardItem1Count` INT NOT NULL,
    `RewardItem2Id` INT NOT NULL,
    `RewardItem2Count` INT NOT NULL,
    `RewardItem3Id` INT NOT NULL,
    `RewardItem3Count` INT NOT NULL,
    `RewardItem4Id` INT NOT NULL,
    `RewardItem4Count` INT NOT NULL,
    `RewardItem5Id` INT NOT NULL,
    `RewardItem5Count` INT NOT NULL,
    `RewardItem6Id` INT NOT NULL,
    `RewardItem6Count` INT NOT NULL,
    `RewardItem7Id` INT NOT NULL,
    `RewardItem7Count` INT NOT NULL,
    `RewardItem8Id` INT NOT NULL,
    `RewardItem8Count` INT NOT NULL,
    `RewardItem9Id` INT NOT NULL,
    `RewardItem9Count` INT NOT NULL,
    `RewardItem10Id` INT NOT NULL,
    `RewardItem10Count` INT NOT NULL,
    `BlockIndex` BIGINT NOT NULL,
    `Timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (`Id`),
    INDEX (`BlockIndex`),
    INDEX (`Timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`EventConsumableItemCrafts` (
    `Id` VARCHAR(100) NOT NULL,
    `AgentAddress` VARCHAR(100) NOT NULL,
    `AvatarAddress` VARCHAR(100) NOT NULL,
    `SlotIndex` INT NOT NULL,
    `EventScheduleId` INT NOT NULL,
    `EventConsumableItemRecipeId` INT NOT NULL,
    `RequiredItem1Id` INT NOT NULL,
    `RequiredItem1Count` INT NOT NULL,
    `RequiredItem2Id` INT NOT NULL,
    `RequiredItem2Count` INT NOT NULL,
    `RequiredItem3Id` INT NOT NULL,
    `RequiredItem3Count` INT NOT NULL,
    `RequiredItem4Id` INT NOT NULL,
    `RequiredItem4Count` INT NOT NULL,
    `RequiredItem5Id` INT NOT NULL,
    `RequiredItem5Count` INT NOT NULL,
    `RequiredItem6Id` INT NOT NULL,
    `RequiredItem6Count` INT NOT NULL,
    `BlockIndex` BIGINT NOT NULL,
    `Timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (`Id`),
    INDEX (`BlockIndex`),
    INDEX (`Timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `data_provider`.`Raiders` (
   `Id` int NOT NULL AUTO_INCREMENT,
   `RaidId` int NOT NULL,
   `AvatarName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
   `HighScore` int NOT NULL,
   `TotalScore` int NOT NULL,
   `Cp` int NOT NULL,
   `Level` int NOT NULL,
   `Address` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
   `IconId` int NOT NULL,
   `PurchaseCount` int NOT NULL,
   `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
   `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),

   PRIMARY KEY (`Id`),
   UNIQUE KEY `IX_Raiders_RaidId_Address` (`RaidId`,`Address`)
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
