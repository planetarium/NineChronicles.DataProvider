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
            Log.Information($"[Adventure Boss] StoreAdventureBossSeason: {seasonList.Count}");
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
                        Log.Information("[Adventure Boss] Season not exist.");
                        await ctx.AdventureBossSeason.AddAsync(season);
                    }
                    else
                    {
                        Log.Information("[Adventure Boss] Season Exist: update");
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
            Log.Information($"[Adventure Boss] StoreAdventureBossWantedList: {wantedList.Count}");
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();
                var tasks = new List<Task>();

                var i = 1;
                foreach (var wanted in wantedList)
                {
                    Log.Information($"[Adventure Boss] Wanted {i++}/{wantedList.Count}");
                    tasks.Add(Task.Run(async () =>
                    {
                        if (await ctx.AdventureBossWanted.FirstOrDefaultAsync(w => w.Id == wanted.Id) is null)
                        {
                            await ctx.AdventureBossWanted.AddAsync(wanted);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
                Log.Information("[Adventure Boss] Wanted Added");
                await ctx.SaveChangesAsync();
                Log.Information("[Adventure Boss] Wanted Saved");
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
            Log.Information($"[Adventure Boss] StoreAdventureBossChallenge: {challengeList.Count}");
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();
                var tasks = new List<Task>();

                var i = 1;
                foreach (var challenge in challengeList)
                {
                    Log.Information($"[Adventure Boss] Challenge {i++}/{challengeList.Count}");
                    tasks.Add(Task.Run(async () =>
                    {
                        if (await ctx.AdventureBossChallenge.FirstOrDefaultAsync(c => c.Id == challenge.Id) is null)
                        {
                            await ctx.AdventureBossChallenge.AddAsync(challenge);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
                Log.Information("[Adventure Boss] Challenge Added");
                await ctx.SaveChangesAsync();
                Log.Information("[Adventure Boss] Challenge Saved");
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
            Log.Information($"[Adventure Boss] StoreAdventureBossRush: {rushList.Count}");
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();
                var tasks = new List<Task>();

                var i = 1;
                foreach (var rush in rushList)
                {
                    Log.Information($"[Adventure Boss] Rush {i++}/{rushList.Count}");
                    tasks.Add(Task.Run(async () =>
                    {
                        if (await ctx.AdventureBossRush.FirstOrDefaultAsync(r => r.Id == rush.Id) is null)
                        {
                            await ctx.AdventureBossRush.AddAsync(rush);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
                Log.Information("[Adventure Boss] Rush Added");
                await ctx.SaveChangesAsync();
                Log.Information("[Adventure Boss] Rush Saved");
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
            Log.Information($"[Adventure Boss] StoreAdventureBossUnlockFloor: {unlockFloorList.Count}");
            NineChroniclesContext? ctx = null;

            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();
                var tasks = new List<Task>();

                var i = 1;
                foreach (var unlock in unlockFloorList)
                {
                    Log.Information($"[Adventure Boss] UnlockFloor {i++}/{unlockFloorList.Count}");
                    tasks.Add(Task.Run(async () =>
                    {
                        if (await ctx.AdventureBossUnlockFloor.FirstOrDefaultAsync(u => u.Id == unlock.Id) is null)
                        {
                            await ctx.AdventureBossUnlockFloor.AddAsync(unlock);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
                Log.Information("[Adventure Boss] UnlockFloor Added");
                await ctx.SaveChangesAsync();
                Log.Information("[Adventure Boss] UnlockFloor Saved");
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
            Log.Information($"[Adventure Boss] StoreAdventureBossClaimReward: {claimList.Count}");
            NineChroniclesContext? ctx = null;

            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();
                var tasks = new List<Task>();

                var i = 1;
                foreach (var claim in claimList)
                {
                    Log.Information($"[Adventure Boss] Claim {i++}/{claimList.Count}");
                    tasks.Add(Task.Run(async () =>
                    {
                        if (await ctx.AdventureBossClaimReward.FirstOrDefaultAsync(c => c.Id == claim.Id) is null)
                        {
                            await ctx.AdventureBossClaimReward.AddAsync(claim);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
                Log.Information("[Adventure Boss] Claim Added");
                await ctx.SaveChangesAsync();
                Log.Information("[Adventure Boss] Claim Saved");
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
