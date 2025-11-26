using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Supabase;
using WorkflowAutomation.Models;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;
using Syncfusion.DocIO;
using System.Collections.Generic;
using System.Linq;

namespace WorkflowAutomation.Services
{
    public class PdfJournalService
    {
        private readonly Client _client;
        private readonly HttpClient _httpClient;

        public PdfJournalService(Client client)
        {
            _client = client;
            _httpClient = new HttpClient();
        }

        public async Task Process(Activit activit)
        {
            Console.WriteLine($"🔄 Starting PDF Journal Publication for {activit.Id}");

            try
            {
                // Mark processing
                await _client.From<Activit>()
                    .Where(a => a.Id == activit.Id)
                    .Set(a => a.Status, "Processing")
                    .Update();

                // Get Word file URL from activity parameters
                if (activit.Parameters == null ||
                    !activit.Parameters.TryGetValue("FileUrl", out string fileUrl) ||
                    string.IsNullOrWhiteSpace(fileUrl))
                    throw new Exception("FileUrl missing in activity parameters.");

                Console.WriteLine($"Word file URL: {fileUrl}");

                // Download Word file
                var wordBytes = await _httpClient.GetByteArrayAsync(fileUrl);
                if (wordBytes == null || wordBytes.Length == 0)
                    throw new Exception("Downloaded Word file is empty.");

                // Convert Word file to PDF
                var pdfBytes = ConvertWordToPdf(wordBytes);

                // Upload PDF to Supabase storage
                var pdfFileName = $"{Guid.NewGuid()}_journal.pdf";
                var pdfPath = $"pdf_journals/{pdfFileName}";

                await _client.Storage
                    .From("pdf")
                    .Upload(pdfBytes, pdfPath, new Supabase.Storage.FileOptions
                    {
                        CacheControl = "3600",
                        Upsert = true
                    });

                var pdfUrl = _client.Storage.From("pdf").GetPublicUrl(pdfPath);

                // Update current activity
                activit.Parameters ??= new Dictionary<string, string>();
                activit.Parameters["PdfUrl"] = pdfUrl;
                activit.Status = "Completed";

                await _client.From<Activit>().Update(activit);

                Console.WriteLine($"✅ PDF Journal Publication Completed {activit.Id}");

                // --- Propagate PdfUrl to the next Package Creation activity ---
                var nextPackageResp = await _client
                    .From<Activit>()
                    .Where(a => a.Status == "Pending" && a.Type == "Package Creation")
                    .Order("order", Supabase.Postgrest.Constants.Ordering.Ascending)
                    .Get();

                var nextPackageActivity = nextPackageResp.Models.FirstOrDefault();
                if (nextPackageActivity != null)
                {
                    nextPackageActivity.Parameters ??= new Dictionary<string, string>();
                    nextPackageActivity.Parameters["PdfUrl"] = pdfUrl;

                    await _client.From<Activit>().Update(nextPackageActivity);
                    Console.WriteLine($"➡️ PdfUrl propagated to next Package Creation activity: {nextPackageActivity.Id}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ PdfJournalService.Process error: {ex.Message}");
                try
                {
                    await _client.From<Activit>()
                        .Where(a => a.Id == activit.Id)
                        .Set(a => a.Status, "Failed")
                        .Update();
                }
                catch { }
            }
        }

        private byte[] ConvertWordToPdf(byte[] wordBytes)
        {
            using var wordStream = new MemoryStream(wordBytes);
            using var wordDoc = new WordDocument();
            wordDoc.Open(wordStream, FormatType.Docx);

            using var renderer = new DocIORenderer();
            using var pdfDoc = renderer.ConvertToPDF(wordDoc);

            using var pdfStream = new MemoryStream();
            pdfDoc.Save(pdfStream);
            return pdfStream.ToArray();
        }
    }
}
