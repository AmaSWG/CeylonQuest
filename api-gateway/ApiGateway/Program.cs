var builder = WebApplication.CreateBuilder(args);

// Listen on a local development port different from Identity Service
builder.WebHost.UseUrls("http://localhost:5000");

// Add named CORS policy for frontend development
const string FrontendPolicy = "FrontendDevelopment";
builder.Services.AddCors(options =>
{
	options.AddPolicy(FrontendPolicy, policy =>
	{
		policy.WithOrigins("http://localhost:5174")
			  .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
			  .AllowAnyHeader();
	});
});

// Add YARP reverse proxy and load routes/clusters from configuration
builder.Services.AddReverseProxy()
	.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/", () => "API Gateway is running.");

// Enable CORS before mapping the reverse proxy so preflight (OPTIONS)
// requests are handled and forwarded correctly by YARP.
app.UseCors(FrontendPolicy);

// Map the reverse proxy endpoints configured in appsettings
app.MapReverseProxy();

app.Run();
