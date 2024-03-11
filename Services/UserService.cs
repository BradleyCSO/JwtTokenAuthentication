using API.Models;
using API.Services.IServices;

namespace API.Services;

public class UserService : IUserService
{
    private readonly IDatabaseService _databaseService;
    private readonly IAuthenticationService _authenticationService;

    public UserService(IDatabaseService databaseService, IAuthenticationService authenticationService)
    {
        _databaseService = databaseService;
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// Creates a user and adds them to the database
    /// </summary>
    /// <param name="createUserRequest">User to create</param>
    /// <returns>Id of created user</returns>
    public async Task<int?> Create(CreateUserRequest createUserRequest) => await _databaseService.InsertUserDataAsync(createUserRequest);

    /// <summary>
    /// Authenticates a user provided a valid AuthenticationRequest (from JSON payload)
    /// </summary>
    /// <param name="authenticationRequest">Request to authenticate</param>
    /// <returns>Authenticated response containing the user and the issued tokens</returns>
    public async Task<AuthenticatedUserLogin?> LogIn(AuthenticationRequest authenticationRequest)
    {
        User? user = await _databaseService.GetUserByUsernameAsync(authenticationRequest);

        if (user == null)
            return null;

        return _authenticationService.GenerateTokens(user.Id);
    }

    /// <summary>
    /// Gets a user by its Id
    /// </summary>
    /// <param name="id">Id to lookup with</param>
    /// <returns>A single user</returns>
    public async Task<User?> GetByIdAsync(int id) => await _databaseService.GetUserByIdAsync(id);
}