namespace NineChronicles.DataProvider.GraphQL.Types
{
    using global::GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class HackAndSlashType : ObjectGraphType<HackAndSlashModel>
    {
        public HackAndSlashType()
        {
            Field(x => x.Agent_Address);
            Field(x => x.Avatar_Address);
            Field(x => x.Stage_Id);
            Field(x => x.Cleared);
            Field(x => x.Block_Hash);

            Name = "HackAndSlash";
        }
    }
}
