namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lib9c.Renderers;
    using Libplanet.Action.State;
    using Nekoyume.Action.CustomEquipmentCraft;
    using NineChronicles.DataProvider.DataRendering.CustomCraft;
    using NineChronicles.DataProvider.Store.Models.CustomCraft;
    using Serilog;

    public partial class RenderSubscriber
    {
        private readonly List<CustomEquipmentCraftModel> _customEquipmentCraftList = new ();

        public void StoreCustomEquipmentCraftList()
        {
            try
            {
                var tasks = new List<Task>();
                Log.Debug("[DataProvider] Store CustomEquipmentCraft list");
                tasks.Add(Task.Run(async () =>
                {
                    Log.Debug($"[CustomCraft] {_customEquipmentCraftList.Count} craft data");
                    await MySqlStore.StoreCustomEquipmentCraftList(_customEquipmentCraftList);
                }));
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }

        private void ClearCustomCraftList()
        {
            Log.Debug("[DataProvider] Clear CustomCraft list");
            _customEquipmentCraftList.Clear();
        }

        partial void SubscribeCustomEquipmentCraft(ActionEvaluation<CustomEquipmentCraft> evt)
        {
            try
            {
                if (evt.Exception is null && evt.Action is { } customEquipmentCraft)
                {
                    var start = DateTimeOffset.UtcNow;
                    var prevState = new World(_blockChainStates.GetWorldState(evt.PreviousState));
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    var craftList = CustomEquipmentCraftData.GetCraftInfo(
                        prevState,
                        outputState,
                        evt.BlockIndex,
                        _blockTimeOffset,
                        customEquipmentCraft
                    );
                    foreach (var craft in craftList)
                    {
                        _customEquipmentCraftList.Add(craft);
                    }

                    var end = DateTimeOffset.UtcNow;
                    Log.Debug(
                        "[DataProvider] Stored {count} AdventureBossSeason action in block #{BlockIndex}. Time taken: {Time} ms",
                        _adventureBossSeasonDict.Count,
                        evt.BlockIndex,
                        end - start
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
