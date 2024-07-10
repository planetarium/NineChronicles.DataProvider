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
            Console.WriteLine($"[Adventure Boss] StoreAdventureBossSeason: {seasonList.Count}");
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
                        Console.WriteLine("[Adventure Boss] Season not exist.");
                        await ctx.AdventureBossSeason.AddAsync(season);
                    }
                    else
                    {
                        Console.WriteLine("[Adventure Boss] Season Exist: update");
                        existSeason.RaffleReward = season.RaffleReward;
                        existSeason.RaffleWinnerAddress = season.RaffleWinnerAddress;
                        ctx.AdventureBossSeason.Update(existSeason);
                    }
                }

                await ctx.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
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
            Console.WriteLine($"[Adventure Boss] StoreAdventureBossWantedList: {wantedList.Count}");
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

                Console.WriteLine("[Adventure Boss] Wanted Added");
                await ctx.SaveChangesAsync();
                Console.WriteLine("[Adventure Boss] Wanted Saved");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
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
            Console.WriteLine($"[Adventure Boss] StoreAdventureBossChallenge: {challengeList.Count}");
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

                Console.WriteLine("[Adventure Boss] Challenge Added");
                await ctx.SaveChangesAsync();
                Console.WriteLine("[Adventure Boss] Challenge Saved");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
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
            Console.WriteLine($"[Adventure Boss] StoreAdventureBossRush: {rushList.Count}");
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

                Console.WriteLine("[Adventure Boss] Rush Added");
                await ctx.SaveChangesAsync();
                Console.WriteLine("[Adventure Boss] Rush Saved");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
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
            Console.WriteLine($"[Adventure Boss] StoreAdventureBossUnlockFloor: {unlockFloorList.Count}");
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

                Console.WriteLine("[Adventure Boss] UnlockFloor Added");
                await ctx.SaveChangesAsync();
                Console.WriteLine("[Adventure Boss] UnlockFloor Saved");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
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
            Console.WriteLine($"[Adventure Boss] StoreAdventureBossClaimReward: {claimList.Count}");
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

                Console.WriteLine("[Adventure Boss] Claim Added");
                await ctx.SaveChangesAsync();
                Console.WriteLine("[Adventure Boss] Claim Saved");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
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
