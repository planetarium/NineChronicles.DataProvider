namespace NineChronicles.DataProvider.GraphQL.Types
{
    using global::GraphQL;
    using global::GraphQL.Types;
    using NineChronicles.DataProvider.Store;

    internal class NineChroniclesSummaryQuery : ObjectGraphType
    {
        public NineChroniclesSummaryQuery(MySqlStore store)
        {
            Store = store;
            Field<StringGraphType>(
                name: "test",
                resolve: context => "Should be done.");
            Field<ListGraphType<HackAndSlashType>>(
                name: "HackAndSlashQuery",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "agent_address" },
                    new QueryArgument<IntGraphType> { Name = "limit" }
                ),
                resolve: context =>
                {
                    string? agentAddress = context.GetArgument<string?>("agent_address", null);
                    int? limit = context.GetArgument<int?>("limit", null);
                    return Store.GetHackAndSlash(agentAddress, limit);
                });
        }

        private MySqlStore Store { get; }
    }
}
