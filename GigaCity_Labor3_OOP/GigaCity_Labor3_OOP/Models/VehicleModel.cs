using System;

namespace GigaCity_Labor3_OOP.Models
{
    public class VehicleModel
    {
        public int Id { get; set; }
        public VehicleType Type { get; set; }
        public int MaxSpeed { get; set; } // км/ч
        public int CurrentSpeed { get; set; } // км/ч
        public int Capacity { get; set; } // вместительность (для автобусов)
        public bool IsMoving { get; set; }
        public int CurrentX { get; set; } // текущая клетка X
        public int CurrentY { get; set; } // текущая клетка Y
        public int DestinationX { get; set; } // клетка назначения X
        public int DestinationY { get; set; } // клетка назначения Y
        public DateTime LastMaintenance { get; set; }
        public OwnerType OwnerType { get; set; }
        public int OwnerId { get; set; } // ID владельца (человека или компании)
        public int MoveProgress { get; set; } // прогресс перемещения между клетками (0-100)

        public VehicleModel()
        {
            IsMoving = false;
            LastMaintenance = DateTime.Now;
            MoveProgress = 0;
        }

        public bool NeedsMaintenance()
        {
            return (DateTime.Now - LastMaintenance).TotalDays > 365;
        }
    }

    public enum VehicleType
    {
        Car,          // Легковой автомобиль
        Bus,          // Автобус
        Truck,        // Грузовик
        Taxi,         // Такси
        Emergency,    // Скорая помощь/полиция/пожарная
        Delivery      // Доставка
    }

    public enum OwnerType
    {
        Private,      // Частный владелец
        Corporate,    // Компания
        Government,   // Государство
        TaxiCompany   // Такси
    }
}