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
            Log.Debug($"[Adventure Boss] StoreAdventureBossSeason: {seasonList.Count}");
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
                        Log.Debug("[Adventure Boss] Season not exist.");
                        await ctx.AdventureBossSeason.AddAsync(season);
                    }
                    else
                    {
                        Log.Debug("[Adventure Boss] Season Exist: update");
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
            Log.Debug($"[Adventure Boss] StoreAdventureBossWantedList: {wantedList.Count}");
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

                await ctx.SaveChangesAsync();
                Log.Debug("[Adventure Boss] Wanted Saved");
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
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
            Log.Debug($"[Adventure Boss] StoreAdventureBossChallenge: {challengeList.Count}");
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

                await ctx.SaveChangesAsync();
                Log.Debug("[Adventure Boss] Challenge Saved");
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
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
            Log.Debug($"[Adventure Boss] StoreAdventureBossRush: {rushList.Count}");
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

                await ctx.SaveChangesAsync();
                Log.Debug("[Adventure Boss] Rush Saved");
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
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
            Log.Debug($"[Adventure Boss] StoreAdventureBossUnlockFloor: {unlockFloorList.Count}");
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

                await ctx.SaveChangesAsync();
                Log.Debug("[Adventure Boss] UnlockFloor Saved");
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
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
            Log.Debug($"[Adventure Boss] StoreAdventureBossClaimReward: {claimList.Count}");
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

                await ctx.SaveChangesAsync();
                Log.Debug("[Adventure Boss] Claim Saved");
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
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
