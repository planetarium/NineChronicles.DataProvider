namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class JoinArenaData
    {
        public static JoinArenaModel GetJoinArenaInfo(
            IAccount previousStates,
            IAccount outputStates,
            Address signer,
            Address avatarAddress,
            int round,
            int championshipId,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = outputStates.GetAvatarStateV2(avatarAddress);
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
                Id = actionId.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                AvatarLevel = avatarState.level,
                ArenaRound = round,
                ChampionshipId = championshipId,
                BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                TimeStamp = blockTime,
            };

            return joinArenaModel;
        }
    }
}
