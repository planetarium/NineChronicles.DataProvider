namespace NineChronicles.DataProvider.GraphQL.Types
{
    using global::GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class AgentType : ObjectGraphType<AgentModel>
    {
        public AgentType()
        {
            Field(x => x.Address);

            Name = "Agent";
        }
    }
}
