namespace WorkflowBackend.DTOs
{
    public class CreateActivityRequest
    {
        public Guid WorkflowId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public Dictionary<string, string> Parameters { get; set; } = new();
        public int Order { get; set; }
    }
}
