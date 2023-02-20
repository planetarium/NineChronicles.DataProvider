namespace NineChronicles.DataProvider.DataRendering
{
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class AgentData
    {
        public static AgentModel GetAgentInfo(ActionBase.ActionEvaluation<ActionBase> ev)
        {
            var agentModel = new AgentModel
            {
                Address = ev.Signer.ToString(),
            };

            return agentModel;
        }
    }
}
