using Microsoft.AspNetCore.Mvc;
using Popug.Infrastructure.Security;
using DomainTask = Popug.Task.Task;

namespace Popug.Task.Controllers;

[ApiController]
[AuthZ]
[Route("[controller]")]
public class TaskController : ControllerBase
{
    private readonly TaskDbContext _context;
    private readonly IConfiguration _configuration;
    
    public TaskController(TaskDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }
    
    [HttpGet]
    public async Task<ActionResult<DomainTask>> GetTask([FromQuery] int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        
        if (task == null)
            return NotFound();
        
        return Ok(task);
    }

    [HttpPut]
    public async Task<ActionResult<int>> AddTask([FromBody] AddTaskRequest request)
    {
        var task = new DomainTask
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            Created = DateTime.UtcNow,
        };
        await _context.Tasks.AddAsync(task);
        await _context.SaveChangesAsync();
        
        return task.Id;
    }
    
}

public class AddTaskRequest
{
    public required string Name { get; set; }
    public required string Description { get; set; }
}