namespace NineChronicles.DataProvider.GraphTypes
{
    using GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class AvatarType : ObjectGraphType<AvatarModel>
    {
        public AvatarType()
        {
            Field(x => x.Address);
            Field(x => x.AgentAddress);
            Field(x => x.Name);
            Field(x => x.AvatarLevel, true);
            Field(x => x.TitleId, true);
            Field(x => x.ArmorId, true);
            Field(x => x.Cp, true);

            Name = "Avatar";
        }
    }
}
