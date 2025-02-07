using Microsoft.EntityFrameworkCore;

namespace Popug.Task;

public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<Task> Tasks => Set<Task>();
    public DbSet<User> Users => Set<User>();
}