using System.Collections.ObjectModel;

namespace GigacityContracts
{
    public interface ICityModule
    {
        string Name { get; }

        // Инициализация модуля с коллекцией ячеек
        void Initialize(ObservableCollection<ICell> cityCells);

        // Показ UI
        void Show();
    }
}
