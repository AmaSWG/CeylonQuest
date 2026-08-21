var builder = WebApplication.CreateBuilder(args);

// Listen on a local development port different from Identity Service
builder.WebHost.UseUrls("http://localhost:5000");

// Add YARP reverse proxy and load routes/clusters from configuration
builder.Services.AddReverseProxy()
	.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/", () => "API Gateway is running.");

// Map the reverse proxy endpoints configured in appsettings
app.MapReverseProxy();

app.Run();
