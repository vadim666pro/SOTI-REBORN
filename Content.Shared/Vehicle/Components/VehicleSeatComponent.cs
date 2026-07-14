using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Marks a Strap entity as a vehicle seat.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(EntitySystems.VehicleSystem))]
public sealed partial class VehicleSeatComponent : Component
{
    /// <summary>
    /// True for the driver seat. Only the entity buckled here receives movement relay.
    /// </summary>
    [DataField]
    public bool IsDriver;

    /// <summary>
    /// Display index for UI / visual purposes.
    /// </summary>
    [DataField]
    public int SeatIndex;
}
