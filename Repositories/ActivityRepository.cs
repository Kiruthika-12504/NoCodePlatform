using Supabase;
using WorkflowBackend.Models;

namespace WorkflowBackend.Repositories;

public class ActivityRepository
{
    private readonly Client _client;

    public ActivityRepository(Client client)
    {
        _client = client;
    }

    // Create new activity
    public async Task<Activity> CreateActivity(Activity activity)
    {
        activity.Id = Guid.NewGuid();
        activity.StartedAt = null;
        activity.EndedAt = null;

        var response = await _client.From<Activity>().Insert(activity);
        return response.Models.First();
    }

    // Update activity status
    public async Task<Activity> UpdateStatus(Guid activityId, string status)
    {
        var fetchResp = await _client.From<Activity>().Where(a => a.Id == activityId).Get();
        var existing = fetchResp.Models.FirstOrDefault();
        if (existing == null) throw new InvalidOperationException($"Activity {activityId} not found");

        existing.Status = status;
        if (status == "InProgress") existing.StartedAt = DateTime.UtcNow;
        if (status == "Completed") existing.EndedAt = DateTime.UtcNow;

        var updateResp = await _client.From<Activity>().Update(existing);
        return updateResp.Models.First();
    }

    // Get activities by workflow
    // Get activities by workflow
public async Task<List<Activity>> GetByWorkflow(Guid workflowId)
{
    var response = await _client
        .From<Activity>()
        .Where(a => a.WorkflowId == workflowId)
        .Order("order", Supabase.Postgrest.Constants.Ordering.Ascending) // ✅ specify ordering
        .Get();

    return response.Models;
}

}
