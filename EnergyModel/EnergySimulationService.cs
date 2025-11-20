using System;
using System.Collections.Generic;
using System.Linq;
using GigacityContracts;

namespace EnergyModel
{
    public class EnergySimulationService
    {
        private readonly Random _random = new Random();
        private readonly Dictionary<(int x, int y), EnergyCellData> _energyData = new();

        public void Initialize(IEnumerable<ICell> cells)
        {
            _energyData.Clear();
            foreach (var cell in cells)
            {
                _energyData[(cell.X, cell.Y)] = new EnergyCellData
                {
                    X = cell.X,
                    Y = cell.Y,
                    TerrainType = cell.TerrainType,
                    ResourceType = cell.ResourceType
                };
            }
        }

        public void SimulateStep()
        {
            foreach (var kvp in _energyData)
            {
                var data = kvp.Value;
                var cell = kvp.Key;

                // Генерация энергии зависит от типа местности и ресурсов
                double generation = CalculateGeneration(data);
                
                // Потребление зависит от типа местности (город потребляет больше)
                double consumption = CalculateConsumption(data);
                
                // Влияние соседей (распределение энергии)
                double neighborInfluence = CalculateNeighborInfluence(cell);
                
                data.Generation = Math.Round(generation, 2);
                data.Consumption = Math.Round(consumption, 2);
                data.NetEnergy = Math.Round(generation + neighborInfluence - consumption, 2);
                data.Status = data.NetEnergy < -0.5 ? EnergyStatus.Critical : 
                             data.NetEnergy < 0 ? EnergyStatus.Deficient : 
                             data.NetEnergy > 2 ? EnergyStatus.Surplus : EnergyStatus.Normal;
            }
        }

        private double CalculateGeneration(EnergyCellData data)
        {
            double baseGen = 0.5;
            
            // Электростанции на ресурсах
            if (data.ResourceType == 1) // Металлы - могут быть электростанции
            {
                baseGen += 15.0 + _random.NextDouble() * 5.0;
            }
            else if (data.ResourceType == 2) // Нефть/Газ
            {
                baseGen += 20.0 + _random.NextDouble() * 10.0;
            }
            
            // Солнечные панели на полях
            if (data.TerrainType == 1) // Meadows
            {
                baseGen += 2.0 + _random.NextDouble() * 3.0;
            }
            
            // Ветряные генераторы в горах
            if (data.TerrainType == 3) // Mountains
            {
                baseGen += 5.0 + _random.NextDouble() * 5.0;
            }
            
            // Гидроэлектростанции у воды
            if (data.TerrainType == 4) // Water
            {
                baseGen += 8.0 + _random.NextDouble() * 4.0;
            }
            
            return baseGen;
        }

        private double CalculateConsumption(EnergyCellData data)
        {
            double baseCons = 0.3;
            
            // Город потребляет много энергии
            if (data.TerrainType == 5) // City
            {
                baseCons += 8.0 + _random.NextDouble() * 4.0;
            }
            
            // Учебные заведения потребляют энергию
            if (data.TerrainType == 6) // Educational
            {
                baseCons += 5.0 + _random.NextDouble() * 3.0;
            }
            
            // Аэропорты и порты потребляют энергию
            if (data.TerrainType == 7 || data.TerrainType == 8) // Airport/Port
            {
                baseCons += 10.0 + _random.NextDouble() * 5.0;
            }
            
            return baseCons;
        }

        private double CalculateNeighborInfluence((int x, int y) cell)
        {
            double influence = 0;
            int count = 0;
            
            // Проверяем соседей (в радиусе 2)
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    var neighborKey = (cell.x + dx, cell.y + dy);
                    if (_energyData.TryGetValue(neighborKey, out var neighbor))
                    {
                        // Энергия распространяется от избыточных ячеек
                        if (neighbor.NetEnergy > 1.0)
                        {
                            influence += neighbor.NetEnergy * 0.1; // 10% передается
                            count++;
                        }
                    }
                }
            }
            
            return count > 0 ? influence / count : 0;
        }

        public EnergyCellData? GetCellData(int x, int y)
        {
            return _energyData.TryGetValue((x, y), out var data) ? data : null;
        }

        public IEnumerable<EnergyCellData> GetAllCells()
        {
            return _energyData.Values;
        }

        public double GetTotalGeneration() => _energyData.Values.Sum(c => c.Generation);
        public double GetTotalConsumption() => _energyData.Values.Sum(c => c.Consumption);
        public int GetDeficitCells() => _energyData.Values.Count(c => c.Status == EnergyStatus.Deficient || c.Status == EnergyStatus.Critical);
        public int GetSurplusCells() => _energyData.Values.Count(c => c.Status == EnergyStatus.Surplus);
    }

    public class EnergyCellData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public byte TerrainType { get; set; }
        public byte ResourceType { get; set; }
        public double Generation { get; set; }
        public double Consumption { get; set; }
        public double NetEnergy { get; set; }
        public EnergyStatus Status { get; set; } = EnergyStatus.Normal;
    }

    public enum EnergyStatus
    {
        Critical,   // Критический дефицит
        Deficient, // Дефицит
        Normal,     // Норма
        Surplus     // Избыток
    }
}

