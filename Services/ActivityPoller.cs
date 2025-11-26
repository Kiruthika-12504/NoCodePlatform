using Microsoft.Extensions.Hosting;
using Supabase; // Use only this
using WorkflowAutomation.Models;
using WorkflowAutomation.Services;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace WorkflowAutomation.Services
{
    public class ActivityPoller : BackgroundService
    {
        private readonly Supabase.Client _client;
        private readonly OnlineQCService _qcService;
        private readonly PdfJournalService _pdfJournalService;
        private readonly PackageCreationService _packageCreationService;

        public ActivityPoller(
            Supabase.Client client,
            OnlineQCService qcService,
            PdfJournalService pdfJournalService,
            PackageCreationService packageCreationService
        )
        {
            _client = client;
            _qcService = qcService;
            _pdfJournalService = pdfJournalService;
            _packageCreationService = packageCreationService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("✅ Activity Poller started...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Fetch all pending activities, ordered by "order"
                    var resp = await _client
                        .From<Activit>()
                        .Where(a => a.Status == "Pending")
                        .Order("order", Supabase.Postgrest.Constants.Ordering.Ascending)
                        .Get();

                    // Process only the first pending activity
                    var activit = resp.Models.FirstOrDefault();
                    if (activit == null)
                    {
                        Console.WriteLine("⏳ No pending activities...");
                        await Task.Delay(5000, stoppingToken);
                        continue;
                    }

                    // Handle Start/End activities directly
                    if (activit.Type == "Start" || activit.Type == "End")
                    {
                        Console.WriteLine($"⏭ Marking {activit.Type} activity as Completed: {activit.Id}");
                        activit.Status = "Completed";
                        await _client.From<Activit>().Update(activit);
                    }
                    else
                    {
                        Console.WriteLine($"✅ Processing: {activit.Type} ({activit.Id})");
                        switch (activit.Type)
                        {
                            case "Online QC":
                                await _qcService.Process(activit);
                                break;

                            case "PDF Journal Publication":
                                await _pdfJournalService.Process(activit);
                                break;

                            case "Package Creation":
                                // Only run if PdfUrl exists
                                if (activit.Parameters == null || !activit.Parameters.ContainsKey("PdfUrl"))
                                {
                                    Console.WriteLine($"⏳ Skipping Package Creation {activit.Id} – PdfUrl not ready yet");
                                    break;
                                }
                                await _packageCreationService.Process(activit);
                                break;

                            default:
                                Console.WriteLine($"⚠️ Unknown activity type: {activit.Type}");
                                break;
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"⚠️ HTTP error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Poller general error: {ex.Message}");
                }

                // Wait 5 seconds before the next poll
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
