// Refactor: Move RenderSubscriber.cs to Subscriber and make all namespaces to NineChronicles.DataProvider.Subscriber

namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Lib9c.Renderers;
    using Libplanet.Action.State;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.DataRendering;
    using NineChronicles.DataProvider.DataRendering.AdventureBoss;
    using NineChronicles.DataProvider.Store.Models.AdventureBoss;
    using Serilog;

    public partial class RenderSubscriber
    {
        private readonly Dictionary<long, AdventureBossSeasonModel> _adventureBossSeasonDict = new ();
        private readonly List<AdventureBossWantedModel> _adventureBossWantedList = new ();
        private readonly List<AdventureBossChallengeModel> _adventureBossChallengeList = new ();
        private readonly List<AdventureBossRushModel> _adventureBossRushList = new ();
        private readonly List<AdventureBossUnlockFloorModel> _adventureBossUnlockFloorList = new ();
        private readonly List<AdventureBossClaimRewardModel> _adventureBossClaimRewardList = new ();

        private void StoreAdventureBossList()
        {
            try
            {
                var tasks = new List<Task>();
                Log.Debug("[DataProvider AdventureBoss] Store adventure boss list");

                tasks.Add(Task.Run(async () =>
                    {
                        Log.Debug($"[DataProvider AdventureBoss] {_adventureBossSeasonDict.Count} Season");
                        await MySqlStore.StoreAdventureBossSeasonList(_adventureBossSeasonDict.Values.ToList());
                    }
                ));

                tasks.Add(Task.Run(async () =>
                    {
                        Log.Debug($"[DataProvider AdventureBoss] {_adventureBossWantedList.Count} Wanted");
                        await MySqlStore.StoreAdventureBossWantedList(_adventureBossWantedList);
                    }
                ));

                tasks.Add(Task.Run(async () =>
                    {
                        Log.Debug($"[DataProvider AdventureBoss] {_adventureBossChallengeList.Count} Challenge");
                        await MySqlStore.StoreAdventureBossChallengeList(_adventureBossChallengeList);
                    }
                ));

                tasks.Add(Task.Run(async () =>
                    {
                        Log.Debug($"[DataProvider AdventureBoss] {_adventureBossRushList.Count} Rush");
                        await MySqlStore.StoreAdventureBossRushList(_adventureBossRushList);
                    }
                ));

                tasks.Add(Task.Run(async () =>
                    {
                        Log.Debug($"[DataProvider AdventureBoss] {_adventureBossUnlockFloorList.Count} Unlock");
                        await MySqlStore.StoreAdventureBossUnlockFloorList(_adventureBossUnlockFloorList);
                    }
                ));

                tasks.Add(Task.Run(async () =>
                    {
                        Log.Debug($"[DataProvider AdventureBoss] {_adventureBossClaimRewardList.Count} claim");
                        await MySqlStore.StoreAdventureBossClaimRewardList(_adventureBossClaimRewardList);
                    }
                ));

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }

        private void ClearAdventureBossList()
        {
            Log.Debug("[DataProvider AdventureBoss] Clear adventure boss action lists");
            _adventureBossSeasonDict.Clear();
            _adventureBossWantedList.Clear();
            _adventureBossChallengeList.Clear();
            _adventureBossRushList.Clear();
            _adventureBossUnlockFloorList.Clear();
            _adventureBossClaimRewardList.Clear();
        }

        partial void SubscribeAdventureBossWanted(ActionEvaluation<Wanted> evt)
        {
            Log.Debug("[DataProvider AdventureBoss] Subscribe Wanted");
            try
            {
                if (evt.Exception is null && evt.Action is { } wanted)
                {
                    var start = DateTimeOffset.UtcNow;
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    var avatarAddress = wanted.AvatarAddress;
                    if (!_avatars.Contains(avatarAddress.ToString()))
                    {
                        Log.Debug("[DataProvider] AvatarInfo Stored {avatarAddress} SubscribeAdventureBossWanted action in block #{index}.", avatarAddress, evt.BlockIndex);
                        _avatars.Add(avatarAddress.ToString());
                        _avatarList.Add(AvatarData.GetAvatarInfo(outputState, evt.Signer, avatarAddress, _blockTimeOffset, BattleType.Adventure));
                    }

                    _adventureBossWantedList.Add(AdventureBossWantedData.GetWantedInfo(
                        outputState, evt.BlockIndex, _blockTimeOffset, wanted
                    ));
                    Log.Debug($"[DataProvider AdventureBoss] Wanted added : {_adventureBossWantedList.Count}");

                    // Update season info
                    _adventureBossSeasonDict[wanted.Season] = AdventureBossSeasonData.GetAdventureBossSeasonInfo(
                        outputState, wanted.Season, _blockTimeOffset
                    );
                    Log.Debug($"[DataProvider AdventureBoss] Season added : {_adventureBossSeasonDict.Count}");
                    var end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored AdventureBossWanted action in block #{BlockIndex}. Time taken: {Time} ms", evt.BlockIndex, end - start);
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "RenderSubscriber Error: {ErrorMessage}, StackTrace: {StackTrace}",
                    e.Message,
                    e.StackTrace
                );
            }
        }

        partial void SubscribeAdventureBossChallenge(ActionEvaluation<ExploreAdventureBoss> evt)
        {
            Log.Debug("[DataProvider AdventureBoss] Subscribe Explore");
            try
            {
                if (evt.Exception is null && evt.Action is { } challenge)
                {
                    var start = DateTimeOffset.UtcNow;
                    var prevState = new World(_blockChainStates.GetWorldState(evt.PreviousState));
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    var avatarAddress = challenge.AvatarAddress;
                    if (!_avatars.Contains(avatarAddress.ToString()))
                    {
                        Log.Debug("[DataProvider] AvatarInfo Stored {avatarAddress} SubscribeAdventureBossChallenge action in block #{index}.", avatarAddress, evt.BlockIndex);
                        _avatars.Add(avatarAddress.ToString());
                        _avatarList.Add(AvatarData.GetAvatarInfo(outputState, evt.Signer, avatarAddress, _blockTimeOffset, BattleType.Adventure));
                    }

                    _adventureBossChallengeList.Add(AdventureBossChallengeData.GetChallengeInfo(
                        prevState, outputState, evt.BlockIndex, _blockTimeOffset, challenge
                    ));
                    Log.Debug($"[DataProvider AdventureBoss] Challenge added : {_adventureBossChallengeList.Count}");
                    var end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored AdventureBossChallenge action in block #{BlockIndex}. Time taken: {Time} ms", evt.BlockIndex, end - start);
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "RenderSubscriber Error: {ErrorMessage}, StackTrace: {StackTrace}",
                    e.Message,
                    e.StackTrace
                );
            }
        }

        partial void SubscribeAdventureBossRush(ActionEvaluation<SweepAdventureBoss> evt)
        {
            Log.Debug("[DataProvider AdventureBoss] Subscribe Rush");
            try
            {
                if (evt.Exception is null && evt.Action is { } rush)
                {
                    var start = DateTimeOffset.UtcNow;
                    var prevState = new World(_blockChainStates.GetWorldState(evt.PreviousState));
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    var avatarAddress = rush.AvatarAddress;
                    if (!_avatars.Contains(avatarAddress.ToString()))
                    {
                        Log.Debug("[DataProvider] AvatarInfo Stored {avatarAddress} SubscribeAdventureBossRush action in block #{index}.", avatarAddress, evt.BlockIndex);
                        _avatars.Add(avatarAddress.ToString());
                        _avatarList.Add(AvatarData.GetAvatarInfo(outputState, evt.Signer, avatarAddress, _blockTimeOffset, BattleType.Adventure));
                    }

                    _adventureBossRushList.Add(AdventureBossRushData.GetRushInfo(
                        prevState, outputState, evt.BlockIndex, _blockTimeOffset, rush
                    ));
                    Log.Debug($"[DataProvider AdventureBoss] Rush added : {_adventureBossRushList.Count}");
                    var end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored AdventureBossRush action in block #{BlockIndex}. Time taken: {Time} ms", evt.BlockIndex, end - start);
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "RenderSubscriber Error: {ErrorMessage}, StackTrace: {StackTrace}",
                    e.Message,
                    e.StackTrace
                );
            }
        }

        partial void SubscribeAdventureBossUnlockFloor(ActionEvaluation<UnlockFloor> evt)
        {
            Log.Debug("[DataProvider AdventureBoss] Subscribe UnlockFloor");
            try
            {
                if (evt.Exception is null && evt.Action is { } unlock)
                {
                    var start = DateTimeOffset.UtcNow;
                    var prevState = new World(_blockChainStates.GetWorldState(evt.PreviousState));
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    var avatarAddress = unlock.AvatarAddress;
                    if (!_avatars.Contains(avatarAddress.ToString()))
                    {
                        Log.Debug("[DataProvider] AvatarInfo Stored {avatarAddress} SubscribeAdventureBossUnlockFloor action in block #{index}.", avatarAddress, evt.BlockIndex);
                        _avatars.Add(avatarAddress.ToString());
                        _avatarList.Add(AvatarData.GetAvatarInfo(outputState, evt.Signer, avatarAddress, _blockTimeOffset, BattleType.Adventure));
                    }

                    _adventureBossUnlockFloorList.Add(AdventureBossUnlockFloorData.GetUnlockInfo(
                        prevState, outputState, evt.BlockIndex, _blockTimeOffset, unlock
                    ));
                    Log.Debug($"[DataProvider AdventureBoss] Unlock added : {_adventureBossUnlockFloorList.Count}");
                    var end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored AdventureBossUnlock action in block #{BlockIndex}. Time taken: {Time} ms", evt.BlockIndex, end - start);
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "RenderSubscriber Error: {ErrorMessage}, StackTrace: {StackTrace}",
                    e.Message,
                    e.StackTrace
                );
            }
        }

        partial void SubscribeAdventureBossClaim(ActionEvaluation<ClaimAdventureBossReward> evt)
        {
            Log.Debug("[DataProvider AdventureBoss] Subscribe Claim");
            try
            {
                if (evt.Exception is null && evt.Action is { } claim)
                {
                    var start = DateTimeOffset.UtcNow;
                    var prevState = new World(_blockChainStates.GetWorldState(evt.PreviousState));
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    var avatarAddress = claim.AvatarAddress;
                    if (!_avatars.Contains(avatarAddress.ToString()))
                    {
                        Log.Debug("[DataProvider] AvatarInfo Stored {avatarAddress} SubscribeAdventureBossClaim action in block #{index}.", avatarAddress, evt.BlockIndex);
                        _avatars.Add(avatarAddress.ToString());
                        _avatarList.Add(AvatarData.GetAvatarInfo(outputState, evt.Signer, avatarAddress, _blockTimeOffset, BattleType.Adventure));
                    }

                    _adventureBossClaimRewardList.Add(AdventureBossClaimRewardData.GetClaimInfo(
                        prevState, evt.BlockIndex, _blockTimeOffset, claim
                    ));
                    Log.Debug($"[DataProvider AdventureBoss] Claim added : {_adventureBossClaimRewardList.Count}");
                    var end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored AdventureBossClaim action in block #{BlockIndex}. Time taken: {Time} ms", evt.BlockIndex, end - start);

                    // Update season info
                    start = DateTimeOffset.UtcNow;
                    var latestSeason = prevState.GetLatestAdventureBossSeason();
                    var season = latestSeason.EndBlockIndex <= evt.BlockIndex
                        ? latestSeason.Season // New season not started
                        : latestSeason.Season - 1; // New season started
                    _adventureBossSeasonDict[season] = AdventureBossSeasonData.GetAdventureBossSeasonInfo(
                        outputState, season, _blockTimeOffset
                    );
                    Log.Debug($"[DataProvider AdventureBoss] Season updated : {_adventureBossSeasonDict.Count}");
                    end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored AdventureBossSeason action in block #{BlockIndex}. Time taken: {Time} ms", evt.BlockIndex, end - start);
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "RenderSubscriber Error: {ErrorMessage}, StackTrace: {StackTrace}",
                    e.Message,
                    e.StackTrace
                );
            }
        }
    }
}
