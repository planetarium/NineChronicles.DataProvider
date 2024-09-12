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
    using Nekoyume.Module;
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
                Log.Debug("[DataProvider] AdventureBossStore adventure boss list");

                tasks.Add(Task.Run(async () =>
                    {
                        Log.Debug($"[Adventure Boss] {_adventureBossSeasonDict.Count} Season");
                        await MySqlStore.StoreAdventureBossSeasonList(_adventureBossSeasonDict.Values.ToList());
                    }
                ));

                tasks.Add(Task.Run(async () =>
                    {
                        Log.Debug($"[DataProvider] AdventureBoss{_adventureBossWantedList.Count} Wanted");
                        await MySqlStore.StoreAdventureBossWantedList(_adventureBossWantedList);
                    }
                ));

                tasks.Add(Task.Run(async () =>
                    {
                        Log.Debug($"[DataProvider] AdventureBoss{_adventureBossChallengeList.Count} Challenge");
                        await MySqlStore.StoreAdventureBossChallengeList(_adventureBossChallengeList);
                    }
                ));

                tasks.Add(Task.Run(async () =>
                    {
                        Log.Debug($"[DataProvider] AdventureBoss{_adventureBossRushList.Count} Rush");
                        await MySqlStore.StoreAdventureBossRushList(_adventureBossRushList);
                    }
                ));

                tasks.Add(Task.Run(async () =>
                    {
                        Log.Debug($"[DataProvider] AdventureBoss{_adventureBossUnlockFloorList.Count} Unlock");
                        await MySqlStore.StoreAdventureBossUnlockFloorList(_adventureBossUnlockFloorList);
                    }
                ));

                tasks.Add(Task.Run(async () =>
                    {
                        Log.Debug($"[DataProvider] AdventureBoss{_adventureBossClaimRewardList.Count} claim");
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
            Log.Debug("[DataProvider] Clear adventure boss action lists");
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
                    _adventureBossWantedList.Add(AdventureBossWantedData.GetWantedInfo(
                        outputState, evt.BlockIndex, _blockTimeOffset, wanted
                    ));
                    var end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored {count} AdventureBossWanted action in block #{BlockIndex}. Time taken: {Time} ms", _adventureBossWantedList.Count, evt.BlockIndex, end - start);

                    // Update season info
                    _adventureBossSeasonDict[wanted.Season] = AdventureBossSeasonData.GetAdventureBossSeasonInfo(
                        outputState, wanted.Season, _blockTimeOffset
                    );
                    end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored {count} AdventureBossSeason action in block #{BlockIndex}. Time taken: {Time} ms", _adventureBossSeasonDict.Count, evt.BlockIndex, end - start);
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
                    _adventureBossChallengeList.Add(AdventureBossChallengeData.GetChallengeInfo(
                        prevState, outputState, evt.BlockIndex, _blockTimeOffset, challenge
                    ));
                    var end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored {count} AdventureBossChallenge action in block #{BlockIndex}. Time taken: {Time} ms", _adventureBossChallengeList.Count, evt.BlockIndex, end - start);
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
                var start = DateTimeOffset.UtcNow;
                if (evt.Exception is null && evt.Action is { } rush)
                {
                    var prevState = new World(_blockChainStates.GetWorldState(evt.PreviousState));
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    _adventureBossRushList.Add(AdventureBossRushData.GetRushInfo(
                        prevState, outputState, evt.BlockIndex, _blockTimeOffset, rush
                    ));
                    var end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored {count} AdventureBossRush action in block #{BlockIndex}. Time taken: {Time} ms", _adventureBossRushList.Count, evt.BlockIndex, end - start);
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
                    _adventureBossUnlockFloorList.Add(AdventureBossUnlockFloorData.GetUnlockInfo(
                        prevState, outputState, evt.BlockIndex, _blockTimeOffset, unlock
                    ));
                    var end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored {count} AdventureBossUnlock action in block #{BlockIndex}. Time taken: {Time} ms", _adventureBossUnlockFloorList.Count, evt.BlockIndex, end - start);
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
                    _adventureBossClaimRewardList.Add(AdventureBossClaimRewardData.GetClaimInfo(
                        prevState, evt.BlockIndex, _blockTimeOffset, claim
                    ));
                    var end = DateTimeOffset.UtcNow;
                    Log.Debug("[DataProvider] Stored {count} AdventureBossClaim action in block #{BlockIndex}. Time taken: {Time} ms", _adventureBossClaimRewardList.Count, evt.BlockIndex, end - start);

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
                    Log.Debug("[DataProvider] Stored {count} AdventureBossSeason action in block #{BlockIndex}. Time taken: {Time} ms", _adventureBossClaimRewardList.Count, evt.BlockIndex, end - start);
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
