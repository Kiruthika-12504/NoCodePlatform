namespace WorkflowBackend.DTOs
{
    public class CreateWorkflowLogRequest
    {
        public Guid WorkflowId { get; set; }
        public Guid ActivityId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
