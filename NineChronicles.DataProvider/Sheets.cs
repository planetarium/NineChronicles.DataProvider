namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using Nekoyume.TableData;

    public class Sheets
    {
        public IReadOnlyDictionary<string, string> Map { get; set; } = null!;

        public T GetSheet<T>()
            where T : ISheet
        {
            var sheet = Activator.CreateInstance<T>();
            sheet.Set(Map[typeof(T).Name]);
            return sheet;
        }
    }
}
