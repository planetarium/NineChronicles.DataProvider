namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class UnlockRuneSlotData
    {
        public static UnlockRuneSlotModel GetUnlockRuneSlotInfo(
            UnlockRuneSlot unlockRuneSlot,
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            Currency ncgCurrency = outputStates.GetGoldCurrency();
            var prevNCGBalance = previousStates.GetBalance(
                signer,
                ncgCurrency);
            var outputNCGBalance = outputStates.GetBalance(
                signer,
                ncgCurrency);
            var burntNCG = prevNCGBalance - outputNCGBalance;
            var unlockRuneSlotModel = new UnlockRuneSlotModel()
            {
                Id = unlockRuneSlot.Id.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
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
