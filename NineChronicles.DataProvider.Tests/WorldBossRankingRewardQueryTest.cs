using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Execution;
using Libplanet;
using Libplanet.Assets;
using Libplanet.Crypto;
using Moq;
using Nekoyume;
using NineChronicles.DataProvider.Store.Models;
using Xunit;

namespace NineChronicles.DataProvider.Tests;

public class WorldBossRankingRewardQueryTest : TestBase
{
    [Theory]
    [InlineData(1)]
    [InlineData(200)]
    public async Task WorldBossRankingReward(int rank)
    {
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
        var a = new Mock<Sheets>();


        var query = $@"query {{
        worldBossRankingReward(raidId: 1, avatarAddress: ""{queryAddress}"") {{
            quantity
            currency {{
                minters
                ticker
                decimalPlaces
            }}
        }}
    }}";
        var result = await ExecuteAsync(query);
        var models = (object[])((Dictionary<string, object>) ((ExecutionNode) result.Data).ToValue())["worldBossRankingReward"];
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
}
