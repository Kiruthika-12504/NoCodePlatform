using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace WorkflowBackend.Models;

[Table("activities")]
public class Activity : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("workflow_id")]
    public Guid WorkflowId { get; set; }

    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("parameters")]
public Dictionary<string, string> Parameters { get; set; } = new();


    [Column("order")]
    public int Order { get; set; }

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }
}
