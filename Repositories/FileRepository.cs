using Supabase;
using WorkflowBackend.DTOs;
using WorkflowBackend.Models;

namespace WorkflowBackend.Repositories
{
    public class FileRepository
    {
        private readonly Client _client;

        public FileRepository(Client client)
        {
            _client = client;
        }

        /// <summary>
        /// Uploads a file to Supabase Storage and returns its public URL.
        /// </summary>
        public async Task<string> Upload(IFormFile file, string folder)
        {
            using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var path = $"{folder}/{fileName}";

            var result = await _client.Storage
                .From("pdf")
                .Upload(bytes, path, new Supabase.Storage.FileOptions
                {
                    CacheControl = "3600",
                    Upsert = true
                });

            var url = _client.Storage
                .From("pdf")
                .GetPublicUrl(path);

            return url;
        }

        /// <summary>
        /// Uploads a file and updates the specified activity's parameters with the file URL.
        /// </summary>
     public async Task<string> UploadAndAttachToActivity(IFormFile file, string folder, Guid activityId)
{
    // 1️⃣ Upload file
    var fileUrl = await Upload(file, folder);

    // 2️⃣ Get workflow ID of this activity
    var activity = await _client
        .From<Activity>()
        .Where(a => a.Id == activityId)
        .Single();

    if (activity == null)
        throw new Exception("Activity not found.");

    var workflowId = activity.WorkflowId;

    // 3️⃣ Prepare parameters dictionary
    var parameters = new Dictionary<string, string>
    {
        { "FileUrl", fileUrl }
    };

    // 4️⃣ Update all activities under the same workflow
    await _client
        .From<Activity>()
        .Where(a => a.WorkflowId == workflowId)
        .Set(a => a.Parameters, parameters)
        .Update();

    return fileUrl;
}

public async Task<string?> GetSignedUrlByActivity(Guid activityId, int expiresInSeconds)
{
    // 1️⃣ Fetch the activity
    var activity = await _client
        .From<Activity>()
        .Where(a => a.Id == activityId)
        .Single();

    if (activity == null || activity.Parameters == null || !activity.Parameters.ContainsKey("FileUrl"))
        return null;

    // 2️⃣ Get the stored file path
    var path = activity.Parameters["FileUrl"];

    // 3️⃣ Generate signed URL (await if it's async)
    var signedUrl = await _client.Storage
        .From("pdf")
        .CreateSignedUrl(path, expiresInSeconds);  // <-- await here

    return signedUrl;
}


    }
}
