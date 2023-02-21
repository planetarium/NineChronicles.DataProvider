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

    public static class HackAndSlashRandomBuffData
    {
        public static HasRandomBuffModel GetHasRandomBuffInfo(
            HackAndSlashRandomBuff hasRandomBuff,
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState prevAvatarState = previousStates.GetAvatarStateV2(hasRandomBuff.AvatarAddress);
            prevAvatarState.worldInformation.TryGetLastClearedStageId(out var currentStageId);
            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
            var prevCrystalBalance = previousStates.GetBalance(
                signer,
                crystalCurrency);
            var outputCrystalBalance = outputStates.GetBalance(
                signer,
                crystalCurrency);
            var burntCrystal = prevCrystalBalance - outputCrystalBalance;
            var hasRandomBuffModel = new HasRandomBuffModel()
            {
                Id = hasRandomBuff.Id.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                AvatarAddress = hasRandomBuff.AvatarAddress.ToString(),
                HasStageId = currentStageId,
                GachaCount = !hasRandomBuff.AdvancedGacha ? 5 : 10,
                BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                TimeStamp = blockTime,
            };

            return hasRandomBuffModel;
        }
    }
}
