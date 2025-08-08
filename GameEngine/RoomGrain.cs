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
public abstract class RoomGrain(ILogger<RoomGrain> logger) : Grain, IRoomGrain
{
    // List of all players in this room (their current game state)
    protected readonly List<PlayerState> _players = [];

    // Player ID of the player whose turn it is
    protected string _currentTurnPlayerId = "";

    // Called when a player joins the room.
    // If they're not already in the player list, they're added with default chips.
    public Task JoinAsync(PlayerInfo player)
    {
        logger.LogInformation("Player {PlayerId} is attempting to join the room.", player.PlayerId);
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
        logger.LogInformation("Fetching list of players.");
        foreach (var player in _players)
        {
            logger.LogInformation("Player in room: Id={PlayerId}, Chips={Chips}, HasFolded={HasFolded}",
                player.PlayerId, player.Chips, player.HasFolded);
        }
        return Task.FromResult(_players.ToArray());
    }

    public abstract Task<GameState> GetStateAsync();
    
    public abstract Task StartGameAsync();
}