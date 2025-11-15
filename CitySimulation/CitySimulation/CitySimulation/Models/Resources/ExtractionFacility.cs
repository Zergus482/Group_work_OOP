namespace CitySimulation.Models.Resources
{
    public class ExtractionFacility
    {
        public string Name { get; set; }
        public ResourceDeposit Deposit { get; set; }
        public double Efficiency { get; set; }
        public int MaxWorkers { get; set; }
        public double MaintenanceCost { get; set; }
        public double DailyProduction { get; set; }

        // Добавляем свойство для отображения типа на русском
        public string TypeRussian => Deposit.TypeRussian;

        // Добавляем свойство для отображения работников
        public int Workers => Deposit.Workers;

        public ExtractionFacility(string name, ResourceDeposit deposit, int maxWorkers = 10)
        {
            Name = name;
            Deposit = deposit;
            Efficiency = 0.8;
            MaxWorkers = maxWorkers;
            MaintenanceCost = 1000;
            DailyProduction = 0;
        }

        public void StartExtraction()
        {
            Deposit.IsActive = true;
            UpdateProduction();
        }

        public void StopExtraction()
        {
            Deposit.IsActive = false;
            DailyProduction = 0;
        }

        public void HireWorker()
        {
            if (Deposit.Workers < MaxWorkers)
            {
                Deposit.Workers++;
                UpdateProduction();
            }
        }

        public void FireWorker()
        {
            if (Deposit.Workers > 0)
            {
                Deposit.Workers--;
                UpdateProduction();
            }
        }

        private void UpdateProduction()
        {
            if (Deposit.IsActive)
            {
                DailyProduction = Deposit.ExtractResource() * Efficiency;
            }
        }
    }
}