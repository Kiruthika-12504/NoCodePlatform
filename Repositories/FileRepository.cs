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

            // 2️⃣ Update activity parameters in Supabase
            await _client
    .From<Activity>()
    .Where(a => a.Id == activityId)
    .Set(a => a.Parameters, new Dictionary<string, string> { { "FileUrl", fileUrl } })
    .Update();


            return fileUrl;
        }
    }
}
