using System.Collections.ObjectModel;

namespace GigacityContracts
{
    // Минимальный контракт для ячейки
    public interface ICell
    {
        int X { get; }
        int Y { get; }
        byte TerrainType { get; }
        byte ResourceType { get; }
        string? ToolTip { get; }
    }

    // Контракт для модуля (Energy/Comms)
    public interface ICityModule
    {
        string Name { get; }
        void Initialize(ObservableCollection<ICell> cityCells);
        void Show();
    }
}
