namespace API.Models;

public class CreateUserRequest : AuthenticationRequest
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}