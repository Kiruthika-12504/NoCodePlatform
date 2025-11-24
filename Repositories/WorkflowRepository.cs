using Supabase;
using WorkflowBackend.Models;

namespace WorkflowBackend.Repositories;

public class WorkflowRepository
{
    private readonly Client _client;

    public WorkflowRepository(Client client)
    {
        _client = client;
    }

    // Create workflow (keeps what you had)
    public async Task<Workflow> CreateWorkflow(Workflow workflow)
    {
        workflow.Id = Guid.NewGuid();
        workflow.CreatedAt = DateTime.UtcNow;
        workflow.UpdatedAt = DateTime.UtcNow;

        var response = await _client.From<Workflow>().Insert(workflow);
        return response.Models.First();
    }

    // Get all workflows
    public async Task<List<Workflow>> GetAllWorkflows()
    {
        var response = await _client
            .From<Workflow>()
            .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    // ===== FIXED UpdateStatus =====
    public async Task<Workflow> UpdateStatus(Guid workflowId, string status)
    {
        // 1) fetch existing workflow
        var fetchResp = await _client
            .From<Workflow>()
.Where(w => w.Id == workflowId)
.Get();


        var existing = fetchResp.Models.FirstOrDefault();
        if (existing == null)
            throw new InvalidOperationException($"Workflow {workflowId} not found");

        // 2) modify fields you want to change
        existing.Status = status;
        existing.UpdatedAt = DateTime.UtcNow;

        // 3) update by passing the model instance (supported in this client)
        var updateResp = await _client.From<Workflow>().Update(existing);

        return updateResp.Models.First();
    }
}
