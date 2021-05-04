namespace NineChronicles.DataProvider.GraphQL.Types
{
    using global::GraphQL.Types;

    internal class NineChroniclesSummaryQuery : ObjectGraphType
    {
        public NineChroniclesSummaryQuery()
        {
            this.Field<StringGraphType>(
                name: "test",
                resolve: context => "Should be done.");
        }
    }
}
