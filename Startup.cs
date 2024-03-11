using API.Middleware;
using API.Models;
using API.Services;
using API.Services.IServices;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace API;
public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPasswordHasher<AuthenticationRequest>, PasswordHasher<AuthenticationRequest>>();
        services.AddScoped<IDatabaseService, DatabaseService>(provider =>
        {
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            NpgsqlConnection? connection = new NpgsqlConnection(connectionString);

            return new DatabaseService(connection, provider.GetRequiredService<ILogger<DatabaseService>>(), provider.GetRequiredService<IPasswordHasher<AuthenticationRequest>>());
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseCors(x => x
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());

        IServiceScope? scope = app.ApplicationServices.CreateAsyncScope();
     
        IDatabaseService? databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
        databaseService.CreateUsersTableIfNotExists();
        databaseService.CreateUserRefreshTokensTableIfNotExists();

        scope.Dispose();

        app.UseMiddleware<JwtMiddleware>();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}