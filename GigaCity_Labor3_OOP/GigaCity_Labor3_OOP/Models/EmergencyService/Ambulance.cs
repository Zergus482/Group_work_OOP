namespace GigaCity_Labor3_OOP.Models.EmergencyService
{
    public class Ambulance : FireFighterVehicle
    {
        public bool HasMedicalEquipment { get; set; }

        public Ambulance()
        {
            Type = "Скорая помощь";
            Capacity = 2;
            Speed = 100;
            HasMedicalEquipment = true;
            IsSuitableForFire = false;
            IsSuitableForRescue = false;
            IsSuitableForMedical = true;
        }
    }
}

