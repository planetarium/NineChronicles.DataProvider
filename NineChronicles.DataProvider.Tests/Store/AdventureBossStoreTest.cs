using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Microsoft.Extensions.DependencyInjection;
using NineChronicles.DataProvider.Store;
using NineChronicles.DataProvider.Store.Models.AdventureBoss;
using Xunit;

namespace NineChronicles.DataProvider.Tests.Store;

public class AdventureBossStoreTest : TestBase
{
    [Fact]
    public async Task UpsertSeasonInfo()
    {
        var provider = Services.BuildServiceProvider();
        var store = provider.GetRequiredService<MySqlStore>();
        var now = DateTimeOffset.UtcNow;
        var season = new AdventureBossSeasonModel
        {
            Season = 1,
            StartBlockIndex = 1L,
            EndBlockIndex = 10L,
            NextSeasonBlockIndex = 20L,
            ClaimableBlockIndex = 100L,
            BossId = 207001,
            FixedRewardData = "600201,30001",
            RandomRewardData = "600203",
            Date = DateOnly.FromDateTime(now.DateTime),
            TimeStamp = now
        };
        await store.StoreAdventureBossSeasonList(new List<AdventureBossSeasonModel> { season });

        var seasonData = Context.AdventureBossSeason.First(s => s.Season == 1);
        Assert.Equal(1L, seasonData.StartBlockIndex);
        Assert.Equal(10L, seasonData.EndBlockIndex);
        Assert.Equal(20L, season.NextSeasonBlockIndex);
        Assert.Equal(100L, seasonData.ClaimableBlockIndex);
        Assert.Equal(207001, seasonData.BossId);
        Assert.Null(seasonData.RaffleWinnerAddress);
        Assert.Equal(0, seasonData.RaffleReward);

        // Update 
        var address = new PrivateKey().Address.ToString();
        season.RaffleWinnerAddress = address;
        season.RaffleReward = 5m;

        await store.StoreAdventureBossSeasonList(new List<AdventureBossSeasonModel> { season });
        seasonData = Context.AdventureBossSeason.First(s => s.Season == 1);
        // Must be same
        Assert.Equal(1L, seasonData.StartBlockIndex);
        Assert.Equal(10L, seasonData.EndBlockIndex);
        Assert.Equal(20L, season.NextSeasonBlockIndex);
        Assert.Equal(100L, seasonData.ClaimableBlockIndex);
        Assert.Equal(207001, seasonData.BossId);

        // Must be changed
        Assert.Equal(address, seasonData.RaffleWinnerAddress);
        Assert.Equal(5, seasonData.RaffleReward);
    }

    protected override IWorldState GetMockState()
    {
        return MockWorldState.CreateModern();
    }
}
