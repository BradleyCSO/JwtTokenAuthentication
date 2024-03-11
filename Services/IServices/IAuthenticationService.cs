using API.Models;

namespace API.Services.IServices;

public interface IAuthenticationService
{
    public AuthenticatedUserLogin GenerateTokens(int userId);
    public string CreateAccessToken(int userId);
    public Task<bool> IsRefreshTokenValidAsync(string refreshToken);
}