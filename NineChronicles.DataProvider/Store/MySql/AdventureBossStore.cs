// Refactor: Move MySqlStore.cs to MySql and make all namespaces to NineChronicles.dataProvider.MySqlStore

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
                        await ctx.AdventureBossSeason.AddAsync(season);
                    }
                    else
                    {
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
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();
                var tasks = new List<Task>();

                foreach (var wanted in wantedList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        if (await ctx.AdventureBossWanted.FirstOrDefaultAsync(w => w.Id == wanted.Id) is null)
                        {
                            await ctx.AdventureBossWanted.AddAsync(wanted);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
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

        public async partial Task StoreAdventureBossChallengeList(List<AdventureBossChallengeModel> challengeList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();
                var tasks = new List<Task>();

                foreach (var challenge in challengeList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        if (await ctx.AdventureBossChallenge.FirstOrDefaultAsync(c => c.Id == challenge.Id) is null)
                        {
                            await ctx.AdventureBossChallenge.AddAsync(challenge);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
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

        public async partial Task StoreAdventureBossRushList(List<AdventureBossRushModel> rushList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();
                var tasks = new List<Task>();

                foreach (var rush in rushList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        if (await ctx.AdventureBossRush.FirstOrDefaultAsync(r => r.Id == rush.Id) is null)
                        {
                            await ctx.AdventureBossRush.AddAsync(rush);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
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

        public async partial Task StoreAdventureBossUnlockFloorList(List<AdventureBossUnlockFloorModel> unlockFloorList)
        {
            NineChroniclesContext? ctx = null;

            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();
                var tasks = new List<Task>();

                foreach (var unlock in unlockFloorList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        if (await ctx.AdventureBossUnlockFloor.FirstOrDefaultAsync(u => u.Id == unlock.Id) is null)
                        {
                            await ctx.AdventureBossUnlockFloor.AddAsync(unlock);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
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

        public async partial Task StoreAdventureBossClaimRewardList(List<AdventureBossClaimRewardModel> claimList)
        {
            NineChroniclesContext? ctx = null;

            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();
                var tasks = new List<Task>();

                foreach (var claim in claimList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        if (await ctx.AdventureBossClaimReward.FirstOrDefaultAsync(c => c.Id == claim.Id) is null)
                        {
                            await ctx.AdventureBossClaimReward.AddAsync(claim);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
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
    }
}
