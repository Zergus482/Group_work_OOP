// MapModel.cs

using System;
using System.Collections.Generic;
using System.Linq;
using GigaCity_Labor3_OOP.Services;

namespace GigaCity_Labor3_OOP.Models
{
    public class MapModel
    {
        public int Width { get; } = 100;
        public int Height { get; } = 100;
        public List<CellViewModel> Cells { get; private set; }
        public HashSet<(int x, int y)> RoadCoordinates { get; private set; }

        // Позиции аэропортов
        public int Airport1X { get; private set; }
        public int Airport1Y { get; private set; }
        public int Airport2X { get; private set; }
        public int Airport2Y { get; private set; }
        
        // Позиции портов
        public int Port1X { get; private set; }
        public int Port1Y { get; private set; }
        public int Port2X { get; private set; }
        public int Port2Y { get; private set; }

        // Парки и велодорожки
        public HashSet<(int x, int y)> ParkCells { get; private set; }
        public HashSet<(int x, int y)> BikePathCells { get; private set; }

        public MapModel()
        {
            RoadCoordinates = new HashSet<(int, int)>();
            Cells = GenerateMap();
            GenerateRoads();
            GenerateParksAndBikePaths();
        }

        private void GenerateRoads()
        {
            foreach (var cell in Cells)
            {
                if (cell.TerrainType == (byte)TerrainType.City)
                {
                    if (cell.Y % 6 == 2)
                    {
                        RoadCoordinates.Add((cell.X, cell.Y));
                    }

                    if (cell.X % 6 == 3)
                    {
                        RoadCoordinates.Add((cell.X, cell.Y));
                    }
                }

                for (int x = 69; x < 70; x++)
                {
                    for (int y = 86; y < 100; y++)
                    {
                        RoadCoordinates.Add((x, y));
                    }
                }

                for (int x = 0; x < 26; x++)
                {
                    for (int y = 32; y < 33; y++)
                    {
                        RoadCoordinates.Add((x, y));
                    }
                }

                for (int x = 69; x < 94; x++)
                {
                    for (int y = 94; y < 95; y++)
                    {
                        RoadCoordinates.Add((x, y));
                    }
                }

                RoadCoordinates.Add((13, 63));
                RoadCoordinates.Add((14, 63));
            }
        }

        public bool IsRoad(int x, int y)
        {
            return RoadCoordinates.Contains((x, y));
        }

        private List<CellViewModel> GenerateMap()
        {
            var cells = new List<CellViewModel>();
            var random = new Random(12345); // Фиксированный seed для одинаковой карты

            byte[,] terrainMap = new byte[Width, Height];

            // 1. Заполняем все полянами
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    terrainMap[x, y] = (byte)TerrainType.Meadows;

            // 2. Рисуем город в центре
            DrawCity(terrainMap);

            // 3. Рисуем природные зоны
            var zones = new[]
            {
                new { CenterX = 20, CenterY = 20, Type = (byte)TerrainType.Forest, Radius = 22 },
                new { CenterX = 80, CenterY = 80, Type = (byte)TerrainType.Forest, Radius = 20 },
                new { CenterX = 80, CenterY = 20, Type = (byte)TerrainType.Forest, Radius = 18 },
                new { CenterX = 15, CenterY = 85, Type = (byte)TerrainType.Mountains, Radius = 16 },
                new { CenterX = 85, CenterY = 15, Type = (byte)TerrainType.Mountains, Radius = 14 },
                new { CenterX = 25, CenterY = 50, Type = (byte)TerrainType.Water, Radius = 12 },
                new { CenterX = 75, CenterY = 50, Type = (byte)TerrainType.Water, Radius = 11 }
            };
            foreach (var zone in zones)
            {
                DrawZone(terrainMap, zone.CenterX, zone.CenterY, zone.Radius, zone.Type, random);
            }

            // 4. НОВЫЙ ШАГ: Рисуем учебные заведения ПОСЛЕ города
            DrawEducationalInstitutions(terrainMap, random);

            // 5. Размещаем аэропорты на удаленном расстоянии
            PlaceAirports(terrainMap);

            // 6. Размещаем порты на противоположных берегах водоемов
            PlacePorts(terrainMap);

            // 7. Создаем ячейки с ресурсами
            // ИСПРАВЛЕНИЕ: Меняем местами X и Y при создании, чтобы соответствовало визуальному отображению
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = new CellViewModel
                    {
                        X = y,  // Меняем местами: X = y
                        Y = x,  // Меняем местами: Y = x
                        TerrainType = terrainMap[x, y],
                        ResourceType = GetResourceType(terrainMap[x, y], random)
                    };
                    cell.ToolTip = $"[{y},{x}] {GetTerrainName(cell.TerrainType)}\nРесурс: {GetResourceName(cell.ResourceType)}";
                    cells.Add(cell);
                }
            }

