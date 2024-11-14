// Refactor: Move RenderSubscriber.cs to Subscriber and make all namespaces to NineChronicles.DataProvider.Subscriber

namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lib9c.Renderers;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.DataRendering.Claim;
    using NineChronicles.DataProvider.Store.Models.Claim;
    using Serilog;

    public partial class RenderSubscriber
    {
        private readonly List<ClaimGiftsModel> _claimGiftsList = new ();

        private void StoreClaimGiftsList()
        {
            try
            {
                var tasks = new List<Task>();
                Log.Debug("[DataProvider] Store claim list");

                tasks.Add(Task.Run(async () =>
                {
                    Log.Debug($"[ClaimGifts] {_claimStakeList.Count} ClaimGifts");
                    await MySqlStore.StoreClaimGiftList(_claimGiftsList);
                }));

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }

        private void ClearClaimList()
        {
            Log.Debug("[DataProvider] Clear claim actions");
            _claimGiftsList.Clear();
        }

        partial void SubscribeClaimGifts(ActionEvaluation<ClaimGifts> evt)
        {
            try
            {
                if (evt.Exception is null && evt.Action is { } claimGifts)
                {
                    var start = DateTimeOffset.UtcNow;
                    _claimGiftsList.Add(ClaimGiftsData.GetClaimInfo(
                        evt.Signer,
                        evt.BlockIndex,
                        _blockTimeOffset,
                        claimGifts
                    ));
                    Log.Debug(
                        $"[DataProvider] Stored {_claimGiftsList.Count} ClaimGifts action in block #{evt.BlockIndex}. Time taken: {DateTimeOffset.UtcNow - start} ms"
                    );
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "[DataProvider] RenderSubscriber Error: {ErrorMessage}, StackTrace: {StackTrace}",
                    e.Message,
                    e.StackTrace
                );
            }
        }
    }
}
