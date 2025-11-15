namespace CitySimulation.Models.Resources
{
    public enum ResourceType
    {
        Oil,
        Gas,
        Coal,
        Iron,
        Copper,
        Gold,
        Water
    }

    public static class ResourceTypeExtensions
    {
        public static string ToRussianString(this ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Oil:
                    return "Нефть";
                case ResourceType.Gas:
                    return "Газ";
                case ResourceType.Coal:
                    return "Уголь";
                case ResourceType.Iron:
                    return "Железо";
                case ResourceType.Copper:
                    return "Медь";
                case ResourceType.Gold:
                    return "Золото";
                case ResourceType.Water:
                    return "Вода";
                default:
                    return type.ToString();
            }
        }
    }
}