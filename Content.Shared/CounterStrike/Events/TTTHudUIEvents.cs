using Robust.Shared.Serialization;

namespace Content.Shared.CounterStrike.Events;

[Serializable, NetSerializable]
public sealed class TTTHudEvent : EntityEventArgs
{
	public float TimeUntilPolice;
	public bool PoliceArrived;

	public TTTHudEvent() { }

	public TTTHudEvent(float timeUntilPolice, bool policeArrived)
	{
		TimeUntilPolice = timeUntilPolice;
		PoliceArrived = policeArrived;
	}
}

[Serializable, NetSerializable]
public sealed class TTTHudClearEvent : EntityEventArgs
{
}
