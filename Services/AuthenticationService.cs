using API.Models;
using API.Services.IServices;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IConfiguration _configuration;
    private readonly IDatabaseService _databaseService;

    public AuthenticationService(IConfiguration configuration, IDatabaseService databaseService)
    {
        _configuration = configuration;
        _databaseService = databaseService;
    }

    /// <summary>
    /// Generates a refresh token that lasts 7 days, persists to db and access token (lasts 1 day) for a user
    /// </summary>
    /// <param name="userId">Stored user id to generate token for</param>
    /// <returns>Generated refresh and access token</returns>
    public AuthenticatedUserLogin GenerateTokens(int userId)
    {
        // Store the refresh token in db and associate it with the user
        RefreshTokenModel refreshToken = new RefreshTokenModel
        {
            UserId = userId,
            Token = Guid.NewGuid().ToString(),
            Expiration = DateTime.UtcNow.AddDays(7)
        };

        // Insert or update this user's (who we know to be authenticated) refresh token
        _databaseService.InsertUserRefreshTokenAsync(refreshToken);

        return new AuthenticatedUserLogin
        {
            UserId = userId,
            AccessToken = CreateAccessToken(userId),
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiration = refreshToken.Expiration
        };
    }

    /// <summary>
    /// Creates access token provided a userId and a value for the Jwt:Secret key from the config
    /// </summary>
    /// <param name="userId">User id to create access token for</param>
    /// <returns>Json Web Token (JWT)</returns>
    /// <exception cref="InvalidOperationException">Throws if the secret key is missing</exception>
    public string CreateAccessToken(int userId)
    {
        string? secretKey = _configuration["Jwt:SecretKey"];

        if (secretKey == null) throw new InvalidOperationException("Secret key is missing.");

        SecurityTokenDescriptor securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("id", userId.ToString()) }),
            Expires = DateTime.UtcNow.AddMinutes(60),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
                SecurityAlgorithms.HmacSha256Signature)
        };

        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(tokenHandler.CreateToken(securityTokenDescriptor));
    }

    /// <summary>
    /// Checks if the refresh token is valid i.e. does it exist in the db
    /// </summary>
    /// <param name="refreshToken">Refresh token to validate</param>
    /// <returns>Boolean task</returns>
    public async Task<bool> IsRefreshTokenValidAsync(string refreshToken)
    {
        // Get the user associated with the refresh token
        int userId = await _databaseService.GetUserIdByRefreshTokenAsync(refreshToken);

        // Refresh token exists for user
        if (userId != 0)
            return true;

        return false;
    }
}