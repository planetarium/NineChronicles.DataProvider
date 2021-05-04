namespace NineChronicles.DataProvider.GraphQL.Types
{
    using System;
    using global::GraphQL.Types;
    using global::GraphQL.Utilities;

    public class NineChroniclesSummarySchema : Schema
    {
        public NineChroniclesSummarySchema(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            this.Query = serviceProvider.GetRequiredService<NineChroniclesSummaryQuery>();
        }
    }
}
