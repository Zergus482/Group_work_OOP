using System;
using System.Collections.Generic;
using System.Linq;
using GigacityContracts;

namespace CommsModel
{
    public class CommunicationsSimulationService
    {
        private readonly Random _random = new Random();
        private readonly Dictionary<(int x, int y), CommCellData> _commData = new();
        private readonly List<CommTower> _towers = new();

        public void Initialize(IEnumerable<ICell> cells)
        {
            _commData.Clear();
            _towers.Clear();
            
            var cellsList = cells.ToList();
            
            // Создаем вышки сотовой связи
            InitializeTowers(cellsList);
            
            // Инициализируем данные для каждой ячейки
            foreach (var cell in cellsList)
            {
                _commData[(cell.X, cell.Y)] = new CommCellData
                {
                    X = cell.X,
                    Y = cell.Y,
                    TerrainType = cell.TerrainType,
                    ResourceType = cell.ResourceType
                };
            }
        }

        private void InitializeTowers(List<ICell> cells)
        {
            // Вышки размещаем в городах и на возвышенностях
            var cityCells = cells.Where(c => c.TerrainType == 5).ToList();
            var mountainCells = cells.Where(c => c.TerrainType == 3).ToList();
            
            // Вышки в городе (каждые 10-15 ячеек)
            int step = 12;
            for (int i = 0; i < cityCells.Count; i += step)
            {
                var cell = cityCells[i];
                _towers.Add(new CommTower
                {
                    X = cell.X,
                    Y = cell.Y,
                    Type = TowerType.Cellular,
                    Range = 8 + _random.NextDouble() * 4,
                    Capacity = 1000 + _random.Next(0, 500)
                });
            }
            
            // Вышки на горах (для дальнего покрытия)
            for (int i = 0; i < Math.Min(5, mountainCells.Count); i++)
            {
                var cell = mountainCells[_random.Next(mountainCells.Count)];
                _towers.Add(new CommTower
                {
                    X = cell.X,
                    Y = cell.Y,
                    Type = TowerType.Cellular,
                    Range = 15 + _random.NextDouble() * 5,
                    Capacity = 2000 + _random.Next(0, 1000)
                });
            }
            
            // Интернет-провайдеры (в крупных городах)
            var cityCenters = cityCells.Where((c, idx) => idx % 20 == 0).Take(3);
            foreach (var cell in cityCenters)
            {
                _towers.Add(new CommTower
                {
                    X = cell.X,
                    Y = cell.Y,
                    Type = TowerType.Internet,
                    Range = 12 + _random.NextDouble() * 3,
                    Capacity = 5000 + _random.Next(0, 2000)
                });
            }
        }

        public void SimulateStep()
        {
            // Обновляем состояние вышек
            foreach (var tower in _towers)
            {
                tower.CurrentLoad = _random.Next(0, (int)(tower.Capacity * 0.9));
                tower.Efficiency = tower.CurrentLoad < tower.Capacity * 0.8 ? 1.0 : 0.7;
            }
            
            // Обновляем покрытие для каждой ячейки
            foreach (var kvp in _commData)
            {
                var data = kvp.Value;
                var cell = kvp.Key;
                
                // Проверяем покрытие от всех вышек
                var coverage = CalculateCoverage(cell.x, cell.y);
                data.CellularCoverage = coverage.Cellular;
                data.InternetCoverage = coverage.Internet;
                data.SignalStrength = coverage.SignalStrength;
                data.Latency = CalculateLatency(cell.x, cell.y, coverage);
                data.Bandwidth = CalculateBandwidth(cell.x, cell.y, coverage);
                
                // Определяем статус
                if (data.CellularCoverage < 30 || data.InternetCoverage < 30)
                {
                    data.Status = CommStatus.NoCoverage;
                }
                else if (data.CellularCoverage < 60 || data.InternetCoverage < 60)
                {
                    data.Status = CommStatus.Poor;
                }
                else if (data.Latency > 200 || data.Bandwidth < 10)
                {
                    data.Status = CommStatus.Slow;
                }
                else
                {
                    data.Status = CommStatus.Good;
                }
            }
        }

