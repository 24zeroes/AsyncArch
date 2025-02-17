namespace Popug.Contracts;

public class UserAdded : IKafkaMessage
{
    public required string Username { get; set; }
    public int Id { get; set; }
    public required List<string> Claims { get; set; }

    public string EventType => GetType().FullName!;
}