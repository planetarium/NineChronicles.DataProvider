# NineChronicles DataProvider

- NineChronicles.DataProvider is an off-chain service that stores NineChronicles game action data to a database mainly for game analysis.
- Currently, this service only supports `MySQL` database.

## Table of Contents

- [Pre-requisite](#pre-requisite)
- [Run](#run)
- [Development Guide](#development-guide)
- [Current Table Descriptions](#current-table-descriptions)
- [Migrating Past Chain Data to MySQL Database](#migrating-past-chain-data-to-mysql-database)

## Pre-requisite

- [MySQL](https://www.mysql.com/) and [Entity Framework Core tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) should be installed in the local machine.

## Run

- Before running the program, please refer to the option values in the latest official [9c-launcher-config.json](https://release.nine-chronicles.com/9c-launcher-config.json) and fill out the variables in [appsettings.json](https://github.com/planetarium/NineChronicles.DataProvider/blob/development/NineChronicles.DataProvider.Executable/appsettings.json).
- In [appsettings.json](https://github.com/planetarium/NineChronicles.DataProvider/blob/development/NineChronicles.DataProvider.Executable/appsettings.json), `AppProtocolVersionToken`, `StorePath`, `PeerStrings`, `MySqlConnectionString` properties **MUST** be filled to run the program.
- To setup the NineChronicles mainnet blockchain store to use in the `StorePath`, download and extract the latest [mainnet snapshot](http://snapshots.nine-chronicles.com/main/partition/full/9c-main-snapshot.zip) to a desired location.
```
$ dotnet run --project ./NineChronicles.DataProvider.Executable/ -- 
```

## Development Guide

- This section lays out the steps in how to log a new action in the database.
- The [TransferAsset](https://github.com/planetarium/lib9c/blob/development/Lib9c/Action/TransferAsset.cs) action is used as an example in this guide.

### 1. Setup Database

- To setup the database, navigate to [NineChronicles.DataProvider/NineChronicles.DataProvider.Executable](https://github.com/planetarium/NineChronicles.DataProvider/tree/development/NineChronicles.DataProvider.Executable) directory on your terminal and run the following migration command.
```
dotnet ef database update -- [Connection String]
- Connection String example: "server=localhost;database=data_provider;port=3306;uid=root;pwd=root;"
```

### 2. Create Model

- In [NineChronicles.DataProvider/NineChronicles.DataProvider/Store/Models](https://github.com/planetarium/NineChronicles.DataProvider/tree/development/NineChronicles.DataProvider/Store/Models) directory, create a model file called `TransferAssetModel.cs`.
- In general, `TxId`, `BlockIndex`, `Date`, and `Timestamp` are useful to add as default properties in a model because these values will help with query speed when table size increases.
```
namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class TransferAssetModel
    {
        [Key]
        public string? TxId { get; set; }

        public long BlockIndex { get; set; }

        public string? Sender { get; set; }

        public string? Recipient { get; set; }

        public decimal Amount { get; set; }

        public DateOnly Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}

```

- In [NineChronicles.DataProvider/NineChronicles.DataProvider/Store/NineChroniclesContext.cs](https://github.com/planetarium/NineChronicles.DataProvider/blob/development/NineChronicles.DataProvider/Store/NineChroniclesContext.cs), add a DbSet called `TransferAssets` and its description for reference.

```
// Table for storing TransferAsset actions
public DbSet<TransferAssetModel> TransferAssets => Set<TransferAssetModel>();
```

### 2. Create Store Method

- In [NineChronicles.DataProvider/NineChronicles.DataProvider/Store/MySqlStore.cs](https://github.com/planetarium/NineChronicles.DataProvider/blob/development/NineChronicles.DataProvider/Store/MySqlStore.cs), add a following method that stores the `TransferAsset` data into MySQL.
```
public void StoreTransferAsset(TransferAssetModel model)
{
    using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
    TransferAssetModel? prevModel =
        ctx.TransferAssets.FirstOrDefault(r => r.TxId == model.TxId);
    if (prevModel is null)
    {
        ctx.TransferAssets.Add(model);
    }
    else
    {
        prevModel.BlockIndex = model.BlockIndex;
        prevModel.Sender = model.Sender;
        prevModel.Recipient = model.Recipient;
        prevModel.Amount = model..Amount;
        prevModel.Date = model.Date;
        prevModel.TimeStamp = model.TimeStamp;
        ctx.TransferAssets.Update(prevModel);
    }

    ctx.SaveChanges();
}

```

### 3. (Optional) Add Data Getter
In some cases, you need to handle state to get data to make model.  
To do this easily, you can make your own data getter inside [NineChronicles.DataProvider/DataRendering/](https://github.com/planetarium/NineChronicles.DataProvider/tree/development/NineChronicles.DataProvider/DataRendering).  

### 4. Render & Store Action Data

- In [NineChronicles.DataProvider/NineChronicles.DataProvider/RenderSubscriber.cs](https://github.com/planetarium/NineChronicles.DataProvider/blob/development/NineChronicles.DataProvider/RenderSubscriber.cs), add a following render code
```
_actionRenderer.EveryRender<TransferAsset>()
    .Subscribe(ev =>
    {
        try
        {
            if (ev.Exception is null && ev.Action is { } transferAsset)
            {
                var model = new TransferAssetModel()
                {
                    TxId = transferAsset.TxId,
                    BlockIndex = transferAsset.BlockIndex,
                    Sender = transferAsset.Sender,
                    Recipient = transferAsset.Recipient,
                    Amount = Convert.ToDecimal(transferAsset.Amount.GetQuantityString()),
                    Date = DateOnly.FromDateTime(_blockTimeOffset.DateTime),
                    TimeStamp = _blockTimeOffset,
                };
                MySqlStore.StoreTransferAsset(model);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
```

### 5. Add Database Migration

- Navigate to [NineChronicles.DataProvider/NineChronicles.DataProvider.Executable](https://github.com/planetarium/NineChronicles.DataProvider/tree/development/NineChronicles.DataProvider.Executable) directory on your terminal and run the following migration command
```
dotnet ef migrations add AddTransferAsset -- [Connection String]
- Connection String example: "server=localhost;database=data_provider;port=3306;uid=root;pwd=root;"
```

## Current Table Descriptions

- Tables that `NineChronicles.DataProvider` stores data into are listed in [NineChroniclesContext.cs](https://github.com/planetarium/NineChronicles.DataProvider/blob/development/NineChronicles.DataProvider/Store/NineChroniclesContext.cs).
- Please refer to each `DbSet`'s comment in [NineChroniclesContext.cs](https://github.com/planetarium/NineChronicles.DataProvider/blob/development/NineChronicles.DataProvider/Store/NineChroniclesContext.cs) for table descriptions.

## Migrating Past Chain Data to MySQL Database

- This migration tool migrates all action data based on [DataRendering](https://github.com/planetarium/NineChronicles.DataProvider/blob/development/NineChronicles.DataProvider/DataRendering) to the designated MySQL database.
- Options such as `offset` and `limit` are provided to specify which block data to migrate.
- **IMPORTANT)** This migration tool requires you to have the necessary `state` data in the blocks you want to migrate (If your chain store lacks the `state` data, this tool will not work). 
```
Usage: NineChronicles.DataProvider.Executable mysql-migration [--store-path <String>] [--mysql-server <String>] [--mysql-port <UInt32>] [--mysql-username <String>] [--mysql-password <String>] [--mysql-database <String>] [--offset <I
nt32>] [--limit <Int32>] [--help]

Migrate action data in rocksdb store to mysql db.

Options:
  -o, --store-path <String>    Rocksdb path to migrate. (Required)
  --mysql-server <String>      A hostname of MySQL server. (Required)
  --mysql-port <UInt32>        A port of MySQL server. (Required)
  --mysql-username <String>    The name of MySQL user. (Required)
  --mysql-password <String>    The password of MySQL user. (Required)
  --mysql-database <String>    The name of MySQL database to use. (Required)
  --offset <Int32>             offset of block index (no entry will migrate from the genesis block).
  --limit <Int32>              limit of block count (no entry will migrate to the chain tip).
  -h, --help                   Show help message

```
