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
    `Timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `BlockIndex` BIGINT NOT NULL,

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
