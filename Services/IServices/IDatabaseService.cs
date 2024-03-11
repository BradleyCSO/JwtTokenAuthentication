using API.Models;

namespace API.Services.IServices;

public interface IDatabaseService
{
    public void CreateUsersTableIfNotExists();
    public void CreateUserRefreshTokensTableIfNotExists();
    public Task<int?> InsertUserDataAsync(CreateUserRequest createUserRequest);
    public Task InsertUserRefreshTokenAsync(RefreshTokenModel refreshToken);
    public Task<User?> GetUserByUsernameAsync(AuthenticationRequest authenticationRequest);
    public Task<int> GetUserIdByRefreshTokenAsync(string refreshToken);
    public Task<User?> GetUserByIdAsync(int? id);
}