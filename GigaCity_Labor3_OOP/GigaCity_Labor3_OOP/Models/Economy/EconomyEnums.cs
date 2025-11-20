namespace GigaCity_Labor3_OOP.Models.Economy
{
    /// <summary>
    /// Базовые типы ресурсов, задействованных в цепочке добычи природных ресурсов (вариант 18).
    /// </summary>
    public enum CommodityType
    {
        IronOre,
        Coal,
        Timber,
        CrudeOil,
        FreshWater,
        Metals
    }

    /// <summary>
    /// Типы готовой продукции производственных цехов (вариант 21).
    /// </summary>
    public enum ProductType
    {
        Steel,
        EngineeredWood,
        Fuel,
        Chemicals,
        Conductors,
        Energy,
        Coolant
    }

    /// <summary>
    /// Зоны городской экономики для визуализации на карте.
    /// </summary>
    public enum EconomicNodeType
    {
        ResourceSite,
        Warehouse,
        Factory,
        Residential,
        TransportHub
    }

    /// <summary>
    /// Специализация производственного цеха.
    /// </summary>
    public enum FactorySpecialization
    {
        Metallurgical,
        WoodProcessing,
        Chemical
    }

    /// <summary>
    /// Состояние объекта: работает, простаивает, перегружен.
    /// </summary>
    public enum OperationalState
    {
        Works,
        Idle,
        Overloaded
    }

    /// <summary>
    /// Тип транспортного коридора.
    /// </summary>
    public enum TransportMode
    {
        Truck,
        Train
    }
}

