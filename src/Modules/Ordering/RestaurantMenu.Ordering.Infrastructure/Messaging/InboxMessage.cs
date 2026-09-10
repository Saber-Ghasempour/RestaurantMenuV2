namespace RestaurantMenu.Ordering.Infrastructure.Messaging;

public sealed class InboxMessage
{
    private InboxMessage() { Consumer = string.Empty; EventName = string.Empty; }
    internal InboxMessage(Guid messageId, string consumer, string eventName,
        DateTimeOffset receivedAtUtc)
    { MessageId = messageId; Consumer = consumer; EventName = eventName; ReceivedAtUtc = receivedAtUtc; }

    public Guid MessageId { get; private set; }
    public string Consumer { get; private set; }
    public string EventName { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public DateTimeOffset? DeadLetteredAtUtc { get; private set; }
    public string? LastError { get; private set; }

    internal void MarkProcessed(DateTimeOffset now)
    { AttemptCount++; ProcessedAtUtc = now; LastError = null; }
    internal void MarkFailed(string error, DateTimeOffset now, int maximumAttempts)
    {
        AttemptCount++; LastError = error.Length <= 2000 ? error : error[..2000];
        if (AttemptCount >= maximumAttempts) DeadLetteredAtUtc = now;
    }
}
