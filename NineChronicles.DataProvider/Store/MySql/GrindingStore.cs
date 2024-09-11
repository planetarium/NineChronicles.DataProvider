// Refactor: Move MySqlStore.cs to MySql and make all namespaces to NineChronicles.dataProvider.MySqlStore

namespace NineChronicles.DataProvider.Store
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using NineChronicles.DataProvider.Store.Models.Grinding;
    using Serilog;

    public partial class MySqlStore
    {
        public async partial Task StoreGrindList(List<GrindingModel> grindingList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                foreach (var grind in grindingList)
                {
                    if (await ctx.Grindings!.FirstOrDefaultAsync(g => g.Id == grind.Id) is null)
                    {
                        await ctx.Grindings!.AddRangeAsync(grind);
                    }
                }

                await ctx.SaveChangesAsync();
                Log.Debug($"[Grinding] {grindingList.Count} grind saved");
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
