namespace WorkflowBackend.DTOs
{
    public class CreateWorkflowRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";

        public WorkflowJsonData WorkflowJson { get; set; } = new();
    }

    public class WorkflowJsonData
    {
        public List<WorkflowActivity> Activities { get; set; } = new();
    }

    public class WorkflowActivity
    {
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
