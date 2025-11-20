using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using GigaCity_Labor3_OOP.Models.Economy;

namespace GigaCity_Labor3_OOP.ViewModels.Economy
{
    public class EconomyFacilityViewModel : INotifyPropertyChanged
    {
        private double _currentProduction;
        private double _storage;
        private OperationalState _state;
        private double _utilization;

        public string Id { get; }
        public string Name { get; }
        public EconomicNodeType NodeType { get; }
        public FactorySpecialization? FactoryType { get; }
        public CommodityType? Commodity { get; }
        public ProductType? Product { get; }
        public string Description { get; }
        public string RiskNote { get; }
        public double BaseThroughput { get; }
        public double Capacity { get; }
        private Point _mapPoint;
        public Point MapPoint
        {
            get => _mapPoint;
            private set
            {
                _mapPoint = value;
                OnPropertyChanged(nameof(MapPoint));
                OnPropertyChanged(nameof(CanvasLeft));
                OnPropertyChanged(nameof(CanvasTop));
                OnPropertyChanged(nameof(CenterPixel));
            }
        }
        public IReadOnlyDictionary<CommodityType, double> InputDemand => _inputDemand;
        public CoreResource? CoreResource { get; private set; }
        public ProcessingStage ProcessingStage { get; private set; } = ProcessingStage.Extraction;
        public BuildingBlueprint? Blueprint { get; private set; }
        public bool IsDraggable { get; set; } = true;
        public bool IsLogisticsHub => Blueprint?.IsLogisticsHub ?? NodeType == EconomicNodeType.TransportHub;
        private bool _isActive = true; // По умолчанию производство включено
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(ToolTip));
                }
            }
        }
        private double _gridX;
        private double _gridY;
        public double GridX 
        { 
            get => _gridX;
            private set
            {
                _gridX = Math.Round(value);
            }
        }
        public double GridY 
        { 
            get => _gridY;
            private set
            {
                _gridY = Math.Round(value);
            }
        }
        public string InventoryLabel => $"{Storage:0.0} т";
        private bool _hasResource = true;
        public bool HasResource 
        { 
            get => _hasResource;
            set
            {
                if (_hasResource != value)
                {
                    _hasResource = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FillBrush)); // Обновляем цвет при изменении
                }
            }
        } // Для добывающих объектов - есть ли ресурс

        private readonly Dictionary<CommodityType, double> _inputDemand = new();

        public string NodeTypeTitle => NodeType switch
        {
            EconomicNodeType.ResourceSite => "Ресурсная зона",
            EconomicNodeType.Warehouse => "Склад",
            EconomicNodeType.Factory => "Промышленный цех",
            EconomicNodeType.Residential => "Жилая зона",
            EconomicNodeType.TransportHub => "Транспортный узел",
            _ => "Объект"
        };

        public string ResourceLabel
        {
            get
            {
                if (Commodity.HasValue)
                {
                    return Commodity switch
                    {
                        CommodityType.IronOre => "Железная руда",
                        CommodityType.Coal => "Уголь",
                        CommodityType.Timber => "Древесина",
                        CommodityType.CrudeOil => "Нефть",
                        CommodityType.FreshWater => "Вода",
                        _ => "Ресурс"
                    };
                }

                if (Product.HasValue)
                {
                    return Product switch
                    {
                        ProductType.Steel => "Сталь",
                        ProductType.EngineeredWood => "Древесные плиты",
                        ProductType.Fuel => "Топливо",
                        ProductType.Chemicals => "Химическая продукция",
                        _ => "Продукция"
                    };
                }

                if (FactoryType.HasValue)
                {
                    return FactoryType switch
                    {
                        FactorySpecialization.Metallurgical => "Металлургия",
                        FactorySpecialization.WoodProcessing => "Деревообработка",
                        FactorySpecialization.Chemical => "Химия",
                        _ => "Производство"
                    };
                }

                return Description;
            }
        }

        public EconomyFacilityViewModel(
            string id,
            string name,
            EconomicNodeType nodeType,
            Point mapPoint,
            double baseThroughput,
            double capacity,
            string description,
            CommodityType? commodity = null,
            ProductType? product = null,
            FactorySpecialization? factoryType = null,
            string riskNote = "")
        {
            Id = id;
            Name = name;
            NodeType = nodeType;
            MapPoint = mapPoint;
            BaseThroughput = baseThroughput;
            Capacity = Math.Max(capacity, 1);
            Description = description;
            Commodity = commodity;
            Product = product;
            FactoryType = factoryType;
            RiskNote = riskNote;
            _state = OperationalState.Idle;
            _gridX = Math.Round(mapPoint.X);
            _gridY = Math.Round(mapPoint.Y);
        }

        public void AttachBlueprint(BuildingBlueprint blueprint)
        {
            Blueprint = blueprint;
            CoreResource = blueprint.Resource;
            ProcessingStage = blueprint.Stage;
        }

        public void UpdateGridPosition(double gridX, double gridY)
        {
            GridX = Math.Round(gridX);
            GridY = Math.Round(gridY);
            MapPoint = new Point(GridX, GridY);
            OnPropertyChanged(nameof(GridX));
            OnPropertyChanged(nameof(GridY));
        }

        public void SetInputDemand(params (CommodityType type, double amount)[] needs)
        {
            _inputDemand.Clear();
            foreach (var (type, amount) in needs)
            {
                _inputDemand[type] = amount;
            }
            OnPropertyChanged(nameof(InputDemand));
        }

        public double CurrentProduction
        {
            get => _currentProduction;
            private set
            {
                if (Math.Abs(_currentProduction - value) > 0.001)
                {
                    _currentProduction = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusSummary));
                    OnPropertyChanged(nameof(ToolTip));
                }
            }
        }

        public double Storage
        {
            get => _storage;
            private set
            {
                if (Math.Abs(_storage - value) > 0.001)
                {
                    _storage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StorageUsage));
                    OnPropertyChanged(nameof(StorageLabel));
                    OnPropertyChanged(nameof(InventoryLabel));
                    OnPropertyChanged(nameof(StatusSummary));
                    OnPropertyChanged(nameof(ToolTip));
                }
            }
        }

        public double StorageUsage => Math.Clamp(Storage / Capacity, 0, 1);
        public string StorageLabel => $"{Storage:0.#}/{Capacity:0.#} т";

        public OperationalState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StrokeBrush));
                    OnPropertyChanged(nameof(ToolTip));
                }
            }
        }

        public double Utilization
        {
            get => _utilization;
            private set
            {
                if (Math.Abs(_utilization - value) > 0.001)
                {
                    _utilization = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusSummary));
                    OnPropertyChanged(nameof(ToolTip));
                }
            }
        }

        public void UpdateState(double production, double storage, OperationalState state, double utilization)
        {
            CurrentProduction = production;
            Storage = Math.Min(storage, Capacity);
            State = state;
            Utilization = Math.Clamp(utilization, 0, 1.2);
        }

        public string StatusText => State switch
        {
            OperationalState.Works => "Работает",
            OperationalState.Idle => "Простаивает",
            OperationalState.Overloaded => "Перегружен",
            _ => "Неизвестно"
        };

        public string StatusSummary
        {
            get
            {
                if (NodeType == EconomicNodeType.ResourceSite)
                {
                    return $"Добыча: {CurrentProduction:0.#}/ч | Запас: {Storage:0.#} т";
                }

                if (NodeType == EconomicNodeType.Factory)
                {
                    return $"Выпуск: {CurrentProduction:0.#}/ч ({Utilization:P0})";
                }

                if (NodeType == EconomicNodeType.Warehouse)
                {
                    return $"Склад: {Storage:0.#}/{Capacity:0.#} т";
                }

                return $"Активность: {CurrentProduction:0.#}";
            }
        }

        public double CanvasLeft => MapPoint.X * MapScale;
        public double CanvasTop => MapPoint.Y * MapScale;
        public double NodeSize => NodeType switch
        {
            EconomicNodeType.ResourceSite => 20,
            EconomicNodeType.Factory => 28,
            EconomicNodeType.Warehouse => 24,
            EconomicNodeType.Residential => 22,
            EconomicNodeType.TransportHub => IsLogisticsHub ? 36 : 24, // Больше для логистического центра
            _ => 20
        };

        public Point CenterPixel => new(CanvasLeft + NodeSize / 2, CanvasTop + NodeSize / 2);

        private static readonly double MapScale = 15;

        public Brush FillBrush
        {
            get
            {
                // Если добывающий объект без ресурса - красный
                if (NodeType == EconomicNodeType.ResourceSite && !HasResource)
                {
                    return new SolidColorBrush(Color.FromRgb(180, 50, 50));
                }

                return NodeType switch
                {
                    EconomicNodeType.ResourceSite => new SolidColorBrush(Color.FromRgb(64, 104, 53)),
                    EconomicNodeType.Warehouse => new SolidColorBrush(Color.FromRgb(90, 90, 120)),
                    EconomicNodeType.Factory => new SolidColorBrush(Color.FromRgb(130, 72, 62)),
                    EconomicNodeType.Residential => new SolidColorBrush(Color.FromRgb(68, 112, 160)),
                    EconomicNodeType.TransportHub => new SolidColorBrush(Color.FromRgb(92, 120, 80)),
                    _ => Brushes.Gray
                };
            }
        }

        public Brush StrokeBrush => State switch
        {
            OperationalState.Works => Brushes.LimeGreen,
            OperationalState.Overloaded => Brushes.OrangeRed,
            OperationalState.Idle => Brushes.Goldenrod,
            _ => Brushes.LightGray
        };

        public string IconText => NodeType switch
        {
            EconomicNodeType.ResourceSite => "R",
            EconomicNodeType.Warehouse => "S",
            EconomicNodeType.Factory => "F",
            EconomicNodeType.Residential => "H",
            EconomicNodeType.TransportHub => "T",
            _ => "?"
        };

        public string SecondaryText
        {
            get
            {
                if (NodeType == EconomicNodeType.ResourceSite && Commodity.HasValue)
                {
                    return Commodity switch
                    {
                        CommodityType.IronOre => "Железная руда",
                        CommodityType.Coal => "Уголь",
                        CommodityType.Timber => "Лес",
                        CommodityType.CrudeOil => "Нефть",
                        CommodityType.FreshWater => "Вода",
                        _ => string.Empty
                    };
                }

                if (NodeType == EconomicNodeType.Factory && Product.HasValue)
                {
                    return Product switch
                    {
                        ProductType.Steel => "Сталь",
                        ProductType.EngineeredWood => "Плиты",
                        ProductType.Fuel => "Топливо",
                        ProductType.Chemicals => "Хим. продукты",
                        _ => string.Empty
                    };
                }

                if (NodeType == EconomicNodeType.Warehouse && Commodity.HasValue)
                {
                    return "Склад " + SecondaryTextFromCommodity(Commodity.Value);
                }

                return Description;
            }
        }

        private static string SecondaryTextFromCommodity(CommodityType commodity)
        {
            return commodity switch
            {
                CommodityType.IronOre => "руды",
                CommodityType.Coal => "угля",
                CommodityType.Timber => "лесоматериала",
                CommodityType.CrudeOil => "нефти",
                CommodityType.FreshWater => "воды",
                _ => string.Empty
            };
        }

        public string ToolTip
        {
            get
            {
                var builder = new List<string>
                {
                    $"{Name}",
                    $"Тип: {NodeTypeTitle}",
                    $"Профиль: {ResourceLabel}",
                    $"Режим: {StatusText}",
                    StatusSummary
                };

                if (!string.IsNullOrWhiteSpace(Description))
                {
                    builder.Add(Description);
                }

                if (!string.IsNullOrWhiteSpace(RiskNote))
                {
                    builder.Add($"Эко-риск: {RiskNote}");
                }

                return string.Join("\n", builder);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString() => $"{Name} ({NodeType})";
    }
}

