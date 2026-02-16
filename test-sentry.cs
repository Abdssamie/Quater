using Sentry;

// Initialize Sentry
SentrySdk.Init(o =>
{
    o.Dsn = "https://1bfe6017932565499080e1ff518bbb17@o4509589925527552.ingest.de.sentry.io/4510886764478545";
    o.Debug = true;
    o.Environment = "test";
});

// Send test message
SentrySdk.CaptureMessage("Hello Sentry - Quater API verification test!");

Console.WriteLine("Test message sent to Sentry. Waiting for transmission...");

// Flush to ensure the event is sent before the app exits
await SentrySdk.FlushAsync(TimeSpan.FromSeconds(5));

Console.WriteLine("Done! Check your Sentry dashboard for the test event.");