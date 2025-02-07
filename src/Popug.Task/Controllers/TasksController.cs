using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Popug.Infrastructure.Security;
using DomainTask = Popug.Task.Task;

namespace Popug.Task.Controllers;

[ApiController]
[AuthZ]
[Route("task")]
public class TasksController : ControllerBase
{
    private readonly TaskDbContext _context;
    private readonly IConfiguration _configuration;
    
    public TasksController(TaskDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }
    
    [HttpGet]
    public async Task<ActionResult<DomainTask>> GetTask([FromQuery] int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        var userId = HttpContext.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
        
        if (task == null)
            return NotFound();
        
        return Ok(task);
    }
    
    [HttpGet("list")]
    public async Task<ActionResult<IList<TaskListResponse>>> GetTaskList()
    {
        return Ok(await _context.Tasks.Select(t => new TaskListResponse {TaskName = t.Name, AssignedUserName = t.AssignedUser.Name}).ToListAsync());
    }
    
    [HttpGet("/myTasks")]
    public async Task<ActionResult> GetMyTask()
    {
        var claimUserId = HttpContext.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
        
        if (!int.TryParse(claimUserId, out int userId))
            return Unauthorized();
        
        var res = await _context.Tasks.Where(t => t.AssignedUserId == userId).ToArrayAsync();
        
        return Ok(res);
    }

    [HttpPut]
    public async Task<ActionResult<int>> AddTask([FromBody] AddTaskRequest request)
    {
        var task = new DomainTask
        {
            Name = request.Name,
            Description = request.Description,
            IsDone = false,
            Created = DateTime.UtcNow,
        };
        await _context.Tasks.AddAsync(task);
        await _context.SaveChangesAsync();
        
        return task.Id;
    }
    
    [AuthZ("admin")]
    [HttpPost("shuffle")]
    public async Task<ActionResult> Shuffle()
    {
        var tasks = await _context.Tasks.Where(t => !t.IsDone).ToListAsync();
        var users = await _context.Users.ToListAsync();

        foreach (var task in tasks)
        {
            var random = new Random();
            var randomIndex = random.Next(0, users.Count);
            var user = users[randomIndex];
            
            task.AssignedUser = user;
        }
        
        await _context.SaveChangesAsync();
        return Ok();
    }
    
}

public class AddTaskRequest
{
    public required string Name { get; set; }
    public required string Description { get; set; }
}

public class TaskListResponse
{
    public string TaskName { get; set; }
    public string AssignedUserName { get; set; }
}