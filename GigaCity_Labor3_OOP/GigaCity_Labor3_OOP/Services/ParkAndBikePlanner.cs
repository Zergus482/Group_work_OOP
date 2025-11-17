using System.Collections.Generic;
using GigaCity_Labor3_OOP.Models;

namespace GigaCity_Labor3_OOP.Services
{
    /// <summary>
    /// Отвечает за размещение парков и велодорожек на городских клетках.
    /// Вынесено в отдельный файл для удобства настройки.
    /// </summary>
    public class ParkAndBikePlanner
    {
        private readonly MapModel _map;

        private readonly (int startX, int startY, int width, int height)[] _parkBlueprints =
        {
            (29, 40, 3, 3),
            (59, 40, 3, 3),
            (41, 70, 3, 3),
            (53, 58, 3, 3)
        };

        private readonly (int x, int y)[] _manualBikePathCoordinates =
        {
            (26,33),(26,34),(26,35),
            (25,35),(25,36),(25,37),
            (24,39),(24,40),(24,41),(24,42),(24,43),
            (23,45),(23,46),(23,47),
            (22,47),(22,48),(22,49),
            (20,52),(20,53),(20,54),
            (19,54),(19,55),
            (18,57),(18,58),
            (17,58),(17,59),(17,60),
            (16,60),(16,61)
        };

        public ParkAndBikePlanner(MapModel map)
        {
            _map = map;
        }

        public HashSet<(int x, int y)> GenerateParks()
        {
            var parks = new HashSet<(int, int)>();

            foreach (var blueprint in _parkBlueprints)
            {
                AddParkArea(parks, blueprint.startX, blueprint.startY, blueprint.width, blueprint.height);
            }

            return parks;
        }

        public HashSet<(int x, int y)> GenerateBikePaths(HashSet<(int x, int y)> parkCells)
        {
            var bikePaths = new HashSet<(int, int)>();

            AddPathsAroundParks(bikePaths, parkCells);
            AddManualBikePaths(bikePaths);

            return bikePaths;
        }

        private void AddParkArea(HashSet<(int x, int y)> parks, int startX, int startY, int width, int height)
        {
            for (int x = startX; x < startX + width; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    if (IsValidParkCell(x, y))
                    {
                        parks.Add((x, y));
                    }
                }
            }
        }

        private bool IsValidParkCell(int x, int y)
        {
            var cell = _map.GetCell(x, y);
            if (cell == null) return false;

            if (cell.TerrainType != (byte)TerrainType.City) return false;
            if (_map.IsRoad(x, y)) return false;

            return true;
        }

        private void AddPathsAroundParks(HashSet<(int x, int y)> bikePaths, HashSet<(int x, int y)> parks)
        {
            foreach (var park in parks)
            {
                // Добавляем кольцевую дорожку вокруг парка
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int x = park.x + dx;
                        int y = park.y + dy;

                        if (parks.Contains((x, y))) continue;

                        if (CanPlaceBikeCell(x, y))
                        {
                            bikePaths.Add((x, y));
                        }
                    }
                }
            }
        }

        private bool CanPlaceBikeCell(int x, int y)
        {
            var cell = _map.GetCell(x, y);
            if (cell == null) return false;

            if (_map.IsRoad(x, y)) return false;
            if (cell.TerrainType == (byte)TerrainType.Water) return false;
            if (cell.TerrainType == (byte)TerrainType.Airport) return false;
            if (cell.TerrainType == (byte)TerrainType.Port) return false;

            return true;
        }

        private void AddManualBikePaths(HashSet<(int x, int y)> bikePaths)
        {
            foreach (var coord in _manualBikePathCoordinates)
            {
                if (CanPlaceBikeCell(coord.x, coord.y))
                {
                    bikePaths.Add(coord);
                }
            }
        }
    }
}