        private (double Cellular, double Internet, double SignalStrength) CalculateCoverage(int x, int y)
        {
            double cellularCoverage = 0;
            double internetCoverage = 0;
            double maxSignal = 0;
            
            foreach (var tower in _towers)
            {
                double distance = Math.Sqrt(Math.Pow(x - tower.X, 2) + Math.Pow(y - tower.Y, 2));
                
                if (distance <= tower.Range)
                {
                    double signalStrength = (1.0 - distance / tower.Range) * tower.Efficiency;
                    maxSignal = Math.Max(maxSignal, signalStrength);
                    
                    if (tower.Type == TowerType.Cellular)
                    {
                        cellularCoverage = Math.Max(cellularCoverage, signalStrength * 100);
                    }
                    else if (tower.Type == TowerType.Internet)
                    {
                        internetCoverage = Math.Max(internetCoverage, signalStrength * 100);
                    }
                }
            }
            
            return (cellularCoverage, internetCoverage, maxSignal);
        }

        private int CalculateLatency(int x, int y, (double Cellular, double Internet, double SignalStrength) coverage)
        {
            int baseLatency = 20;
            
            // Базовая задержка зависит от силы сигнала
            int signalLatency = (int)((1.0 - coverage.SignalStrength) * 150);
            
            // Задержка увеличивается при перегрузке вышек
            int loadLatency = 0;
            foreach (var tower in _towers)
            {
                double distance = Math.Sqrt(Math.Pow(x - tower.X, 2) + Math.Pow(y - tower.Y, 2));
                if (distance <= tower.Range && tower.CurrentLoad > tower.Capacity * 0.8)
                {
                    loadLatency += (int)((tower.CurrentLoad / tower.Capacity) * 50);
                }
            }
            
            return baseLatency + signalLatency + loadLatency;
        }

        private double CalculateBandwidth(int x, int y, (double Cellular, double Internet, double SignalStrength) coverage)
        {
            double baseBandwidth = 0;
            
            foreach (var tower in _towers)
            {
                double distance = Math.Sqrt(Math.Pow(x - tower.X, 2) + Math.Pow(y - tower.Y, 2));
                
                if (distance <= tower.Range)
                {
                    double signalQuality = (1.0 - distance / tower.Range) * tower.Efficiency;
                    double availableCapacity = tower.Capacity - tower.CurrentLoad;
                    double bandwidth = (availableCapacity / tower.Capacity) * signalQuality * 100;
                    
                    if (tower.Type == TowerType.Internet)
                    {
                        baseBandwidth += bandwidth;
                    }
                    else
                    {
                        baseBandwidth += bandwidth * 0.3; // Сотовая связь дает меньше пропускной способности
                    }
                }
            }
            
            return Math.Round(baseBandwidth, 2);
        }

        public CommCellData? GetCellData(int x, int y)
        {
            return _commData.TryGetValue((x, y), out var data) ? data : null;
        }

        public IEnumerable<CommCellData> GetAllCells()
        {
            return _commData.Values;
        }

        public IEnumerable<CommTower> GetTowers()
        {
            return _towers;
        }

        public double GetAverageCellularCoverage() => 
            _commData.Values.Average(c => c.CellularCoverage);
        
        public double GetAverageInternetCoverage() => 
            _commData.Values.Average(c => c.InternetCoverage);
        
        public int GetCellsWithoutCoverage() => 
            _commData.Values.Count(c => c.Status == CommStatus.NoCoverage);
        
        public int GetOverloadedTowers() => 
            _towers.Count(t => t.CurrentLoad > t.Capacity * 0.85);
    }

    public class CommCellData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public byte TerrainType { get; set; }
        public byte ResourceType { get; set; }
        public double CellularCoverage { get; set; }
        public double InternetCoverage { get; set; }
        public double SignalStrength { get; set; }
        public int Latency { get; set; }
        public double Bandwidth { get; set; }
        public CommStatus Status { get; set; } = CommStatus.NoCoverage;
    }

    public class CommTower
    {
        public int X { get; set; }
        public int Y { get; set; }
        public TowerType Type { get; set; }
        public double Range { get; set; }
        public int Capacity { get; set; }
        public int CurrentLoad { get; set; }
        public double Efficiency { get; set; } = 1.0;
    }

    public enum TowerType
    {
        Cellular,
        Internet
    }

    public enum CommStatus
    {
        NoCoverage,
        Poor,
        Slow,
        Good
    }
}

