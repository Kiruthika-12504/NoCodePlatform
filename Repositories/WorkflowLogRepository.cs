using Supabase;
using WorkflowBackend.Models;

namespace WorkflowBackend.Repositories;

public class WorkflowLogRepository
{
    private readonly Client _client;

    public WorkflowLogRepository(Client client)
    {
        _client = client;
    }

    // Create log
    public async Task<WorkflowLog> CreateLog(WorkflowLog log)
    {
        log.Id = Guid.NewGuid();
        log.Timestamp = DateTime.UtcNow;

        var response = await _client.From<WorkflowLog>().Insert(log);
        return response.Models.First();
    }

    // Get logs by workflow
    // Get logs by workflow
public async Task<List<WorkflowLog>> GetByWorkflow(Guid workflowId)
{
    var response = await _client
        .From<WorkflowLog>()
        .Where(l => l.WorkflowId == workflowId)
        .Order("timestamp", Supabase.Postgrest.Constants.Ordering.Ascending) // ✅ specify ordering
        .Get();

    return response.Models;
}
}
