using API.Models;
using API.Services.IServices;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace API.Services;

public class DatabaseService : IDatabaseService
{
    private readonly NpgsqlConnection _connection;
    private readonly ILogger<DatabaseService> _logger;
    private readonly IPasswordHasher<AuthenticationRequest> _passwordHasher;

    public DatabaseService(NpgsqlConnection connection, ILogger<DatabaseService> logger,
        IPasswordHasher<AuthenticationRequest> passwordHasher)
    {
        _connection = connection;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public void CreateUsersTableIfNotExists()
    {
        try
        {
            _connection.Open();

            new NpgsqlCommand(
                "CREATE TABLE IF NOT EXISTS users (" +
                "id SERIAL PRIMARY KEY," +
                "firstname TEXT NOT NULL," +
                "lastname TEXT NOT NULL," +
                "username TEXT NOT NULL UNIQUE," +
                "password TEXT NOT NULL" +
                ")", _connection).ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating users table");
        }
        finally
        {
            _connection.Close();
        }
    }

    public void CreateUserRefreshTokensTableIfNotExists()
    {
        try
        {
            _connection.Open();

            new NpgsqlCommand(
                "CREATE TABLE IF NOT EXISTS user_refresh_tokens (" +
                "id SERIAL PRIMARY KEY," +
                "token TEXT NOT NULL UNIQUE," +
                "expiration TIMESTAMP NOT NULL" +
                ")", _connection).ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user refresh token table");
        }
        finally
        {
            _connection.Close();
        }
    }

    public async Task<int?> InsertUserDataAsync(CreateUserRequest createUserRequest)
    {
        try
        {
            await _connection.OpenAsync();

            NpgsqlCommand? query = new NpgsqlCommand(
                "INSERT INTO users (firstname, lastname, username, password) " +
                "VALUES (@firstname, @lastname, @username, @password)" +
                "RETURNING id", _connection);

            query.Parameters.AddWithValue("firstname", createUserRequest.FirstName);
            query.Parameters.AddWithValue("lastname", createUserRequest.LastName);
            query.Parameters.AddWithValue("username", createUserRequest.Username);
            query.Parameters.AddWithValue("password", createUserRequest.Password);

            return Convert.ToInt32(await query.ExecuteScalarAsync());
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error inserting user data for {createUserRequest.Username}");
        }
        finally
        {
            await _connection.CloseAsync();
        }

        return null;
    }

    public async Task InsertUserRefreshTokenAsync(RefreshTokenModel refreshToken)
    {
        try
        {
            await _connection.OpenAsync();

            NpgsqlCommand? query = new NpgsqlCommand("INSERT INTO user_refresh_tokens (id, token, expiration) " +
                                          "VALUES (@id, @token, @expiration)", _connection);

            query.Parameters.AddWithValue("id", refreshToken.UserId);
            query.Parameters.AddWithValue("token", refreshToken.Token);
            query.Parameters.AddWithValue("expiration", refreshToken.Expiration);

            await query.ExecuteScalarAsync();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error inserting user refresh token for user with id {refreshToken.UserId} into user_refresh tokens users table");
        }
        finally
        {
            await _connection.CloseAsync();
        }
    }

    public async Task<int> GetUserIdByRefreshTokenAsync(string refreshToken)
    {
        try
        {
            await _connection.OpenAsync();

            NpgsqlCommand? query = new NpgsqlCommand("SELECT * FROM user_refresh_tokens WHERE token = @refreshToken", _connection);
            query.Parameters.AddWithValue("refreshToken", refreshToken);

            return await query.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Couldn't find user for token {refreshToken}");
        }
        finally
        {
            await _connection.CloseAsync();
        }

        return 0;
    }

    public async Task<User?> GetUserByUsernameAsync(AuthenticationRequest authenticationRequest)
    {
        try
        {
            await _connection.OpenAsync();

            NpgsqlCommand query = new NpgsqlCommand("SELECT * FROM users WHERE username = @username", _connection);
            query.Parameters.AddWithValue("username", authenticationRequest.Username);

            NpgsqlDataReader reader = await query.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                PasswordVerificationResult passwordVerificationResult =
                    _passwordHasher.VerifyHashedPassword(authenticationRequest, reader["password"]?.ToString() ?? throw new InvalidOperationException("Hashed password not found"), authenticationRequest.Password);

                if (passwordVerificationResult == PasswordVerificationResult.Success)
                    return GetUser(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Couldn't find user with username {authenticationRequest.Username}");
        }
        finally
        {
            await _connection.CloseAsync();
        }

        return null;
    }

    public async Task<User?> GetUserByIdAsync(int? id)
    {
        try
        {
            await _connection.OpenAsync();

            NpgsqlCommand? query = new NpgsqlCommand("SELECT * FROM users WHERE id = @id", _connection);
            query.Parameters.AddWithValue("id", id ?? throw new InvalidOperationException("Id not found"));

            NpgsqlDataReader? reader = await query.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return GetUser(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Couldn't find user with id {id}");
        }
        finally
        {
            await _connection.CloseAsync();
        }

        return null;
    }

    private User GetUser(NpgsqlDataReader reader)
    {
        return new()
        {
            Id = reader["id"] as int? ?? throw new InvalidOperationException("Id not found"),
            FirstName = reader["firstname"]?.ToString() ?? throw new InvalidOperationException("First name not found"),
            LastName = reader["lastname"]?.ToString() ?? throw new InvalidOperationException("Last name not found"),
            Username = reader["username"]?.ToString() ?? throw new InvalidOperationException("Username name not found")
        };
    }
}