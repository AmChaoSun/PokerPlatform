using GameContracts; // Import shared contracts/interfaces for the game
using Orleans;       // Orleans framework base types like Grain
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace GameEngine;

// RoomGrain is the server-side representation of a single poker room.
// It holds the list of players, game state, and processes game actions like bet/fold.
public class RoomGrain : Grain, IRoomGrain
{
    private readonly ILogger<RoomGrain> _logger;

    public RoomGrain(ILogger<RoomGrain> logger)
    {
        _logger = logger;
    }

    // List of all players in this room (their current game state)
private readonly List<PlayerState> _players = [];

// Community cards shared on the table (e.g., flop, turn, river)
private readonly List<string> _communityCards = [];

    // Total chips currently in the pot
    private int _pot = 0;

    // Player ID of the player whose turn it is
    private string _currentTurnPlayerId = "";

    // Called when a player joins the room.
    // If they're not already in the player list, they're added with default chips.
    public Task JoinAsync(PlayerInfo player)
    {
        _logger.LogInformation("Player {PlayerId} is attempting to join the room.", player.PlayerId);
        // Only add the player if not already present
        if (_players.All(p => p.PlayerId != player.PlayerId))
        {
            // Start with 1000 chips and mark them as not folded
            _players.Add(new PlayerState
            {
                PlayerId = player.PlayerId,
                Chips = 1000,
                HasFolded = false
            });
        }
        return Task.CompletedTask;
    }

    // Return all player states in the room as an array.
    // Useful for showing current table status to clients.
    public Task<PlayerState[]> GetPlayersAsync()
    {
        _logger.LogInformation("Fetching list of players.");
        return Task.FromResult(_players.ToArray());
    }

    // Placeholder for processing a player's bet.
    // You will eventually update pot, validate turn, update player chips, etc.
    public Task BetAsync(string playerId, int amount)
    {
        _logger.LogInformation("Player {PlayerId} is attempting to bet {Amount}.", playerId, amount);
        // TODO: implement bet logic
        return Task.CompletedTask;
    }

    // Placeholder for when a player folds (i.e., leaves the current hand).
    // You will eventually mark them as folded and check if round is over.
    public Task FoldAsync(string playerId)
    {
        _logger.LogInformation("Player {PlayerId} is folding.", playerId);
        // TODO: implement fold logic
        return Task.CompletedTask;
    }

    // Returns a snapshot of the current game state:
    // - community cards on the table
    // - how many chips are in the pot
    // - whose turn it is to act
    public Task<GameState> GetStateAsync()
    {
        _logger.LogInformation("Getting current game state.");
        return Task.FromResult(new GameState
        {
            CommunityCards = _communityCards.ToArray(),
            Pot = _pot,
            CurrentTurnPlayerId = _currentTurnPlayerId
        });
    }
}