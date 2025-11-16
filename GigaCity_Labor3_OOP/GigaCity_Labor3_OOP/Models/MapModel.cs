// MapModel.cs

using System;
using System.Collections.Generic;
using System.Linq;

namespace GigaCity_Labor3_OOP.Models
{
    public class MapModel
    {
        public int Width { get; } = 100;
        public int Height { get; } = 100;
        public List<CellViewModel> Cells { get; private set; }
        
        // Позиции аэропортов
        public int Airport1X { get; private set; }
        public int Airport1Y { get; private set; }
        public int Airport2X { get; private set; }
        public int Airport2Y { get; private set; }

        public MapModel()
        {
            Cells = GenerateMap();
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

            // 6. Создаем ячейки с ресурсами
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = new CellViewModel
                    {
                        X = x,
                        Y = y,
                        TerrainType = terrainMap[x, y],
                        ResourceType = GetResourceType(terrainMap[x, y], random)
                    };
                    cell.ToolTip = $"[{x},{y}] {GetTerrainName(cell.TerrainType)}\nРесурс: {GetResourceName(cell.ResourceType)}";
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
            // Учебные заведения и аэропорты не имеют ресурсов
            if (terrainType == (byte)TerrainType.City || 
                terrainType == (byte)TerrainType.Educational || 
                terrainType == (byte)TerrainType.Airport) return 0;

            double chance = random.NextDouble();
            return terrainType switch
            {
                (byte)TerrainType.Meadows => chance < 0.2 ? (byte)ResourceType.Gas : chance < 0.4 ? (byte)ResourceType.Plants : (byte)ResourceType.None,
                (byte)TerrainType.Forest => chance < 0.6 ? (byte)ResourceType.Trees : chance < 0.8 ? (byte)ResourceType.Plants : (byte)ResourceType.None,
                (byte)TerrainType.Mountains => chance < 0.5 ? (byte)ResourceType.Metals : chance < 0.7 ? (byte)ResourceType.Oil : (byte)ResourceType.None,
                (byte)TerrainType.Water => chance < 0.4 ? (byte)ResourceType.Metals : chance < 0.6 ? (byte)ResourceType.Oil : (byte)ResourceType.None,
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
    }
}