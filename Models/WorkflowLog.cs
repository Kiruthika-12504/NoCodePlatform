using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace WorkflowBackend.Models;

[Table("workflow_logs")]
public class WorkflowLog : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("workflow_id")]
    public Guid WorkflowId { get; set; }

    [Column("activity_id")]
    public Guid? ActivityId { get; set; }

    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
