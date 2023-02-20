namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class UnlockRuneSlotData
    {
        public static UnlockRuneSlotModel GetUnlockRuneSlotInfo(
            ActionBase.ActionEvaluation<UnlockRuneSlot> ev,
            UnlockRuneSlot unlockRuneSlot,
            DateTimeOffset blockTime
        )
        {
            var previousStates = ev.PreviousStates;
            Currency ncgCurrency = ev.OutputStates.GetGoldCurrency();
            var prevNCGBalance = previousStates.GetBalance(
                ev.Signer,
                ncgCurrency);
            var outputNCGBalance = ev.OutputStates.GetBalance(
                ev.Signer,
                ncgCurrency);
            var burntNCG = prevNCGBalance - outputNCGBalance;
            var unlockRuneSlotModel = new UnlockRuneSlotModel()
            {
                Id = unlockRuneSlot.Id.ToString(),
                BlockIndex = ev.BlockIndex,
                AgentAddress = ev.Signer.ToString(),
                AvatarAddress = unlockRuneSlot.AvatarAddress.ToString(),
                SlotIndex = unlockRuneSlot.SlotIndex,
                BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };

            return unlockRuneSlotModel;
        }
    }
}
