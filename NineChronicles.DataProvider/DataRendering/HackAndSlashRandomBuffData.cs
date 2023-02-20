namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class HackAndSlashRandomBuffData
    {
        public static HasRandomBuffModel GetHasRandomBuffInfo(
            ActionBase.ActionEvaluation<HackAndSlashRandomBuff> ev,
            HackAndSlashRandomBuff hasRandomBuff,
            DateTimeOffset blockTime
        )
        {
            var previousStates = ev.PreviousStates;
            AvatarState prevAvatarState = previousStates.GetAvatarStateV2(hasRandomBuff.AvatarAddress);
            prevAvatarState.worldInformation.TryGetLastClearedStageId(out var currentStageId);
            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
            var prevCrystalBalance = previousStates.GetBalance(
                ev.Signer,
                crystalCurrency);
            var outputCrystalBalance = ev.OutputStates.GetBalance(
                ev.Signer,
                crystalCurrency);
            var burntCrystal = prevCrystalBalance - outputCrystalBalance;
            var hasRandomBuffModel = new HasRandomBuffModel()
            {
                Id = hasRandomBuff.Id.ToString(),
                BlockIndex = ev.BlockIndex,
                AgentAddress = ev.Signer.ToString(),
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
