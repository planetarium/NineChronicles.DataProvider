namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class JoinArenaData
    {
        public static JoinArenaModel GetJoinArenaInfo(
            ActionBase.ActionEvaluation<JoinArena> ev,
            JoinArena joinArena,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(joinArena.avatarAddress);
            var previousStates = ev.PreviousStates;
            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
            var prevCrystalBalance = previousStates.GetBalance(
                ev.Signer,
                crystalCurrency);
            var outputCrystalBalance = ev.OutputStates.GetBalance(
                ev.Signer,
                crystalCurrency);
            var burntCrystal = prevCrystalBalance - outputCrystalBalance;
            var joinArenaModel = new JoinArenaModel()
            {
                Id = joinArena.Id.ToString(),
                BlockIndex = ev.BlockIndex,
                AgentAddress = ev.Signer.ToString(),
                AvatarAddress = joinArena.avatarAddress.ToString(),
                AvatarLevel = avatarState.level,
                ArenaRound = joinArena.round,
                ChampionshipId = joinArena.championshipId,
                BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                TimeStamp = blockTime,
            };

            return joinArenaModel;
        }
    }
}
