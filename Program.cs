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
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im92bHdnYWxkcnVtdnZpZXhkbWpkIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjM2MTI3MjMsImV4cCI6MjA3OTE4ODcyM30.qF-jvnr9J2nGH-zF2xr6fA4ZYt5kLG8ZhdAoczoLIok",
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
