using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using GigaCity_Labor3_OOP.Models.Economy;

namespace GigaCity_Labor3_OOP.ViewModels.Economy
{
    public class ResourceFlowViewModel : INotifyPropertyChanged
    {
        private double _currentThroughput;

        public EconomyFacilityViewModel Source { get; }
        public EconomyFacilityViewModel Target { get; }
        public CommodityType? Commodity { get; }
        public ProductType? Product { get; }
        public CoreResource? CoreResourceType { get; }
        public TransportMode Mode { get; }
        public bool IsExport { get; }
        public double PlannedThroughput { get; }
        public string Label { get; }
        public IReadOnlyList<Point> RoadPath { get; }

        public ResourceFlowViewModel(
            EconomyFacilityViewModel source,
            EconomyFacilityViewModel target,
            double plannedThroughput,
            TransportMode mode,
            string label,
            CommodityType? commodity = null,
            ProductType? product = null,
            CoreResource? coreResource = null,
            IReadOnlyList<Point>? roadPath = null,
            bool isExport = false)
        {
            Source = source;
            Target = target;
            PlannedThroughput = Math.Max(plannedThroughput, 1);
            Mode = mode;
            Label = label;
            Commodity = commodity;
            Product = product;
            CoreResourceType = coreResource;
            RoadPath = roadPath != null && roadPath.Count >= 2
                ? roadPath
                : new List<Point> { source.CenterPixel, target.CenterPixel };
            IsExport = isExport;
        }

        public double CurrentThroughput
        {
            get => _currentThroughput;
            private set
            {
                if (Math.Abs(_currentThroughput - value) > 0.001)
                {
                    _currentThroughput = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Stability));
                    OnPropertyChanged(nameof(FlowBrush));
                    OnPropertyChanged(nameof(Opacity));
                }
            }
        }

        public double Stability => Math.Clamp(CurrentThroughput / PlannedThroughput, 0, 1);

        public double Distance
        {
            get
            {
                var dx = Target.CenterPixel.X - Source.CenterPixel.X;
                var dy = Target.CenterPixel.Y - Source.CenterPixel.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }

        public Brush FlowBrush
        {
            get
            {
                if (CoreResourceType.HasValue)
                {
                    return CoreResourceType.Value switch
                    {
                        CoreResource.Wood => new SolidColorBrush(Color.FromRgb(108, 158, 98)),
                        CoreResource.Iron => new SolidColorBrush(Color.FromRgb(170, 170, 180)),
                        CoreResource.Copper => new SolidColorBrush(Color.FromRgb(210, 140, 90)),
                        CoreResource.Oil => new SolidColorBrush(Color.FromRgb(70, 96, 150)),
                        CoreResource.Coal => new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                        CoreResource.Water => new SolidColorBrush(Color.FromRgb(90, 150, 210)),
                        _ => Brushes.SlateGray
                    };
                }

                if (Commodity.HasValue)
                {
                    return Commodity switch
                    {
                        CommodityType.IronOre => new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                        CommodityType.Coal => new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                        CommodityType.Timber => new SolidColorBrush(Color.FromRgb(120, 80, 40)),
                        CommodityType.CrudeOil => new SolidColorBrush(Color.FromRgb(60, 60, 100)),
                        CommodityType.FreshWater => new SolidColorBrush(Color.FromRgb(70, 120, 200)),
                        _ => Brushes.SlateGray
                    };
                }

                if (Product.HasValue)
                {
                    return Product switch
                    {
                        ProductType.Steel => new SolidColorBrush(Color.FromRgb(160, 180, 210)),
                        ProductType.EngineeredWood => new SolidColorBrush(Color.FromRgb(175, 120, 70)),
                        ProductType.Fuel => new SolidColorBrush(Color.FromRgb(210, 160, 50)),
                        ProductType.Chemicals => new SolidColorBrush(Color.FromRgb(140, 100, 200)),
                        ProductType.Conductors => new SolidColorBrush(Color.FromRgb(220, 180, 90)),
                        ProductType.Energy => new SolidColorBrush(Color.FromRgb(255, 215, 80)),
                        ProductType.Coolant => new SolidColorBrush(Color.FromRgb(100, 200, 255)),
                        _ => Brushes.DarkKhaki
                    };
                }

                return Brushes.LightSteelBlue;
            }
        }

        public double Opacity => 0.35 + Stability * 0.55;

        public double StrokeThickness => 1.2 + Math.Clamp(PlannedThroughput / 35, 0.2, 1.8);

        public double LabelX => (Source.CenterPixel.X + Target.CenterPixel.X) / 2 - 20;
        public double LabelY => (Source.CenterPixel.Y + Target.CenterPixel.Y) / 2 - 10;

        public void RegisterThroughput(double amount)
        {
            CurrentThroughput = amount;
        }

        public StreamGeometry ArrowGeometry
        {
            get
            {
                var geometry = new StreamGeometry();
                using var ctx = geometry.Open();
                ctx.BeginFigure(Source.CenterPixel, false, false);
                ctx.LineTo(Target.CenterPixel, true, true);
                geometry.Freeze();
                return geometry;
            }
        }

        public PointCollection ArrowHead
        {
            get
            {
                var start = Source.CenterPixel;
                var end = Target.CenterPixel;
                var dx = end.X - start.X;
                var dy = end.Y - start.Y;
                var length = Math.Sqrt(dx * dx + dy * dy);
                if (length < 0.001)
                {
                    return new PointCollection();
                }

                var ux = dx / length;
                var uy = dy / length;
                var basePoint = new Point(end.X - ux * 10, end.Y - uy * 10);
                var perp = new Vector(-uy, ux) * 4;

                return new PointCollection
                {
                    end,
                    new Point(basePoint.X + perp.X, basePoint.Y + perp.Y),
                    new Point(basePoint.X - perp.X, basePoint.Y - perp.Y)
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

