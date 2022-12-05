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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorldBossRanking(bool hex)
    {
        var targetAvatarAddress = new PrivateKey().ToAddress();
        var queryAddress = hex ? targetAvatarAddress.ToHex() : targetAvatarAddress.ToString();
        var query = $@"query {{
        worldBossRanking(raidId: 1, avatarAddress: ""{queryAddress}"") {{
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
                var avatarAddress = idx == 0 && i == 0 ? targetAvatarAddress : new PrivateKey().ToAddress();
                var model = new RaiderModel(
                    idx + 1,
                    i.ToString(),
                    i,
                    i + 1,
                    i + 2,
                    GameConfig.DefaultAvatarArmorId,
                    i,
                    avatarAddress.ToHex(),
                    0
                );
                Context.Raiders.Add(model);
            }
        }

        var block = new BlockModel
        {
            Index = 1L,
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
        var result = await ExecuteAsync(query);
        var data = (Dictionary<string, object>)((Dictionary<string, object>) ((ExecutionNode) result.Data).ToValue())["worldBossRanking"];
        Assert.Equal(1L, data["blockIndex"]);
        var models = (object[]) data["rankingInfo"];
        Assert.Equal(101, models.Length);
        var raider = (Dictionary<string, object>)models.Last();
        Assert.Equal(targetAvatarAddress.ToHex(), raider["address"]);
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
                new PrivateKey().ToAddress().ToHex(),
                0
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
