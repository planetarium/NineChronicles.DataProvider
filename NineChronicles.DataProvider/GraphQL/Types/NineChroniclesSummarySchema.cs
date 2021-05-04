using System;
using GraphQL.Types;
using GraphQL.Utilities;

namespace NineChronicles.DataProvider.GraphQL.Types
{
    public class NineChroniclesSummarySchema : Schema
    {
        
        public NineChroniclesSummarySchema(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            Query = serviceProvider.GetRequiredService<NineChroniclesSummaryQuery>();
        }
    }
}
