using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GigaCity_Labor3_OOP.Models;

namespace GigaCity_Labor3_OOP
{
    public partial class MainWindow
    {
        // Коллекция визуальных элементов кораблей
        private readonly Dictionary<Ship, Polygon> _shipVisuals = new Dictionary<Ship, Polygon>();

        private void InitializeShipVisuals()
        {
            // Подписываемся на изменения коллекции кораблей
            if (ViewModel?.Ships != null)
            {
                ViewModel.Ships.CollectionChanged += Ships_CollectionChanged;
            }
        }

        private void Ships_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Обновляем UI в UI потоке
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.NewItems != null)
                {
                    foreach (Ship ship in e.NewItems)
                    {
                        CreateShipVisual(ship);
                        ship.PropertyChanged += Ship_PropertyChanged;
                    }
                }

                if (e.OldItems != null)
                {
                    foreach (Ship ship in e.OldItems)
                    {
                        RemoveShipVisual(ship);
                        ship.PropertyChanged -= Ship_PropertyChanged;
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Normal);
        }

        private void CreateShipVisual(Ship ship)
        {
            // Создаем корабль в виде прямоугольника с треугольным носом
            var shipShape = new Polygon
            {
                Fill = new SolidColorBrush(Colors.Brown),
                Stroke = new SolidColorBrush(Colors.DarkBlue),
                StrokeThickness = 2,
                ToolTip = $"Корабль [{ship.FromPortX},{ship.FromPortY}] -> [{ship.ToPortX},{ship.ToPortY}]",
                Opacity = 1.0
            };

            // Размер корабля
            double width = 18;
            double height = 12;
            double halfWidth = width / 2;
            double halfHeight = height / 2;

            // Вычисляем направление движения для поворота корабля
            double dx = ship.ToPortX - ship.FromPortX;
            double dy = ship.ToPortY - ship.FromPortY;
            double angle = Math.Atan2(dy, dx) * 180 / Math.PI;

            // Создаем точки корабля (прямоугольник с треугольным носом)
            var points = new PointCollection
            {
                new Point(halfWidth, 0),                    // Нос корабля (вершина треугольника)
                new Point(halfWidth * 0.3, -halfHeight),    // Левая верхняя точка
                new Point(-halfWidth, -halfHeight),          // Левая задняя точка
                new Point(-halfWidth, halfHeight),           // Правая задняя точка
                new Point(halfWidth * 0.3, halfHeight)       // Правая верхняя точка
            };

            shipShape.Points = points;

            // Поворачиваем корабль в направлении движения
            var transform = new RotateTransform(angle, 0, 0);
            shipShape.RenderTransformOrigin = new Point(0.5, 0.5);
            shipShape.RenderTransform = transform;

            // Устанавливаем Z-Index, чтобы корабли были поверх карты
            Panel.SetZIndex(shipShape, 999);

            // Убеждаемся, что координаты в пределах Canvas
            double x = Math.Max(0, Math.Min(MapCanvas.Width - width, ship.X - halfWidth));
            double y = Math.Max(0, Math.Min(MapCanvas.Height - height, ship.Y - halfHeight));
            
            Canvas.SetLeft(shipShape, x);
            Canvas.SetTop(shipShape, y);

            MapCanvas.Children.Add(shipShape);
            _shipVisuals[ship] = shipShape;
            
            // Отладочный вывод
            System.Diagnostics.Debug.WriteLine($"Корабль создан: X={ship.X}, Y={ship.Y}, Canvas X={x}, Canvas Y={y}");
        }

        private void RemoveShipVisual(Ship ship)
        {
            if (_shipVisuals.TryGetValue(ship, out var polygon))
            {
                MapCanvas.Children.Remove(polygon);
                _shipVisuals.Remove(ship);
            }
        }

        private void Ship_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Обновляем UI в UI потоке
            if (sender is Ship ship && _shipVisuals.TryGetValue(ship, out var polygon))
            {
                if (e.PropertyName == nameof(Ship.X) || e.PropertyName == nameof(Ship.Y))
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Убеждаемся, что координаты в пределах Canvas
                        double width = 18;
                        double height = 12;
                        double halfWidth = width / 2;
                        double halfHeight = height / 2;
                        double x = Math.Max(0, Math.Min(MapCanvas.Width - width, ship.X - halfWidth));
                        double y = Math.Max(0, Math.Min(MapCanvas.Height - height, ship.Y - halfHeight));
                        Canvas.SetLeft(polygon, x);
                        Canvas.SetTop(polygon, y);
                        
                        // Обновляем поворот корабля в направлении движения
                        double currentDx = ship.ToPortX * 15.0 + 7.5 - ship.X;
                        double currentDy = ship.ToPortY * 15.0 + 7.5 - ship.Y;
                        double angle = Math.Atan2(currentDy, currentDx) * 180 / Math.PI;
                        if (polygon.RenderTransform is RotateTransform rotateTransform)
                        {
                            rotateTransform.Angle = angle;
                        }
                        else
                        {
                            // Если трансформация еще не создана, создаем ее
                            polygon.RenderTransform = new RotateTransform(angle);
                            polygon.RenderTransformOrigin = new Point(0.5, 0.5);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Render);
                }
            }
        }
    }
}

