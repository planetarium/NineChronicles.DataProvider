namespace NineChronicles.DataProvider.GraphTypes
{
    using System;
    using GraphQL.Types;
    using Microsoft.Extensions.DependencyInjection;
    using NineChronicles.DataProvider.Queries;

    public class NineChroniclesSummarySchema : Schema
    {
        public NineChroniclesSummarySchema(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            Query = serviceProvider.GetRequiredService<NineChroniclesSummaryQuery>();
        }
    }
}