            return cells;
        }

        /// <summary>
        /// Размещает 4-6 учебных заведений случайным образом в пределах города.
        /// </summary>
        private void DrawEducationalInstitutions(byte[,] terrainMap, Random random)
        {
            int centerX = Width / 2;
            int centerY = Height / 2;
            int cityRadius = 35; // Радиус города, как в DrawCity

            // Определяем, сколько заведений будет создано (от 4 до 6)
            int institutionCount = random.Next(4, 7);

            for (int i = 0; i < institutionCount; i++)
            {
                bool placed = false;
                int attempts = 0;
                while (!placed && attempts < 100) // Делаем 100 попыток найти место
                {
                    // Генерируем случайную точку в квадрате, описывающем город
                    int x = random.Next(centerX - cityRadius, centerX + cityRadius);
                    int y = random.Next(centerY - cityRadius, centerY + cityRadius);

                    // Проверяем, что точка внутри круга города и это уже городская клетка
                    double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                    if (distance < cityRadius && terrainMap[x, y] == (byte)TerrainType.City)
                    {
                        // Размещаем учебное заведение
                        terrainMap[x, y] = (byte)TerrainType.Educational;
                        placed = true;
                    }
                    attempts++;
                }
            }
        }

        private void DrawCity(byte[,] terrainMap)
        {
            int centerX = Width / 2;
            int centerY = Height / 2;
            int cityRadius = 35;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                    double noise = Math.Sin(x * 0.1) * Math.Cos(y * 0.08) * 10;

                    if (distance < cityRadius + noise)
                    {
                        terrainMap[x, y] = (byte)TerrainType.City;
                    }
                }
            }
        }

        private void DrawZone(byte[,] terrainMap, int centerX, int centerY, int baseRadius, byte terrainType, Random random)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (terrainMap[x, y] == (byte)TerrainType.City || terrainMap[x, y] == (byte)TerrainType.Educational) continue;

                    double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                    double noise1 = Math.Sin(x * 0.15 + y * 0.1) * 4;
                    double noise2 = Math.Cos(x * 0.12 - y * 0.08) * 3;
                    double noise3 = Math.Sin(x * 0.08 + y * 0.12) * 5;
                    double effectiveRadius = baseRadius + noise1 + noise2 + noise3;

                    if (distance < effectiveRadius)
                    {
                        terrainMap[x, y] = terrainType;
                    }
                }
            }
        }

        private byte GetResourceType(byte terrainType, Random random)
        {
            // Учебные заведения, аэропорты и порты не имеют ресурсов
            if (terrainType == (byte)TerrainType.City || 
                terrainType == (byte)TerrainType.Educational || 
                terrainType == (byte)TerrainType.Airport ||
                terrainType == (byte)TerrainType.Port) return 0;

            double chance = random.NextDouble();
            return terrainType switch
            {
                // Поляны - газ и растения (не используются в упрощенной системе)
                (byte)TerrainType.Meadows => chance < 0.25 ? (byte)ResourceType.Gas : chance < 0.5 ? (byte)ResourceType.Plants : (byte)ResourceType.None,
                // Лес - деревья (основной ресурс для Wood) - увеличиваем вероятность
                (byte)TerrainType.Forest => chance < 0.9 ? (byte)ResourceType.Trees : chance < 0.98 ? (byte)ResourceType.Plants : (byte)ResourceType.None,
                // Горы - металлы (Iron, Copper) и уголь (Coal), иногда нефть (Oil) - увеличиваем вероятность металлов
                (byte)TerrainType.Mountains => chance < 0.6 ? (byte)ResourceType.Metals : chance < 0.85 ? (byte)ResourceType.Metals : chance < 0.95 ? (byte)ResourceType.Oil : (byte)ResourceType.None,
                // Водоемы - вода (всегда доступна по типу местности), иногда металлы и нефть
                (byte)TerrainType.Water => chance < 0.2 ? (byte)ResourceType.Metals : chance < 0.4 ? (byte)ResourceType.Oil : (byte)ResourceType.None, // Вода определяется по типу местности
                _ => 0
            };
        }

        private string GetTerrainName(byte terrainType)
        {
            return terrainType switch
            {
                (byte)TerrainType.Meadows => "Поляна",
                (byte)TerrainType.Forest => "Лес",
                (byte)TerrainType.Mountains => "Горы",
                (byte)TerrainType.Water => "Водоем",
                (byte)TerrainType.City => "Город",
                (byte)TerrainType.Educational => "Учебное заведение",
                (byte)TerrainType.Airport => "Аэропорт",
                (byte)TerrainType.Port => "Порт",
                _ => "Неизвестно"
            };
        }

        private string GetResourceName(byte resourceType)
        {
            return resourceType switch
            {
                (byte)ResourceType.None => "Нет ресурсов",
                (byte)ResourceType.Metals => "Металлы",
                (byte)ResourceType.Oil => "Нефть",
                (byte)ResourceType.Gas => "Газ",
                (byte)ResourceType.Trees => "Деревья",
                (byte)ResourceType.Plants => "Растения",
                _ => "Неизвестно"
            };
        }

        /// <summary>
        /// Размещает 2 аэропорта на удаленном расстоянии друг от друга
        /// </summary>
        private void PlaceAirports(byte[,] terrainMap)
        {
            // Первый аэропорт на координатах (17, 73)
            Airport1X = 17;
            Airport1Y = 73;
            
            // Второй аэропорт в правом нижнем углу (максимально удален от первого)
            Airport2X = 95;
            Airport2Y = 95;
            
            // Размещаем аэропорты (3x3 клетки для каждого)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int x1 = Airport1X + dx;
                    int y1 = Airport1Y + dy;
                    int x2 = Airport2X + dx;
                    int y2 = Airport2Y + dy;
                    
                    if (x1 >= 0 && x1 < Width && y1 >= 0 && y1 < Height)
                    {
                        // Принудительно размещаем аэропорт (перезаписываем все, кроме других аэропортов)
                        terrainMap[x1, y1] = (byte)TerrainType.Airport;
                    }
                    
                    if (x2 >= 0 && x2 < Width && y2 >= 0 && y2 < Height)
                    {
                        // Принудительно размещаем аэропорт (перезаписываем все, кроме других аэропортов)
                        terrainMap[x2, y2] = (byte)TerrainType.Airport;
                    }
                }
            }
        }

        /// <summary>
        /// Размещает 2 порта на противоположных берегах водоема
        /// </summary>
        private void PlacePorts(byte[,] terrainMap)
        {
            // Используем первый водоем (CenterX = 25, CenterY = 50, Radius = 12)
            int waterCenterX = 25;
            int waterCenterY = 50;
            int waterRadius = 12;
            
            // Первый порт на левом берегу (западный берег)
            Port1X = 22;
            Port1Y = 33;

            // Второй порт на правом берегу (восточный берег)
            Port2X = 11;
            Port2Y = 62;
            
            // Убеждаемся, что порты в пределах карты
            if (Port1X < 0) Port1X = 0;
            if (Port1X >= Width) Port1X = Width - 1;
            if (Port1Y < 0) Port1Y = 0;
            if (Port1Y >= Height) Port1Y = Height - 1;
            
            if (Port2X < 0) Port2X = 0;
            if (Port2X >= Width) Port2X = Width - 1;
            if (Port2Y < 0) Port2Y = 0;
            if (Port2Y >= Height) Port2Y = Height - 1;
            
            // Размещаем порты (2x2 клетки для каждого)
            for (int dx = 0; dx <= 1; dx++)
            {
                for (int dy = 0; dy <= 1; dy++)
                {
                    int x1 = Port1X + dx;
                    int y1 = Port1Y + dy;
                    int x2 = Port2X + dx;
                    int y2 = Port2Y + dy;
                    
                    if (x1 >= 0 && x1 < Width && y1 >= 0 && y1 < Height)
                    {
                        // Размещаем порт только на суше (не на воде и не в городе)
                        if (terrainMap[x1, y1] != (byte)TerrainType.Water && 
                            terrainMap[x1, y1] != (byte)TerrainType.City &&
                            terrainMap[x1, y1] != (byte)TerrainType.Educational &&
                            terrainMap[x1, y1] != (byte)TerrainType.Airport)
                        {
                            terrainMap[x1, y1] = (byte)TerrainType.Port;
                        }
                    }
                    
                    if (x2 >= 0 && x2 < Width && y2 >= 0 && y2 < Height)
                    {
                        // Размещаем порт только на суше (не на воде и не в городе)
                        if (terrainMap[x2, y2] != (byte)TerrainType.Water && 
                            terrainMap[x2, y2] != (byte)TerrainType.City &&
                            terrainMap[x2, y2] != (byte)TerrainType.Educational &&
                            terrainMap[x2, y2] != (byte)TerrainType.Airport)
                        {
                            terrainMap[x2, y2] = (byte)TerrainType.Port;
                        }
                    }
                }
            }
        }

        private void GenerateParksAndBikePaths()
        {
            var planner = new ParkAndBikePlanner(this);
            ParkCells = planner.GenerateParks();
            BikePathCells = planner.GenerateBikePaths(ParkCells);
        }

        public bool IsPark(int x, int y)
        {
            return ParkCells != null && ParkCells.Contains((x, y));
        }

        public bool IsBikePath(int x, int y)
        {
            return BikePathCells != null && BikePathCells.Contains((x, y));
        }

        public CellViewModel GetCell(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                System.Diagnostics.Debug.WriteLine($"GetCell: координаты вне границ [{x}, {y}], Width={Width}, Height={Height}");
                return null;
            }
            
            // Всегда ищем клетку по координатам, а не по индексу, чтобы избежать проблем с перепутанными координатами
            var cell = Cells.FirstOrDefault(c => c.X == x && c.Y == y);
            if (cell != null)
            {
                return cell;
            }
            
            // Если не нашли по координатам, пробуем по индексу (на случай если координаты в клетках перепутаны)
            int index = x * Height + y;
            if (index >= 0 && index < Cells.Count)
            {
                var cellByIndex = Cells[index];
                System.Diagnostics.Debug.WriteLine($"GetCell: клетка [{x}, {y}] не найдена по координатам, но найдена по индексу {index} с координатами [{cellByIndex.X}, {cellByIndex.Y}]");
                // Если координаты в найденной клетке перепутаны, пробуем поменять местами
                if (cellByIndex.X == y && cellByIndex.Y == x)
                {
                    System.Diagnostics.Debug.WriteLine($"GetCell: координаты перепутаны! Ищем клетку с координатами [{y}, {x}]");
                    var swapped = Cells.FirstOrDefault(c => c.X == y && c.Y == x);
                    if (swapped != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetCell: найдена клетка с перепутанными координатами [{swapped.X}, {swapped.Y}]");
                        return swapped;
                    }
                }
                return cellByIndex;
            }
            
            System.Diagnostics.Debug.WriteLine($"GetCell: клетка [{x}, {y}] не найдена ни по координатам, ни по индексу {index}");
            return null;
        }
    }
}