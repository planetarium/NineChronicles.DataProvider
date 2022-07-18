namespace NineChronicles.DataProvider.GraphQL.Types
{
    using System;
    using global::GraphQL.Types;
    using global::GraphQL.Utilities;
    using Microsoft.Extensions.DependencyInjection;

    public class NineChroniclesSummarySchema : Schema
    {
        public NineChroniclesSummarySchema(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            Query = serviceProvider.GetRequiredService<NineChroniclesSummaryQuery>();
        }
    }
}
