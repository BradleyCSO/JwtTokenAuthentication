namespace API.Models;

public class RefreshTokenModel
{
    public required int UserId { get; set; }
    public required string Token { get; set; }
    public required DateTime Expiration { get; set; }
}