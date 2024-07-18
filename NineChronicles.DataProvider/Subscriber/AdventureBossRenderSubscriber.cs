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

                tasks.Add(Task.Run(async () =>
                    {
                        await MySqlStore.StoreAdventureBossSeasonList(_adventureBossSeasonDict.Values.ToList());
                    }
                ));

                tasks.Add(Task.Run(async () =>
                    {
                        await MySqlStore.StoreAdventureBossWantedList(_adventureBossWantedList);
                    }
                ));

                tasks.Add(Task.Run(async () =>
                    {
                        await MySqlStore.StoreAdventureBossChallengeList(_adventureBossChallengeList);
                    }
                ));

                tasks.Add(Task.Run(async () =>
                    {
                        await MySqlStore.StoreAdventureBossRushList(_adventureBossRushList);
                    }
                ));

                tasks.Add(Task.Run(async () =>
                    {
                        await MySqlStore.StoreAdventureBossUnlockFloorList(_adventureBossUnlockFloorList);
                    }
                ));

                tasks.Add(Task.Run(async () =>
                    {
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
            _adventureBossSeasonDict.Clear();
            _adventureBossWantedList.Clear();
            _adventureBossChallengeList.Clear();
            _adventureBossRushList.Clear();
            _adventureBossUnlockFloorList.Clear();
            _adventureBossClaimRewardList.Clear();
        }

        partial void SubscribeAdventureBossWanted(ActionEvaluation<Wanted> evt)
        {
            try
            {
                if (evt.Exception is null && evt.Action is { } wanted)
                {
                    var start = DateTimeOffset.UtcNow;
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    var avatarAddress = wanted.AvatarAddress;
                    if (!_avatars.Contains(avatarAddress.ToString()))
                    {
                        _avatars.Add(avatarAddress.ToString());
                        _avatarList.Add(AvatarData.GetAvatarInfo(outputState, evt.Signer, avatarAddress, _blockTimeOffset, BattleType.Adventure));
                    }

                    _adventureBossWantedList.Add(AdventureBossWantedData.GetWantedInfo(
                        outputState, evt.BlockIndex, _blockTimeOffset, wanted
                    ));

                    // Update season info
                    _adventureBossSeasonDict[wanted.Season] = AdventureBossSeasonData.GetAdventureBossSeasonInfo(
                        outputState, wanted.Season, _blockTimeOffset
                    );
                    var end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored AdventureBossWanted action in block #{BlockIndex}. Time taken: {Time} ms", evt.BlockIndex, end - start);
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

        partial void SubscribeAdventureBossChallenge(ActionEvaluation<ExploreAdventureBoss> evt)
        {
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
                        _avatars.Add(avatarAddress.ToString());
                        _avatarList.Add(AvatarData.GetAvatarInfo(outputState, evt.Signer, avatarAddress, _blockTimeOffset, BattleType.Adventure));
                    }

                    _adventureBossChallengeList.Add(AdventureBossChallengeData.GetChallengeInfo(
                        prevState, outputState, evt.BlockIndex, _blockTimeOffset, challenge
                    ));
                    var end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored AdventureBossChallenge action in block #{BlockIndex}. Time taken: {Time} ms", evt.BlockIndex, end - start);
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

        partial void SubscribeAdventureBossRush(ActionEvaluation<SweepAdventureBoss> evt)
        {
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
                        _avatars.Add(avatarAddress.ToString());
                        _avatarList.Add(AvatarData.GetAvatarInfo(outputState, evt.Signer, avatarAddress, _blockTimeOffset, BattleType.Adventure));
                    }

                    _adventureBossRushList.Add(AdventureBossRushData.GetRushInfo(
                        prevState, outputState, evt.BlockIndex, _blockTimeOffset, rush
                    ));
                    var end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored AdventureBossRush action in block #{BlockIndex}. Time taken: {Time} ms", evt.BlockIndex, end - start);
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

        partial void SubscribeAdventureBossUnlockFloor(ActionEvaluation<UnlockFloor> evt)
        {
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
                        _avatars.Add(avatarAddress.ToString());
                        _avatarList.Add(AvatarData.GetAvatarInfo(outputState, evt.Signer, avatarAddress, _blockTimeOffset, BattleType.Adventure));
                    }

                    _adventureBossUnlockFloorList.Add(AdventureBossUnlockFloorData.GetUnlockInfo(
                        prevState, outputState, evt.BlockIndex, _blockTimeOffset, unlock
                    ));
                    var end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored AdventureBossUnlock action in block #{BlockIndex}. Time taken: {Time} ms", evt.BlockIndex, end - start);
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

        partial void SubscribeAdventureBossClaim(ActionEvaluation<ClaimAdventureBossReward> evt)
        {
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
                        _avatars.Add(avatarAddress.ToString());
                        _avatarList.Add(AvatarData.GetAvatarInfo(outputState, evt.Signer, avatarAddress, _blockTimeOffset, BattleType.Adventure));
                    }

                    _adventureBossClaimRewardList.Add(AdventureBossClaimRewardData.GetClaimInfo(
                        prevState, evt.BlockIndex, _blockTimeOffset, claim
                    ));
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
                    end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored AdventureBossSeason action in block #{BlockIndex}. Time taken: {Time} ms", evt.BlockIndex, end - start);
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
