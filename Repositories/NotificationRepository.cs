using Supabase;
using WorkflowBackend.Models;

namespace WorkflowBackend.Repositories;

public class NotificationRepository
{
    private readonly Client _client;

    public NotificationRepository(Client client)
    {
        _client = client;
    }

    // Create notification
    public async Task<Notification> CreateNotification(Notification notification)
    {
        notification.Id = Guid.NewGuid();
        notification.Timestamp = DateTime.UtcNow;
        notification.Sent = false;

        var response = await _client.From<Notification>().Insert(notification);
        return response.Models.First();
    }

    // Mark notification as sent
    public async Task<Notification> MarkAsSent(Guid notificationId)
    {
        var fetchResp = await _client.From<Notification>().Where(n => n.Id == notificationId).Get();
        var existing = fetchResp.Models.FirstOrDefault();
        if (existing == null) throw new InvalidOperationException($"Notification {notificationId} not found");

        existing.Sent = true;

        var updateResp = await _client.From<Notification>().Update(existing);
        return updateResp.Models.First();
    }

    // Get notifications by workflow
    public async Task<List<Notification>> GetByWorkflow(Guid workflowId)
    {
        var response = await _client.From<Notification>().Where(n => n.WorkflowId == workflowId).Get();
        return response.Models;
    }
}
