using Orleans;

namespace GameContracts;

[Alias("RoomGrain")]
public interface IRoomGrain : IGrainWithStringKey
{
    Task JoinAsync(PlayerInfo player);
    Task<PlayerState[]> GetPlayersAsync();
    Task<GameState> GetStateAsync();
    Task StartGameAsync();
}

[GenerateSerializer]
[Alias("PlayerInfo")]
public record PlayerInfo
{
    [Id(0)] public required string PlayerId { get; init; }
    [Id(1)] public required string Nickname { get; init; }
}

[GenerateSerializer]
[Alias("PlayerState")]
public record PlayerState
{
    [Id(0)] public required string PlayerId { get; init; }
    [Id(1)] public int Chips { get; init; }
    [Id(2)] public bool HasFolded { get; init; }
}

[GenerateSerializer]
[Alias("GameState")]
public record GameState
{
    [Id(0)] public required string[] CommunityCards { get; init; }
    [Id(1)] public int Pot { get; init; }
    [Id(2)] public required string CurrentTurnPlayerId { get; init; }
    [Id(3)] public required string Phase { get; init; }
}

public enum PlayerActionType
{
    Fold,
    Check,
    Call,
    Raise,
    AllIn
}