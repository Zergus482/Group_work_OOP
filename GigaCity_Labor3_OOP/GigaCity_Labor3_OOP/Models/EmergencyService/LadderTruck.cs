namespace GigaCity_Labor3_OOP.Models.EmergencyService
{
    public class LadderTruck : FireFighterVehicle
    {
        public int LadderHeight { get; set; } // Высота лестницы в метрах

        public LadderTruck()
        {
            Type = "Автолестница";
            Capacity = 3;
            Speed = 70;
            LadderHeight = 30;
            IsSuitableForFire = true;
            IsSuitableForRescue = true;
            IsSuitableForMedical = false;
        }
    }
}

