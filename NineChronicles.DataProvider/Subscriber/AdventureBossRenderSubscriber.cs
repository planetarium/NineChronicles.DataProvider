// Refactor: Move RenderSubscriber.cs to Subscriber and make all namespaces to NineChronicles.DataProvider.Subscriber

namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using Lib9c.Renderers;
    using Libplanet.Action.State;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.DataRendering.AdventureBoss;
    using NineChronicles.DataProvider.Store.Models.AdventureBoss;
    using Serilog;

    public partial class RenderSubscriber
    {
        private readonly List<AdventureBossSeasonModel> _adventureBossSeasonList = new ();
        private readonly List<AdventureBossWantedModel> _adventureBossWantedList = new ();
        private readonly List<AdventureBossChallengeModel> _adventureBossChallengeList = new ();
        private readonly List<AdventureBossRushModel> _adventureBossRushList = new ();
        private readonly List<AdventureBossUnlockFloorModel> _adventureBossUnlockFloorList = new ();
        private readonly List<AdventureBossClaimRewardModel> _adventureBossClaimRewardList = new ();

        private void StoreAdventureBossList()
        {
            Log.Information("[Adventure Boss] Store adventure boss list");

            Log.Information($"[Adventure Boss] {_adventureBossSeasonList.Count} Season");
            MySqlStore.StoreAdventureBossSeasonList(_adventureBossSeasonList);

            Log.Information($"[Adventure Boss] {_adventureBossWantedList.Count} Wanted");
            MySqlStore.StoreAdventureBossWantedList(_adventureBossWantedList);

            Log.Information($"[Adventure Boss] {_adventureBossChallengeList.Count} Challenge");
            MySqlStore.StoreAdventureBossChallengeList(_adventureBossChallengeList);

            Log.Information($"[Adventure Boss] {_adventureBossRushList.Count} Rush");
            MySqlStore.StoreAdventureBossRushList(_adventureBossRushList);

            Log.Information($"[Adventure Boss] {_adventureBossUnlockFloorList.Count} Unlock");
            MySqlStore.StoreAdventureBossUnlockFloorList(_adventureBossUnlockFloorList);

            Log.Information($"[Adventure Boss] {_adventureBossClaimRewardList.Count} claim");
            MySqlStore.StoreAdventureBossClaimRewardList(_adventureBossClaimRewardList);
        }

        private void ClearAdventureBossList()
        {
            _adventureBossWantedList.Clear();
            _adventureBossChallengeList.Clear();
            _adventureBossRushList.Clear();
            _adventureBossUnlockFloorList.Clear();
            _adventureBossClaimRewardList.Clear();
        }

        partial void SubscribeAdventureBossWanted(ActionEvaluation<Wanted> evt)
        {
            Log.Information("[Adventure Boss] Subscribe Wanted");
            try
            {
                if (evt.Exception is null && evt.Action is { } wanted)
                {
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    _adventureBossWantedList.Add(AdventureBossWantedData.GetWantedInfo(
                        outputState, evt.BlockIndex, _blockTimeOffset, wanted
                    ));

                    // Update season info
                    _adventureBossSeasonList.Add(AdventureBossSeasonData.GetAdventureBossSeasonInfo(
                        outputState, wanted.Season, _blockTimeOffset
                    ));
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
            Log.Information("[Adventure Boss] Subscribe Explore");
            try
            {
                if (evt.Exception is null && evt.Action is { } challenge)
                {
                    var prevState = new World(_blockChainStates.GetWorldState(evt.PreviousState));
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    _adventureBossChallengeList.Add(AdventureBossChallengeData.GetChallengeInfo(
                        prevState, outputState, evt.BlockIndex, _blockTimeOffset, challenge
                    ));
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
            Log.Information("[Adventure Boss] Subscribe Rush");
            try
            {
                if (evt.Exception is null && evt.Action is { } rush)
                {
                    var prevState = new World(_blockChainStates.GetWorldState(evt.PreviousState));
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    _adventureBossRushList.Add(AdventureBossRushData.GetRushInfo(
                        prevState, outputState, evt.BlockIndex, _blockTimeOffset, rush
                    ));
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
            Log.Information("[Adventure Boss] Subscribe UnlockFloor");
            try
            {
                if (evt.Exception is null && evt.Action is { } unlock)
                {
                    var prevState = new World(_blockChainStates.GetWorldState(evt.PreviousState));
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    _adventureBossUnlockFloorList.Add(AdventureBossUnlockFloorData.GetUnlockInfo(
                        prevState, outputState, evt.BlockIndex, _blockTimeOffset, unlock
                    ));
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
            Log.Information("[Adventure Boss] Subscribe Claim");
            try
            {
                if (evt.Exception is null && evt.Action is { } claim)
                {
                    var prevState = new World(_blockChainStates.GetWorldState(evt.PreviousState));
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    _adventureBossClaimRewardList.Add(AdventureBossClaimRewardData.GetClaimInfo(
                        prevState, evt.BlockIndex, _blockTimeOffset, claim
                    ));

                    // Update season info
                    var latestSeason = prevState.GetLatestAdventureBossSeason();
                    var season = latestSeason.EndBlockIndex <= evt.BlockIndex
                        ? latestSeason.Season // New season not started
                        : latestSeason.Season - 1; // New season started
                    _adventureBossSeasonList.Add(AdventureBossSeasonData.GetAdventureBossSeasonInfo(
                        outputState, season, _blockTimeOffset
                    ));
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
