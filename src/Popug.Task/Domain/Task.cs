namespace Popug.Task;


public class Task
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime Created { get; set; }
    public int? AssignedUserId { get; set; }
}