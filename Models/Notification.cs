using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace WorkflowBackend.Models;

[Table("notifications")]
public class Notification : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("workflow_id")]
    public Guid WorkflowId { get; set; }

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("sent")]
    public bool Sent { get; set; } = false;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
