using DynamicDasboardWebAPI.Repositories;
using DynamicDasboardWebAPI.Repositories.TestAutomation;
using DynamicDasboardWebAPI.Services;
using DynamicDasboardWebAPI.Services.LLM;
using DynamicDasboardWebAPI.Services.TestAutomation;
using DynamicDasboardWebAPI.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the DI container
builder.Services.AddControllers(); // This registers all controller-related services

//Register the database connection service
builder.Services.AddScoped<IDbConnection>(provider =>
{
    // Fetch the connection string from appsettings.json
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
return new Microsoft.Data.SqlClient.SqlConnection(connectionString);
});

builder.Logging.AddEventLog(config => {
    config.SourceName = "DynamicDashboard";
    config.LogName = "Application";
});

// Register the dynamic database connection factory
builder.Services.AddScoped<DbConnectionFactory>(provider =>
{
    var appDbConnection = provider.GetRequiredService<IDbConnection>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new DbConnectionFactory(appDbConnection, configuration);
});

// Configure CORS to allow requests from the Blazor app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        policy.WithOrigins("http://localhost:5200", "http://localhost:7291") // Allow requests from the Blazor app
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register repositories and services

builder.Services.AddHttpClient();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            JsonIgnoreCondition.WhenWritingNull;
    });


//builder.Services.AddMemoryCache();
//builder.Services.AddSingleton<ServerCa>();

builder.Services.AddScoped<LogsRepository>();
builder.Services.AddScoped<TableRepository>();
builder.Services.AddScoped<ColumnRepository>();
builder.Services.AddScoped<RelationshipRepository>();
builder.Services.AddScoped<ILogsService, LogsService>();
builder.Services.AddScoped<QueryRepository>();

builder.Services.AddScoped<QueryRepository>();
builder.Services.AddScoped<BatchProcessingRepository>();
builder.Services.AddScoped<QueryLogsRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<DatabaseRepository>();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<TableService>();
builder.Services.AddScoped<ColumnService>();
builder.Services.AddScoped<RelationshipService>();
builder.Services.AddScoped<QueryService>();
// Register LLM services
builder.Services.AddHttpClient<ILLMService>(); // HttpClient for LLM services

builder.Services.AddScoped<SchemaAnalysisService>();
builder.Services.AddScoped<LLMServiceFactory>();
builder.Services.AddScoped<QueryService>();
builder.Services.AddScoped<QueryService>();

// Register the batch processing service
builder.Services.AddScoped<BatchProcessingService>();
builder.Services.AddScoped<DatabaseService>();

builder.Services.AddScoped<DatabaseSchemaRepository>();
builder.Services.AddScoped<DatabaseSchemaService>();

builder.Services.AddScoped<DatasetComparisonService>();
builder.Services.AddScoped<TestAutomationRepository>();
builder.Services.AddScoped<TestAutomationService>();

builder.Services.AddScoped<DashboardRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ILLMService>(provider =>
{
    var factory = provider.GetRequiredService<LLMServiceFactory>();
    return factory.CreateLlmService();
});

// Register Dashboard services
builder.Services.AddScoped<DashboardRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IDashboardGenerationService>(provider =>
{
    var llmService = provider.GetRequiredService<ILLMService>();
    var schemaService = provider.GetRequiredService<DatabaseSchemaService>();
    var logsService = provider.GetRequiredService<ILogsService>();
    var configuration = provider.GetRequiredService<IConfiguration>(); // ADD THIS

    var templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "dashboard-templates.json");

    return new DashboardGenerationService(llmService, schemaService, logsService, templatesPath, configuration); // ADD configuration
});


// Register HttpClient with a base address
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://dynamicdashboardAPIs/"), Timeout= TimeSpan.FromMinutes(15) });

builder.Services.AddSingleton<Func<string, HttpClient>>(serviceProvider =>
{
    return (clientName) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        // Read timeout dynamically from configuration
        var timeoutSeconds = configuration.GetValue<int>("LlmService:Timeout");
        if (timeoutSeconds <= 0) timeoutSeconds = 1000; 

        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        // Apply client-specific configuration
        if (clientName == "ClaudeApi")
        {
            var apiEndpoint = configuration["LlmService:ClaudeEndpoint"];
            if (!string.IsNullOrEmpty(apiEndpoint))
                client.BaseAddress = new Uri(apiEndpoint);

            // Add Claude-specific headers
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }
        else if (clientName == "DeepSeekApi")
        {
            var apiEndpoint = configuration["LlmService:DeepSeekEndpoint"];
            if (!string.IsNullOrEmpty(apiEndpoint))
                client.BaseAddress = new Uri(apiEndpoint);
        }
        else if (clientName.ToLower() == "sqlcoder")
        {
            var apiEndpoint = configuration["LlmService:sqlcoderEndpoint"];
            if (!string.IsNullOrEmpty(apiEndpoint))
                client.BaseAddress = new Uri(apiEndpoint);
        }

        return client;
    };
});

// Register Swagger for API documentation
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Enable Swagger and developer exception page in development environment
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

// Use custom exception middleware
app.UseMiddleware<CustomExceptionMiddleware>();

app.UseRouting();

// Use CORS policy
app.UseCors("AllowBlazorApp");

// Map controller routes
app.MapControllers();

// Use authorization middleware
app.UseAuthorization();

app.Run();