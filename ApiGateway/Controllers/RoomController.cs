using GameContracts;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
[ApiController]
[Route("api/[controller]")]
public class RoomController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;

    public RoomController(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    [HttpPost("join")]
    public async Task<IActionResult> JoinRoom([FromBody] PlayerInfo player)
    {
        var room = _grainFactory.GetGrain<IRoomGrain>("test-room");
        await room.JoinAsync(player);
        var players = await room.GetPlayersAsync();
        return Ok(players);
    }
}
}