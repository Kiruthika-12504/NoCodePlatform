using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System.Text.Json.Nodes;
using WorkflowBackend.DTOs;

namespace WorkflowBackend.Models
{
    [Table("workflows")]
    public class Workflow : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("status")]
        public string Status { get; set; } = "Pending";

        [Column("workflow_json")]
        public WorkflowJsonData WorkflowJson { get; set; } = new();

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}