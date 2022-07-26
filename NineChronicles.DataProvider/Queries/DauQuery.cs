namespace NineChronicles.DataProvider.Queries
{
    using GraphQL;
    using GraphQL.Types;
    using NineChronicles.DataProvider.GraphTypes;
    using NineChronicles.DataProvider.Store;

    internal class DauQuery : ObjectGraphType
    {
        public DauQuery(MySqlStore store)
        {
            Store = store;
            Field<IntGraphType>(
                name: "DauCount",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "date",
                        Description = "Date in YYYY-MM-DD format",
                    }
                ),
                resolve: context =>
                {
                    string date = context.GetArgument<string>("date");
                    var dauCount = Store.GetDauCount(date);
                    return dauCount;
                });
            Field<ListGraphType<AgentType>>(
                name: "DauAgents",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType>
                    {
                        Name = "date",
                        Description = "Date in YYYY-MM-DD format",
                    }
                ),
                resolve: context =>
                {
                    string date = context.GetArgument<string>("date");
                    var agents = Store.GetDauAgents(date);
                    return agents;
                });
        }

        private MySqlStore Store { get; }
    }
}
