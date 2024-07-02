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
        public async partial Task StoreAdventureBossSeasonList(List<AdventureBossSeasonModel> seasonList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();
                var tasks = new List<Task>();

                foreach (var season in seasonList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        if (ctx.AdventureBossSeason.FindAsync(season.Season).Result is null)
                        {
                            await ctx.AdventureBossSeason.AddRangeAsync(season);
                        }
                        else
                        {
                            ctx.AdventureBossSeason.UpdateRange(season);
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
                        if (ctx.AdventureBossWanted.FindAsync(wanted.Id).Result is null)
                        {
                            await ctx.AdventureBossWanted.AddRangeAsync(wanted);
                        }
                        else
                        {
                            ctx.AdventureBossWanted.UpdateRange(wanted);
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
                        if (ctx.AdventureBossChallenge.FindAsync(challenge.Id).Result is null)
                        {
                            await ctx.AdventureBossChallenge.AddRangeAsync(challenge);
                        }
                        else
                        {
                            ctx.AdventureBossChallenge.UpdateRange(challenge);
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
                        if (ctx.AdventureBossRush.FindAsync(rush.Id).Result is null)
                        {
                            await ctx.AdventureBossRush.AddRangeAsync(rush);
                        }
                        else
                        {
                            ctx.AdventureBossRush.UpdateRange(rush);
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
                        if (ctx.AdventureBossUnlockFloor.FindAsync(unlock.Id).Result is null)
                        {
                            await ctx.AdventureBossUnlockFloor.AddRangeAsync(unlock);
                        }
                        else
                        {
                            ctx.AdventureBossUnlockFloor.UpdateRange(unlock);
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
                        if (ctx.AdventureBossClaimReward.FindAsync(claim.Id).Result is null)
                        {
                            await ctx.AdventureBossClaimReward.AddRangeAsync(claim);
                        }
                        else
                        {
                            ctx.AdventureBossClaimReward.UpdateRange(claim);
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
