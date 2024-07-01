// Refactor: Move MySqlStore.cs to MySql and make all namespaces to NineChronicles.dataProvider.MySqlStore

namespace NineChronicles.DataProvider.Store
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NineChronicles.DataProvider.Store.Models.AdventureBoss;
    using Serilog;

    public partial class MySqlStore
    {
        public partial void StoreAdventureBossSeasonList(List<AdventureBossSeasonModel> seasonList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var season in seasonList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.AdventureBossSeason.FindAsync(season.Season).Result is null)
                        {
                            await ctx.AdventureBossSeason.AddRangeAsync(season);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext
                                updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.AdventureBossSeason.UpdateRange(season);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        public partial void StoreAdventureBossWantedList(List<AdventureBossWantedModel> wantedList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var wanted in wantedList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.AdventureBossWanted.FindAsync(wanted.Id).Result is null)
                        {
                            await ctx.AdventureBossWanted.AddRangeAsync(wanted);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext
                                updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.AdventureBossWanted.UpdateRange(wanted);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        public partial void StoreAdventureBossChallengeList(List<AdventureBossChallengeModel> challengeList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var challenge in challengeList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.AdventureBossChallenge.FindAsync(challenge.Id).Result is null)
                        {
                            await ctx.AdventureBossChallenge.AddRangeAsync(challenge);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext
                                updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.AdventureBossChallenge.UpdateRange(challenge);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        public partial void StoreAdventureBossRushList(List<AdventureBossRushModel> rushList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var rush in rushList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.AdventureBossRush.FindAsync(rush.Id).Result is null)
                        {
                            await ctx.AdventureBossRush.AddRangeAsync(rush);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext
                                updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.AdventureBossRush.UpdateRange(rush);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        public partial void StoreAdventureBossUnlockFloorList(List<AdventureBossUnlockFloorModel> unlockFloorList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var unlock in unlockFloorList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.AdventureBossUnlockFloor.FindAsync(unlock.Id).Result is null)
                        {
                            await ctx.AdventureBossUnlockFloor.AddRangeAsync(unlock);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext
                                updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.AdventureBossUnlockFloor.UpdateRange(unlock);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        public partial void StoreAdventureBossClaimRewardList(List<AdventureBossClaimRewardModel> claimList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var claim in claimList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.AdventureBossClaimReward.FindAsync(claim.Id).Result is null)
                        {
                            await ctx.AdventureBossClaimReward.AddRangeAsync(claim);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext
                                updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.AdventureBossClaimReward.UpdateRange(claim);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }
    }
}
