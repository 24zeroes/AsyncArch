namespace Popug.Contracts;

public class SecurityToken
{
    public required int UserId { get; set; }
    public required string UserName { get; set; }
    public required string[] Claims { get; set; }
}