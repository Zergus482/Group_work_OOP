using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CityFinance.ViewModels;
using GigaCity_Labor3_OOP.Models;
using GigaCity_Labor3_OOP.Models.Economy;
using GigaCity_Labor3_OOP.Services;
using TheFinancialSystem;

namespace GigaCity_Labor3_OOP.ViewModels.Economy
{
    public class EconomySimulationViewModel : INotifyPropertyChanged
    {
        private readonly DispatcherTimer _simulationTimer;
        private readonly DispatcherTimer _transportTimer;
        private readonly Dictionary<string, BuildingBlueprint> _blueprints;
        private readonly Dictionary<EconomyFacilityViewModel, double> _inputBuffer = new();
        private readonly Dictionary<ResourceFlowViewModel, int> _activeVehiclesPerFlow = new();
        private readonly Dictionary<CoreResource, ResourceStatViewModel> _resourceStatsIndex;
        private readonly HashSet<(int x, int y)> _roadCoordinates;
        private readonly PathFindingService? _pathFinder;
        private readonly Random _random = new();
        private readonly int _mapWidth;
        private readonly int _mapHeight;
        private readonly double _cellPixelSize;
        private const int MaxVehicles = 8; // Еще больше уменьшено для снижения нагрузки
        private const int MaxVehiclesPerFlow = 1; // Уменьшено с 3 до 1 для снижения перегруженности
        private const int MaxRoadSearchRadius = 18;

        private EconomyFacilityViewModel? _selectedFacility;
        private double _economicIndicator;
        private string _balanceSummary = "Запуск симуляции...";
        private Point? _pendingPlacementCell;
        private bool _isPaused;
        private double _totalProduction = 0;
        // Отслеживание произведенной продукции по типам
        private readonly Dictionary<ProductType, double> _producedProducts = new();
        // Отслеживание ресурсов в логистических центрах по типам
        private readonly Dictionary<EconomyFacilityViewModel, Dictionary<CoreResource, double>> _hubResources = new();
        // Отслеживание входных буферов по типам ресурсов для производственных объектов
        private readonly Dictionary<EconomyFacilityViewModel, Dictionary<CoreResource, double>> _inputBuffersByResource = new();

        public ObservableCollection<EconomyFacilityViewModel> Facilities { get; } = new();
        public ObservableCollection<ResourceFlowViewModel> Flows { get; } = new();
        public ObservableCollection<TransportUnitViewModel> TransportUnits { get; } = new();
        public ObservableCollection<RoadConnectionViewModel> RoadConnections { get; } = new();
        public ObservableCollection<ResourceStatViewModel> ResourceStats { get; }
        public ObservableCollection<BuildMenuEntry> BuildMenu { get; }
        public ObservableCollection<BuildMenuEntry> CombinedBuildMenu { get; }

        public EconomyFacilityViewModel? SelectedFacility
        {
            get => _selectedFacility;
            set
            {
                if (_selectedFacility != value)
                {
                    _selectedFacility = value;
                    OnPropertyChanged();
                }
            }
        }

