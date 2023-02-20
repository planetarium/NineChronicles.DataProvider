namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class HasWithRandomBuffData
    {
        public static HasWithRandomBuffModel GetHasWithRandomBuffInfo(
            ActionBase.ActionEvaluation<HackAndSlash> ev,
            HackAndSlash has,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(has.AvatarAddress);
            bool isClear = avatarState.stageMap.ContainsKey(has.StageId);

            var hasModel = new HasWithRandomBuffModel()
            {
                Id = has.Id.ToString(),
                BlockIndex = ev.BlockIndex,
                AgentAddress = ev.Signer.ToString(),
                AvatarAddress = has.AvatarAddress.ToString(),
                StageId = has.StageId,
                BuffId = (int)has.StageBuffId!,
                Cleared = isClear,
                TimeStamp = blockTime,
            };

            return hasModel;
        }
    }
}
