namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Globalization;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class BattleGrandFinaleData
    {
        public static BattleGrandFinaleModel GetBattleGrandFinaleInfo(
            BattleGrandFinale battleGrandFinale,
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = outputStates.GetAvatarStateV2(battleGrandFinale.myAvatarAddress);
            var scoreAddress = battleGrandFinale.myAvatarAddress.Derive(string.Format(CultureInfo.InvariantCulture, BattleGrandFinale.ScoreDeriveKey, battleGrandFinale.grandFinaleId));
            previousStates.TryGetState(scoreAddress, out Integer previousGrandFinaleScore);
            outputStates.TryGetState(scoreAddress, out Integer outputGrandFinaleScore);

            var battleGrandFinaleModel = new BattleGrandFinaleModel()
            {
                Id = battleGrandFinale.Id.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
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
