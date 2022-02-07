using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TodoApp.Data;
using TodoApp.Filters;
using TodoApp.Helpers;
using TodoApp.Models;
using TodoApp.Services;
using TodoApp.Wrappers;

namespace TodoApp.Controllers;

[ApiController]
[Route("users")]
public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IUriService _uriService;

    public AccountController(ILogger<AccountController> logger, AppDbContext context, IConfiguration config,
        IUriService uriService)
    {
        _logger = logger;
        _context = context;
        _config = config;
        _uriService = uriService;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult GetUsers([FromQuery] PaginationFilter filter)
    {
        var pagedData = _context.Users.OrderBy(u => u.Id)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(user => new UserDto
            {
                Email = user.Email,
                Id = user.Id,
                Name = user.Name,
                Role = user.Role.ToString()
            }).ToList();
        return Ok(PaginationHelper.CreatePagedResponse(pagedData, filter, _context.Users.Count(), _uriService,
            Request.Path.Value!));
    }

    [Authorize]
    [HttpGet("{id:long}")]
    public IActionResult GetUser(long id)
    {
        var user = _context.Users.Find(id);
        if (user is null)
            return NotFound();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString()
        });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetMe()
    {
        var user = HelperMethods.GetUserByClaims(User, _context);
        return Ok(new ApiResponse(new UserDto
            {Email = user.Email, Id = user.Id, Name = user.Name, Role = user.Role.ToString()}));
    }

    [HttpPost("login")]
    public IActionResult Authenticate(UserLoginCredentialsDto credentials)
    {
        ClaimsIdentity identity;
        try
        {
            identity = GetIdentity(credentials.Email, credentials.Password);
        }
        catch (InvalidCredentialException ice)
        {
            return Unauthorized(new ApiResponse {Message = ice.Message});
        }

        var now = DateTime.UtcNow;
        var authOptions = _config.GetSection("JWT");
        var jwt = new JwtSecurityToken(
            issuer: authOptions["Issuer"],
            audience: authOptions["Audience"],
            notBefore: now,
            claims: identity.Claims,
            expires: now.Add(TimeSpan.FromMinutes(double.Parse(authOptions["Lifetime"]))),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.ASCII.GetBytes(authOptions["Key"])),
                SecurityAlgorithms.HmacSha256
            )
        );
        var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
        return Ok(new ApiResponse(new {access_token = encodedJwt, expires = jwt.ValidTo}));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRegisterCredentialsDto credentials)
    {
        if (_context.Users.Any(user => user.Email == credentials.Email))
            return Conflict(new ApiResponse {Message = "This email is already registered."});

        var (hashedPassword, salt) = HashPassword(credentials.Password);
        var user = new User
        {
            Email = credentials.Email,
            Name = credentials.Name,
            PasswordHash = hashedPassword,
            Role = Roles.User,
            Salt = salt
        };
        user = _context.Users.Add(user).Entity;
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new {id = user.Id}, new ApiResponse(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString()
        }));
    }

    private static (string hash, string salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(32);
        return (hash: HashPassword(password, salt), salt: Convert.ToBase64String(salt));
    }

    private static string HashPassword(string password, byte[] salt)
    {
        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA512,
            iterationCount: 100000,
            numBytesRequested: 64
        );
        return Convert.ToBase64String(hash);
    }

    private ClaimsIdentity GetIdentity(string email, string password)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == email);
        if (user is null || user.PasswordHash != HashPassword(password, Convert.FromBase64String(user.Salt)))
            throw new InvalidCredentialException("Invalid username or password.");

        var claims = new List<Claim>
        {
            new("sub", user.Email),
            new("role", user.Role.ToString())
        };
        return new ClaimsIdentity(claims, "Token", "sub", "role");
    }
}
