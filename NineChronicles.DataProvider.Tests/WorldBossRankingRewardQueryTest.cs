using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Execution;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Types.Assets;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Nekoyume;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using NineChronicles.DataProvider.Store.Models;
using Xunit;

namespace NineChronicles.DataProvider.Tests;

public class WorldBossRankingRewardQueryTest : TestBase
{
    [Theory]
    [InlineData(1, 1L, false, false, 1, "0")]
    [InlineData(200, 1L, false, true, 1, "0")]
    [InlineData(1, 20L, true, true, 1, "900000")]
    [InlineData(2, 20L, true, false, 1, "625000")]
    [InlineData(3, 20L, true, true, 1, "400000")]
    [InlineData(10, 20L, true, false, 1, "250000")]
    [InlineData(100, 20L, true, false, 1, "150000")]
    [InlineData(140, 20L, true, false, 1, "150000")]
    [InlineData(199, 20L, true, false, 1, "150000")]
    [InlineData(200, 20L, true, false, 1, "15000")]
    [InlineData(1, 20L, true, true, 2, "900000")]
    [InlineData(2, 20L, true, false, 2, "625000")]
    [InlineData(3, 20L, true, true, 2, "400000")]
    [InlineData(10, 20L, true, false, 2, "250000")]
    [InlineData(100, 20L, true, false, 2, "150000")]
    [InlineData(140, 20L, true, false, 2, "25000")]
    [InlineData(199, 20L, true, false, 2, "15000")]
    [InlineData(200, 20L, true, false, 2, "15000")]
    public async Task WorldBossRankingReward(int rank, long blockIndex, bool canReceive, bool hex, int raidId, string expectedCrystal)
    {
        if (canReceive)
        {
            Context.WorldBossSeasonMigrationModels.Add(new WorldBossSeasonMigrationModel { RaidId = raidId });
        }
        string queryAddress = null;
        for (int i = 0; i < 200; i++)
        {
            var avatarAddress = new PrivateKey().Address;
            if (i + 1 == rank)
            {
                queryAddress = hex ? avatarAddress.ToHex() : avatarAddress.ToString();
            }
            var model = new RaiderModel(
                raidId,
                i.ToString(),
                200 -i,
                200 - i,
                i + 2,
                GameConfig.DefaultAvatarArmorId,
                i,
                avatarAddress.ToString(),
                0
            );
            Context.Raiders.Add(model);
        }

        Assert.NotNull(queryAddress);

        await SaveBlock(blockIndex);


        var query = $@"query {{
        worldBossRankingReward(raidId: {raidId}, avatarAddress: ""{queryAddress}"") {{
            raider {{
                ranking
            }}
            rewards {{
                quantity
                currency {{
                    minters
                    ticker
                    decimalPlaces
                }}
            }}
        }}
    }}";
        var result = await ExecuteAsync(query);
        if (canReceive)
        {
            var dictionary = (Dictionary<string, object>)((Dictionary<string, object>) ((ExecutionNode) result.Data).ToValue())["worldBossRankingReward"];
            var raider = (Dictionary<string, object>) dictionary["raider"];
            Assert.Equal(rank, raider["ranking"]);
            var models = (object[])dictionary["rewards"];
            Assert.True(models.Any());
            foreach (var model in models)
            {
                var rewardInfo = Assert.IsType<Dictionary<string, object>>(model);
                var quantity = (string)rewardInfo["quantity"];
                var rawCurrency = (Dictionary<string, object>)rewardInfo["currency"];
                var ticker = (string) rawCurrency["ticker"];
#pragma warning disable CS0618
                // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                var currency = Currency.Legacy(
                    ticker,
                    (byte)rawCurrency["decimalPlaces"],
                    (IImmutableSet<Address>?)rawCurrency["minters"]);
#pragma warning restore CS0618
                FungibleAssetValue.Parse(currency, quantity);
                if (ticker == "CRYSTAL")
                {
                    Assert.Equal(expectedCrystal, quantity);
                }
            }
        }
        else
        {
            Assert.Single(result.Errors!);
        }
    }

    [Theory]
    [InlineData(11L, true, 100, 2, 101, 2)]
    [InlineData(11L, true, 0, 2, 1, 2)]
    [InlineData(11L, true, 100, 100, 101, 100)]
    [InlineData(11L, true, 0, 50, 1, 50)]
    [InlineData(11L, false, 100, 2, 100, 2)]
    public async Task WorldBossRankingRewards(long blockIndex, bool canReceive, int offset, int limit, int expectedRank, int expectedCount)
    {
        if (canReceive)
        {
            Context.WorldBossSeasonMigrationModels.Add(new WorldBossSeasonMigrationModel { RaidId = 1 });
        }

        for (int i = 0; i < 200; i++)
        {
            var avatarAddress = new PrivateKey().Address;
            var model = new RaiderModel(
                1,
                i.ToString(),
                200 -i,
                200 - i,
                i + 2,
                GameConfig.DefaultAvatarArmorId,
                i,
                avatarAddress.ToString(),
                0
            );
            Context.Raiders.Add(model);
        }

        await SaveBlock(blockIndex);

        int raidId = canReceive ? 1 : -1;
        var query = $@"query {{
        worldBossRankingRewards(raidId: {raidId}, offset: {offset}, limit: {limit}) {{
            raider {{
                ranking
            }}
            rewards {{
                quantity
                currency {{
                    minters
                    ticker
                    decimalPlaces
                }}
            }}
        }}
    }}";
        var result = await ExecuteAsync(query);
        if (canReceive)
        {
            var queryData = (object[])((Dictionary<string, object>) ((ExecutionNode) result.Data).ToValue())["worldBossRankingRewards"];
            Assert.Equal(expectedCount, queryData.Length);
            var dictionary = (Dictionary<string, object>)queryData.First();
            var raider = (Dictionary<string, object>) dictionary["raider"];
            Assert.Equal(expectedRank, raider["ranking"]);
            var models = (object[])dictionary["rewards"];
            Assert.True(models.Any());
            foreach (var model in models)
            {
                var rewardInfo = Assert.IsType<Dictionary<string, object>>(model);
                var quantity = (string)rewardInfo["quantity"];
                var rawCurrency = (Dictionary<string, object>)rewardInfo["currency"];
#pragma warning disable CS0618
                var currency = Currency.Legacy(ticker: (string) rawCurrency["ticker"], decimalPlaces: (byte) rawCurrency["decimalPlaces"], minters: (IImmutableSet<Address>?) rawCurrency["minters"]);
#pragma warning restore CS0618
                FungibleAssetValue.Parse(currency, quantity);
            }
        }
        else
        {
            Assert.Single(result.Errors!);
        }
    }


    [Theory]
    [InlineData(1, 0, 1, "900000")]
    [InlineData(1, 1, 2, "625000")]
    [InlineData(1, 2, 3, "400000")]
    [InlineData(1, 9, 10, "250000")]
    [InlineData(1, 99, 100, "150000")]
    [InlineData(1, 198, 199, "150000")]
    [InlineData(2, 139, 140, "25000")]
    [InlineData(2, 199, 200, "15000")]
    [InlineData(2, 198, 199, "15000")]
    public async Task WorldBossRankingRewards_Rate(int raidId, int offset, int expectedRank, string expectedCrystal)
    {
        Context.WorldBossSeasonMigrationModels.Add(new WorldBossSeasonMigrationModel { RaidId = raidId });

        for (int i = 0; i < 200; i++)
        {
            var avatarAddress = new PrivateKey().Address;
            var model = new RaiderModel(
                raidId,
                i.ToString(),
                200 -i,
                200 - i,
                i + 2,
                GameConfig.DefaultAvatarArmorId,
                i,
                avatarAddress.ToString(),
                0
            );
            Context.Raiders.Add(model);
        }

        await SaveBlock(11L);


        var query = $@"query {{
        worldBossRankingRewards(raidId: {raidId}, offset: {offset}, limit: 1) {{
            raider {{
                ranking
            }}
            rewards {{
                quantity
                currency {{
                    minters
                    ticker
                    decimalPlaces
                }}
            }}
        }}
    }}";
        var result = await ExecuteAsync(query);
        var queryData = (object[])((Dictionary<string, object>) ((ExecutionNode) result.Data).ToValue())["worldBossRankingRewards"];
        Assert.Single(queryData);
        var dictionary = (Dictionary<string, object>)queryData.First();
        var raider = (Dictionary<string, object>) dictionary["raider"];
        Assert.Equal(expectedRank, raider["ranking"]);
        var models = (object[])dictionary["rewards"];
        Assert.True(models.Any());
        foreach (var model in models)
        {
            var rewardInfo = Assert.IsType<Dictionary<string, object>>(model);
            var quantity = (string)rewardInfo["quantity"];
            var rawCurrency = (Dictionary<string, object>)rewardInfo["currency"];
            var ticker = (string) rawCurrency["ticker"];
#pragma warning disable CS0618
            var currency = Currency.Legacy(ticker: ticker, decimalPlaces: (byte) rawCurrency["decimalPlaces"], minters: (IImmutableSet<Address>?) rawCurrency["minters"]);
#pragma warning restore CS0618
            FungibleAssetValue.Parse(currency, quantity);
            if (ticker == "CRYSTAL")
            {
                Assert.Equal(expectedCrystal, quantity);
            }
        }
    }

    private async Task SaveBlock(long blockIndex)
    {

        var block = new BlockModel
        {
            Index = blockIndex,
            Hash = "4582250d0da33b06779a8475d283d5dd210c683b9b999d74d03fac4f58fa6bce",
            Miner = "47d082a115c63e7b58b1532d20e631538eafadde",
            Difficulty = 0L,
            Nonce = "dff109a0abf1762673ed",
            PreviousHash = "asd",
            ProtocolVersion = 1,
            PublicKey = ByteUtil.Hex(new PrivateKey().PublicKey.ToImmutableArray(false)),
            StateRootHash = "ce667fcd0b69076d9ff7e7755daa2f35cb0488e4c47978468dfbd6b88fca8a90",
            TotalDifficulty = 0L,
            TxCount = 1,
            TxHash = "fd47c10ffbee8ff2da8fa08cec3072de06a72f73693f5d3399b093b0877fa954",
            TimeStamp = DateTimeOffset.UtcNow
        };
        Context.Blocks.Add(block);
        await Context.SaveChangesAsync();
    }

    protected override IWorldState GetMockState()
    {
        var mockWorldState = MockWorldState.CreateModern();
        mockWorldState = mockWorldState.SetAccount(ReservedAddresses.LegacyAccount,
            new Account(mockWorldState.GetAccountState(ReservedAddresses.LegacyAccount))
                .SetState(
                    Addresses.GetSheetAddress<WorldBossListSheet>(),
                    @"id,boss_id,started_block_index,ended_block_index,fee,ticket_price,additional_ticket_price,max_purchase_count
                1,900001,0,10,300,200,100,10
                2,900001,0,10,300,200,100,10
                3,900001,0,10,300,200,100,10".Serialize())
                .SetState(
                    Addresses.GetSheetAddress<RuneSheet>(),
                    @"id,ticker
                1001,RUNE_FENRIR1
                1002,RUNE_FENRIR2
                1003,RUNE_FENRIR3
                ".Serialize())
                .SetState(
                    Addresses.GetSheetAddress<WorldBossRankingRewardSheet>(),
                    @"id,boss_id,ranking_min,ranking_max,rate_min,rate_max,rune_1_id,rune_1_qty,rune_2_id,rune_2_qty,rune_3_id,rune_3_qty,crystal,item_1_id,item_1_qty,item_2_id,item_2_qty
                1,900001,1,1,0,0,1001,3500,1002,1200,1003,300,900000,600201,100,500000,1000
                2,900001,2,2,0,0,1001,2200,1002,650,1003,150,625000,600201,100,500000,1000
                3,900001,3,3,0,0,1001,1450,1002,450,1003,100,400000,600201,100,500000,1000
                4,900001,4,10,0,0,1001,1000,1002,330,1003,70,250000,600201,100,500000,1000
                5,900001,11,100,0,0,1001,560,1002,150,1003,40,150000,600201,100,500000,1000
                6,900001,0,0,1,30,1001,370,1002,105,1003,25,100000,600201,50,500000,100
                7,900001,0,0,31,50,1001,230,1002,60,1003,10,50000,600201,50,500000,100
                8,900001,0,0,51,70,1001,75,1002,20,1003,5,25000,600201,50,500000,100
                9,900001,0,0,71,100,1001,40,1002,10,0,0,15000,600201,50,500000,100".Serialize())
                .SetState(
                    Addresses.GetSheetAddress<MaterialItemSheet>(),
                    @"id,_name,item_sub_type,grade,elemental_type
500000,AP 스톤,ApStone,4,Normal
600201,Golden Dust,EquipmentMaterial,4,Normal
".Serialize())
        );
        return mockWorldState;
    }
}
