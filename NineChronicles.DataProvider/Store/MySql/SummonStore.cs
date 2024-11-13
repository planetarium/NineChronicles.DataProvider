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
