using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameContracts;
using Microsoft.Extensions.Logging;

namespace GameEngine;

public class HoldemRoom : RoomGrain, IHoldemRoom
{
    private readonly ILogger<HoldemRoom> _logger;

    // --- In-memory hand state (per hand) ---
    private readonly List<string> _communityCards = new();        // Flop/Turn/River
    private readonly Dictionary<string, int> _roundBets = new();   // playerId -> bet in current round
    private readonly HashSet<string> _folded = new();              // folded players
    private readonly List<string> _deck = new();                   // 52-card deck
    private readonly Random _rng = new();

    private int _pot;
    private string? _currentTurnPlayerId;
    private HoldemGamePhase _phase = HoldemGamePhase.NotStarted;

    public HoldemRoom(ILogger<HoldemRoom> logger) : base(logger)
    {
        _logger = logger;
    }

    public override Task<GameState> GetStateAsync()
    {
        _logger.LogInformation("Holdem:GetState Phase={Phase} Pot={Pot}", _phase, _pot);

        return Task.FromResult(new GameState
        {
            CommunityCards = _communityCards.ToArray(),
            Pot = _pot,
            CurrentTurnPlayerId = _currentTurnPlayerId,
            Phase = _phase.ToString()
        });
    }

    public override async Task StartGameAsync()
    {
        // Validate we have >=2 seated players
        var players = await GetPlayersAsync();
        var activeIds = players.Select(p => p.PlayerId).ToList();
        if (activeIds.Count < 2)
            throw new InvalidOperationException("Need at least 2 players to start a Holdem hand.");

        _logger.LogInformation("Holdem:StartGame with {Count} players", activeIds.Count);

        // Init a fresh hand
        BuildAndShuffleDeck();
        _communityCards.Clear();
        _roundBets.Clear();
        _folded.Clear();
        _pot = 0;

        // TODO: (Later) deal hole cards to each player and keep private mapping
        _phase = HoldemGamePhase.HoleCardsDealt;

        // Choose first turn = first non-folded player in current seating (from RoomGrain order)
        _currentTurnPlayerId = activeIds.FirstOrDefault();
        _logger.LogInformation("Holdem:New hand started. First turn -> {Player}", _currentTurnPlayerId);
    }

    public async Task BetAsync(string playerId, int amount)
    {
        _logger.LogInformation("Holdem:Bet Player={PlayerId} Amount={Amount} Phase={Phase}", playerId, amount, _phase);

        if (_phase is HoldemGamePhase.NotStarted or HoldemGamePhase.Ended or HoldemGamePhase.Showdown)
            throw new InvalidOperationException("Bet not allowed in current phase.");

        var players = await GetPlayersAsync();
        var seatedIds = players.Select(p => p.PlayerId).ToHashSet();
        if (!seatedIds.Contains(playerId))
            throw new InvalidOperationException("Player not seated in this room.");

        if (_folded.Contains(playerId))
            throw new InvalidOperationException("Player already folded.");

        EnsureTurn(playerId);

        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Bet must be non-negative.");

        // Record/update this player's bet for the current betting round
        _roundBets[playerId] = _roundBets.TryGetValue(playerId, out var prev) ? prev + amount : amount;
        _pot += amount;

        // Advance to the next eligible player
        await AdvanceTurnAsync(players);

        // If betting round complete, auto progress the game
        if (IsBettingRoundComplete(players))
        {
            _logger.LogInformation("Holdem:Betting round complete. Progressing.");
            await ProgressAsync();
        }
    }

    public async Task FoldAsync(string playerId)
    {
        _logger.LogInformation("Holdem:Fold Player={PlayerId} Phase={Phase}", playerId, _phase);

        if (_phase is HoldemGamePhase.NotStarted or HoldemGamePhase.Ended)
            throw new InvalidOperationException("Fold not allowed in current phase.");

        var players = await GetPlayersAsync();
        var seatedIds = players.Select(p => p.PlayerId).ToHashSet();
        if (!seatedIds.Contains(playerId))
            throw new InvalidOperationException("Player not seated in this room.");

        EnsureTurn(playerId);

        _folded.Add(playerId);

        // If only one player remains, end hand immediately (no showdown)
        var remaining = ActivePlayers(players);
        if (remaining.Count == 1)
        {
            _logger.LogInformation("Holdem:Only one player remains—ending hand.");
            await EvaluateWinnersInternal(remaining);
            return;
        }

        // Advance to next turn
        await AdvanceTurnAsync(players);

        // If betting round complete after this fold, progress
        if (IsBettingRoundComplete(players))
        {
            await ProgressAsync();
        }
    }

