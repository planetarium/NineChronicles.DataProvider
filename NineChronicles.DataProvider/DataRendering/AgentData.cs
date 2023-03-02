namespace NineChronicles.DataProvider.DataRendering
{
    using Libplanet;
    using NineChronicles.DataProvider.Store.Models;

    public static class AgentData
    {
        public static AgentModel GetAgentInfo(Address address)
        {
            var agentModel = new AgentModel
            {
                Address = address.ToString(),
            };

            return agentModel;
        }
    }
}
