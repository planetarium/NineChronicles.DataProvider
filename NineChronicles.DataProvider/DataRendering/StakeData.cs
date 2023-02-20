namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Linq;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;
    using static Lib9c.SerializeKeys;

    public static class StakeData
    {
        public static StakeModel GetStakeInfo(
            ActionBase.ActionEvaluation<Stake> ev,
            DateTimeOffset blockTime
        )
        {
            ev.OutputStates.TryGetStakeState(ev.Signer, out StakeState stakeState);
            var prevStakeStartBlockIndex =
                !ev.PreviousStates.TryGetStakeState(ev.Signer, out StakeState prevStakeState)
                    ? 0 : prevStakeState.StartedBlockIndex;
            var newStakeStartBlockIndex = stakeState.StartedBlockIndex;
            var currency = ev.OutputStates.GetGoldCurrency();
            var balance = ev.OutputStates.GetBalance(ev.Signer, currency);
            var stakeStateAddress = StakeState.DeriveAddress(ev.Signer);
            var previousAmount = ev.PreviousStates.GetBalance(stakeStateAddress, currency);
            var newAmount = ev.OutputStates.GetBalance(stakeStateAddress, currency);

            var stakeModel = new StakeModel()
            {
                BlockIndex = ev.BlockIndex,
                AgentAddress = ev.Signer.ToString(),
                PreviousAmount = Convert.ToDecimal(previousAmount.GetQuantityString()),
                NewAmount = Convert.ToDecimal(newAmount.GetQuantityString()),
                RemainingNCG = Convert.ToDecimal(balance.GetQuantityString()),
                PrevStakeStartBlockIndex = prevStakeStartBlockIndex,
                NewStakeStartBlockIndex = newStakeStartBlockIndex,
                TimeStamp = blockTime,
            };

            return stakeModel;
        }
    }
}
