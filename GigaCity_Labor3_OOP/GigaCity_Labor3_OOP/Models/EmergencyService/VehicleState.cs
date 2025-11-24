namespace GigaCity_Labor3_OOP.Models.EmergencyService
{
    public enum VehicleState
    {
        InGarage,      // В гараже
        OnCall,        // На вызове
        Returning,     // Возвращается
        Maintenance,   // На обслуживании
        Idle,          // Ожидание (для скорой помощи - в больнице)
        OnMission,     // На вызове (движется к цели)
        Transporting   // Перевозка пациента в больницу
    }
}

