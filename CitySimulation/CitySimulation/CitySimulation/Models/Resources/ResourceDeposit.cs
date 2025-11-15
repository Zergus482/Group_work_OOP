namespace CitySimulation.Models.Resources
{
    public class ResourceDeposit
    {
        public ResourceType Type { get; set; }
        public string Name { get; set; }
        public double Capacity { get; set; }
        public double CurrentAmount { get; set; }
        public double ExtractionRate { get; set; }
        public bool IsActive { get; set; }
        public int Workers { get; set; }

        // Добавляем свойство для русского названия
        public string TypeRussian => Type.ToRussianString();

        public ResourceDeposit(ResourceType type, string name, double capacity, double extractionRate)
        {
            Type = type;
            Name = name;
            Capacity = capacity;
            CurrentAmount = capacity;
            ExtractionRate = extractionRate;
            IsActive = false;
            Workers = 0;
        }

        public double ExtractResource()
        {
            if (!IsActive || CurrentAmount <= 0) return 0;

            var extracted = ExtractionRate * (1 + Workers * 0.1);
            if (extracted > CurrentAmount)
                extracted = CurrentAmount;

            CurrentAmount -= extracted;
            return extracted;
        }
    }
}