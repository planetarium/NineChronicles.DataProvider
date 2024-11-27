namespace NineChronicles.DataProvider.DataRendering.Claim
{
    using System;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models.Claim;

    public static class ClaimGiftsData
    {
        public static ClaimGiftsModel GetClaimInfo(
        Address signer,
        long blockIndex,
        DateTimeOffset blockTime,
        ClaimGifts action
            )
        {
            return new ClaimGiftsModel
            {
                Id = action.Id.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                AvatarAddress = action.AvatarAddress.ToString(),
                GiftId = action.GiftId,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };
        }
    }
}
