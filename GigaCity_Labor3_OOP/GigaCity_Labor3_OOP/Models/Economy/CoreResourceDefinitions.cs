using System.Collections.Generic;

namespace GigaCity_Labor3_OOP.Models.Economy
{
    public enum CoreResource
    {
        Wood,
        Iron,
        Copper,
        Oil,
        Coal,
        Water
    }

    public enum ProcessingStage
    {
        Extraction,
        Refining,
        Manufacturing,
        Logistics
    }

    public class ResourceCycleStep
    {
        public CoreResource Resource { get; init; }
        public ProcessingStage Stage { get; init; }
        public string OutputName { get; init; } = string.Empty;
        public IReadOnlyDictionary<CoreResource, double> InputCosts { get; init; } = new Dictionary<CoreResource, double>();
        public double OutputPerMinute { get; init; }
    }

    public class BuildingBlueprint
    {
        public string Id { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public CoreResource Resource { get; init; }
        public ProcessingStage Stage { get; init; }
        public double OutputPerMinute { get; init; }
        public IReadOnlyDictionary<CoreResource, double> InputCosts { get; init; } = new Dictionary<CoreResource, double>();
        public bool IsLogisticsHub => Stage == ProcessingStage.Logistics;
        public string IconGlyph { get; init; } = "◎";
    }
}

