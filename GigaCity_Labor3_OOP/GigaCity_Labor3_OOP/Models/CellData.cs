using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaCity_Labor3_OOP.Models
{
    public struct CellData
    {
        public byte TerrainType { get; set; }    // 1-5: Meadows, Forest, Mountains, Water, City
        public byte ResourceType { get; set; }   // 0-5: None, Metals, Oil, Gas, Trees, Plants

        public string GetToolTip(int x, int y)
        {
            string terrain = TerrainType switch
            {
                1 => "Поляна",
                2 => "Лес",
                3 => "Горы",
                4 => "Водоем",
                5 => "Город",
                _ => "Неизвестно"
            };

            string resource = ResourceType switch
            {
                0 => "Нет ресурсов",
                1 => "Металлы",
                2 => "Нефть",
                3 => "Газ",
                4 => "Деревья",
                5 => "Растения",
                _ => "Неизвестно"
            };

            return $"[{x},{y}] {terrain}\nРесурс: {resource}";
        }
    }
}
