CREATE TABLE IF NOT EXISTS `data_provider`.`agent` (
   `address` VARCHAR NOT NULL,

   PRIMARY KEY (`address`),
   UNIQUE INDEX `address_UNIQUE` (`address`)
);

CREATE TABLE IF NOT EXISTS `data_provider`.`avatar` (
    `address` VARCHAR NOT NULL,
    `agent_address` VARCHAR NOT NULL,
    
    PRIMARY KEY (`address`),
    INDEX `fk_avatar_agent_idx` (`agent_address`),
    UNIQUE INDEX `address_UNIQUE` (`address`),
    UNIQUE INDEX `agent_address_UNIQUE` (`agent_address`),
    CONSTRAINT `fk_avatar_agent`
        FOREIGN KEY (`agent_address`)
            REFERENCES `agent` (`address`)
);

CREATE TABLE IF NOT EXISTS `data_provider`.`hack_and_slash` (
    `avatar_address` VARCHAR NOT NULL,
    `agent_address` VARCHAR NOT NULL,
    `stage_id` INT NOT NULL,
    `cleared` BOOLEAN NOT NULL,
    
    INDEX `fk_hack_and_slash_avatar1_idx` (`avatar_address`),
    INDEX `fk_hack_and_slash_agent1_idx` (`agent_address`),
    CONSTRAINT `fk_hack_and_slash_avatar1`
        FOREIGN KEY (`avatar_address`)
            REFERENCES `avatar` (`address`),
    CONSTRAINT `fk_hack_and_slash_agent1`
        FOREIGN KEY (`agent_address`)
            REFERENCES `agent` (`address`)
);
