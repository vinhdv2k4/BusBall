public enum ColorType
{
    None = 0, Red = 1, Green = 2, Blue = 3, Yellow = 4, Purple = 5,
    Orange = 6, Pink = 7, Cyan = 8, Forest = 9, Brown = 10, Black = 11
}

public enum BusType { Small, Medium, Large }

public enum BusState
{
    Idle, Blocked, GotHit, MoveStraight, EnterRoad, FollowRoad,
    Finished, Despawn, GarageOut
}

public enum BusFinishMode { Default, Magnet }
public enum BusMechanicType { Normal, HurryBus, HiddenBus, FrozenBus }
public enum BusOutObjectType { Bus, Garage }

public enum HitDirection { Left, Right, Front, Back }

public enum VfxType
{
    SmokeDefault, SmokeHurryBus, BusHit, BusReleaseBall,
    BusSmallLanding, BusMediumLanding, BusLargeLanding, BoxDie,
    RevealBus, StuckBus, Electric, IceBreak
}
