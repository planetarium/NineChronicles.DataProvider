using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using GraphQL.Execution;
using Libplanet;
using Libplanet.Assets;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using NineChronicles.DataProvider.Store.Models;
using Xunit;

namespace NineChronicles.DataProvider.Tests;

public class WorldBossRankingRewardQueryTest : TestBase
{
    private string _csv = string.Empty;

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(200, false)]
    [InlineData(200, true)]
    public async Task WorldBossRankingReward(int rank, bool canReceive)
    {
        if (canReceive)
        {
            _csv = @"id,boss_id,started_block_index,ended_block_index,fee,ticket_price,additional_ticket_price,max_purchase_count
1,900001,0,0,300,200,100,10";
        }
        string queryAddress = null;
        for (int i = 0; i < 200; i++)
        {
            var avatarAddress =  new PrivateKey().ToAddress().ToHex();
            if (i + 1 == rank)
            {
                queryAddress = avatarAddress;
            }
            var model = new RaiderModel(
                1,
                i.ToString(),
                200 -i,
                200 - i,
                i + 2,
                GameConfig.DefaultAvatarArmorId,
                i,
                avatarAddress
            );
            Context.Raiders.Add(model);
        }

        Assert.NotNull(queryAddress);

        await Context.SaveChangesAsync();


        var query = $@"query {{
        worldBossRankingReward(raidId: 1, avatarAddress: ""{queryAddress}"") {{
            ranking
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
            Assert.Equal(rank, dictionary["ranking"]);
            var models = (object[])dictionary["rewards"];
            Assert.True(models.Any());
            foreach (var model in models)
            {
                var rewardInfo = Assert.IsType<Dictionary<string, object>>(model);
                var quantity = (string)rewardInfo["quantity"];
                var rawCurrency = (Dictionary<string, object>)rewardInfo["currency"];
                var currency = new Currency(ticker: (string) rawCurrency["ticker"], decimalPlaces: (byte) rawCurrency["decimalPlaces"], minters: (IImmutableSet<Address>?) rawCurrency["minters"]);
                FungibleAssetValue.Parse(currency, quantity);
            }
        }
        else
        {
            Assert.Single(result.Errors!);
        }
    }

    protected override IValue? GetStateMock(Address address)
    {
        if (address.Equals(Addresses.GetSheetAddress<WorldBossListSheet>()))
        {
            return _csv.Serialize();
        }

        if (address.Equals(Addresses.GetSheetAddress<RuneSheet>()))
        {
            return @"id,ticker
1001,RUNE_FENRIR1
1002,RUNE_FENRIR2
1003,RUNE_FENRIR3
".Serialize();
        }

        if (address.Equals(Addresses.GetSheetAddress<WorldBossRankingRewardSheet>()))
        {
            return @"id,boss_id,ranking_min,ranking_max,rate_min,rate_max,rune_1_id,rune_1_qty,rune_2_id,rune_2_qty,rune_3_id,rune_3_qty,crystal
1,900001,1,1,0,0,1001,3500,1002,1200,1003,300,900000
2,900001,2,2,0,0,1001,2200,1002,650,1003,150,625000
3,900001,3,3,0,0,1001,1450,1002,450,1003,100,400000
4,900001,4,10,0,0,1001,1000,1002,330,1003,70,250000
5,900001,11,100,0,0,1001,560,1002,150,1003,40,150000
6,900001,0,0,1,30,1001,370,1002,105,1003,25,100000
7,900001,0,0,31,50,1001,230,1002,60,1003,10,50000
8,900001,0,0,51,70,1001,75,1002,20,1003,5,25000
9,900001,0,0,71,100,1001,40,1002,10,0,0,15000".Serialize();
        }

        return null;
    }

    private IReadOnlyList<IValue?> GetStatesMock(IReadOnlyList<Address> addresses) =>
        addresses.Select(GetStateMock).ToArray();

    protected override FungibleAssetValue GetBalanceMock(Address address, Currency currency)
    {
        return FungibleAssetValue.FromRawValue(currency, 0);
    }
}
