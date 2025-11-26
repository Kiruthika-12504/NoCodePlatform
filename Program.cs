using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Supabase;
using WorkflowAutomation.Services;
using Syncfusion.Licensing;

SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JEaF5cXmRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXhcdXRdQmleUkx2WUJWYE0=");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton(provider =>
        {
            return new Client(
                "https://ovlwgaldrumvviexdmjd.supabase.co",
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im92bHdnYWxkcnVtdnZpZXhkbWpkIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2MzYxMjcyMywiZXhwIjoyMDc5MTg4NzIzfQ.5xmgI4a7xUBOMvh4QIkmmTpoaeoXXZBME6v3QwEaq4Q",
                new SupabaseOptions
                {
                    AutoConnectRealtime = false
                });
        });
        services.AddSingleton<OnlineQCService>();
        services.AddSingleton<PdfJournalService>();
        services.AddSingleton<PackageCreationService>();
        services.AddHostedService<ActivityPoller>();
    })
    .Build();

await host.RunAsync();
