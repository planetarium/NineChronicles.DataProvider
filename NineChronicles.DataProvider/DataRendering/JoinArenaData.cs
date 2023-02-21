namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class JoinArenaData
    {
        public static JoinArenaModel GetJoinArenaInfo(
            JoinArena joinArena,
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = outputStates.GetAvatarStateV2(joinArena.avatarAddress);
            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
            var prevCrystalBalance = previousStates.GetBalance(
                signer,
                crystalCurrency);
            var outputCrystalBalance = outputStates.GetBalance(
                signer,
                crystalCurrency);
            var burntCrystal = prevCrystalBalance - outputCrystalBalance;
            var joinArenaModel = new JoinArenaModel()
            {
                Id = joinArena.Id.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
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
