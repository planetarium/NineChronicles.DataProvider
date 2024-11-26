namespace NineChronicles.DataProvider.Store
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using NineChronicles.DataProvider.Store.Models.Claim;
    using Serilog;

    /// <summary>
    /// Store actions related to claim things.
    /// - ClaimGifts
    /// and so on.
    /// </summary>
    public partial class MySqlStore
    {
        public async partial Task StoreClaimGiftList(List<ClaimGiftsModel> claimGiftList)
        {
            NineChroniclesContext? ctx = null;
            try
            {
                ctx = await _dbContextFactory.CreateDbContextAsync();

                foreach (var claim in claimGiftList)
                {
                    if (!await ctx.ClaimGifts.AnyAsync(c => c.Id == claim.Id))
                    {
                        await ctx.ClaimGifts.AddAsync(claim);
                    }
                }

                await ctx.SaveChangesAsync();
                Log.Debug($"[ClaimGifts] {claimGiftList.Count} ClaimGifts saved.");
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
