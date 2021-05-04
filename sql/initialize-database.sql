CREATE TABLE IF NOT EXISTS `agents` (
  `address` VARCHAR NOT NULL,

  PRIMARY KEY (`address`),
  UNIQUE INDEX `address_UNIQUE` (`address`)
);

CREATE TABLE IF NOT EXISTS `avatars` (
  `address` VARCHAR NOT NULL,
  `agents_address` VARCHAR NOT NULL,

  PRIMARY KEY (`address`),
  INDEX `fk_avatars_agents_idx` (`agents_address`),
  UNIQUE INDEX `address_UNIQUE` (`address`)
  UNIQUE INDEX `agents_address_UNIQUE` (`agents_address`),
  CONSTRAINT `fk_avatars_agents`
    FOREIGN KEY (`agents_address`)
    REFERENCES `agents` (`address`)
);

CREATE TABLE IF NOT EXISTS `hack_and_slash` (
  `avatars_address` VARCHAR NOT NULL,
  `agents_address` VARCHAR NOT NULL,
  `stage_id` VARCHAR NOT NULL,
  `cleared` BOOLEAN NOT NULL,
  `succeed` BOOLEAN NOT NULL,

  INDEX `fk_hack_and_slash_avatars1_idx` (`avatars_address`),
  INDEX `fk_hack_and_slash_agents1_idx` (`agents_address`),
  CONSTRAINT `fk_hack_and_slash_avatars1`
    FOREIGN KEY (`avatars_address`)
    REFERENCES `avatars` (`address`),
  CONSTRAINT `fk_hack_and_slash_agents1`
    FOREIGN KEY (`agents_address`)
    REFERENCES `agents` (`address`)
);
