namespace NineChronicles.DataProvider.GraphTypes
{
    using GraphQL.Types;
    using Libplanet.Explorer.GraphTypes;
    using NineChronicles.DataProvider.Store.Models;

    public class WorldBossRankingType : ObjectGraphType<WorldBossRankingModel>
    {
        public WorldBossRankingType()
        {
            Field<IntGraphType>(
                nameof(WorldBossRankingModel.Ranking),
                description: "Season ranking.",
                resolve: context => context.Source.Ranking
            );
            Field<NonNullGraphType<AddressType>>(
                nameof(WorldBossRankingModel.Address),
                description: "Address of avatar.",
                resolve: context => context.Source.Address
            );
            Field<NonNullGraphType<StringGraphType>>(
                nameof(WorldBossRankingModel.AvatarName),
                description: "Name of avatar.",
                resolve: context => context.Source.AvatarName
            );
            Field<NonNullGraphType<IntGraphType>>(
                nameof(WorldBossRankingModel.HighScore),
                description: "Season high score.",
                resolve: context => context.Source.HighScore
            );
            Field<NonNullGraphType<IntGraphType>>(
                nameof(WorldBossRankingModel.TotalScore),
                description: "Season total score.",
                resolve: context => context.Source.TotalScore
            );
            Field<NonNullGraphType<IntGraphType>>(
                nameof(WorldBossRankingModel.Cp),
                description: "CombatPoint of avatar.",
                resolve: context => context.Source.Cp
            );
            Field<NonNullGraphType<IntGraphType>>(
                nameof(WorldBossRankingModel.Level),
                description: "Level of avatar.",
                resolve: context => context.Source.Level
            );
            Field<NonNullGraphType<IntGraphType>>(
                nameof(WorldBossRankingModel.IconId),
                description: "Icon sprite id.",
                resolve: context => context.Source.IconId
            );
        }
    }
}
