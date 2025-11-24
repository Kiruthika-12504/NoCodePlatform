using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;
using System.Collections.Generic;

namespace WorkflowAutomation.Models;

[Table("activities")]
public class Activit : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("workflow_id")]
    public Guid WorkflowId { get; set; }

    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("order")]
    public int Order { get; set; }

    // Only one Parameters property
    [Column("parameters")]
    public Dictionary<string, string> Parameters { get; set; } = new();
    
    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

}