    /// <summary>
    /// Internal orchestrator for advancing phases. Not exposed to clients.
    /// </summary>
    private async Task ProgressAsync()
    {
        _logger.LogInformation("Holdem:Progress Phase={Phase}", _phase);

        switch (_phase)
        {
            case HoldemGamePhase.HoleCardsDealt:
                RevealFlop();
                _phase = HoldemGamePhase.Flop;
                await BeginNewBettingRoundAsync();
                break;

            case HoldemGamePhase.Flop:
                RevealTurn();
                _phase = HoldemGamePhase.Turn;
                await BeginNewBettingRoundAsync();
                break;

            case HoldemGamePhase.Turn:
                RevealRiver();
                _phase = HoldemGamePhase.River;
                await BeginNewBettingRoundAsync();
                break;

            case HoldemGamePhase.River:
                _phase = HoldemGamePhase.Showdown;
                var players = await GetPlayersAsync();
                await EvaluateWinnersInternal(ActivePlayers(players));
                break;

            case HoldemGamePhase.Showdown:
            case HoldemGamePhase.Ended:
                _logger.LogInformation("Holdem:Already at {Phase}", _phase);
                break;

            case HoldemGamePhase.NotStarted:
            default:
                throw new InvalidOperationException("Progress called before game started.");
        }
    }

    // ----------------------- Internals -----------------------

    private void BuildAndShuffleDeck()
    {
        _deck.Clear();
        var ranks = new[] { "2","3","4","5","6","7","8","9","T","J","Q","K","A" };
        var suits = new[] { "C", "D", "H", "S" };
        foreach (var r in ranks)
        foreach (var s in suits)
            _deck.Add($"{r}{s}");

        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
        }
    }

    private void RevealFlop()
    {
        BurnOne();
        _communityCards.Add(DrawOne());
        _communityCards.Add(DrawOne());
        _communityCards.Add(DrawOne());
        _logger.LogInformation("Holdem:Flop {Cards}", string.Join(",", _communityCards));
    }

    private void RevealTurn()
    {
        BurnOne();
        var card = DrawOne();
        _communityCards.Add(card);
        _logger.LogInformation("Holdem:Turn {Card}", card);
    }

    private void RevealRiver()
    {
        BurnOne();
        var card = DrawOne();
        _communityCards.Add(card);
        _logger.LogInformation("Holdem:River {Card}", card);
    }

    private void BurnOne()
    {
        if (_deck.Count > 0) _deck.RemoveAt(0);
    }

    private string DrawOne()
    {
        if (_deck.Count == 0) throw new InvalidOperationException("Deck exhausted.");
        var c = _deck[0];
        _deck.RemoveAt(0);
        return c;
    }

    private async Task BeginNewBettingRoundAsync()
    {
        _roundBets.Clear();

        // Move turn to first still-active player based on current seating from RoomGrain
        var players = await GetPlayersAsync();
        var first = ActivePlayers(players).FirstOrDefault();
        _currentTurnPlayerId = first;

        _logger.LogInformation("Holdem:New betting round started. CurrentTurn={Player}", _currentTurnPlayerId);
    }

    private void EnsureTurn(string playerId)
    {
        if (!string.Equals(_currentTurnPlayerId, playerId, StringComparison.Ordinal))
            throw new InvalidOperationException("Not this player's turn.");
    }

    private async Task AdvanceTurnAsync(PlayerState[] players)
    {
        var order = players.Select(p => p.PlayerId).ToList();
        if (order.Count == 0) { _currentTurnPlayerId = null; return; }

        var idx = _currentTurnPlayerId is null ? -1 : order.IndexOf(_currentTurnPlayerId);
        var visited = 0;

        while (visited < order.Count)
        {
            idx = (idx + 1) % order.Count;
            visited++;
            var candidate = order[idx];

            if (!_folded.Contains(candidate))
            {
                _currentTurnPlayerId = candidate;
                _logger.LogInformation("Holdem:Next turn -> {Player}", _currentTurnPlayerId);
                return;
            }
        }

        // If we looped everyone (e.g., all folded), let caller decide next steps.
        _currentTurnPlayerId = null;
    }

    private bool IsBettingRoundComplete(PlayerState[] players)
    {
        var active = ActivePlayers(players);
        if (active.Count == 0) return true;

        // Everyone active has placed a bet and all bets match
        var placed = active.All(p => _roundBets.ContainsKey(p));
        if (!placed) return false;

        var maxBet = active.Max(p => _roundBets[p]);
        var allMatched = active.All(p => _roundBets[p] == maxBet);
        return allMatched;
    }

    private List<string> ActivePlayers(PlayerState[] players)
        => players.Where(p => !_folded.Contains(p.PlayerId))
                  .Select(p => p.PlayerId)
                  .ToList();

    private Task EvaluateWinnersInternal(List<string> activePlayerIds)
    {
        // MVP stub: if multiple remain at showdown, just pick the first in order
        var winner = activePlayerIds.FirstOrDefault();
        _logger.LogInformation("Holdem:EvaluateWinners (MVP) Winner={Winner} Pot={Pot}", winner, _pot);

        // TODO: integrate hand evaluator & side pots
        _phase = HoldemGamePhase.Ended;
        return Task.CompletedTask;
    }
}