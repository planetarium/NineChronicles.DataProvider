namespace NineChronicles.DataProvider.GraphTypes
{
    using GraphQL.Types;
    using Libplanet.Crypto;
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

            // FIXME should be use AddressType
            Field<NonNullGraphType<StringGraphType>>(
                nameof(WorldBossRankingModel.Address),
                description: "Address of avatar.",
                resolve: context =>
                {
                    var address = context.Source.Address!;

                    // Return Address.ToHex for api backward compatibility
                    if (address.StartsWith("0x"))
                    {
                        return address.Replace("0x", string.Empty);
                    }

                    return address;
                });
            Field<NonNullGraphType<StringGraphType>>(
                nameof(WorldBossRankingModel.AvatarName),
                description: "Name of avatar.",
                resolve: context => context.Source.AvatarName
            );
            Field<NonNullGraphType<LongGraphType>>(
                nameof(WorldBossRankingModel.HighScore),
                description: "Season high score.",
                resolve: context => context.Source.HighScore
            );
            Field<NonNullGraphType<LongGraphType>>(
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
