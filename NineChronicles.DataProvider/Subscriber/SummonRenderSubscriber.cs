namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lib9c.Renderers;
    using Libplanet.Action.State;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.DataRendering;
    using NineChronicles.DataProvider.DataRendering.Summon;
    using NineChronicles.DataProvider.Store.Models.Summon;
    using Serilog;

    public partial class RenderSubscriber
    {
        private readonly List<CostumeSummonModel> _costumeSummonList = new ();

        public void StoreSummonList()
        {
            try
            {
                var tasks = new List<Task>();
                Log.Debug("[DataProvider] Store costume summon list");
                tasks.Add(Task.Run(async () =>
                {
                    Log.Debug($"[CostumeSummon] {_costumeSummonList.Count} CostumeSummons");
                    await MySqlStore.StoreCostumeSummonList(_costumeSummonList);
                }));

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        partial void SubscribeCostumeSummon(ActionEvaluation<CostumeSummon> evt)
        {
            try
            {
                if (evt.Exception is null && evt.Action is { } costumeSummon)
                {
                    var start = System.DateTimeOffset.UtcNow;
                    var inputState = new World(_blockChainStates.GetWorldState(evt.PreviousState));
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    var avatarAddress = costumeSummon.AvatarAddress;

                    if (!_avatars.Contains(avatarAddress))
                    {
                        _avatars.Add(avatarAddress);
                        _avatarList.Add(AvatarData.GetAvatarInfo(
                            outputState,
                            evt.Signer,
                            avatarAddress,
                            _blockTimeOffset,
                            Nekoyume.Model.EnumType.BattleType.Adventure
                        ));
                    }

                    _costumeSummonList.Add(CostumeSummonData.GetSummonInfo(
                        inputState, outputState, evt.Signer, evt.BlockIndex, _blockTimeOffset, costumeSummon
                    ));

                    Log.Debug(
                        $"[DataProvider] Stored CostumeSummon action in block #{evt.BlockIndex}. Time taken: {DateTimeOffset.UtcNow - start} ms"
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

        private void ClearSummonList()
        {
            Log.Debug("[Summon] Clear summon list");
            _costumeSummonList.Clear();
        }
    }
}
