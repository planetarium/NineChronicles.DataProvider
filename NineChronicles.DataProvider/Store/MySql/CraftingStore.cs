namespace NineChronicles.DataProvider.Store
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using NineChronicles.DataProvider.Store.Models;
    using Serilog;

    /// <summary>
    /// Store functions related to crafting.
    /// - ItemEnhancement
    /// - CombinationEquipment
    /// - RapidCombination
    /// and so on.
    /// </summary>
    public partial class MySqlStore
    {
        // RapidCombination
        public async partial Task StoreRapidCombinationList(List<RapidCombinationModel> rapidCombinationList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                foreach (var rc in rapidCombinationList)
                {
                    if (await ctx.RapidCombinations.FirstOrDefaultAsync(r => r.Id == rc.Id) is null)
                    {
                        await ctx.RapidCombinations.AddAsync(rc);
                    }
                }

                await ctx.SaveChangesAsync();
                Log.Debug($"[RapidCombination] {rapidCombinationList.Count} RapidCombinations saved.");
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
