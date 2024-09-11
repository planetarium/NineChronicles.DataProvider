namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NineChronicles.DataProvider.Store.Models.Crafting;
    using Serilog;

    public partial class RenderSubscriber
    {
        private List<RapidCombinationModel> _rapidCombinationList = new ();

        // RapidCombination
        private void StoreRapidCombinationList()
        {
            try
            {
                var tasks = new List<Task>();
                Log.Debug("[Crafting] Store RapidCombination list");

                tasks.Add(Task.Run(async () =>
                {
                    Log.Debug($"[RapidCombination] {_rapidCombinationList.Count}");
                    await MySqlStore.StoreRapidCombinationList(_rapidCombinationList);
                }));
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }

        private void ClearRapidCombinationList()
        {
            Log.Debug("[Crafting] Clear crafting related action data");
            _rapidCombinationList.Clear();
        }
    }
}
