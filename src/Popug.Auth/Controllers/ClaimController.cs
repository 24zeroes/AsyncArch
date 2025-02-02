using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Popug.Auth.Data;
using Popug.Auth.Security;

namespace Popug.Auth.Controllers;

[ApiController]
[Route("[controller]")]
public class ClaimController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly IConfiguration _configuration;
    
    public ClaimController(AuthDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }
    [HttpPut]
    public async Task<ActionResult<GetUserResponse>> AddClaim([FromBody] ClaimRequest request)
    {
        var user = await _context.Users.Include(u => u.Claims).FirstAsync(x => x.Username == request.Username);
        
        if (user.Claims.Any(x => x.Name == request.Claim))
            return BadRequest($"User already have a claim {request.Claim}");
        
        user.Claims.Add(new Claim() { Name = request.Claim });
        await _context.SaveChangesAsync();
        
        return Ok();
    }
    
    [HttpDelete]
    public async Task<ActionResult> DeleteClaim([FromBody]ClaimRequest request)
    {
        var user = await _context.Users.Include(u => u.Claims).FirstAsync(x => x.Username == request.Username);
        
        if (user.Claims.All(x => x.Name != request.Claim))
            return BadRequest($"User do not have such a claim {request.Username}");
        
        if (user.Claims.Count == 1)
            return BadRequest($"Cannot delete last claim for user {request.Claim}");
        
        user.Claims.RemoveAll(x => x.Name == request.Claim);
        await _context.SaveChangesAsync();
        
        return Ok();
    }
}

public class ClaimRequest
{
    public string Username { get; set; }
    public string Claim { get; set; }
}

