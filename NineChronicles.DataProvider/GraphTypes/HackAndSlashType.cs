namespace NineChronicles.DataProvider.GraphTypes
{
    using GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class HackAndSlashType : ObjectGraphType<HackAndSlashModel>
    {
        public HackAndSlashType()
        {
            Field(x => x.AgentAddress);
            Field(x => x.AvatarAddress);
            Field(x => x.StageId);
            Field(x => x.Cleared);
            Field(x => x.BlockIndex);

            Name = "HackAndSlash";
        }
    }
}
