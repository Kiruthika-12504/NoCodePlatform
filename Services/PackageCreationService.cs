using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Supabase;
using WorkflowAutomation.Models;

namespace WorkflowAutomation.Services
{
    public class PackageCreationService
    {
        private readonly Client _client;

        public PackageCreationService(Client client)
        {
            _client = client;
        }

        public async Task Process(Activit activit)
        {
            Console.WriteLine($"🔄 Starting Package Creation for {activit.Id}");

            try
            {
                // Mark activity as processing
                await _client
                    .From<Activit>()
                    .Where(a => a.Id == activit.Id)
                    .Set(a => a.Status, "Processing")
                    .Update();

                // 1️⃣ List all PDF files in the pdf_journals folder
                var objects = await _client.Storage
                    .From("pdf")
                    .List("pdf_journals/"); // Make sure the trailing slash is included

                // Filter only PDF files
                var pdfFiles = objects
                    .Where(f => f.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!pdfFiles.Any())
                    throw new Exception("No PDF files found in pdf_journals folder.");

                Console.WriteLine($"Found {pdfFiles.Count} PDF(s) to package.");

                // 2️⃣ Download PDFs and create ZIP
                using var zipStream = new MemoryStream();
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    foreach (var pdf in pdfFiles)
                    {
                        // Use the full path in the bucket
                        var fullPath = $"pdf_journals/{pdf.Name}";

                        var pdfBytes = await _client.Storage
                            .From("pdf")
                            .Download(fullPath, onProgress: null);

                        var entry = archive.CreateEntry(pdf.Name); // Use only the file name inside ZIP
                        using var entryStream = entry.Open();
                        await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
                    }
                }

                zipStream.Position = 0;

                // 3️⃣ Upload ZIP back to bucket
                var zipFileName = $"{Guid.NewGuid()}_package.zip";
                var zipPath = $"packages/{zipFileName}";

                await _client.Storage
                    .From("pdf")
                    .Upload(zipStream.ToArray(), zipPath, new Supabase.Storage.FileOptions
                    {
                        CacheControl = "3600",
                        Upsert = true
                    });

                var zipUrl = _client.Storage.From("pdf").GetPublicUrl(zipPath);

                // 4️⃣ Update activity parameters and mark completed
                activit.Parameters ??= new System.Collections.Generic.Dictionary<string, string>();
                activit.Parameters["ZipUrl"] = zipUrl;

                await _client
                    .From<Activit>()
                    .Where(a => a.Id == activit.Id)
                    .Set(a => a.Parameters, activit.Parameters)
                    .Set(a => a.Status, "Completed")
                    .Update();

                Console.WriteLine($"✅ Package Creation Completed {activit.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ PackageCreationService.Process error: {ex.Message}");
                try
                {
                    await _client
                        .From<Activit>()
                        .Where(a => a.Id == activit.Id)
                        .Set(a => a.Status, "Failed")
                        .Update();
                }
                catch { }
            }
        }
    }
}
