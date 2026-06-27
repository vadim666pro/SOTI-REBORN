namespace Content.Shared.CounterStrike.Events;

/// <summary>
/// Raised when a Counter-Strike bomb is successfully planted.
/// Used to suspend round timer and team-elimination win conditions.
/// </summary>
public sealed class CsBombPlantedEvent : EntityEventArgs
{
    public EntityUid Bomb;
    public EntityUid Site;

    public CsBombPlantedEvent(EntityUid bomb, EntityUid site)
    {
        Bomb = bomb;
        Site = site;
    }
}

/// <summary>
/// Raised when a planted Counter-Strike bomb is defused.
/// </summary>
public sealed class CsBombDefusedEvent : EntityEventArgs
{
    public EntityUid Bomb;
    public EntityUid Site;

    public CsBombDefusedEvent(EntityUid bomb, EntityUid site)
    {
        Bomb = bomb;
        Site = site;
    }
}

/// <summary>
/// Raised when a planted Counter-Strike bomb explodes.
/// </summary>
public sealed class CsBombExplodedEvent : EntityEventArgs
{
    public EntityUid Bomb;
    public EntityUid? Site;

    public CsBombExplodedEvent(EntityUid bomb, EntityUid? site)
    {
        Bomb = bomb;
        Site = site;
    }
}
