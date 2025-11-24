using Microsoft.Extensions.Hosting;
using Supabase; // Use only this
using WorkflowAutomation.Models;
using WorkflowAutomation.Services;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                    var resp = await _client
                        .From<Activit>()
                        .Where(a => a.Status == "Pending")
                        .Order("order", Supabase.Postgrest.Constants.Ordering.Ascending)
                        .Get();

                    var activit = resp.Models.FirstOrDefault();

                    if (activit != null)
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
                                await _packageCreationService.Process(activit);
                                break;

                            default:
                                Console.WriteLine($"⚠️ Unknown activity type: {activit.Type}");
                                break;
                        }
                    }
                    else
                    {
                        Console.WriteLine("⏳ No pending activities...");
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

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
