namespace ApiGateway.Contracts.Requests;

public class JoinRoomRequest
{
    public required string PlayerId { get; set; }
    public required string Nickname { get; set; }
    public required string RoomId { get; set; }
}
