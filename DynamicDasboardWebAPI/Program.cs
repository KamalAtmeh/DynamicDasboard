using DynamicDasboardWebAPI.Repositories;
using DynamicDasboardWebAPI.Services;
using DynamicDasboardWebAPI.Utilities;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the DI container
builder.Services.AddControllers(); // This registers all controller-related services

// Register the database connection service
builder.Services.AddScoped<IDbConnection>(provider =>
{
    // Fetch the connection string from appsettings.json
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    return new Microsoft.Data.SqlClient.SqlConnection(connectionString);
});

// Register the dynamic database connection factory
builder.Services.AddScoped<DbConnectionFactory>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new DbConnectionFactory(configuration);
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
builder.Services.AddScoped<LogsRepository>();
builder.Services.AddScoped<TableRepository>();
builder.Services.AddScoped<ColumnRepository>();
builder.Services.AddScoped<RelationshipRepository>();
builder.Services.AddScoped<ILogsService, LogsService>();
builder.Services.AddScoped<QueryRepository>();

builder.Services.AddScoped<NlQueryRepository>();
builder.Services.AddScoped<BatchProcessingRepository>();
builder.Services.AddHttpClient<QueryGeneratorService>();
builder.Services.AddScoped<QueryLogsRepository>();
//builder.Services.AddScoped<QueryService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<DatabaseRepository>();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<TableService>();
builder.Services.AddScoped<ColumnService>();
builder.Services.AddScoped<RelationshipService>();
builder.Services.AddScoped<QueryService>();

// Add NlQueryService
builder.Services.AddScoped<NlQueryService>();
// Register the batch processing service
builder.Services.AddScoped<BatchProcessingService>();
builder.Services.AddScoped<IDataAccessService, DataAccessService>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();

// Register HttpClient with a base address
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://dynamicdashboardAPIs/") });

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
