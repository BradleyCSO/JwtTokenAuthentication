using API.Models;
using API.Services.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using AuthorizeAttribute = API.Middleware.AuthorizeAttribute;

namespace API.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<UserController> _logger;
    private readonly IPasswordHasher<AuthenticationRequest> _passwordHasher;

    public UserController(IUserService userService, IAuthenticationService authenticationService,
        IPasswordHasher<AuthenticationRequest> passwordHasher, ILogger<UserController> logger)
    {
        _userService = userService;
        _authenticationService = authenticationService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest createUserRequest)
    {
        try
        {
            createUserRequest.Password = _passwordHasher.HashPassword(createUserRequest, createUserRequest.Password);
            int? user = await _userService.Create(createUserRequest);

            if (user == null) return new ObjectResult(new { error = new { message = "Error creating user" } }) { StatusCode = 500 };

            return new ObjectResult(new { user }) { StatusCode = 201 };
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            return new ObjectResult(new { error = new { code = "CONFLICT", message = "User already exists" } }) { StatusCode = 409 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user");
            return new StatusCodeResult(304);
        }
    }

    [HttpPost("authenticate")]
    public async Task<IActionResult> AuthenticateAsync([FromBody] AuthenticationRequest authenticationRequest)
    {
        try
        {
            AuthenticatedUserLogin? authenticatedUserLogin = await _userService.LogIn(authenticationRequest);

            if (authenticatedUserLogin == null)
                return new StatusCodeResult(401);

            // Add the access token to the requester's response headers
            Response.Headers.Add("Authorization", authenticatedUserLogin.AccessToken);
                
            return new ObjectResult(new { authenticatedUserLogin }) { StatusCode = 200};
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate");
            return new StatusCodeResult(500);
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshAccessTokenAsync(int userId, string refreshToken)
    {
        try
        {
            // Validate the refresh token.
            if (await _authenticationService.IsRefreshTokenValidAsync(refreshToken))
            {
                // If it is valid, generate a new access token which will be used by client to make further requests
                string newAccessToken = _authenticationService.CreateAccessToken(userId);

                return Ok(new
                {
                    AccessToken = newAccessToken
                });
            }

            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token");
            return new StatusCodeResult(500);
        }
    }

    [Authorize]
    [HttpGet("id")]
    public async Task<IActionResult> GetUserAsync(int userId)
    {
        try
        {
            User? user = await _userService.GetByIdAsync(userId);

            if (user == null)
                return new StatusCodeResult(404);

            return new ObjectResult(new { user }) { StatusCode = 200 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user with {userId}", userId);
            return new StatusCodeResult(500);
        }
    }
}