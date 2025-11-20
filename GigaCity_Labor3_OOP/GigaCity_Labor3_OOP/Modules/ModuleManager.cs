using System.Collections.Generic;
using System.Collections.ObjectModel;
using GigacityContracts;

namespace GigaCity_Labor3_OOP.Modules
{
    public static class ModuleManager
    {
        public static List<ICityModule> Modules { get; } = new();

        public static void RegisterModule(ICityModule module, ObservableCollection<ICell> cells)
        {
            module.Initialize(cells);
            Modules.Add(module);
        }
    }
}
