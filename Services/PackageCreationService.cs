using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Supabase;
using WorkflowAutomation.Models;
using System.Collections.Generic;

namespace WorkflowAutomation.Services
{
    public class PackageCreationService
    {
        private readonly Client _client;
        private readonly HttpClient _httpClient;

        public PackageCreationService(Client client)
        {
            _client = client;
            _httpClient = new HttpClient();
        }

        public async Task Process(Activit activit)
        {
            Console.WriteLine($"🔄 Starting Package Creation for Activity {activit.Id}");

            try
            {
                // Mark activity as Processing
                activit.Status = "Processing";
                await _client.From<Activit>().Update(activit);

                // Get PDF URL from activity parameters
                if (activit.Parameters == null ||
                    !activit.Parameters.TryGetValue("PdfUrl", out string pdfUrl) ||
                    string.IsNullOrWhiteSpace(pdfUrl))
                    throw new Exception("PdfUrl missing in activity parameters.");

                Console.WriteLine($"File URL for zipping: {pdfUrl}");

                // Download PDF
                var fileBytes = await _httpClient.GetByteArrayAsync(pdfUrl);
                if (fileBytes == null || fileBytes.Length == 0)
                    throw new Exception("Downloaded PDF file is empty.");

                var fileName = Path.GetFileName(pdfUrl);

                // Create ZIP in memory
                using var zipStream = new MemoryStream();
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    var entry = archive.CreateEntry(fileName);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                }
                zipStream.Position = 0;

                // Upload ZIP
                var zipFileName = $"{activit.Id}_package.zip";
                var zipPath = $"packages/{zipFileName}";

                await _client.Storage
                    .From("pdf")
                    .Upload(zipStream.ToArray(), zipPath, new Supabase.Storage.FileOptions
                    {
                        CacheControl = "3600",
                        Upsert = true
                    });

                var zipUrl = _client.Storage.From("pdf").GetPublicUrl(zipPath);

                // Update activity with ZIP URL and mark Completed
                activit.Parameters ??= new Dictionary<string, string>();
                activit.Parameters["ZipUrl"] = zipUrl;
                activit.Status = "Completed";

                await _client.From<Activit>().Update(activit);

                Console.WriteLine($"✅ Package Creation Completed for Activity {activit.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ PackageCreationService.Process error: {ex.Message}");
                try
                {
                    activit.Status = "Failed";
                    await _client.From<Activit>().Update(activit);
                }
                catch { }
            }
        }
    }
}
