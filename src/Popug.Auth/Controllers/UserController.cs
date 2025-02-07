using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Popug.Auth.Data;
using Popug.Infrastructure.Kafka;
using Popug.Infrastructure.Security;
using Popug.Contracts;

namespace Popug.Auth.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private const string SsoTokenKey = "ssoToken";
    private readonly Cryptor _cryptor;
    private readonly AuthDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly Producer _producer;
    
    public UserController(AuthDbContext context, Cryptor cryptor, IConfiguration configuration, Producer producer)
    {
        _cryptor = cryptor;
        _context = context;
        _configuration = configuration;
        _producer = producer;
    }
    [HttpGet("/user")]
    public async Task<ActionResult<GetUserResponse>> GetUser()
    {
        HttpContext.Request.Cookies.TryGetValue(SsoTokenKey, out var token);
        if (token is null)
            return Unauthorized();
        
        var user = JsonSerializer.Deserialize<SecurityToken>(_cryptor.Decrypt(token));
        var dbUser = await _context.Users.Include(u => u.Claims).FirstOrDefaultAsync(x => x.Id == user.UserId);
        
        if (dbUser is null)
            return Unauthorized();
        
        return Ok(new GetUserResponse
        {
            Username = dbUser.Username,
            Claims = dbUser.Claims.Select(x => x.Name).ToArray(),
        });
    }
    
    [HttpPost("/user/add")]
    public async Task<ActionResult> AddUser([FromBody]AddUserRequest request)
    {
        if (request.Claims.Count == 0)
            return BadRequest("User should contain at least one claim. Use default 'user' claim");
        
        if (await _context.Users.Where(x => x.Username == request.Username).AnyAsync())
            return BadRequest("Username already exists");
        var newUser = new User()
        {
            Username = request.Username,
            PasswordHash = _cryptor.GetHash(request.Password),
            Claims = request.Claims.Select(x => new Claim() { Name = x }).ToList(),
        };
        await _context.Users.AddAsync(newUser);
        await _context.SaveChangesAsync();
        
        var userAddedMessage = new UserAdded()
        {
            Username = newUser.Username, 
            Id = newUser.Id, 
            Claims = newUser.Claims.Select(x => x.Name).ToList()
        };
        await _producer.ProduceAsync("auth-cud", userAddedMessage);
        
        return Ok();
    }
    
    [HttpPost("/user/register")]
    public async Task<ActionResult> RegisterUser([FromBody]RegisterUserRequest request)
    {
        if (request.Password == null || request.Password.Length < 6)
            return BadRequest("Password must be at least 6 characters long");
        
        if (await _context.Users.Include(u => u.Claims).Where(x => x.Username == request.Username).AnyAsync())
            return BadRequest("Username already exists");
        
        var newUser = new User()
        {
            Username = request.Username,
            PasswordHash = _cryptor.GetHash(request.Password),
            Claims = new List<Claim>() { new Claim() {Name = "user"}},
        };
        await _context.Users.AddAsync(newUser);
        await _context.SaveChangesAsync();
        
        return Ok();
    }
    
    [HttpGet("/user/list")]
    public async Task<ActionResult<List<GetAllUsersResponse>>> GetAllUsers()
    {
        var res = await _context.Users
            .Include(u => u.Claims)
            .Select(x => new GetAllUsersResponse
            {
                Username = x.Username, 
                Claims = x.Claims.Select(x => x.Name).ToArray(),
            }).ToListAsync();
        
        return Ok(res);
    }
}

public class GetAllUsersResponse
{
    public string Username { get; set; }
    public string[] Claims { get; set; }
}

public class RegisterUserRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class AddUserRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public List<string> Claims { get; set; }
}

public class GetUserResponse
{
    public string Username { get; set; }
    public string[] Claims { get; set; }
}