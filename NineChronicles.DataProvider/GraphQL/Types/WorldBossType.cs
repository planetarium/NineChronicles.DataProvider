namespace NineChronicles.DataProvider.GraphQL.Types
{
    using global::GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class WorldBossType : ObjectGraphType<WorldBossModel>
    {
        public WorldBossType()
        {
            Field(x => x.Id);
            Field(x => x.BlockIndex);
            Field(x => x.AgentAddress);
            Field(x => x.AvatarAddress);
            Field(x => x.AvatarLevel, nullable: true);

            /*
             * 추가할 GQL 필드
             */

            Field(x => x.TimeStamp);

            Name = "WorldBoss";
        }
    }
}
