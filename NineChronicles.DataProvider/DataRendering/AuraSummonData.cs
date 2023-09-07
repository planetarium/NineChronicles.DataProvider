namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using NineChronicles.DataProvider.Store.Models;

    public static class AuraSummonData
    {
        public static AuraSummonModel GetAuraSummonInfo(
            IAccount previousStates,
            IAccount outputStates,
            Address signer,
            Address avatarAddress,
            int groupId,
            int summonCount,
            Guid actionId,
            long blockIndex
        )
        {
            return new AuraSummonModel
            {
                Id = actionId.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                GroupId = groupId,
                SummonCount = summonCount,
                BlockIndex = blockIndex,
            };
        }

        public static AuraSummonFailModel GetAuraSummonFailInfo(
            IAccount previousStates,
            IAccount outputStates,
            Address signer,
            Address avatarAddress,
            int groupId,
            int summonCount,
            Guid actionId,
            long blockIndex,
            Exception exc
        )
        {
            return new AuraSummonFailModel
            {
                Id = actionId.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                GroupId = groupId,
                SummonCount = summonCount,
                BlockIndex = blockIndex,
                Exception = exc.ToString(),
            };
        }
    }
}
