namespace Popug.Contracts;

public class UserAdded : IKafkaMessage
{
    public string Username { get; set; }
    public int Id { get; set; }
    public List<string> Claims { get; set; }
    public string EventType => nameof(UserAdded);
}