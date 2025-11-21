namespace GigaCity_Labor3_OOP.Models.EmergencyService
{
    public class FireEngine : FireFighterVehicle
    {
        public FireEngine()
        {
            Type = "Пожарная машина";
            Capacity = 4; // Экипаж
            Speed = 80;
            IsSuitableForFire = true;
            IsSuitableForRescue = true;
            IsSuitableForMedical = false;
        }
    }
}

