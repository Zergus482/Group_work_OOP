namespace CitySimulation.Models.Utilities
{
    public class UtilityConnection
    {
        public ResidentialBuilding Building { get; set; }
        public UtilityService Service { get; set; }
        public bool IsActive { get; set; }

        public UtilityConnection(ResidentialBuilding building, UtilityService service)
        {
            Building = building;
            Service = service;
            IsActive = false;
        }

        public bool Activate()
        {
            IsActive = Service.ConnectBuildingToUtilities(Building);
            return IsActive;
        }

        public void Deactivate()
        {
            Service.DisconnectBuildingFromUtilities(Building);
            IsActive = false;
        }
    }
}
