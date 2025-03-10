﻿using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Popug.Auth.Data;
using Popug.Infrastructure.Security;
using Popug.Contracts;

namespace Popug.Auth.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private const string SsoTokenKey = "ssoToken";
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
        var user = await _context.Users.Where(x => x.Username == request.Username)
            .Include(user => user.Claims).FirstAsync();

        if (!_cryptor.VerifyHash(request.Password, user.PasswordHash))
            return BadRequest("No such user exists");

        var securityToken = new SecurityToken()
        {
            UserId = user.Id,
            UserName = user.Username,
            Claims = user.Claims.Select(x => x.Name).ToArray()
        };

        var token = _cryptor.Encrypt(JsonSerializer.Serialize(securityToken));
        var cookie = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddHours(24),
            SameSite = SameSiteMode.None,
            Secure = true,
            Domain = _configuration["DomainName"],
            Path = "/",
        };
        HttpContext.Response.Cookies.Append(SsoTokenKey, token, cookie);
        
        return Ok(token);
    }
    
    [HttpGet("/check")]  
    public async Task<ActionResult<string>> Check([FromQuery] string? claim)
    {
        HttpContext.Request.Cookies.TryGetValue(SsoTokenKey, out var token);
        if (token is null)
            return Unauthorized();
        
        var securityToken = JsonSerializer.Deserialize<SecurityToken>(_cryptor.Decrypt(token));
        if (securityToken is null)
            return Unauthorized();
        
        var user = await _context.Users.Include(u => u.Claims)
            .Where(x => x.Id == securityToken.UserId).FirstOrDefaultAsync();
        if (user is null)
            return Unauthorized();

        if (claim is not null && user.Claims.All(x => x.Name != claim))
            return Unauthorized();
        
        return Ok();
    }
    
    [HttpGet("/logout")]  
    public async Task<ActionResult<string>> Logout()
    {
        HttpContext.Request.Cookies.TryGetValue(SsoTokenKey, out var token);
        if (token is null)
            return Unauthorized();
        
        HttpContext.Response.Cookies.Delete(SsoTokenKey);
        return Ok();
    }
}

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}