CREATE TABLE IF NOT EXISTS `data_provider`.`Agents` (
    `Address` VARCHAR(100) NOT NULL,

    PRIMARY KEY (`Address`),
    UNIQUE INDEX `Address_UNIQUE` (`Address`)
);

CREATE TABLE IF NOT EXISTS `data_provider`.`Avatars` (
    `Address` VARCHAR(100) NOT NULL,
    `AgentAddress` VARCHAR(100) NOT NULL,
    `Name` VARCHAR(100) NOT NULL,
    
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
    `Timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `BlockIndex` LONG NOT NULL,
    
    INDEX `fk_HackAndSlashes_Avatar1_idx` (`AvatarAddress`),
    INDEX `fk_HackAndSlashes_Agent1_idx` (`AgentAddress`),
    CONSTRAINT `fk_HackAndSlashes_Avatar1`
        FOREIGN KEY (`AvatarAddress`)
            REFERENCES `Avatars` (`Address`),
    CONSTRAINT `fk_HackAndSlashes_Agent1`
        FOREIGN KEY (`AgentAddress`)
            REFERENCES `Agents` (`Address`)
);
