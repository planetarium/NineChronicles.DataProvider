using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Execution;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Mocks;
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
        var targetAvatarAddress = new PrivateKey().Address;
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
                var avatarAddress = idx == 0 && i == 0 ? targetAvatarAddress : new PrivateKey().Address;
                var model = new RaiderModel(
                    idx + 1,
                    i.ToString(),
                    i,
                    i + 1,
                    i + 2,
                    GameConfig.DefaultAvatarArmorId,
                    i,
                    avatarAddress.ToString(),
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
        var data = (Dictionary<string, object>)((Dictionary<string, object>)((ExecutionNode)result.Data).ToValue())["worldBossRanking"];
        Assert.Equal(1L, data["blockIndex"]);
        var models = (object[]) data["rankingInfo"];
        Assert.Equal(101, models.Length);
        var raider = (Dictionary<string, object>)models.Last();
        // FIXME should be use AddressType
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
                new PrivateKey().Address.ToString(),
                0
            );
            Context.Raiders.Add(model);
        }

        await Context.SaveChangesAsync();
        var result = await ExecuteAsync(query);
        var count = (int)((Dictionary<string, object>) ((ExecutionNode) result.Data).ToValue())["worldBossTotalUsers"];
        Assert.Equal(200, count);
    }

    [Fact]
    public async Task WorldBossRanking_Sort()
    {
        var avatarAddresses = new List<Address>
        {
            new PrivateKey().Address,
            new PrivateKey().Address,
            new PrivateKey().Address,
            new PrivateKey().Address,
            new PrivateKey().Address,
            new PrivateKey().Address,
        };
        var totalScores = new List<int>
        {
            100,
            100,
            90,
            90,
            80,
            70,
        };
        var targetAvatarAddress = avatarAddresses[0];
        var queryAddress = targetAvatarAddress.ToString();
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
        for (int i = 0; i < 6; i++)
        {
            var avatarAddress = avatarAddresses[i];
            var model = new RaiderModel(
                1,
                i.ToString(),
                i + 1,
                totalScores[i],
                i + 2,
                GameConfig.DefaultAvatarArmorId,
                i,
                avatarAddress.ToString(),
                0
            );
            Context.Raiders.Add(model);
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
        Assert.Equal(6, Context.Raiders.Count());
        var result = await ExecuteAsync(query);
        var data = (Dictionary<string, object>)((Dictionary<string, object>)((ExecutionNode)result.Data).ToValue())["worldBossRanking"];
        Assert.Equal(1L, data["blockIndex"]);
        var models = (object[]) data["rankingInfo"];
        // season 1
        Assert.Equal(6, models.Length);
        var expectedRanking = new Dictionary<int, int>()
        {
            [0] = 2,
            [1] = 2,
            [2] = 4,
            [3] = 4,
            [4] = 5,
            [5] = 6,
        };
        for (int j = 0; j < 6; j++)
        {
            var model = (Dictionary<string, object>)models[j];
            Assert.Equal(expectedRanking[j], model["ranking"]);
            Assert.Contains(new Address((string)model["address"]), avatarAddresses);
        }
    }

    public void Dispose()
    {
        CleanUp();

    }

    protected override IWorldState GetMockState()
    {
        return  MockWorldState.CreateModern();
    }
}
