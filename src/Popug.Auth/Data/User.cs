namespace Popug.Auth.Data;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    
    public List<Claim> Claims { get; set; } = new List<Claim>();
}