namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Lib9c.Renderers;
    using Libplanet.Action.State;
    using Nekoyume.Action;
    using Nekoyume.Action.CustomEquipmentCraft;
    using Nekoyume.Model.EnumType;
    using NineChronicles.DataProvider.DataRendering;
    using NineChronicles.DataProvider.DataRendering.Crafting;
    using NineChronicles.DataProvider.Store.Models.Crafting;
    using Serilog;

    public partial class RenderSubscriber
    {
        private List<RapidCombinationModel> _rapidCombinationList = new ();
        private List<CustomEquipmentCraftModel> _customEquipmentCraftList = new ();
        private List<UnlockCombinationSlotModel> _unlockCombinationSlotList = new ();

        // Store
        private void StoreCraftingData()
        {
            try
            {
                var tasks = new List<Task>();

                // RapidCombination
                Log.Debug("[Crafting] Store RapidCombination list");
                tasks.Add(Task.Run(async () =>
                {
                    Log.Debug($"[RapidCombination] {_rapidCombinationList.Count}");
                    await MySqlStore.StoreRapidCombinationList(_rapidCombinationList);
                }));

                // CustomEquipmentCraft
                Log.Debug("[Crafting] Store CustomEquipmentCraft list");
                tasks.Add(Task.Run(async () =>
                {
                    Log.Debug($"[CustomEquipmentCraft] {_customEquipmentCraftList.Count}");
                    await MySqlStore.StoreCustomEquipmentCraftList(_customEquipmentCraftList);
                }));

                // UnlockCombinationSlot
                Log.Debug("[Crafting] Store UnlockCombinationSlot list");
                tasks.Add(Task.Run(async () =>
                {
                    Log.Debug($"[UnlockCombinationSlot] {_unlockCombinationSlotList.Count}");
                    await MySqlStore.StoreUnlockCombinationSlotList(_unlockCombinationSlotList);
                }));

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }

        // Clear
        private void ClearCraftingList()
        {
            Log.Debug("[Crafting] Clear crafting related action data");
            _rapidCombinationList.Clear();
            _customEquipmentCraftList.Clear();
            _unlockCombinationSlotList.Clear();
        }

        // Subscribe
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

        partial void SubscribeCustomEquipmentCraft(ActionEvaluation<CustomEquipmentCraft> evt)
        {
            try
            {
                if (evt.Exception == null && evt.Action is { } customEquipmentCraft)
                {
                    var start = DateTimeOffset.UtcNow;
                    var prevState = new World(_blockChainStates.GetWorldState(evt.PreviousState));
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    var avatarAddress = customEquipmentCraft.AvatarAddress;

                    if (!_avatars.Contains(avatarAddress))
                    {
                        _avatars.Add(avatarAddress);
                        _avatarList.Add(
                            AvatarData.GetAvatarInfo(
                                outputState, evt.Signer, avatarAddress, _blockTimeOffset, BattleType.Adventure
                            )
                        );
                    }

                    var actionId = Guid.NewGuid();
                    _customEquipmentCraftList = _customEquipmentCraftList.Concat(
                        CustomEquipmentCraftData.GetCustomEquipmentCraftInfo(
                            prevState,
                            outputState,
                            new ReplayRandom(evt.RandomSeed),
                            evt.Signer,
                            actionId,
                            customEquipmentCraft,
                            evt.BlockIndex,
                            _blockTimeOffset
                        )
                    ).ToList();
                    Log.Debug(
                        "[DataProvider] Stored RapidCombination action in block #{index}. Time Taken: {time} ms.",
                        evt.BlockIndex,
                        (DateTimeOffset.UtcNow - start).Milliseconds
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

        partial void SubscribeUnlockCombinationSlot(ActionEvaluation<UnlockCombinationSlot> evt)
        {
            try
            {
                if (evt.Exception is null && evt.Action is { } unlockCombinationSlot)
                {
                    var start = DateTimeOffset.UtcNow;
                    _unlockCombinationSlotList.Add(UnlockCombinationSlotData.GetUnlockCombinationSlotInfo(
                        new World(_blockChainStates.GetWorldState(evt.PreviousState)),
                        evt.Signer,
                        evt.Action,
                        evt.BlockIndex,
                        _blockTimeOffset
                    ));

                    Log.Debug(
                        "[DataProvider] Stored RapidCombination action in block #{index}. Time Taken: {time} ms.",
                        evt.BlockIndex,
                        (DateTimeOffset.UtcNow - start).Milliseconds
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
