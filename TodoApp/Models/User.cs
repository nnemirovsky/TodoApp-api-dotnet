namespace TodoApp.Models;

public enum Roles
{
    Admin,
    User
}

public class User
{
    public long Id { get; init; }
    public string Name { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string PasswordHash { get; init; } = default!;
    public string Salt { get; init; } = default!;
    public Roles Role { get; init; }
}

public record UserDto
{
    public long Id { get; init; }
    public string Name { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Role { get; init; } = default!;
}

public record UserRegisterCredentialsDto
{
    public string Name { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
}

public record UserLoginCredentialsDto
{
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
}
