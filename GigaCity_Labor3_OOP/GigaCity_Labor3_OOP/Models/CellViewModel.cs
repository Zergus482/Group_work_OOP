using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GigaCity_Labor3_OOP.Models
{
    public class CellViewModel : INotifyPropertyChanged
    {
        public int X { get; set; }
        public int Y { get; set; }
        public byte TerrainType { get; set; }
        public byte ResourceType { get; set; }
        public string ToolTip { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
