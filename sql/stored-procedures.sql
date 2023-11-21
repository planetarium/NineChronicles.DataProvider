// Stored Procedures

DELIMITER $$
CREATE DEFINER=`admin`@`%` PROCEDURE `Craft_Rankings_Procedure`()
BEGIN
DELETE FROM data_provider.CraftRankings;
INSERT INTO data_provider.CraftRankings (
    `AvatarAddress`,
    `AgentAddress`,
    `BlockIndex`,
    `CraftCount`,
    `Ranking`,
    `ArmorId`,
    `AvatarLevel`,
    `Cp`,
    `Name`,
    `TitleId`
)
SELECT
    `h`.`AvatarAddress`,
    `AgentAddress`,
    `BlockIndex`,
    `CraftCount`,
    row_number() over(ORDER BY `CraftCount` DESC, `h`.`BlockIndex`) `Ranking`,
    (SELECT `a`.`ArmorId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `ArmorId`,
    (SELECT `a`.`AvatarLevel` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `AvatarLevel`,
    (SELECT `a`.`Cp` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Cp`,
    (SELECT `a`.`Name` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Name`,
    (SELECT `a`.`TitleId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `TitleId`
FROM (
    SELECT cr.AvatarAddress, cr.AgentAddress, cr.CraftCount, cr.BlockIndex, ROW_NUMBER() OVER(ORDER BY CraftCount DESC) Ranking
    FROM (
    SELECT a.AvatarAddress, MAX(a.AgentAddress) as AgentAddress, MAX(a.BlockIndex) as BlockIndex, SUM(CraftCount) as CraftCount
    FROM (
    SELECT AvatarAddress, MAX(AgentAddress) as AgentAddress, BlockIndex, SUM(CraftCount) as CraftCount
    FROM (
    SELECT AvatarAddress, AgentAddress, BlockIndex, Count(*) as CraftCount
    FROM data_provider.CombinationConsumables
    GROUP BY AvatarAddress, AgentAddress, BlockIndex
    UNION ALL
    SELECT AvatarAddress, AgentAddress, BlockIndex, Count(*) as CraftCount
    FROM data_provider.CombinationEquipments
    GROUP BY AvatarAddress, AgentAddress, BlockIndex
    UNION ALL
    SELECT AvatarAddress, AgentAddress, BlockIndex, Count(*) as CraftCount
    FROM data_provider.ItemEnhancements
    GROUP BY AvatarAddress, AgentAddress, BlockIndex
    ) as subquery
    GROUP BY AvatarAddress, BlockIndex
    ) as a
    GROUP BY AvatarAddress
    ) as cr
    ) as `h`
ON DUPLICATE KEY UPDATE AvatarAddress = h.`AvatarAddress`, AgentAddress = h.`AgentAddress`, CraftCount = h.`CraftCount`, BlockIndex = h.`BlockIndex`, Ranking = h.`Ranking`;
END$$
DELIMITER ;


DELIMITER $$
CREATE DEFINER=`admin`@`%` PROCEDURE `Stage_Ranking_Procedure`()
BEGIN
DELETE FROM data_provider.StageRanking;
INSERT INTO data_provider.StageRanking (
    `Ranking`,
    `ClearedStageId`,
    `AvatarAddress`,
    `AgentAddress`,
    `Name`,
    `AvatarLevel`,
    `TitleId`,
    `ArmorId`,
    `Cp`,
    `BlockIndex`
)
SELECT
    sr.`Ranking`,
    sr.`ClearedStageId`,
    sr.`AvatarAddress`,
    sr.`AgentAddress`,
    sr.`Name`,
    sr.`AvatarLevel`,
    sr.`TitleId`,
    sr.`ArmorId`,
    sr.`Cp`,
    sr.`BlockIndex`
FROM
    (SELECT
        `h`.`AvatarAddress`, `h`.`AgentAddress`, MAX(`h`.`StageId`) AS `ClearedStageId`,
        (SELECT `a`.`Name` FROM `Avatars` AS `a` WHERE `a`.`Address` = `h`.`AvatarAddress` LIMIT 1) AS `Name`,
        (SELECT `a`.`AvatarLevel` FROM `Avatars` AS `a` WHERE `a`.`Address` = `h`.`AvatarAddress` LIMIT 1) AS `AvatarLevel`,
        (SELECT `a`.`TitleId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `h`.`AvatarAddress` LIMIT 1) AS `TitleId`,
        (SELECT `a`.`ArmorId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `h`.`AvatarAddress` LIMIT 1) AS `ArmorId`,
        (SELECT `a`.`Cp` FROM `Avatars` AS `a` WHERE `a`.`Address` = `h`.`AvatarAddress` LIMIT 1) AS `Cp`,
        MIN(`h`.`BlockIndex`) AS `BlockIndex`,
        row_number() over(ORDER BY MAX(`h`.`StageId`) DESC, MIN(`h`.`BlockIndex`)) Ranking
    FROM `HackAndSlashes` AS `h`
    WHERE (`h`.`Mimisbrunnr` = 0) AND `h`.`Cleared`
    GROUP BY `h`.`AvatarAddress`, `h`.`AgentAddress`
    ) as sr
ON DUPLICATE KEY UPDATE ClearedStageId = sr.`ClearedStageId`, AvatarLevel = sr.`AvatarLevel`, TitleId = sr.`TitleId`, Cp = sr.`Cp`, Ranking = sr.`Ranking`;
END$$
DELIMITER ;

DELIMITER $$
CREATE DEFINER=`admin`@`%` PROCEDURE `Equipment_Ranking_Procedure`()
BEGIN
DELETE FROM data_provider.EquipmentRanking;
INSERT INTO data_provider.EquipmentRanking (
    `ItemId`,
    `AgentAddress`,
    `AvatarAddress`,
    `EquipmentId`,
    `Cp`,
    `Level`,
    `ItemSubType`,
    `Name`,
    `AvatarLevel`,
    `TitleId`,
    `ArmorId`,
    `Ranking`
)
SELECT
    er.`ItemId`,
    er.`AgentAddress`,
    er.`AvatarAddress`,
    er.`EquipmentId`,
    er.`Cp`,
    er.`Level`,
    er.`ItemSubType`,
    er.`Name`,
    er.`AvatarLevel`,
    er.`TitleId`,
    er.`ArmorId`,
    er.`Ranking`
FROM
    (SELECT
        `ItemId`, `AgentAddress`, `AvatarAddress`, `EquipmentId`, `Cp`, `Level`, `ItemSubType`,
        (SELECT `a`.`Name` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Name`,
        (SELECT `a`.`AvatarLevel` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `AvatarLevel`,
        (SELECT `a`.`TitleId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `TitleId`,
        (SELECT `a`.`ArmorId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `ArmorId`,
        ROW_NUMBER() OVER(ORDER BY `Cp` DESC, `Level` DESC) Ranking
    FROM `Equipments`
    ) as er
ON DUPLICATE KEY UPDATE AvatarAddress = er.`AvatarAddress`, AgentAddress = er.`AgentAddress`, EquipmentId = er.`EquipmentId`, Cp = er.`Cp`, Level = er.`Level`, ItemSubType = er.`ItemSubType`, Name = er.`Name`, AvatarLevel = er.`AvatarLevel`, TitleId = er.`TitleId`, ArmorId = er.`ArmorId`, Ranking = er.`Ranking`;
END$$
DELIMITER ;

DELIMITER $$
CREATE DEFINER=`admin`@`%` PROCEDURE `Equipment_Ranking_Armor_Procedure`()
BEGIN
DELETE FROM data_provider.EquipmentRankingArmor;
INSERT INTO data_provider.EquipmentRankingArmor (
    `ItemId`,
    `AgentAddress`,
    `AvatarAddress`,
    `EquipmentId`,
    `Cp`,
    `Level`,
    `ItemSubType`,
    `Name`,
    `AvatarLevel`,
    `TitleId`,
    `ArmorId`,
    `Ranking`
)
SELECT
    er.`ItemId`,
    er.`AgentAddress`,
    er.`AvatarAddress`,
    er.`EquipmentId`,
    er.`Cp`,
    er.`Level`,
    er.`ItemSubType`,
    er.`Name`,
    er.`AvatarLevel`,
    er.`TitleId`,
    er.`ArmorId`,
    er.`Ranking`
FROM
    (SELECT
        `ItemId`, `AgentAddress`, `AvatarAddress`, `EquipmentId`, `Cp`, `Level`, `ItemSubType`,
        (SELECT `a`.`Name` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Name`,
        (SELECT `a`.`AvatarLevel` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `AvatarLevel`,
        (SELECT `a`.`TitleId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `TitleId`,
        (SELECT `a`.`ArmorId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `ArmorId`,
        ROW_NUMBER() OVER(ORDER BY `Cp` DESC, `Level` DESC) Ranking
    FROM `Equipments` where `ItemSubType` = "Armor"
    ) as er
ON DUPLICATE KEY UPDATE AvatarAddress = er.`AvatarAddress`, AgentAddress = er.`AgentAddress`, EquipmentId = er.`EquipmentId`, Cp = er.`Cp`, Level = er.`Level`, ItemSubType = er.`ItemSubType`, Name = er.`Name`, AvatarLevel = er.`AvatarLevel`, TitleId = er.`TitleId`, ArmorId = er.`ArmorId`, Ranking = er.`Ranking`;
END$$
DELIMITER ;

DELIMITER $$
CREATE DEFINER=`admin`@`%` PROCEDURE `Equipment_Ranking_Belt_Procedure`()
BEGIN
DELETE FROM data_provider.EquipmentRankingBelt;
INSERT INTO data_provider.EquipmentRankingBelt (
    `ItemId`,
    `AgentAddress`,
    `AvatarAddress`,
    `EquipmentId`,
    `Cp`,
    `Level`,
    `ItemSubType`,
    `Name`,
    `AvatarLevel`,
    `TitleId`,
    `ArmorId`,
    `Ranking`
)
SELECT
    er.`ItemId`,
    er.`AgentAddress`,
    er.`AvatarAddress`,
    er.`EquipmentId`,
    er.`Cp`,
    er.`Level`,
    er.`ItemSubType`,
    er.`Name`,
    er.`AvatarLevel`,
    er.`TitleId`,
    er.`ArmorId`,
    er.`Ranking`
FROM
    (SELECT
        `ItemId`, `AgentAddress`, `AvatarAddress`, `EquipmentId`, `Cp`, `Level`, `ItemSubType`,
        (SELECT `a`.`Name` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Name`,
        (SELECT `a`.`AvatarLevel` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `AvatarLevel`,
        (SELECT `a`.`TitleId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `TitleId`,
        (SELECT `a`.`ArmorId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `ArmorId`,
        ROW_NUMBER() OVER(ORDER BY `Cp` DESC, `Level` DESC) Ranking
    FROM `Equipments` where `ItemSubType` = "Belt"
    ) as er
ON DUPLICATE KEY UPDATE AvatarAddress = er.`AvatarAddress`, AgentAddress = er.`AgentAddress`, EquipmentId = er.`EquipmentId`, Cp = er.`Cp`, Level = er.`Level`, ItemSubType = er.`ItemSubType`, Name = er.`Name`, AvatarLevel = er.`AvatarLevel`, TitleId = er.`TitleId`, ArmorId = er.`ArmorId`, Ranking = er.`Ranking`;
END$$
DELIMITER ;

DELIMITER $$
CREATE DEFINER=`admin`@`%` PROCEDURE `Equipment_Ranking_Necklace_Procedure`()
BEGIN
DELETE FROM data_provider.EquipmentRankingNecklace;
INSERT INTO data_provider.EquipmentRankingNecklace (
    `ItemId`,
    `AgentAddress`,
    `AvatarAddress`,
    `EquipmentId`,
    `Cp`,
    `Level`,
    `ItemSubType`,
    `Name`,
    `AvatarLevel`,
    `TitleId`,
    `ArmorId`,
    `Ranking`
)
SELECT
    er.`ItemId`,
    er.`AgentAddress`,
    er.`AvatarAddress`,
    er.`EquipmentId`,
    er.`Cp`,
    er.`Level`,
    er.`ItemSubType`,
    er.`Name`,
    er.`AvatarLevel`,
    er.`TitleId`,
    er.`ArmorId`,
    er.`Ranking`
FROM
    (SELECT
        `ItemId`, `AgentAddress`, `AvatarAddress`, `EquipmentId`, `Cp`, `Level`, `ItemSubType`,
        (SELECT `a`.`Name` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Name`,
        (SELECT `a`.`AvatarLevel` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `AvatarLevel`,
        (SELECT `a`.`TitleId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `TitleId`,
        (SELECT `a`.`ArmorId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `ArmorId`,
        ROW_NUMBER() OVER(ORDER BY `Cp` DESC, `Level` DESC) Ranking
    FROM `Equipments` where `ItemSubType` = "Necklace"
    ) as er
ON DUPLICATE KEY UPDATE AvatarAddress = er.`AvatarAddress`, AgentAddress = er.`AgentAddress`, EquipmentId = er.`EquipmentId`, Cp = er.`Cp`, Level = er.`Level`, ItemSubType = er.`ItemSubType`, Name = er.`Name`, AvatarLevel = er.`AvatarLevel`, TitleId = er.`TitleId`, ArmorId = er.`ArmorId`, Ranking = er.`Ranking`;
END$$
DELIMITER ;

DELIMITER $$
CREATE DEFINER=`admin`@`%` PROCEDURE `Equipment_Ranking_Ring_Procedure`()
BEGIN
DELETE FROM data_provider.EquipmentRankingRing;
INSERT INTO data_provider.EquipmentRankingRing (
    `ItemId`,
    `AgentAddress`,
    `AvatarAddress`,
    `EquipmentId`,
    `Cp`,
    `Level`,
    `ItemSubType`,
    `Name`,
    `AvatarLevel`,
    `TitleId`,
    `ArmorId`,
    `Ranking`
)
SELECT
    er.`ItemId`,
    er.`AgentAddress`,
    er.`AvatarAddress`,
    er.`EquipmentId`,
    er.`Cp`,
    er.`Level`,
    er.`ItemSubType`,
    er.`Name`,
    er.`AvatarLevel`,
    er.`TitleId`,
    er.`ArmorId`,
    er.`Ranking`
FROM
    (SELECT
        `ItemId`, `AgentAddress`, `AvatarAddress`, `EquipmentId`, `Cp`, `Level`, `ItemSubType`,
        (SELECT `a`.`Name` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Name`,
        (SELECT `a`.`AvatarLevel` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `AvatarLevel`,
        (SELECT `a`.`TitleId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `TitleId`,
        (SELECT `a`.`ArmorId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `ArmorId`,
        ROW_NUMBER() OVER(ORDER BY `Cp` DESC, `Level` DESC) Ranking
    FROM `Equipments` where `ItemSubType` = "Ring"
    ) as er
ON DUPLICATE KEY UPDATE AvatarAddress = er.`AvatarAddress`, AgentAddress = er.`AgentAddress`, EquipmentId = er.`EquipmentId`, Cp = er.`Cp`, Level = er.`Level`, ItemSubType = er.`ItemSubType`, Name = er.`Name`, AvatarLevel = er.`AvatarLevel`, TitleId = er.`TitleId`, ArmorId = er.`ArmorId`, Ranking = er.`Ranking`;
END$$
DELIMITER ;

DELIMITER $$
CREATE DEFINER=`admin`@`%` PROCEDURE `Equipment_Ranking_Weapon_Procedure`()
BEGIN
DELETE FROM data_provider.EquipmentRankingWeapon;
INSERT INTO data_provider.EquipmentRankingWeapon (
    `ItemId`,
    `AgentAddress`,
    `AvatarAddress`,
    `EquipmentId`,
    `Cp`,
    `Level`,
    `ItemSubType`,
    `Name`,
    `AvatarLevel`,
    `TitleId`,
    `ArmorId`,
    `Ranking`
)
SELECT
    er.`ItemId`,
    er.`AgentAddress`,
    er.`AvatarAddress`,
    er.`EquipmentId`,
    er.`Cp`,
    er.`Level`,
    er.`ItemSubType`,
    er.`Name`,
    er.`AvatarLevel`,
    er.`TitleId`,
    er.`ArmorId`,
    er.`Ranking`
FROM
    (SELECT
        `ItemId`, `AgentAddress`, `AvatarAddress`, `EquipmentId`, `Cp`, `Level`, `ItemSubType`,
        (SELECT `a`.`Name` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Name`,
        (SELECT `a`.`AvatarLevel` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `AvatarLevel`,
        (SELECT `a`.`TitleId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `TitleId`,
        (SELECT `a`.`ArmorId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `ArmorId`,
        ROW_NUMBER() OVER(ORDER BY `Cp` DESC, `Level` DESC) Ranking
    FROM `Equipments` where `ItemSubType` = "Weapon"
    ) as er
ON DUPLICATE KEY UPDATE AvatarAddress = er.`AvatarAddress`, AgentAddress = er.`AgentAddress`, EquipmentId = er.`EquipmentId`, Cp = er.`Cp`, Level = er.`Level`, ItemSubType = er.`ItemSubType`, Name = er.`Name`, AvatarLevel = er.`AvatarLevel`, TitleId = er.`TitleId`, ArmorId = er.`ArmorId`, Ranking = er.`Ranking`;
END$$
DELIMITER ;


// EVENT SCHEDULERS
CREATE DEFINER=`admin`@`%` EVENT `CraftRankings` ON SCHEDULE EVERY 1 HOUR STARTS NOW() ON COMPLETION NOT PRESERVE ENABLE DO CALL Craft_Rankings_Procedure();
CREATE DEFINER=`admin`@`%` EVENT `EquipmentRanking` ON SCHEDULE EVERY 1 HOUR STARTS NOW() ON COMPLETION NOT PRESERVE ENABLE DO CALL Equipment_Ranking_Procedure();
CREATE DEFINER=`admin`@`%` EVENT `EquipmentRankingArmor` ON SCHEDULE EVERY 1 HOUR STARTS NOW() ON COMPLETION NOT PRESERVE ENABLE DO CALL Equipment_Ranking_Armor_Procedure();
CREATE DEFINER=`admin`@`%` EVENT `EquipmentRankingBelt` ON SCHEDULE EVERY 1 HOUR STARTS NOW() ON COMPLETION NOT PRESERVE ENABLE DO CALL Equipment_Ranking_Belt_Procedure();
CREATE DEFINER=`admin`@`%` EVENT `EquipmentRankingNecklace` ON SCHEDULE EVERY 1 HOUR STARTS NOW() ON COMPLETION NOT PRESERVE ENABLE DO CALL Equipment_Ranking_Necklace_Procedure();
CREATE DEFINER=`admin`@`%` EVENT `EquipmentRankingRing` ON SCHEDULE EVERY 1 HOUR STARTS NOW() ON COMPLETION NOT PRESERVE ENABLE DO CALL Equipment_Ranking_Ring_Procedure();
CREATE DEFINER=`admin`@`%` EVENT `EquipmentRankingWeapon` ON SCHEDULE EVERY 1 HOUR STARTS NOW() ON COMPLETION NOT PRESERVE ENABLE DO CALL Equipment_Ranking_Weapon_Procedure();
CREATE DEFINER=`admin`@`%` EVENT `StageRanking` ON SCHEDULE EVERY 1 HOUR STARTS NOW() ON COMPLETION NOT PRESERVE ENABLE DO CALL Stage_Ranking_Procedure();
