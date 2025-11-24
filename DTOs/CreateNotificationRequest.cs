namespace WorkflowBackend.DTOs
{
    public class CreateNotificationRequest
    {
        public Guid WorkflowId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
