using Orleans;

namespace GameContracts;

[Alias("HoldemRoom")]
public interface IHoldemRoom : IRoomGrain
{
    Task BetAsync(string playerId, int amount);
    Task FoldAsync(string playerId);
}