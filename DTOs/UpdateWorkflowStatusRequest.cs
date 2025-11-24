namespace WorkflowBackend.DTOs
{
    public class UpdateWorkflowStatusRequest
    {
        public Guid WorkflowId { get; set; }
        public string Status { get; set; } = string.Empty; 
    }
}
