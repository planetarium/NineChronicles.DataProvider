namespace NineChronicles.DataProvider.Queries
{
    using System;
    using System.Linq;
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
                    DateTimeOffset offsetDate = DateTimeOffset.Parse(date);
                    var dau = Store.GetDau(
                        offsetDate.ToString("yyyy-MM-dd"));
                    return dau.Count();
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
                    DateTimeOffset offsetDate = DateTimeOffset.Parse(date);
                    var agents = Store.GetDau(
                        offsetDate.ToString("yyyy-MM-dd"));
                    return agents;
                });
        }

        private MySqlStore Store { get; }
    }
}
