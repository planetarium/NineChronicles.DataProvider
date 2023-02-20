namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Globalization;
    using Bencodex.Types;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class BattleGrandFinaleData
    {
        public static BattleGrandFinaleModel GetBattleGrandFinaleInfo(
            ActionBase.ActionEvaluation<BattleGrandFinale> ev,
            BattleGrandFinale battleGrandFinale,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(battleGrandFinale.myAvatarAddress);
            var previousStates = ev.PreviousStates;
            var scoreAddress = battleGrandFinale.myAvatarAddress.Derive(string.Format(CultureInfo.InvariantCulture, BattleGrandFinale.ScoreDeriveKey, battleGrandFinale.grandFinaleId));
            previousStates.TryGetState(scoreAddress, out Integer previousGrandFinaleScore);
            ev.OutputStates.TryGetState(scoreAddress, out Integer outputGrandFinaleScore);

            var battleGrandFinaleModel = new BattleGrandFinaleModel()
            {
                Id = battleGrandFinale.Id.ToString(),
                BlockIndex = ev.BlockIndex,
                AgentAddress = ev.Signer.ToString(),
                AvatarAddress = battleGrandFinale.myAvatarAddress.ToString(),
                AvatarLevel = avatarState.level,
                EnemyAvatarAddress = battleGrandFinale.enemyAvatarAddress.ToString(),
                GrandFinaleId = battleGrandFinale.grandFinaleId,
                Victory = outputGrandFinaleScore > previousGrandFinaleScore,
                GrandFinaleScore = outputGrandFinaleScore,
                Date = blockTime,
                TimeStamp = blockTime,
            };

            return battleGrandFinaleModel;
        }
    }
}
