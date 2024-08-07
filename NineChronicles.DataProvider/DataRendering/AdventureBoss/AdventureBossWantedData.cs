namespace NineChronicles.DataProvider.DataRendering.AdventureBoss
{
    using System;
    using System.Linq;
    using Libplanet.Action.State;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models.AdventureBoss;

    public static class AdventureBossWantedData
    {
        public static AdventureBossWantedModel GetWantedInfo(
            IWorld outputStates,
            long blockIndex,
            DateTimeOffset blockTime,
            Wanted wanted
        )
        {
            var bountyBoard = outputStates.GetBountyBoard(wanted.Season);
            var investor = bountyBoard.Investors.First(inv => inv.AvatarAddress == wanted.AvatarAddress);

            return new AdventureBossWantedModel
            {
                Id = wanted.Id.ToString(),
                BlockIndex = blockIndex,
                Season = wanted.Season,
                AvatarAddress = wanted.AvatarAddress.ToString(),
                Bounty = Convert.ToDecimal(wanted.Bounty.GetQuantityString()),
                Count = investor.Count,
                TotalBounty = Convert.ToDecimal(bountyBoard.totalBounty().GetQuantityString()),
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };
        }
    }
}