        public double EconomicIndicator
        {
            get => _economicIndicator;
            private set
            {
                if (Math.Abs(_economicIndicator - value) > 0.1)
                {
                    _economicIndicator = Math.Clamp(value, 0, 100);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EconomicIndicatorText));
                }
            }
        }

        public string EconomicIndicatorText => $"{EconomicIndicator:0}%";

        public string BalanceSummary
        {
            get => _balanceSummary;
            private set
            {
                if (_balanceSummary != value)
                {
                    _balanceSummary = value;
                    OnPropertyChanged();
                }
            }
        }

        public double TotalProduction
        {
            get => _totalProduction;
            private set
            {
                if (Math.Abs(_totalProduction - value) > 0.1)
                {
                    _totalProduction = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalProductionText));
                }
            }
        }

        public string TotalProductionText => $"Всего произведено: {TotalProduction:0.0} т";

        public bool IsPaused
        {
            get => _isPaused;
            private set
            {
                if (_isPaused != value)
                {
                    _isPaused = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand PlaceBuildingCommand { get; }
        public ICommand OptimizeRoutesCommand { get; }
        public ICommand DispatchFleetCommand { get; }
        public ICommand TogglePauseCommand { get; }
        public ICommand SelectFacilityCommand { get; }
        public ICommand DeleteSelectedFacilityCommand { get; }
        public ICommand PlaceWarehouseCommand { get; }
        public ICommand ToggleFacilityActiveCommand { get; }
        public ICommand ToggleProductProductionCommand { get; }
        
        // Получить количество произведенной продукции
        public double GetProducedProduct(ProductType productType)
        {
            return _producedProducts.TryGetValue(productType, out var amount) ? amount : 0;
        }
        
        // Получить все типы произведенной продукции
        public Dictionary<ProductType, double> GetAllProducedProducts()
        {
            return new Dictionary<ProductType, double>(_producedProducts);
        }
        
        // Включить/выключить производство определенного типа продукции
        public void ToggleProductProduction(ProductType productType, bool isActive)
        {
            var facilities = Facilities.Where(f => 
                f.Blueprint?.Stage == ProcessingStage.Manufacturing && 
                ConvertProduct(f.Blueprint.Resource) == productType).ToList();
            
            foreach (var facility in facilities)
            {
                facility.IsActive = isActive;
            }
        }
        
        // Проверить, достаточно ли ресурсов для производства определенного типа продукции
        public string CheckProductionResources(ProductType productType, EconomyFacilityViewModel? hub = null)
        {
            var facilities = Facilities.Where(f => 
                f.Blueprint?.Stage == ProcessingStage.Manufacturing && 
                ConvertProduct(f.Blueprint.Resource) == productType).ToList();
            
            if (facilities.Count == 0) return "Нет производственных объектов для этого типа продукции";
            
            var facility = facilities.First();
            if (facility.Blueprint?.InputCosts == null) return string.Empty;
            
            // Находим ближайший логистический центр
            if (hub == null)
            {
                var hubs = Facilities.Where(f => f.Blueprint?.Stage == ProcessingStage.Logistics).ToList();
                if (hubs.Count == 0) return "Нет логистического центра";
                hub = hubs.OrderBy(h => Distance(facility, h)).First();
            }
            
            var missingResources = new List<string>();
            foreach (var inputCost in facility.Blueprint.InputCosts)
            {
                var requiredResource = inputCost.Key;
                var requiredAmount = inputCost.Value;
                var availableAmount = GetHubResource(hub, requiredResource);
                
                if (availableAmount < requiredAmount)
                {
                    var resourceName = requiredResource switch
                    {
                        CoreResource.Wood => "дерева",
                        CoreResource.Iron => "железа",
                        CoreResource.Copper => "меди",
                        CoreResource.Oil => "нефти",
                        CoreResource.Coal => "угля",
                        CoreResource.Water => "воды",
                        _ => requiredResource.ToString()
                    };
                    missingResources.Add($"{resourceName} (нужно {requiredAmount:0.#} т/мин, есть {availableAmount:0.#} т)");
                }
            }
            
            if (missingResources.Count > 0)
            {
                return $"Не хватает ресурсов: {string.Join(", ", missingResources)}";
            }
            
            return string.Empty;
        }
        
        // Получить количество ресурсов определенного типа в логистическом центре
        public double GetHubResource(EconomyFacilityViewModel hub, CoreResource resource)
        {
            if (!_hubResources.ContainsKey(hub))
                return 0;
            
            return _hubResources[hub].TryGetValue(resource, out var amount) ? amount : 0;
        }
        
        // Получить все ресурсы в логистическом центре
        public Dictionary<CoreResource, double> GetHubResources(EconomyFacilityViewModel hub)
        {
            if (!_hubResources.ContainsKey(hub))
                return new Dictionary<CoreResource, double>();
            
            return new Dictionary<CoreResource, double>(_hubResources[hub]);
        }

        private readonly MapModel? _mapModel;

        public EconomySimulationViewModel(
            HashSet<(int x, int y)> roadCoordinates,
            int mapWidth,
            int mapHeight,
            double cellPixelSize = 15,
            MapModel? mapModel = null)
        {
            _mapWidth = Math.Max(mapWidth, 1);
            _mapHeight = Math.Max(mapHeight, 1);
            _cellPixelSize = cellPixelSize;
            _mapModel = mapModel;
            _roadCoordinates = roadCoordinates != null
                ? new HashSet<(int x, int y)>(roadCoordinates)
                : new HashSet<(int, int)>();
            _pathFinder = _roadCoordinates.Count > 0 ? new PathFindingService(_roadCoordinates) : null;

            _blueprints = BuildBlueprintCatalog().ToDictionary(b => b.Id, b => b);
            ResourceStats = new ObservableCollection<ResourceStatViewModel>(CreateStats());
            _resourceStatsIndex = ResourceStats.ToDictionary(s => s.Resource, s => s);

            BuildMenu = new ObservableCollection<BuildMenuEntry>(_blueprints.Values.Select(b => new BuildMenuEntry
            {
                BlueprintId = b.Id,
                Header = $"{b.DisplayName} ({b.Stage})",
                Description = b.Description
            }));

            PlaceBuildingCommand = new RelayCommand(param =>
            {
                System.Diagnostics.Debug.WriteLine($"PlaceBuildingCommand вызван, param={param}, _pendingPlacementCell.HasValue={_pendingPlacementCell.HasValue}");
                
                if (param is BuildMenuEntry entry)
                {
                    if (!_pendingPlacementCell.HasValue)
                    {
                        System.Diagnostics.Debug.WriteLine("ОШИБКА: _pendingPlacementCell не установлен!");
                        MessageBox.Show("Клетка для размещения не выбрана. Кликните ПКМ по карте, затем выберите объект из меню.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    var gridX = (int)_pendingPlacementCell.Value.X;
                    var gridY = (int)_pendingPlacementCell.Value.Y;
                    System.Diagnostics.Debug.WriteLine($"Попытка разместить {entry.Header} ({entry.BlueprintId}) на [{gridX}, {gridY}]");
                    
                    var result = TryPlaceBuilding(entry.BlueprintId, gridX, gridY);
                    if (!result)
                    {
                        // Сообщение об ошибке уже показано в TryPlaceBuilding
                        System.Diagnostics.Debug.WriteLine($"Не удалось разместить {entry.Header} на [{gridX}, {gridY}]");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"✓ Успешно размещен {entry.Header} на [{gridX}, {gridY}]");
                    }
                    _pendingPlacementCell = null;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ОШИБКА: param не является BuildMenuEntry, тип={param?.GetType().Name}");
                }
            });

            // Создаем объединенное меню с больницей, домом и экономическими объектами
            CombinedBuildMenu = new ObservableCollection<BuildMenuEntry>();
            CombinedBuildMenu.Add(new BuildMenuEntry
            {
                Header = "Разместить больницу",
                Description = "Разместить больницу на карте",
                ClickHandler = "PlaceMedicalPoint"
            });
            CombinedBuildMenu.Add(new BuildMenuEntry
            {
                Header = "Разместить жилой дом",
                Description = "Разместить жилой дом на карте",
                ClickHandler = "PlaceResidentialBuilding"
            });
            // Добавляем экономические объекты с командами
            foreach (var blueprint in _blueprints.Values)
            {
                CombinedBuildMenu.Add(new BuildMenuEntry
                {
                    BlueprintId = blueprint.Id,
                    Header = $"{blueprint.DisplayName} ({blueprint.Stage})",
                    Description = blueprint.Description,
                    Command = PlaceBuildingCommand,
                    CommandParameter = new BuildMenuEntry { BlueprintId = blueprint.Id, Header = $"{blueprint.DisplayName} ({blueprint.Stage})", Description = blueprint.Description }
                });
            }

            OptimizeRoutesCommand = new RelayCommand(_ => RebuildNetwork());
            DispatchFleetCommand = new RelayCommand(_ => DispatchBulkFleet());
            TogglePauseCommand = new RelayCommand(_ => TogglePause());
            SelectFacilityCommand = new RelayCommand(param =>
            {
                if (param is EconomyFacilityViewModel facility)
                {
                    SelectedFacility = facility;
                }
            });
            DeleteSelectedFacilityCommand = new RelayCommand(param =>
            {
                // Можем удалить либо выбранный объект, либо переданный в параметре
                var facilityToDelete = param as EconomyFacilityViewModel ?? SelectedFacility;
                if (facilityToDelete != null)
                {
                    DeleteFacility(facilityToDelete);
                }
            });
            PlaceWarehouseCommand = new RelayCommand(_ =>
            {
                if (_pendingPlacementCell.HasValue)
                {
                    // Создаем склад (refining stage) для первого доступного ресурса
                    var warehouseBlueprints = _blueprints.Values.Where(b => b.Stage == ProcessingStage.Refining).ToList();
                    if (warehouseBlueprints.Any())
                    {
                        var blueprint = warehouseBlueprints.First();
                        TryPlaceBuilding(blueprint.Id, (int)_pendingPlacementCell.Value.X, (int)_pendingPlacementCell.Value.Y);
                        _pendingPlacementCell = null;
                    }
                }
            });

            ToggleFacilityActiveCommand = new RelayCommand(param =>
            {
                if (param is EconomyFacilityViewModel facility)
                {
                    facility.IsActive = !facility.IsActive;
                }
            });

            ToggleProductProductionCommand = new RelayCommand(param =>
            {
                if (param is ProductType productType)
                {
                    var facilities = Facilities.Where(f => 
                        f.Blueprint?.Stage == ProcessingStage.Manufacturing && 
                        ConvertProduct(f.Blueprint.Resource) == productType).ToList();
                    
                    if (facilities.Count > 0)
                    {
                        // Переключаем состояние всех объектов, производящих этот тип продукции
                        bool newState = !facilities.First().IsActive;
                        foreach (var facility in facilities)
                        {
                            facility.IsActive = newState;
                        }
                    }
                }
            });

            SeedInitialLayout();
            RebuildNetwork();

            _simulationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _simulationTimer.Tick += (_, _) => SimulationTick();
            _simulationTimer.Start();

            // Увеличена задержка для снижения нагрузки на UI
            _transportTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _transportTimer.Tick += (_, _) => AnimateTransport();
            _transportTimer.Start();
        }

        public void SetPlacementCell(int gridX, int gridY)
        {
            var clampedX = Math.Clamp(gridX, 0, _mapWidth - 1);
            var clampedY = Math.Clamp(gridY, 0, _mapHeight - 1);
            _pendingPlacementCell = new Point(clampedX, clampedY);
            System.Diagnostics.Debug.WriteLine($"SetPlacementCell: ({gridX}, {gridY}) -> ({clampedX}, {clampedY}), _pendingPlacementCell установлен");
            
            // Проверяем клетку для отладки
            if (_mapModel != null)
            {
                var cell = _mapModel.GetCell(clampedX, clampedY);
                if (cell != null)
                {
                    System.Diagnostics.Debug.WriteLine($"  Клетка [{clampedX}, {clampedY}]: TerrainType={cell.TerrainType}, ResourceType={cell.ResourceType}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  ВНИМАНИЕ: Клетка [{clampedX}, {clampedY}] не найдена!");
                }
            }
        }

        public bool MoveFacility(EconomyFacilityViewModel facility, int gridX, int gridY)
        {
            // Добывающие объекты нельзя перемещать
            if (facility.Blueprint?.Stage == ProcessingStage.Extraction)
            {
                MessageBox.Show("Добывающие объекты нельзя перемещать! Они должны оставаться на месте добычи ресурсов.", "Невозможно переместить", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            var clampedX = Math.Clamp(gridX, 0, _mapWidth - 1);
            var clampedY = Math.Clamp(gridY, 0, _mapHeight - 1);
            facility.UpdateGridPosition(clampedX, clampedY);
            
            RebuildNetwork();
            return true;
        }

        private void SimulationTick()
        {
            if (IsPaused) return;

            double totalProduced = 0;
            foreach (var facility in Facilities)
            {
                var before = facility.Storage;
                RunProductionFor(facility);
                var after = facility.Storage;
                totalProduced += Math.Max(0, after - before);
            }

            TotalProduction += totalProduced;

            ExecuteFlows();
            UpdateStats();
            UpdateIndicator();
            UpdateBalanceSummary();
        }

        private void DeleteFacility(EconomyFacilityViewModel facility)
        {
            if (!Facilities.Contains(facility))
            {
                return;
            }

            // Удаляем связанные потоки
            var flowsToRemove = Flows.Where(f => f.Source == facility || f.Target == facility).ToList();
            foreach (var flow in flowsToRemove)
            {
                Flows.Remove(flow);
            }

            // Удаляем связанные транспортные единицы
            var transportToRemove = TransportUnits.Where(t => t.Flow.Source == facility || t.Flow.Target == facility).ToList();
            foreach (var transport in transportToRemove)
            {
                TransportUnits.Remove(transport);
            }

            // Удаляем из активных транспортных единиц
            foreach (var entry in _activeVehiclesPerFlow.Where(k => k.Key.Source == facility || k.Key.Target == facility).ToList())
            {
                _activeVehiclesPerFlow.Remove(entry.Key);
            }

            Facilities.Remove(facility);
            _inputBuffer.Remove(facility);

            if (SelectedFacility == facility)
            {
                SelectedFacility = null;
            }

            // Перестраиваем сеть после удаления
            RebuildNetwork();

            RebuildNetwork();
        }

        private void RunProductionFor(EconomyFacilityViewModel facility)
        {
            var blueprint = facility.Blueprint;
            if (blueprint == null) return;
            
            // Если производство выключено, просто обновляем состояние на Idle
            // Добывающие объекты всегда активны (нельзя остановить добычу)
            // Производственные объекты можно останавливать
            if (!facility.IsActive && blueprint.Stage != ProcessingStage.Extraction)
            {
                facility.UpdateState(0, facility.Storage, OperationalState.Idle, 0);
                return;
            }

            double perTick = blueprint.OutputPerMinute / 60.0;
            double capacity = facility.Capacity;
            double storage = facility.Storage;
            double inputBuffer = _inputBuffer.TryGetValue(facility, out var buf) ? buf : 0;

            switch (blueprint.Stage)
            {
                case ProcessingStage.Extraction:
                    {
                        // Добыча происходит ТОЛЬКО если есть ресурс на карте
                        bool hasResourceNow = false;
                        if (_mapModel != null)
                        {
                            var cell = _mapModel.GetCell((int)facility.GridX, (int)facility.GridY);
                            hasResourceNow = cell != null && HasResourceForExtraction(cell, blueprint.Resource);
                            facility.HasResource = hasResourceNow;
                        }
                        else
                        {
                            // Если карта недоступна, используем сохраненное значение
                            hasResourceNow = facility.HasResource;
                        }

                        // Добыча происходит только если есть ресурс
                        double produced = 0;
                        if (hasResourceNow)
                        {
                            produced = Math.Min(perTick, capacity - storage);
                            storage += produced;
                        }
                        
                        // Обновляем состояние: production (в час), storage, state, utilization
                        var newState = hasResourceNow && produced > 0 ? OperationalState.Works : OperationalState.Idle;
                        double utilization = hasResourceNow ? 1.0 : 0.0;
                        facility.UpdateState(produced * 60, storage, newState, utilization);
                        break;
                    }
                case ProcessingStage.Refining:
                case ProcessingStage.Manufacturing:
                    {
                        // Проверяем наличие всех необходимых ресурсов
                        if (!_inputBuffersByResource.ContainsKey(facility))
                            _inputBuffersByResource[facility] = new Dictionary<CoreResource, double>();
                        
                        var inputBuffers = _inputBuffersByResource[facility];
                        
                        // Вычисляем максимальное количество циклов на основе всех входных ресурсов
                        double maxCycles = double.MaxValue;
                        foreach (var inputCost in blueprint.InputCosts)
                        {
                            var resource = inputCost.Key;
                            var requiredPerMinute = inputCost.Value;
                            var requiredPerTick = requiredPerMinute / 60.0;
                            
                            if (!inputBuffers.ContainsKey(resource))
                                inputBuffers[resource] = 0;
                            
                            var available = inputBuffers[resource];
                            if (requiredPerTick > 0)
                            {
                                var cyclesForThisResource = available / requiredPerTick;
                                maxCycles = Math.Min(maxCycles, cyclesForThisResource);
                            }
                        }
                        
                        // Также ограничиваем по вместимости
                        maxCycles = Math.Min(maxCycles, (capacity - storage) / Math.Max(0.001, perTick));
                        maxCycles = Math.Max(0, maxCycles);
                        
                        double produced = maxCycles * perTick;
                        
                        // Потребляем все необходимые ресурсы
                        foreach (var inputCost in blueprint.InputCosts)
                        {
                            var resource = inputCost.Key;
                            var requiredPerMinute = inputCost.Value;
                            var requiredPerTick = requiredPerMinute / 60.0;
                            var consumed = maxCycles * requiredPerTick;
                            
                            if (inputBuffers.ContainsKey(resource))
                            {
                                inputBuffers[resource] = Math.Max(0, inputBuffers[resource] - consumed);
                            }
                        }
                        
                        storage += produced;
                        
                        // Отслеживаем произведенную продукцию
                        var productType = ConvertProduct(blueprint.Resource);
                        if (productType.HasValue && produced > 0)
                        {
                            if (!_producedProducts.ContainsKey(productType.Value))
                                _producedProducts[productType.Value] = 0;
                            _producedProducts[productType.Value] += produced;
                        }
                        
                        // Определяем состояние на основе загрузки
                        double utilization = maxCycles > 0 ? 1.0 : 0.0;
                        facility.UpdateState(produced * 60, Math.Min(capacity, storage), DetermineState(facility, storage), utilization);
                        break;
                    }
                case ProcessingStage.Logistics:
                    // Логистический центр аккумулирует ресурсы
                    facility.UpdateState(0, storage, OperationalState.Works, 1);
                    break;
            }

        }

        private void ExecuteFlows()
        {
            foreach (var flow in Flows)
            {
                var source = flow.Source;
                var target = flow.Target;
                var sourceStorage = source.Storage;
                
                // Если целевой объект - производственный и он неактивен, пропускаем поток
                // Ресурсы останутся в логистическом центре
                if (target.Blueprint?.Stage == ProcessingStage.Manufacturing && !target.IsActive)
                {
                    continue;
                }
                
                // ВАЖНО: проверяем, что поток имеет правильный тип ресурса
                // Если источник - добывающий объект, тип ресурса должен совпадать с ресурсом источника
                if (source.Blueprint?.Stage == ProcessingStage.Extraction && flow.CoreResourceType.HasValue)
                {
                    if (source.Blueprint.Resource != flow.CoreResourceType.Value)
                    {
                        continue; // Пропускаем поток с неправильным типом ресурса
                    }
                }
                
                // Если источник - логистический центр, проверяем наличие ресурса нужного типа
                if (source.Blueprint?.Stage == ProcessingStage.Logistics && flow.CoreResourceType.HasValue)
                {
                    var availableResource = GetHubResource(source, flow.CoreResourceType.Value);
                    if (availableResource <= 0)
                    {
                        continue; // Нет ресурса этого типа в логистическом центре
                    }
                    // Используем доступное количество ресурса, а не общее хранилище
                    sourceStorage = availableResource;
                }
                
                // Если нет ресурсов в источнике, пропускаем поток, но все равно пытаемся создать транспорт для визуализации
                if (sourceStorage <= 0)
                {
                    // Создаем транспорт даже если нет ресурсов, чтобы показать что поток активен
                    if (TransportUnits.Count < MaxVehicles)
                    {
                        var hasEntry = _activeVehiclesPerFlow.TryGetValue(flow, out var active);
                        if (!hasEntry || active < MaxVehiclesPerFlow)
                        {
                            // Создаем транспорт с минимальной нагрузкой для визуализации
                            SpawnTransport(flow, 0.1);
                        }
                    }
                    continue;
                }

                double transferPerTick = Math.Min(sourceStorage, flow.PlannedThroughput / 60.0);
                
                // Обновляем источник (если это не логистический центр)
                if (source.Blueprint?.Stage != ProcessingStage.Logistics)
                {
                    source.UpdateState(source.CurrentProduction, Math.Max(0, sourceStorage - transferPerTick), DetermineState(source, sourceStorage - transferPerTick), source.Utilization);
                }

                if (target.Blueprint?.Stage == ProcessingStage.Logistics)
                {
                    double targetStorage = Math.Min(target.Capacity, target.Storage + transferPerTick);
                    target.UpdateState(transferPerTick, targetStorage, OperationalState.Works, 1);
                    
                    // Отслеживаем ресурсы по типам в логистическом центре
                    // ВАЖНО: используем тип ресурса из потока, а не из Blueprint логистического центра
                    // ДОПОЛНИТЕЛЬНО: проверяем, что источник - это реальный добывающий объект, а не логистический центр
                    if (flow.CoreResourceType.HasValue && source.Blueprint?.Stage == ProcessingStage.Extraction)
                    {
                        // Проверяем, что тип ресурса в потоке совпадает с ресурсом источника
                        if (source.Blueprint.Resource == flow.CoreResourceType.Value)
                        {
                            if (!_hubResources.ContainsKey(target))
                                _hubResources[target] = new Dictionary<CoreResource, double>();
                            
                            if (!_hubResources[target].ContainsKey(flow.CoreResourceType.Value))
                                _hubResources[target][flow.CoreResourceType.Value] = 0;
                            
                            _hubResources[target][flow.CoreResourceType.Value] += transferPerTick;
                        }
                    }
                }
                else
                {
                    // Если ресурсы передаются из логистического центра в производственный объект
                    // вычитаем их из отслеживания ресурсов в логистическом центре
                    if (source.Blueprint?.Stage == ProcessingStage.Logistics && flow.CoreResourceType.HasValue)
                    {
                        if (_hubResources.ContainsKey(source) && 
                            _hubResources[source].ContainsKey(flow.CoreResourceType.Value))
                        {
                            _hubResources[source][flow.CoreResourceType.Value] = 
                                Math.Max(0, _hubResources[source][flow.CoreResourceType.Value] - transferPerTick);
                        }
                    }
                    
                    // Добавляем ресурс в буфер по типу для производственного объекта
                    if (target.Blueprint?.Stage == ProcessingStage.Manufacturing && flow.CoreResourceType.HasValue)
                    {
                        if (!_inputBuffersByResource.ContainsKey(target))
                            _inputBuffersByResource[target] = new Dictionary<CoreResource, double>();
                        
                        if (!_inputBuffersByResource[target].ContainsKey(flow.CoreResourceType.Value))
                            _inputBuffersByResource[target][flow.CoreResourceType.Value] = 0;
                        
                        _inputBuffersByResource[target][flow.CoreResourceType.Value] += transferPerTick;
                    }
                    else
                    {
                        // Для других объектов используем старую систему
                        _inputBuffer[target] = _inputBuffer.TryGetValue(target, out var buf)
                            ? buf + transferPerTick
                            : transferPerTick;
                    }
                }

                flow.RegisterThroughput(transferPerTick * 60);
                SpawnTransport(flow, transferPerTick * 60);
            }
        }

        private void SpawnTransport(ResourceFlowViewModel flow, double payloadPerHour)
        {
            if (payloadPerHour <= 0.1) return;
            if (TransportUnits.Count >= MaxVehicles) return;

            var hasEntry = _activeVehiclesPerFlow.TryGetValue(flow, out var active);
            if (hasEntry && active >= MaxVehiclesPerFlow) return;

            TransportUnits.Add(new TransportUnitViewModel(flow, flow.Mode, payloadPerHour));
            _activeVehiclesPerFlow[flow] = hasEntry ? active + 1 : 1;
        }

        private void AnimateTransport()
        {
            var completed = new List<TransportUnitViewModel>();
            foreach (var unit in TransportUnits)
            {
                unit.Advance();
                if (unit.HasArrived)
                {
                    completed.Add(unit);
                }
            }

            foreach (var unit in completed)
            {
                TransportUnits.Remove(unit);
                if (_activeVehiclesPerFlow.TryGetValue(unit.Flow, out var active) && active > 0)
                {
                    if (active == 1)
                    {
                        _activeVehiclesPerFlow.Remove(unit.Flow);
                    }
                    else
                    {
                        _activeVehiclesPerFlow[unit.Flow] = active - 1;
                    }
                }
            }
        }

        private void DispatchBulkFleet()
        {
            foreach (var flow in Flows)
            {
                SpawnTransport(flow, flow.PlannedThroughput);
            }
        }

        private void TogglePause()
        {
            IsPaused = !IsPaused;
        }

        private void UpdateStats()
        {
            // Показываем произведенную продукцию вместо добытых ресурсов
            // Маппинг ProductType -> CoreResource для отображения
            var productToResourceMap = new Dictionary<ProductType, CoreResource>
            {
                { ProductType.Steel, CoreResource.Iron }, // Сталь отображается как железо
                { ProductType.EngineeredWood, CoreResource.Wood }, // Доски отображаются как дерево
                { ProductType.Conductors, CoreResource.Copper }, // Провода отображаются как медь
                { ProductType.Fuel, CoreResource.Oil }, // Топливо отображается как нефть
                { ProductType.Chemicals, CoreResource.Oil }, // Химикаты отображаются как нефть
                { ProductType.Coolant, CoreResource.Water } // Охладитель отображается как вода
            };
            
            // Создаем словарь произведенной продукции по CoreResource
            var producedByResource = new Dictionary<CoreResource, double>();
            foreach (var product in _producedProducts)
            {
                if (productToResourceMap.TryGetValue(product.Key, out var resource))
                {
                    if (!producedByResource.ContainsKey(resource))
                        producedByResource[resource] = 0;
                    producedByResource[resource] += product.Value;
                }
            }
            
            // Для угля показываем добытое количество (уголь не производится, только добывается)
            // Суммируем storage всех добывающих объектов угля
            var coalExtraction = Facilities
                .Where(f => f.Blueprint != null && 
                           f.Blueprint.Stage == ProcessingStage.Extraction &&
                           f.Blueprint.Resource == CoreResource.Coal)
                .Sum(f => f.Storage);
            producedByResource[CoreResource.Coal] = coalExtraction;
            
            // Обновляем статистики на основе произведенной продукции
            foreach (var statPair in _resourceStatsIndex)
            {
                producedByResource.TryGetValue(statPair.Key, out var total);
                statPair.Value.PushSample(total);
            }
        }

        private void UpdateIndicator()
        {
            double flowStability = Flows.Any() ? Flows.Average(f => f.Stability) : 0;
            double manufacturingTotal = Facilities.Count(f => f.Blueprint?.Stage == ProcessingStage.Manufacturing);
            double manufacturingWorking = Facilities.Count(f => f.Blueprint?.Stage == ProcessingStage.Manufacturing && f.State == OperationalState.Works);
            double utilization = manufacturingTotal == 0 ? 0 : manufacturingWorking / manufacturingTotal;
            EconomicIndicator = 60 * utilization + 40 * flowStability;
        }

        private OperationalState DetermineState(EconomyFacilityViewModel facility, double storage)
        {
            var ratio = storage / Math.Max(1, facility.Capacity);
            if (ratio >= 0.85) return OperationalState.Overloaded;
            if (ratio <= 0.15) return OperationalState.Idle;
            return OperationalState.Works;
        }

        private void UpdateBalanceSummary()
        {
            var busiest = Flows.OrderByDescending(f => f.CurrentThroughput).FirstOrDefault();
            string busiestText = busiest != null
                ? $"{busiest.Label}: {busiest.CurrentThroughput:0.0} т/ч"
                : "Нет активных потоков";

            var bottleneck = Facilities
                .Where(f => f.Blueprint?.Stage != ProcessingStage.Logistics)
                .OrderByDescending(f => f.StorageUsage)
                .FirstOrDefault();

            string bottleneckText = bottleneck != null
                ? $"{bottleneck.Name}: {(bottleneck.StorageUsage * 100):0}% заполн."
                : "Узкое место не найдено";

            BalanceSummary = $"{busiestText} • {bottleneckText}";
        }

        public void RebuildNetwork()
        {
            RoadConnections.Clear();
            Flows.Clear();
            TransportUnits.Clear();
            _activeVehiclesPerFlow.Clear();
            // Очищаем отслеживание ресурсов в логистических центрах при перестройке сети
            _hubResources.Clear();

            var hubs = Facilities.Where(f => f.Blueprint?.Stage == ProcessingStage.Logistics).ToList();

            foreach (var facility in Facilities)
            {
                if (facility.Blueprint?.Stage == ProcessingStage.Logistics) continue;
                var closestHub = hubs.OrderBy(h => Distance(facility, h)).FirstOrDefault();
                if (closestHub != null)
                {
                    RoadConnections.Add(new RoadConnectionViewModel
                    {
                        Start = facility.CenterPixel,
                        End = closestHub.CenterPixel,
                        Stroke = Brushes.DimGray,
                        Thickness = 2.2
                    });
                }
            }

            foreach (CoreResource resource in Enum.GetValues(typeof(CoreResource)))
            {
                // Исключаем логистические центры из группировки - они не должны влиять на создание потоков
                // ВАЖНО: проверяем, что объект действительно является добывающим или производственным
                var stageGroups = Facilities
                    .Where(f => f.Blueprint != null && 
                                f.Blueprint.Resource == resource && 
                                f.Blueprint.Stage != ProcessingStage.Logistics &&
                                (f.Blueprint.Stage == ProcessingStage.Extraction || f.Blueprint.Stage == ProcessingStage.Manufacturing))
                    .GroupBy(f => f.Blueprint!.Stage)
                    .ToDictionary(g => g.Key, g => g.OrderBy(f => f.GridX + f.GridY).ToList());

                // Потоки создаются ТОЛЬКО если есть добывающие объекты для этого ресурса
                if (!stageGroups.TryGetValue(ProcessingStage.Extraction, out var extraction)) continue;
                
                // Дополнительная проверка: должны быть реальные добывающие объекты
                if (extraction.Count == 0) continue;
                
                // Дополнительная проверка: все добывающие объекты должны иметь правильный ресурс
                if (extraction.Any(e => e.Blueprint?.Resource != resource)) continue;

                // Создаем потоки от добывающих объектов к логистическому центру
                // Добывающие объекты ВСЕГДА отправляют ресурсы в логистический центр
                if (hubs.Count > 0)
                {
                    foreach (var source in extraction)
                    {
                        var hub = hubs.OrderBy(h => Distance(source, h)).First();
                        double throughput = source.Blueprint?.OutputPerMinute ?? 10;
                        
                        // Проверяем, нет ли уже такого потока
                        var existingFlow = Flows.FirstOrDefault(f => 
                            f.Source == source && f.Target == hub && f.CoreResourceType == resource);
                        
                        if (existingFlow == null)
                        {
                            Flows.Add(new ResourceFlowViewModel(
                                source,
                                hub,
                                throughput,
                                TransportMode.Truck,
                                $"{resource} → хаб",
                                ConvertResource(resource),
                                coreResource: resource,
                                roadPath: BuildRoadPath(source, hub)));
                        }
                    }
                }

                // Упрощенная система: добыча -> производство (без переработки)
                if (stageGroups.TryGetValue(ProcessingStage.Manufacturing, out var manufacturing))
                {
                    // Создаем потоки для основного ресурса для ВСЕХ производственных объектов
                    // Каждый производственный объект должен получать ресурсы
                    foreach (var manufacturingFacility in manufacturing)
                    {
                        // Проверяем, что этот производственный объект действительно использует текущий ресурс
                        if (manufacturingFacility.Blueprint?.Resource != resource) continue;
                        
                        // Создаем потоки от всех добывающих объектов к этому производственному объекту
                        if (hubs.Count > 0)
                        {
                            foreach (var extractSource in extraction)
                            {
                                var hub = hubs.OrderBy(h => Distance(extractSource, h) + Distance(h, manufacturingFacility)).First();
                                
                                // Поток от добывающего объекта к логистическому центру (если еще не создан)
                                var existingExtractToHubFlow = Flows.FirstOrDefault(f => 
                                    f.Source == extractSource && f.Target == hub && f.CoreResourceType == resource);
                                
                                if (existingExtractToHubFlow == null)
                                {
                                    double throughput = extractSource.Blueprint?.OutputPerMinute ?? 10;
                                    Flows.Add(new ResourceFlowViewModel(
                                        extractSource,
                                        hub,
                                        throughput,
                                        TransportMode.Truck,
                                        $"{resource} → хаб",
                                        ConvertResource(resource),
                                        coreResource: resource,
                                        roadPath: BuildRoadPath(extractSource, hub)));
                                }
                                
                                // Поток от логистического центра к производственному объекту
                                // Для каждого производственного объекта создаем отдельный поток
                                var existingHubToManufacturingFlow = Flows.FirstOrDefault(f =>
                                    f.Source == hub && f.Target == manufacturingFacility &&
                                    f.CoreResourceType == resource);
                                
                                if (existingHubToManufacturingFlow == null)
                                {
                                    // Пропускная способность потока = потребность производственного объекта
                                    // Это позволяет каждому объекту получать нужное количество ресурсов
                                    double requiredPerMinute = manufacturingFacility.Blueprint?.InputCosts?.TryGetValue(resource, out var req) == true 
                                        ? req 
                                        : extractSource.Blueprint?.OutputPerMinute ?? 10;
                                    
                                    var product = ConvertProduct(resource);
                                    Flows.Add(new ResourceFlowViewModel(
                                        hub,
                                        manufacturingFacility,
                                        requiredPerMinute,
                                        TransportMode.Truck,
                                        $"Хаб → {manufacturingFacility.Name}",
                                        ConvertResource(resource),
                                        product: product,
                                        coreResource: resource,
                                        roadPath: BuildRoadPath(hub, manufacturingFacility)));
                                }
                            }
                        }
                    }
                    
                    // Создаем потоки для всех дополнительных ресурсов, необходимых для производства
                    // ТОЛЬКО для производственных объектов, которые используют текущий ресурс как основной
                    foreach (var manufacturingFacility in manufacturing)
                    {
                        // Проверяем, что этот производственный объект действительно использует текущий ресурс
                        if (manufacturingFacility.Blueprint?.Resource != resource) continue;
                        
                        if (manufacturingFacility.Blueprint?.InputCosts != null)
                        {
                            foreach (var inputCost in manufacturingFacility.Blueprint.InputCosts)
                            {
                                var requiredResource = inputCost.Key;
                                // Пропускаем основной ресурс, для него поток уже создан
                                if (requiredResource == resource) continue;
                                
                                // Находим добывающие объекты для этого ресурса
                                var requiredExtraction = Facilities
                                    .Where(f => f.Blueprint?.Resource == requiredResource && 
                                                f.Blueprint?.Stage == ProcessingStage.Extraction)
                                    .ToList();
                                
                                if (requiredExtraction.Count > 0 && hubs.Count > 0)
                                {
                                    // Создаем потоки от добывающих объектов к логистическому центру
                                    foreach (var extractSource in requiredExtraction)
                                    {
                                        var hub = hubs.OrderBy(h => Distance(extractSource, h)).First();
                                        double throughput = extractSource.Blueprint?.OutputPerMinute ?? 10;
                                        
                                        var existingFlow = Flows.FirstOrDefault(f => 
                                            f.Source == extractSource && f.Target == hub && 
                                            f.CoreResourceType == requiredResource);
                                        
                                        if (existingFlow == null)
                                        {
                                            Flows.Add(new ResourceFlowViewModel(
                                                extractSource,
                                                hub,
                                                throughput,
                                                TransportMode.Truck,
                                                $"{requiredResource} → хаб",
                                                ConvertResource(requiredResource),
                                                coreResource: requiredResource,
                                                roadPath: BuildRoadPath(extractSource, hub)));
                                        }
                                        
                                        // Создаем поток от логистического центра к производственному объекту
                                        // Для каждого производственного объекта создаем отдельный поток
                                        var existingHubToManufacturingFlow = Flows.FirstOrDefault(f =>
                                            f.Source == hub && f.Target == manufacturingFacility &&
                                            f.CoreResourceType == requiredResource);
                                        
                                        if (existingHubToManufacturingFlow == null)
                                        {
                                            // Пропускная способность потока = потребность производственного объекта
                                            double requiredPerMinute = manufacturingFacility.Blueprint?.InputCosts?.TryGetValue(requiredResource, out var req) == true 
                                                ? req 
                                                : throughput;
                                            
                                            Flows.Add(new ResourceFlowViewModel(
                                                hub,
                                                manufacturingFacility,
                                                requiredPerMinute,
                                                TransportMode.Truck,
                                                $"Хаб → {manufacturingFacility.Name} ({requiredResource})",
                                                ConvertResource(requiredResource),
                                                coreResource: requiredResource,
                                                roadPath: BuildRoadPath(hub, manufacturingFacility)));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CreateFlows(
            List<EconomyFacilityViewModel> fromList,
            List<EconomyFacilityViewModel> toList,
            CoreResource resource,
            List<EconomyFacilityViewModel> hubs,
            bool finalStage = false)
        {
            foreach (var source in fromList)
            {
                var target = toList.OrderBy(t => Distance(source, t)).FirstOrDefault();
                if (target == null) continue;

                double throughput = source.Blueprint?.OutputPerMinute ?? 10;
                if (hubs.Count > 0)
                {
                    var hub = hubs.OrderBy(h => Distance(source, h) + Distance(h, target)).First();
                    var product = finalStage ? ConvertProduct(resource) : null;
                    Flows.Add(new ResourceFlowViewModel(
                        source,
                        hub,
                        throughput,
                        TransportMode.Truck,
                        $"{resource} → хаб",
                        ConvertResource(resource),
                        coreResource: resource,
                        roadPath: BuildRoadPath(source, hub)));
                    Flows.Add(new ResourceFlowViewModel(
                        hub,
                        target,
                        throughput,
                        TransportMode.Truck,
                        $"Хаб → {target.Name}",
                        ConvertResource(resource),
                        product: product,
                        coreResource: resource,
                        roadPath: BuildRoadPath(hub, target)));
                }
                else
                {
                    var product = finalStage ? ConvertProduct(resource) : null;
                    Flows.Add(new ResourceFlowViewModel(
                        source,
                        target,
                        throughput,
                        TransportMode.Truck,
                        $"{resource} поток",
                        ConvertResource(resource),
                        product: product,
                        coreResource: resource,
                        roadPath: BuildRoadPath(source, target)));
                }
            }
        }

        private void SeedInitialLayout()
        {
            // Только логистический центр - остальное игрок строит сам
            var hub = PlaceBuilding("logistics-hub", 16, 63);
            hub.IsDraggable = false;
        }

        public bool TryPlaceBuilding(string blueprintId, int gridX, int gridY)
        {
            if (!_blueprints.TryGetValue(blueprintId, out var blueprint))
            {
                System.Diagnostics.Debug.WriteLine($"Blueprint '{blueprintId}' не найден");
                MessageBox.Show($"Объект '{blueprintId}' не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            gridX = Math.Clamp(gridX, 0, _mapWidth - 1);
            gridY = Math.Clamp(gridY, 0, _mapHeight - 1);

            System.Diagnostics.Debug.WriteLine($"Попытка разместить {blueprint.DisplayName} на [{gridX}, {gridY}], Stage={blueprint.Stage}, Resource={blueprint.Resource}");

            // Для добывающих объектов - БЛОКИРУЕМ размещение без ресурса
            if (blueprint.Stage == ProcessingStage.Extraction)
            {
                if (_mapModel == null)
                {
                    System.Diagnostics.Debug.WriteLine($"ОШИБКА: MapModel = null!");
                    MessageBox.Show("Карта не загружена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Получаем клетку - координаты уже правильно преобразованы в CanvasToGrid
                System.Diagnostics.Debug.WriteLine($"Попытка получить клетку: GetCell({gridX}, {gridY})");
                var cell = _mapModel.GetCell(gridX, gridY);
                System.Diagnostics.Debug.WriteLine($"GetCell({gridX}, {gridY}) вернул клетку [{cell?.X}, {cell?.Y}]");
                
                if (cell == null)
                {
                    MessageBox.Show($"Клетка [{gridX}, {gridY}] не найдена на карте", "Ошибка размещения", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                
                // Используем координаты из найденной клетки для размещения
                System.Diagnostics.Debug.WriteLine($"Используем клетку [{cell.X}, {cell.Y}] для размещения {blueprint.DisplayName}");
                // Обновляем gridX и gridY на правильные координаты из найденной клетки
                gridX = cell.X;
                gridY = cell.Y;
                
                System.Diagnostics.Debug.WriteLine($"Клетка найдена: X={cell.X}, Y={cell.Y}, TerrainType={cell.TerrainType}, ResourceType={cell.ResourceType}");
                
                bool hasResource = HasResourceForExtraction(cell, blueprint.Resource);
                System.Diagnostics.Debug.WriteLine($"HasResourceForExtraction({blueprint.Resource}) = {hasResource}");
                
                if (!hasResource)
                {
                    // Нельзя разместить добывающий объект без ресурса
                    var msg = $"Нельзя разместить {blueprint.DisplayName} на этой клетке!\n\nКлетка [{gridX}, {gridY}]:\nТип местности: {GetTerrainName(cell.TerrainType)}\nРесурс: {GetResourceName(cell.ResourceType)}\n\nТребуется:\n{GetRequiredResourceInfo(blueprint.Resource)}";
                    MessageBox.Show(msg, "Невозможно разместить", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine($"✓ Ресурс найден для {blueprint.DisplayName} на [{gridX}, {gridY}]");
            }

            // Проверяем, нет ли уже объекта на этой клетке
            var existingFacility = Facilities.FirstOrDefault(f => 
                Math.Abs(f.GridX - gridX) < 0.5 && Math.Abs(f.GridY - gridY) < 0.5);
            if (existingFacility != null)
            {
                MessageBox.Show($"На этой клетке уже есть объект: {existingFacility.Name}", "Невозможно разместить", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Размещаем объект - все объекты бесплатные для упрощения игры
            PlaceBuilding(blueprintId, gridX, gridY);
            System.Diagnostics.Debug.WriteLine($"✓ Объект {blueprint.DisplayName} успешно размещен на [{gridX}, {gridY}]");
            return true;
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

        private string GetCoreResourceName(CoreResource resource)
        {
            return resource switch
            {
                CoreResource.Wood => "Дерево",
                CoreResource.Iron => "Железо",
                CoreResource.Copper => "Медь",
                CoreResource.Oil => "Нефть",
                CoreResource.Coal => "Уголь",
                CoreResource.Water => "Вода",
                _ => "Неизвестный ресурс"
            };
        }

        private string GetRequiredResourceInfo(CoreResource resource)
        {
            return resource switch
            {
                CoreResource.Wood => "• Лес (тип местности)",
                CoreResource.Iron => "• Горы с металлами",
                CoreResource.Copper => "• Горы или водоемы с металлами",
                CoreResource.Oil => "• Горы или водоемы с нефтью",
                CoreResource.Coal => "• Горы (тип местности)",
                CoreResource.Water => "• Водоем (тип местности)",
                _ => "Неизвестный ресурс"
            };
        }


        private EconomyFacilityViewModel PlaceBuilding(string blueprintId, int gridX, int gridY)
        {
            if (!_blueprints.TryGetValue(blueprintId, out var blueprint))
            {
                throw new InvalidOperationException($"Blueprint '{blueprintId}' not found");
            }

            gridX = Math.Clamp(gridX, 0, _mapWidth - 1);
            gridY = Math.Clamp(gridY, 0, _mapHeight - 1);

            bool hasResource = true;
            // Проверяем ресурсы для добывающих объектов (Extraction)
            // Перерабатывающие объекты (Manufacturing) могут быть в любом месте
            if (blueprint.Stage == ProcessingStage.Extraction && _mapModel != null)
            {
                var cell = _mapModel.GetCell(gridX, gridY);
                hasResource = cell != null && HasResourceForExtraction(cell, blueprint.Resource);
            }

            var nodeType = blueprint.Stage switch
            {
                ProcessingStage.Extraction => EconomicNodeType.ResourceSite,
                ProcessingStage.Refining => EconomicNodeType.Warehouse,
                ProcessingStage.Manufacturing => EconomicNodeType.Factory,
                ProcessingStage.Logistics => EconomicNodeType.TransportHub,
                _ => EconomicNodeType.ResourceSite
            };

            var facility = new EconomyFacilityViewModel(
                $"facility-{Guid.NewGuid():N}",
                blueprint.DisplayName,
                nodeType,
                new Point(gridX, gridY),
                blueprint.OutputPerMinute,
                blueprint.IsLogisticsHub ? 600 : 320,
                blueprint.Description,
                commodity: ConvertResource(blueprint.Resource))
            {
                // Добывающие объекты нельзя перемещать - они должны быть на ресурсе
                IsDraggable = !blueprint.IsLogisticsHub && blueprint.Stage != ProcessingStage.Extraction,
                HasResource = hasResource // Сохраняем информацию о наличии ресурса
            };

            facility.AttachBlueprint(blueprint);
            
            // Для добывающих объектов проверяем ресурс и сразу начинаем добычу
            if (blueprint.Stage == ProcessingStage.Extraction && _mapModel != null)
            {
                var finalCell = _mapModel.GetCell((int)facility.GridX, (int)facility.GridY);
                bool hasResourceFinal = finalCell != null && HasResourceForExtraction(finalCell, blueprint.Resource);
                facility.HasResource = hasResourceFinal;
                
                // Если есть ресурс - сразу начинаем добычу (устанавливаем состояние "Работает")
                if (hasResourceFinal)
                {
                    // Начинаем добычу сразу - устанавливаем начальное состояние "Работает"
                    // production = 0 (пока ничего не добыто), storage = 0, state = Works, utilization = 1.0
                    facility.UpdateState(0, 0, OperationalState.Works, 1.0);
                }
                else
                {
                    facility.UpdateState(0, 0, OperationalState.Idle, 0);
                }
            }
            else
            {
                facility.UpdateState(0, 0, OperationalState.Idle, 0);
            }
            
            Facilities.Add(facility);
            _inputBuffer[facility] = 0;
            if (facility.Blueprint?.Stage == ProcessingStage.Manufacturing)
            {
                _inputBuffersByResource[facility] = new Dictionary<CoreResource, double>();
            }
            
            // Перестраиваем сеть после добавления нового объекта, чтобы создать потоки для него
            RebuildNetwork();
            
            return facility;
        }

        private IEnumerable<BuildingBlueprint> BuildBlueprintCatalog()
        {
            // Упрощенная система: добыча -> производство (без промежуточной переработки)
            
            // Wood - добыча и производство
            // Добыча: 30 ед/мин, Производство: 10 ед/мин (соотношение 3:1)
            yield return new BuildingBlueprint { Id = "wood-extract", DisplayName = "Лесозаготовка", Resource = CoreResource.Wood, Stage = ProcessingStage.Extraction, OutputPerMinute = 30, Description = "Добыча древесины", IconGlyph = "🌲" };
            yield return new BuildingBlueprint { Id = "wood-manufacture", DisplayName = "Панельный завод", Resource = CoreResource.Wood, Stage = ProcessingStage.Manufacturing, OutputPerMinute = 10, Description = "Производство досок", IconGlyph = "🏗", InputCosts = new Dictionary<CoreResource, double> { { CoreResource.Wood, 30 } } };

            // Iron - добыча и производство
            // Добыча: 30 ед/мин железа, Производство: 10 ед/мин стали (2 части железа + 1 часть угля = 1 часть стали)
            yield return new BuildingBlueprint { Id = "iron-extract", DisplayName = "Железный рудник", Resource = CoreResource.Iron, Stage = ProcessingStage.Extraction, OutputPerMinute = 30, Description = "Добыча железной руды", IconGlyph = "⛏" };
            yield return new BuildingBlueprint { Id = "iron-manufacture", DisplayName = "Металлургический комбинат", Resource = CoreResource.Iron, Stage = ProcessingStage.Manufacturing, OutputPerMinute = 10, Description = "Производство стали", IconGlyph = "⚙", InputCosts = new Dictionary<CoreResource, double> { { CoreResource.Iron, 20 }, { CoreResource.Coal, 10 } } };

            // Copper - добыча и производство
            // Добыча: 30 ед/мин, Производство: 10 ед/мин (соотношение 3:1)
            yield return new BuildingBlueprint { Id = "copper-extract", DisplayName = "Медный карьер", Resource = CoreResource.Copper, Stage = ProcessingStage.Extraction, OutputPerMinute = 30, Description = "Добыча медной руды", IconGlyph = "⛏" };
            yield return new BuildingBlueprint { Id = "copper-manufacture", DisplayName = "Завод проводов", Resource = CoreResource.Copper, Stage = ProcessingStage.Manufacturing, OutputPerMinute = 10, Description = "Производство проводов", IconGlyph = "🔌", InputCosts = new Dictionary<CoreResource, double> { { CoreResource.Copper, 30 } } };

            // Oil - добыча и производство
            // Добыча: 30 ед/мин, Производство: 10 ед/мин (соотношение 3:1)
            yield return new BuildingBlueprint { Id = "oil-extract", DisplayName = "Нефтяной промысел", Resource = CoreResource.Oil, Stage = ProcessingStage.Extraction, OutputPerMinute = 30, Description = "Добыча нефти", IconGlyph = "🛢" };
            yield return new BuildingBlueprint { Id = "oil-manufacture", DisplayName = "Химический завод", Resource = CoreResource.Oil, Stage = ProcessingStage.Manufacturing, OutputPerMinute = 10, Description = "Производство химикатов", IconGlyph = "🧪", InputCosts = new Dictionary<CoreResource, double> { { CoreResource.Oil, 30 } } };

            // Coal - только добыча (используется для производства стали)
            yield return new BuildingBlueprint { Id = "coal-extract", DisplayName = "Угольный разрез", Resource = CoreResource.Coal, Stage = ProcessingStage.Extraction, OutputPerMinute = 30, Description = "Добыча угля", IconGlyph = "⛏" };

            // Water - только добыча (убрана очистка и технопарк)
            yield return new BuildingBlueprint { Id = "water-extract", DisplayName = "Водозабор", Resource = CoreResource.Water, Stage = ProcessingStage.Extraction, OutputPerMinute = 30, Description = "Забор воды", IconGlyph = "💧" };

            // Logistics
            // Логистический центр не должен иметь конкретный ресурс - он хранит все ресурсы
            // Используем CoreResource.Wood как значение по умолчанию, но это не влияет на функциональность
            yield return new BuildingBlueprint { Id = "logistics-hub", DisplayName = "Логистический центр", Resource = CoreResource.Wood, Stage = ProcessingStage.Logistics, OutputPerMinute = 0, Description = "Оптимизация потоков", IconGlyph = "⚙" };
        }

        private IEnumerable<ResourceStatViewModel> CreateStats()
        {
            // Отображаем произведенную продукцию вместо добытых ресурсов
            // Используем CoreResource для совместимости, но показываем названия продукции
            yield return new ResourceStatViewModel(CoreResource.Wood, "Доски", Color.FromRgb(120, 170, 110)); // Доски (EngineeredWood)
            yield return new ResourceStatViewModel(CoreResource.Iron, "Сталь", Color.FromRgb(190, 190, 190)); // Сталь (Steel)
            yield return new ResourceStatViewModel(CoreResource.Copper, "Провода", Color.FromRgb(210, 140, 90)); // Провода (Conductors)
            yield return new ResourceStatViewModel(CoreResource.Oil, "Топливо", Color.FromRgb(80, 100, 150)); // Топливо/Химикаты (Fuel/Chemicals)
            yield return new ResourceStatViewModel(CoreResource.Coal, "Уголь", Color.FromRgb(70, 70, 70)); // Уголь (не производится, только добывается)
            yield return new ResourceStatViewModel(CoreResource.Water, "Охладитель", Color.FromRgb(90, 150, 220)); // Охладитель (Coolant)
        }

        private IReadOnlyList<Point> BuildRoadPath(EconomyFacilityViewModel source, EconomyFacilityViewModel target)
        {
            var pathPoints = new List<Point> { source.CenterPixel };

            if (_pathFinder == null || _roadCoordinates.Count == 0)
            {
                pathPoints.Add(target.CenterPixel);
                return pathPoints;
            }

            var startCell = SnapToRoadCell((int)Math.Round(source.GridX), (int)Math.Round(source.GridY));
            var endCell = SnapToRoadCell((int)Math.Round(target.GridX), (int)Math.Round(target.GridY));

            var gridPath = _pathFinder.FindPath(startCell.X, startCell.Y, endCell.X, endCell.Y);
            if (gridPath.Count == 0)
            {
                pathPoints.Add(target.CenterPixel);
                return pathPoints;
            }

            foreach (var cell in gridPath)
            {
                pathPoints.Add(ToPixel(cell.X, cell.Y));
            }

            pathPoints.Add(target.CenterPixel);
            return pathPoints;
        }

        private (int X, int Y) SnapToRoadCell(int startX, int startY)
        {
            int clampedX = Math.Clamp(startX, 0, _mapWidth - 1);
            int clampedY = Math.Clamp(startY, 0, _mapHeight - 1);

            if (_roadCoordinates.Contains((clampedX, clampedY)))
            {
                return (clampedX, clampedY);
            }

            var visited = new HashSet<(int, int)>();
            var queue = new Queue<((int x, int y) point, int distance)>();
            queue.Enqueue(((clampedX, clampedY), 0));
            visited.Add((clampedX, clampedY));

            var directions = new (int dx, int dy)[] { (-1, 0), (1, 0), (0, -1), (0, 1) };

            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                if (item.distance > MaxRoadSearchRadius)
                {
                    break;
                }

                foreach (var (dx, dy) in directions)
                {
                    int nx = item.point.x + dx;
                    int ny = item.point.y + dy;

                    if (nx < 0 || ny < 0 || nx >= _mapWidth || ny >= _mapHeight)
                    {
                        continue;
                    }

                    if (!visited.Add((nx, ny)))
                    {
                        continue;
                    }

                    if (_roadCoordinates.Contains((nx, ny)))
                    {
                        return (nx, ny);
                    }

                    queue.Enqueue(((nx, ny), item.distance + 1));
                }
            }

            return (clampedX, clampedY);
        }

        private bool HasResourceForExtraction(CellViewModel cell, CoreResource requiredResource)
        {
            if (cell == null) return false;

            // Упрощенный и четкий маппинг CoreResource на TerrainType и ResourceType карты
            // Приоритет отдается типу местности для основных ресурсов
            return requiredResource switch
            {
                // Дерево - в лесах (тип местности Forest - это и есть ресурс)
                CoreResource.Wood => cell.TerrainType == (byte)TerrainType.Forest,
                
                // Железо - металлы в горах (нужен явный ResourceType.Metals)
                CoreResource.Iron => cell.TerrainType == (byte)TerrainType.Mountains && cell.ResourceType == (byte)ResourceType.Metals,
                
                // Медь - металлы в горах или водоемах (нужен явный ResourceType.Metals)
                CoreResource.Copper => cell.ResourceType == (byte)ResourceType.Metals && 
                                      (cell.TerrainType == (byte)TerrainType.Mountains || cell.TerrainType == (byte)TerrainType.Water),
                
                // Нефть - в горах или водоемах с нефтью (нужен явный ResourceType.Oil)
                CoreResource.Oil => cell.ResourceType == (byte)ResourceType.Oil && 
                                   (cell.TerrainType == (byte)TerrainType.Mountains || cell.TerrainType == (byte)TerrainType.Water),
                
                // Уголь - в горах (тип местности Mountains - это и есть ресурс)
                CoreResource.Coal => cell.TerrainType == (byte)TerrainType.Mountains,
                
                // Вода - только в водоемах (тип местности Water - это и есть ресурс)
                CoreResource.Water => cell.TerrainType == (byte)TerrainType.Water,
                
                _ => false
            };
        }

        public bool CanPlaceExtractionAt(int x, int y, CoreResource resource)
        {
            if (_mapModel == null) return true; // Если карта недоступна, разрешаем
            var cell = _mapModel.GetCell(x, y);
            return cell != null && HasResourceForExtraction(cell, resource);
        }

        private Point ToPixel(int gridX, int gridY) => new(gridX * _cellPixelSize + _cellPixelSize / 2, gridY * _cellPixelSize + _cellPixelSize / 2);

        private CommodityType ConvertResource(CoreResource resource) => resource switch
        {
            CoreResource.Wood => CommodityType.Timber,
            CoreResource.Iron => CommodityType.IronOre,
            CoreResource.Copper => CommodityType.Metals,
            CoreResource.Oil => CommodityType.CrudeOil,
            CoreResource.Coal => CommodityType.Coal,
            CoreResource.Water => CommodityType.FreshWater,
            _ => CommodityType.Timber
        };

        public ProductType? ConvertProduct(CoreResource resource) => resource switch
        {
            CoreResource.Wood => ProductType.EngineeredWood,
            CoreResource.Iron => ProductType.Steel,
            CoreResource.Oil => ProductType.Fuel,
            CoreResource.Coal => ProductType.Steel, // Убрана энергия, уголь используется для стали
            CoreResource.Copper => ProductType.Conductors,
            CoreResource.Water => ProductType.Coolant,
            _ => null
        };

        private double Distance(EconomyFacilityViewModel a, EconomyFacilityViewModel b)
        {
            var dx = a.GridX - b.GridX;
            var dy = a.GridY - b.GridY;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

