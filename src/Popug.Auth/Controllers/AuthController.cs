using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Popug.Auth.Data;
using Popug.Auth.Security;

namespace Popug.Auth.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly Cryptor _cryptor;
    private readonly AuthDbContext _context;
    private readonly IConfiguration _configuration;
    
    public AuthController(AuthDbContext context, Cryptor cryptor, IConfiguration configuration)
    {
        _cryptor = cryptor;
        _context = context;
        _configuration = configuration;
    }
    
    [HttpPost]
    public async Task<ActionResult<string>> Login([FromBody]LoginRequest request)
    {
        var user = await _context.Users.Where(x => x.Username == request.Username).FirstAsync();

        if (!_cryptor.VerifyHash(request.Password, user.PasswordHash))
            return BadRequest("No such user exists");

        var token = _cryptor.Encrypt(JsonSerializer.Serialize(user));
        var cookie = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddHours(24),
            SameSite = SameSiteMode.None,
            Secure = true,
            Domain = _configuration["DomainName"],
            Path = "/",
        };

        HttpContext.Response.Cookies.Append("ssoToken", token, cookie);
        
        return Ok(token);
    }
    
    [HttpPost("/user/add")]
    public async Task<ActionResult> AddUser([FromBody]AddUserRequest request)
    {
        if (await _context.Users.Where(x => x.Username == request.Username).AnyAsync())
            return BadRequest("Username already exists");
        var newUser = new User()
        {
            Username = request.Username,
            PasswordHash = _cryptor.GetHash(request.Password),
            ClaimList = string.Join(", ", request.Claims),
        };
        await _context.Users.AddAsync(newUser);
        await _context.SaveChangesAsync();
        
        return Ok();
    }
    
    [HttpGet("/check")]  
    public async Task<ActionResult<string>> Check([FromQuery] string token)
    {
        var user = JsonSerializer.Deserialize<User>(_cryptor.Decrypt(token));
        if (!await _context.Users.Where(x => x.Username == user.Username).AnyAsync())
            return Unauthorized();
        
        return Ok();
    }
}

public class LoginRequest
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