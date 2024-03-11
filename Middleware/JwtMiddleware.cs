using API.Services.IServices;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace API.Middleware;

/// <summary>
/// Intercepts HTTP requests and assigns a user to a context provided a successful JWT validation
/// </summary>
public class JwtMiddleware
{
    private readonly RequestDelegate _requestDelegate;
    private readonly IConfiguration _configuration;

    public JwtMiddleware(RequestDelegate requestDelegate, IConfiguration configuration)
    {
        _requestDelegate = requestDelegate;
        _configuration = configuration;
    }

    public async Task Invoke(HttpContext context)
    {
        string? token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        IUserService? userService = context.RequestServices.GetRequiredService<IUserService>();

        if (token != null)
            await ValidateTokenAsync(context, userService, token);

        await _requestDelegate(context);
    }

    /// <summary>
    /// Validates user token 
    /// </summary>
    /// <param name="userService"></param>
    /// <param name="token"></param>
    /// <param name="context"></param>
    /// <returns>A validated token, or null</returns>
    public async Task ValidateTokenAsync(HttpContext context, IUserService userService, string token)
    {
        try
        {
            JwtSecurityTokenHandler? tokenHandler = new JwtSecurityTokenHandler();

            TokenValidationResult? jwtToken = await tokenHandler.ValidateTokenAsync(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"] ?? string.Empty)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero // Expires at token expiration time
            });

            context.Items["User"] = await userService.GetByIdAsync(Convert.ToInt32(jwtToken.Claims["id"])); // Attach user to context
        }
        catch
        { 
            // Empty catch block
        }
    }
}