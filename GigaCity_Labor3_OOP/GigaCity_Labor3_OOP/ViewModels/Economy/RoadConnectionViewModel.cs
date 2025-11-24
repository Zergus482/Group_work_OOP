using System.Windows;
using System.Windows.Media;

namespace GigaCity_Labor3_OOP.ViewModels.Economy
{
    public class RoadConnectionViewModel
    {
        public Point Start { get; init; }
        public Point End { get; init; }
        public Brush Stroke { get; init; } = Brushes.DimGray;
        public double Thickness { get; init; } = 2;
    }

    public class BuildMenuEntry
    {
        public string Header { get; init; } = string.Empty;
        public string BlueprintId { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public System.Windows.Input.ICommand? Command { get; init; }
        public object? CommandParameter { get; init; }
        public string? ClickHandler { get; init; } // Имя обработчика события Click
    }
}

