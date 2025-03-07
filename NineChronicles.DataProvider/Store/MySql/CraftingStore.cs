namespace NineChronicles.DataProvider.Store
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
                    if (!await ctx.RapidCombinations.AnyAsync(r => r.Id == rc.Id))
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
            List<CustomEquipmentCraftModel> customEquipmentCraftList
        )
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                // This is for count update
                var iconCraftCountDict = new Dictionary<(string, int), int>();

                // Add new CustomCraft data
                foreach (var craftData in customEquipmentCraftList)
                {
                    if (!await ctx.CustomEquipmentCraft.AnyAsync(c => c.Id == craftData.Id))
                    {
                        if (iconCraftCountDict.ContainsKey((craftData.ItemSubType!, craftData.IconId)))
                        {
                            iconCraftCountDict[(craftData.ItemSubType!, craftData.IconId)]++;
                        }
                        else
                        {
                            iconCraftCountDict[(craftData.ItemSubType!, craftData.IconId)] = 1;
                        }

                        await ctx.CustomEquipmentCraft.AddAsync(craftData);
                    }
                }

                // Upsert CustomCraft count
                foreach (var ((itemSubType, iconId), count) in iconCraftCountDict)
                {
                    var countData = await ctx.CustomEquipmentCraftCount.FirstOrDefaultAsync(c => c.IconId == iconId);
                    if (countData is null)
                    {
                        await ctx.CustomEquipmentCraftCount.AddAsync(new CustomEquipmentCraftCountModel
                        {
                            IconId = iconId,
                            ItemSubType = itemSubType,
                            Count = count,
                        });
                    }
                    else
                    {
                        countData.Count += count;
                        ctx.Update(countData);
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

        public partial List<CustomEquipmentCraftCountModel> GetCustomEquipmentCraftCount(string? itemSubType)
        {
            using var ctx = _dbContextFactory.CreateDbContext();
            return itemSubType is null
                ? ctx.CustomEquipmentCraftCount.ToList()
                : ctx.CustomEquipmentCraftCount.Where(c => c.ItemSubType == itemSubType).ToList();
        }

        // UnlockCombinationSlot
        public async partial Task StoreUnlockCombinationSlotList(
            List<UnlockCombinationSlotModel> unlockCombinationSlotList
        )
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                foreach (var ucs in unlockCombinationSlotList)
                {
                    if (!await ctx.UnlockCombinationSlot.AnyAsync(u => u.Id == ucs.Id))
                    {
                        await ctx.UnlockCombinationSlot.AddAsync(ucs);
                    }
                }

                await ctx.SaveChangesAsync();
                Log.Debug($"[UnlockCombinationSlot] {unlockCombinationSlotList.Count} UnlockCombinationSlot saved.");
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

        // Synthesize
        public async partial Task StoreSynthesizeList(List<SynthesizeModel> synthesizeList)
        {
            NineChroniclesContext? ctx = null;

            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                foreach (var synth in synthesizeList)
                {
                    if (!await ctx.Synthesizes.AnyAsync(s => s.Id == synth.Id))
                    {
                        await ctx.Synthesizes.AddAsync(synth);
                    }
                }

                await ctx.SaveChangesAsync();
                Log.Debug($"[Synthesize] {synthesizeList.Count} Synthesize saved.");
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
