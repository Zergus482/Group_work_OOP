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
        public string? ToolTip { get; set; }
        

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        private readonly CellData _model;

       


       
    }
    public class CellAdapter : GigacityContracts.ICell
    {
        private readonly GigaCity_Labor3_OOP.Models.CellViewModel _c;
        public CellAdapter(GigaCity_Labor3_OOP.Models.CellViewModel c) { _c = c; }
        public int X => _c.X;
        public int Y => _c.Y;
        public byte TerrainType => _c.TerrainType;
        public byte ResourceType => _c.ResourceType;
        public string? ToolTip => _c.ToolTip;
    }

}
