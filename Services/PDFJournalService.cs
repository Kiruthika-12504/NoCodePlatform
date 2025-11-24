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

                // Convert Word file to PDF using Syncfusion.DocIO + DocIORenderer
                var pdfBytes = ConvertWordToPdf(wordBytes);

                // Save PDF to Supabase Storage
                var pdfFileName = $"{Guid.NewGuid()}_journal.pdf";
                var pdfPath = $"pdf_journals/{pdfFileName}";

                await _client.Storage
                    .From("pdf")
                    .Upload(pdfBytes, pdfPath, new Supabase.Storage.FileOptions { CacheControl = "3600", Upsert = true });

                var pdfUrl = _client.Storage.From("pdf").GetPublicUrl(pdfPath);

                // Update activity parameters
                activit.Parameters ??= new System.Collections.Generic.Dictionary<string, string>();
                activit.Parameters["PdfUrl"] = pdfUrl;

                await _client.From<Activit>()
                    .Where(a => a.Id == activit.Id)
                    .Set(a => a.Parameters, activit.Parameters)
                    .Set(a => a.Status, "Completed")
                    .Update();

                Console.WriteLine($"✅ PDF Journal Publication Completed {activit.Id}");
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

    // Open Word document in .NET Core version
    using var wordDoc = new WordDocument();
    wordDoc.Open(wordStream, FormatType.Docx);

    // Create renderer
    using var renderer = new DocIORenderer();
    using var pdfDoc = renderer.ConvertToPDF(wordDoc);

    using var pdfStream = new MemoryStream();
    pdfDoc.Save(pdfStream);
    return pdfStream.ToArray();
}

    }
}
