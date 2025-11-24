using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Supabase;
using static Supabase.Postgrest.Constants;
using WorkflowAutomation.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WorkflowAutomation.Services
{
    public class OnlineQCService
    {
        private readonly Client _client;
        private readonly HttpClient _httpClient;

        public OnlineQCService(Client client)
        {
            _client = client;
            _httpClient = new HttpClient();
        }

        public async Task Process(Activit activit)
        {
            Console.WriteLine($"🔄 Starting Online QC for {activit?.Id}");

            try
            {
                if (activit?.Parameters == null)
                    throw new Exception("Activity.Parameters is null.");

                // Mark processing
                await _client
                    .From<Activit>()
                    .Where(a => a.Id == activit.Id)
                    .Set(a => a.Status, "Processing")
                    .Update();

                await _client
                    .From<Activit>()
                    .Where(a => a.Id == activit.Id)
                    .Set(a => a.StartedAt, DateTime.UtcNow)
                    .Update();

                // Get file URL from Parameters
                if (!activit.Parameters.TryGetValue("FileUrl", out string fileUrl) || string.IsNullOrWhiteSpace(fileUrl))
                    throw new Exception("FileUrl parameter is missing or empty.");

                Console.WriteLine($"File URL: {fileUrl}");

                // Download file
                var fileBytes = await DownloadFile(fileUrl);
                if (fileBytes == null || fileBytes.Length == 0)
                    throw new Exception("Downloaded file is empty.");

                Console.WriteLine($"✅ Downloaded {fileBytes.Length} bytes.");

                // Extract Word data
                var extractedData = await ExtractDataFromWord(fileBytes);

                // Validate
                var (isValid, validationErrors) = ValidateData(extractedData);

                // Save QC results
                await SaveQcResults(activit, extractedData, isValid);

                if (isValid)
                    Console.WriteLine($"✅ Online QC Completed {activit.Id}");
                else
                {
                    Console.WriteLine($"⚠️ Online QC Failed {activit.Id}");
                    foreach (var err in validationErrors)
                        Console.WriteLine($"    - {err}");
                }

                // Activate next activity
                if (isValid)
                    await ActivateNextActivit(activit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ OnlineQCService.Process error for {activit?.Id}: {ex.Message}");
                try
                {
                    if (activit != null)
                    {
                        await _client
                            .From<Activit>()
                            .Where(a => a.Id == activit.Id)
                            .Set(a => a.Status, "Failed")
                            .Update();
                    }
                }
                catch { }
            }
        }

        #region Helper Methods

        private async Task<byte[]> DownloadFile(string url)
        {
            try
            {
                return await _httpClient.GetByteArrayAsync(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error downloading file: {ex.Message}");
                throw;
            }
        }

        private async Task<Dictionary<string, string>> ExtractDataFromWord(byte[] fileBytes)
        {
            var data = new Dictionary<string, string>();

            try
            {
                using var ms = new MemoryStream(fileBytes);
                using var doc = WordprocessingDocument.Open(ms, false);

                int wordCount = 0;
                int paraCount = 0;

                var paragraphs = doc.MainDocumentPart.Document.Body.Elements<Paragraph>();
                paraCount = paragraphs.Count();

                foreach (var para in paragraphs)
                {
                    if (!string.IsNullOrWhiteSpace(para.InnerText))
                        wordCount += para.InnerText.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
                }

                data["WordCount"] = wordCount.ToString();
                data["Paragraphs"] = paraCount.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error reading Word file: {ex.Message}");
                throw;
            }

            await Task.CompletedTask;
            return data;
        }

        private (bool isValid, List<string> errors) ValidateData(Dictionary<string, string> data)
        {
            var errors = new List<string>();
            bool isValid = true;

            if (!int.TryParse(data.GetValueOrDefault("WordCount"), out int wc) || wc < 50)
            {
                errors.Add("Word count is too low (<50).");
                isValid = false;
            }

            if (!int.TryParse(data.GetValueOrDefault("Paragraphs"), out int paraCount) || paraCount < 2)
            {
                errors.Add("Document has too few paragraphs (<2).");
                isValid = false;
            }

            return (isValid, errors);
        }

        private async Task SaveQcResults(Activit activit, Dictionary<string, string> data, bool isValid)
        {
            await _client
                .From<Activit>()
                .Where(a => a.Id == activit.Id)
                .Set(a => a.Parameters, data)
                .Set(a => a.Status, isValid ? "Completed" : "Failed")
                .Set(a => a.EndedAt, DateTime.UtcNow)
                .Update();
        }

        private async Task ActivateNextActivit(Activit completed)
        {
            try
            {
                var resp = await _client
                    .From<Activit>()
                    .Filter("workflow_id", Operator.Equals, completed.WorkflowId.ToString())
                    .Filter("\"order\"", Operator.Equals, completed.Order + 1)
                    .Get();

                var next = resp.Models.FirstOrDefault();

                if (next != null)
                {
                    Console.WriteLine($"➡️ Activating next activity: {next.Type} ({next.Id})");

                    await _client
                        .From<Activit>()
                        .Where(a => a.Id == next.Id)
                        .Set(a => a.Status, "Pending")
                        .Update();
                }
                else
                {
                    Console.WriteLine($"🏁 Workflow {completed.WorkflowId} completed (no next activity).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ ActivateNextActivity error: {ex.Message}");
            }
        }

        #endregion
    }
}
