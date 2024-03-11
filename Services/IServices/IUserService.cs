using API.Models;

namespace API.Services.IServices;

public interface IUserService
{
    public Task<AuthenticatedUserLogin?> LogIn(AuthenticationRequest authenticationRequest);
    public Task<int?> Create(CreateUserRequest createUserRequest);
    public Task<User?> GetByIdAsync(int id);
}