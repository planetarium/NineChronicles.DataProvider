namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Globalization;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.State;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class BattleGrandFinaleData
    {
        public static BattleGrandFinaleModel GetBattleGrandFinaleInfo(
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            Address myAvatarAddress,
            Address enemyAvatarAddress,
            int grandFinaleId,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = outputStates.GetAvatarStateV2(myAvatarAddress);
            var scoreAddress = myAvatarAddress.Derive(string.Format(CultureInfo.InvariantCulture, BattleGrandFinale.ScoreDeriveKey, grandFinaleId));
            previousStates.TryGetState(scoreAddress, out Integer previousGrandFinaleScore);
            outputStates.TryGetState(scoreAddress, out Integer outputGrandFinaleScore);

            var battleGrandFinaleModel = new BattleGrandFinaleModel()
            {
                Id = actionId.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                AvatarAddress = myAvatarAddress.ToString(),
                AvatarLevel = avatarState.level,
                EnemyAvatarAddress = enemyAvatarAddress.ToString(),
                GrandFinaleId = grandFinaleId,
                Victory = outputGrandFinaleScore > previousGrandFinaleScore,
                GrandFinaleScore = outputGrandFinaleScore,
                Date = blockTime,
                TimeStamp = blockTime,
            };

            return battleGrandFinaleModel;
        }
    }
}
