namespace Content.Server.Vehicle.Systems;

/// <summary>
/// Server-side vehicle system. Currently all logic lives in the shared system.
/// Extend here for server-only features (damage, fuel, NPC drivers, etc.).
/// </summary>
public sealed class VehicleSystem : Shared.Vehicle.EntitySystems.VehicleSystem
{
}
