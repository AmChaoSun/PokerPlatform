namespace GameContracts;

public enum HoldemGamePhase
{
    NotStarted,
    HoleCardsDealt,
    Flop,
    Turn,
    River,
    Showdown,
    Ended
}