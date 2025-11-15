namespace CitySimulation.Models.Utilities
{
    public class ResidentialBuilding
    {
        public string Address { get; set; }
        public int Residents { get; set; }
        public double ElectricityConsumption { get; set; }
        public double WaterConsumption { get; set; }
        public double GasConsumption { get; set; }
        public double SewageProduction { get; set; }
        public bool HasUtilities { get; set; }

        // Добавляем свойство для русского отображения
        public string UtilitiesStatus => HasUtilities ? "Оказана" : "Не оказана";

        public ResidentialBuilding(string address, int residents)
        {
            Address = address;
            Residents = residents;
            CalculateConsumption();
            HasUtilities = false;
        }

        private void CalculateConsumption()
        {
            ElectricityConsumption = Residents * 5.2;
            WaterConsumption = Residents * 0.15;
            GasConsumption = Residents * 2.1;
            SewageProduction = Residents * 0.12;
        }

        public void UpdateResidents(int newResidents)
        {
            Residents = newResidents;
            CalculateConsumption();
        }
    }
}