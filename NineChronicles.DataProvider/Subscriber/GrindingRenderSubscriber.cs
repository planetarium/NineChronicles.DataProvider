namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lib9c.Renderers;
    using Libplanet.Action.State;
    using Nekoyume.Action;
    using Nekoyume.Model.EnumType;
    using NineChronicles.DataProvider.DataRendering;
    using NineChronicles.DataProvider.DataRendering.Grinding;
    using NineChronicles.DataProvider.Store.Models.Grinding;
    using Serilog;

    public partial class RenderSubscriber
    {
        private readonly List<GrindingModel> _grindList = new ();

        public void StoreGrindList()
        {
            try
            {
                var tasks = new List<Task>();
                Log.Debug("[DataProvider] Store grinding list");

                tasks.Add(Task.Run(async () =>
                    {
                        Log.Debug($"[Grinding] {_grindList.Count} Grindings");
                        await MySqlStore.StoreGrindList(_grindList);
                    }
                ));

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        partial void SubscribeGrinding(ActionEvaluation<Grinding> ev)
        {
            try
            {
                if (ev.Action is { } grinding)
                {
                    var start = DateTimeOffset.UtcNow;
                    var inputState = new World(_blockChainStates.GetWorldState(ev.PreviousState));
                    var outputState = new World(_blockChainStates.GetWorldState(ev.OutputState));
                    var avatarAddress = grinding.AvatarAddress;
                    if (!_avatars.Contains(avatarAddress))
                    {
                        _avatars.Add(avatarAddress);
                        _avatarList.Add(AvatarData.GetAvatarInfo(
                            outputState, ev.Signer, avatarAddress, _blockTimeOffset, BattleType.Adventure
                        ));
                    }

                    var grindList = GrindingData.GetGrindingInfo(
                        inputState,
                        ev.Signer,
                        grinding.AvatarAddress,
                        grinding.EquipmentIds,
                        grinding.Id,
                        ev.BlockIndex,
                        _blockTimeOffset
                    );

                    foreach (var grind in grindList)
                    {
                        _grindList.Add(grind);
                    }

                    var end = DateTimeOffset.UtcNow;
                    Log.Debug(
                        "[DataProvider] Stored Grinding action in block #{index}. Time Taken: {time} ms.",
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

        private void ClearGrindList()
        {
            Log.Debug("[Grinding] Clear grind list");
            _grindList.Clear();
        }
    }
}
