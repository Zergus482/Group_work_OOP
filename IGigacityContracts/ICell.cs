using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigacityContracts
{
    public interface ICell
    {
        int X { get; }
        int Y { get; }
        byte TerrainType { get; }
        byte ResourceType { get; }
        string? ToolTip { get; }
    }
}
