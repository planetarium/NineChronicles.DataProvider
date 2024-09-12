namespace NineChronicles.DataProvider.Store
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using NineChronicles.DataProvider.Store.Models.Crafting;
    using Serilog;

    /// <summary>
    /// Store functions related to crafting.
    /// - RapidCombination
    /// - CustomEquipmentCraft
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

        // CustomEquipmentCraft
        public async partial Task StoreCustomEquipmentCraftList(
            List<CustomEquipmentCraftModel> customEquipmentCraftList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();
                foreach (var cec in customEquipmentCraftList)
                {
                    if (await ctx.CustomEquipmentCraft.FirstOrDefaultAsync(c => c.Id == cec.Id) is null)
                    {
                        await ctx.CustomEquipmentCraft.AddAsync(cec);
                    }
                }

                await ctx.SaveChangesAsync();
                Log.Debug($"[CustomEquipmentCraft] {customEquipmentCraftList.Count} CustomEquipmentCraft saved.");
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
