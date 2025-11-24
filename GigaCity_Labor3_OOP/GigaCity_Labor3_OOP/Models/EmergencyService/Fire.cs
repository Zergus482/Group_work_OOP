namespace GigaCity_Labor3_OOP.Models.EmergencyService
{
    public class Fire : Disaster
    {
        public double Intensity { get; set; } // Интенсивность пожара (0-100)
        public string BuildingType { get; set; }

        public Fire()
        {
            Type = EmergencyType.Fire;
            Intensity = 50;
            Severity = 50;
        }
    }
}

