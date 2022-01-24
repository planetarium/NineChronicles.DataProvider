﻿namespace NineChronicles.DataProvider.GraphQL.Types
{
    using global::GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class CraftRankingType : ObjectGraphType<CraftRankingOutputModel>
    {
        public CraftRankingType()
        {
            Field(x => x.AvatarAddress);
            Field(x => x.AgentAddress);
            Field(x => x.BlockIndex);
            Field(x => x.CraftCount);
            Field(x => x.Name);
            Field(x => x.AvatarLevel, nullable: true);
            Field(x => x.TitleId, nullable: true);
            Field(x => x.ArmorId, nullable: true);
            Field(x => x.Cp, nullable: true);
            Field(x => x.Ranking);

            Name = "CraftRanking";
        }
    }
}
