using System;

namespace GigaCity_Labor3_OOP.Models
{
    public class RoadModel
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int SpeedLimit { get; set; } // км/ч
        public bool IsOneWay { get; set; }
        public DateTime ConstructionDate { get; set; }
        public decimal MaintenanceCost { get; set; } // стоимость обслуживания в год
        public int TrafficLevel { get; set; } // уровень загруженности (0-100)

        public RoadModel()
        {
            ConstructionDate = DateTime.Now;
            IsOneWay = false;
            TrafficLevel = 0;
            SpeedLimit = 60; // Стандартное ограничение скорости
            MaintenanceCost = 5000; // Стандартная стоимость
        }

        public void UpdateTrafficLevel(int vehicleCount)
        {
            // Обновляем уровень загруженности на основе количества машин
            TrafficLevel = Math.Min(100, vehicleCount * 10);
        }
    }
}