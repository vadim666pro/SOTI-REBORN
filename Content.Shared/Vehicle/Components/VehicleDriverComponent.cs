using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Marker component added to an entity that is currently buckled to a vehicle's driver seat.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(EntitySystems.VehicleSystem))]
public sealed partial class VehicleDriverComponent : Component
{
    /// <summary>
    /// The vehicle entity this driver is controlling.
    /// </summary>
    [DataField]
    public EntityUid Vehicle;
}
