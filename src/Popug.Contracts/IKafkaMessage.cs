namespace Popug.Contracts;

public interface IKafkaMessage
{
    public string EventType { get; }
}