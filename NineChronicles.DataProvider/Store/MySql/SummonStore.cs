namespace NineChronicles.DataProvider.Store
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using NineChronicles.DataProvider.Store.Models.Summon;
    using Serilog;

    public partial class MySqlStore
    {
        public async partial Task StoreAuraSummonList(List<AuraSummonModel> auraSummonList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                foreach (var summon in auraSummonList)
                {
                    if (await ctx.AuraSummons.AnyAsync(s => s.Id == summon.Id))
                    {
                        await ctx.AddAsync(summon);
                    }
                }

                await ctx.SaveChangesAsync();
                Log.Debug($"[AuraSummon] {auraSummonList.Count} aura summons saved.");
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

        public async partial Task StoreAuraSummonFailList(List<AuraSummonFailModel> auraSummonFailList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                foreach (var summon in auraSummonFailList)
                {
                    if (await ctx.AuraSummonFails.AnyAsync(s => s.Id == summon.Id))
                    {
                        await ctx.AddAsync(summon);
                    }
                }

                await ctx.SaveChangesAsync();
                Log.Debug($"[AuraSummon] {auraSummonFailList.Count} failed aura summons saved.");
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

        public async partial Task StoreRuneSummonList(List<RuneSummonModel> runeSummonList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                foreach (var summon in runeSummonList)
                {
                    if (await ctx.RuneSummons.AnyAsync(s => s.Id == summon.Id))
                    {
                        await ctx.AddAsync(summon);
                    }
                }

                await ctx.SaveChangesAsync();
                Log.Debug($"[RuneSummon] {runeSummonList.Count} rune summons saved.");
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

        public async partial Task StoreRuneSummonFailList(List<RuneSummonFailModel> runeSummonFailList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                foreach (var summon in runeSummonFailList)
                {
                    if (await ctx.RuneSummonFails.AnyAsync(s => s.Id == summon.Id))
                    {
                        await ctx.AddAsync(summon);
                    }
                }

                await ctx.SaveChangesAsync();
                Log.Debug($"[RuneSummon] {runeSummonFailList.Count} failed rune summons saved.");
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

        public async partial Task StoreCostumeSummonList(List<CostumeSummonModel> costumeSummonList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                foreach (var summon in costumeSummonList)
                {
                    if (await ctx.CostumeSummons.AnyAsync(s => s.Id == summon.Id))
                    {
                        await ctx.AddAsync(summon);
                    }
                }

                await ctx.SaveChangesAsync();
                Log.Debug($"[CostumeSummon] {costumeSummonList.Count} costume summons saved.");
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
