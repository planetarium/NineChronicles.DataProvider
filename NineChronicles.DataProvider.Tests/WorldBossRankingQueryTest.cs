using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using GraphQL.Execution;
using Libplanet;
using Libplanet.Assets;
using Libplanet.Crypto;
using Nekoyume;
using NineChronicles.DataProvider.Store.Models;
using Xunit;

namespace NineChronicles.DataProvider.Tests;

public class WorldBossRankingQueryTest : TestBase, IDisposable
{
    [Fact]
    public async Task WorldBossRanking()
    {
        var targetAvatarAddress = new PrivateKey().ToAddress().ToHex();
        var query = $@"query {{
        worldBossRanking(raidId: 1, avatarAddress: ""{targetAvatarAddress}"") {{
            blockIndex
            rankingInfo {{
                ranking
                avatarName
                address
            }}
        }}
    }}";
        for (int idx = 0; idx < 2; idx++)
        {
            for (int i = 0; i < 200; i++)
            {
                var avatarAddress = idx == 0 && i == 0 ? targetAvatarAddress : new PrivateKey().ToAddress().ToHex();
                var model = new RaiderModel(
                    idx + 1,
                    i.ToString(),
                    i,
                    i + 1,
                    i + 2,
                    GameConfig.DefaultAvatarArmorId,
                    i,
                    avatarAddress
                );
                Context.Raiders.Add(model);
            }
        }

        await Context.SaveChangesAsync();
        var result = await ExecuteAsync(query);
        var data = (Dictionary<string, object>)((Dictionary<string, object>) ((ExecutionNode) result.Data).ToValue())["worldBossRanking"];
        Assert.Equal(0L, data["blockIndex"]);
        var models = (object[]) data["rankingInfo"];
        Assert.Equal(101, models.Length);
        var raider = (Dictionary<string, object>)models.Last();
        Assert.Equal(targetAvatarAddress, raider["address"]);
    }

    [Fact]
    public async Task WorldBossTotalUsers()
    {
        const string query = @"query {
        worldBossTotalUsers(raidId: 1)
    }";
        for (int i = 0; i < 200; i++)
        {
            var model = new RaiderModel(
                1,
                i.ToString(),
                i,
                i + 1,
                i + 2,
                GameConfig.DefaultAvatarArmorId,
                i,
                new PrivateKey().ToAddress().ToHex()
            );
            Context.Raiders.Add(model);
        }

        await Context.SaveChangesAsync();
        var result = await ExecuteAsync(query);
        var count = (int)((Dictionary<string, object>) ((ExecutionNode) result.Data).ToValue())["worldBossTotalUsers"];
        Assert.Equal(200, count);

    }

    public void Dispose()
    {
        CleanUp();

    }

    protected override IValue? GetStateMock(Address address)
    {
        throw new NotImplementedException();
    }

    protected override FungibleAssetValue GetBalanceMock(Address address, Currency currency)
    {
        throw new NotImplementedException();
    }
}
