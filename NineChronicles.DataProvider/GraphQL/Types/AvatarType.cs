namespace NineChronicles.DataProvider.GraphQL.Types
{
    using global::GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class AvatarType : ObjectGraphType<AvatarModel>
    {
        public AvatarType()
        {
            Field(x => x.Address);
            Field(x => x.AgentAddress);
            Field(x => x.Name);
            Field(x => x.AvatarLevel, nullable: true);
            Field(x => x.TitleId, nullable: true);
            Field(x => x.ArmorId, nullable: true);
            Field(x => x.Cp, nullable: true);

            Name = "Avatar";
        }
    }
}
