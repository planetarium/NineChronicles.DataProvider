namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Lib9c.Renderers;
    using Libplanet.Action.State;
    using Nekoyume.Action;
    using Nekoyume.Model.EnumType;
    using NineChronicles.DataProvider.DataRendering;
    using NineChronicles.DataProvider.DataRendering.Crafting;
    using NineChronicles.DataProvider.Store.Models.Crafting;
    using Serilog;

    public partial class RenderSubscriber
    {
        private List<RapidCombinationModel> _rapidCombinationList = new ();

        // RapidCombination
        private void StoreRapidCombinationList()
        {
            try
            {
                var tasks = new List<Task>();
                Log.Debug("[Crafting] Store RapidCombination list");

                tasks.Add(Task.Run(async () =>
                {
                    Log.Debug($"[RapidCombination] {_rapidCombinationList.Count}");
                    await MySqlStore.StoreRapidCombinationList(_rapidCombinationList);
                }));

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }

        private void ClearRapidCombinationList()
        {
            Log.Debug("[Crafting] Clear crafting related action data");
            _rapidCombinationList.Clear();
        }

        partial void SubscribeRapidCombination(ActionEvaluation<RapidCombination> ev)
        {
            try
            {
                if (ev.Exception == null && ev.Action is { } rapidCombination)
                {
                    var start = DateTimeOffset.UtcNow;
                    var inputState = new World(_blockChainStates.GetWorldState(ev.PreviousState));
                    var outputState = new World(_blockChainStates.GetWorldState(ev.OutputState));
                    var avatarAddress = rapidCombination.avatarAddress;
                    if (!_avatars.Contains(avatarAddress))
                    {
                        _avatars.Add(avatarAddress);
                        _avatarList.Add(
                            AvatarData.GetAvatarInfo(
                                outputState, ev.Signer, avatarAddress, _blockTimeOffset, BattleType.Adventure
                            )
                        );
                    }

                    _rapidCombinationList = _rapidCombinationList.Concat(
                        RapidCombinationData.GetRapidCombinationInfo(
                            inputState,
                            ev.Signer,
                            rapidCombination.avatarAddress,
                            rapidCombination.slotIndexList,
                            rapidCombination.Id,
                            ev.BlockIndex,
                            _blockTimeOffset)
                    ).ToList();
                    var end = DateTimeOffset.UtcNow;
                    Log.Debug(
                        "[DataProvider] Stored RapidCombination action in block #{index}. Time Taken: {time} ms.",
                        ev.BlockIndex,
                        (end - start).Milliseconds
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "[DataProvider] RenderSubscriber Error: {ErrorMessage}, StackTrace: {StackTrace}",
                    ex.Message,
                    ex.StackTrace
                );
            }
        }
    }
}
