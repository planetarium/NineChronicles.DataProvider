// Refactor: Move MySqlStore.cs to MySql and make all namespaces to NineChronicles.dataProvider.MySqlStore
// NOTE: Only `StoreAdventureBossSeasonList` will update incoming data because other data does not have any data to be updated.

namespace NineChronicles.DataProvider.Store
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using NineChronicles.DataProvider.Store.Models.AdventureBoss;
    using Serilog;

    public partial class MySqlStore
    {
        public async partial Task StoreAdventureBossSeasonList(List<AdventureBossSeasonModel> seasonList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();
                foreach (var season in seasonList)
                {
                    // Season info will be updated once claim executed.
                    // So season info needs to be updated.
                    var existSeason = await ctx.AdventureBossSeason.FirstOrDefaultAsync(s => s.Season == season.Season);
                    if (existSeason is null)
                    {
                        Log.Debug("[DataProvider] AdventureBossSeason does not exist.");
                        await ctx.AdventureBossSeason.AddAsync(season);
                    }
                    else
                    {
                        Log.Debug($"[DataProvider] AdventureBossSeason Exist: update: {seasonList.Count}");
                        existSeason.RaffleReward = season.RaffleReward;
                        existSeason.RaffleWinnerAddress = season.RaffleWinnerAddress;
                        ctx.AdventureBossSeason.Update(existSeason);
                    }
                }

                await ctx.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
                Log.Debug(e.StackTrace);
            }
            finally
            {
                if (ctx is not null)
                {
                    await ctx.DisposeAsync();
                }
            }
        }

        public async partial Task StoreAdventureBossWantedList(List<AdventureBossWantedModel> wantedList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                foreach (var wanted in wantedList)
                {
                    if (await ctx.AdventureBossWanted.FirstOrDefaultAsync(w => w.Id == wanted.Id) is null)
                    {
                        await ctx.AdventureBossWanted.AddAsync(wanted);
                    }
                }

                Log.Debug("[DataProvider] AdventureBossWanted Added");
                await ctx.SaveChangesAsync();
                Log.Debug($"[DataProvider] AdventureBossWanted Saved: {wantedList.Count}");
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
                Log.Debug(e.StackTrace);
            }
            finally
            {
                if (ctx is not null)
                {
                    await ctx.DisposeAsync();
                }
            }
        }

        public async partial Task StoreAdventureBossChallengeList(List<AdventureBossChallengeModel> challengeList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                foreach (var challenge in challengeList)
                {
                    if (await ctx.AdventureBossChallenge.FirstOrDefaultAsync(c => c.Id == challenge.Id) is null)
                    {
                        await ctx.AdventureBossChallenge.AddAsync(challenge);
                    }
                }

                Log.Debug("[DataProvider] AdventureBossChallenge Added");
                await ctx.SaveChangesAsync();
                Log.Debug($"[DataProvider] AdventureBossChallenge Saved: {challengeList.Count}");
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
                Log.Debug(e.StackTrace);
            }
            finally
            {
                if (ctx is not null)
                {
                    await ctx.DisposeAsync();
                }
            }
        }

        public async partial Task StoreAdventureBossRushList(List<AdventureBossRushModel> rushList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                foreach (var rush in rushList)
                {
                    if (await ctx.AdventureBossRush.FirstOrDefaultAsync(r => r.Id == rush.Id) is null)
                    {
                        await ctx.AdventureBossRush.AddAsync(rush);
                    }
                }

                Log.Debug("[DataProvider] AdventureBossRush Added");
                await ctx.SaveChangesAsync();
                Log.Debug($"[DataProvider] AdventureBossRush Saved: {rushList.Count}");
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
                Log.Debug(e.StackTrace);
            }
            finally
            {
                if (ctx is not null)
                {
                    await ctx.DisposeAsync();
                }
            }
        }

        public async partial Task StoreAdventureBossUnlockFloorList(List<AdventureBossUnlockFloorModel> unlockFloorList)
        {
            NineChroniclesContext? ctx = null;

            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                foreach (var unlock in unlockFloorList)
                {
                    if (await ctx.AdventureBossUnlockFloor.FirstOrDefaultAsync(u => u.Id == unlock.Id) is null)
                    {
                        await ctx.AdventureBossUnlockFloor.AddAsync(unlock);
                    }
                }

                Log.Debug("[DataProvider] AdventureBossUnlockFloor Added");
                await ctx.SaveChangesAsync();
                Log.Debug($"[DataProvider] AdventureBossUnlockFloor Saved: {unlockFloorList.Count}");
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
                Log.Debug(e.StackTrace);
            }
            finally
            {
                if (ctx is not null)
                {
                    await ctx.DisposeAsync();
                }
            }
        }

        public async partial Task StoreAdventureBossClaimRewardList(List<AdventureBossClaimRewardModel> claimList)
        {
            NineChroniclesContext? ctx = null;

            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                foreach (var claim in claimList)
                {
                    if (await ctx.AdventureBossClaimReward.FirstOrDefaultAsync(c => c.Id == claim.Id) is null)
                    {
                        await ctx.AdventureBossClaimReward.AddAsync(claim);
                    }
                }

                Log.Debug("[DataProvider] AdventureBossClaim Added");
                await ctx.SaveChangesAsync();
                Log.Debug($"[DataProvider] AdventureBossClaim Saved: {claimList.Count}");
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
                Log.Debug(e.StackTrace);
            }
            finally
            {
                if (ctx is not null)
                {
                    await ctx.DisposeAsync();
                }
            }
        }
    }
}
