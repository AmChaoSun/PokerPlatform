using Orleans;
using GameContracts;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
[ApiController]
[Route("api/[controller]")]
public class RoomController(IGrainFactory grainFactory) : ControllerBase
{
    [HttpPost("{roomId}/join")]
    public async Task<IActionResult> JoinRoom(string roomId, [FromBody] PlayerInfo player)
    {
        var room = grainFactory.GetGrain<IHoldemRoom>(roomId);
        await room.JoinAsync(player);
        var players = await room.GetPlayersAsync();
        return Ok(players);
    }

    // DTOs for requests
    public record BetRequest(string PlayerId, int Amount);
    public record FoldRequest(string PlayerId);

    [HttpGet("{roomId}/state")]
    public async Task<ActionResult<GameState>> GetState(string roomId)
    {
        var room = grainFactory.GetGrain<IHoldemRoom>(roomId);
        var state = await room.GetStateAsync();
        return Ok(state);
    }

    [HttpPost("{roomId}/start")]
    public async Task<IActionResult> Start(string roomId)
    {
        var room = grainFactory.GetGrain<IHoldemRoom>(roomId);
        await room.StartGameAsync();
        return Ok();
    }

    [HttpPost("{roomId}/bet")]
    public async Task<IActionResult> Bet(string roomId, [FromBody] BetRequest req)
    {
        var room = grainFactory.GetGrain<IHoldemRoom>(roomId);
        await room.BetAsync(req.PlayerId, req.Amount);
        return Ok();
    }

    [HttpPost("{roomId}/fold")]
    public async Task<IActionResult> Fold(string roomId, [FromBody] FoldRequest req)
    {
        var room = grainFactory.GetGrain<IHoldemRoom>(roomId);
        await room.FoldAsync(req.PlayerId);
        return Ok();
    }
}
}