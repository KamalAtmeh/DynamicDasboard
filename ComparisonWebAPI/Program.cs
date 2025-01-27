using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the DI container
builder.Services.AddControllers(); // This registers all controller-related services

// Add a scoped service for IDbConnection to the DI container
builder.Services.AddScoped<IDbConnection>(provider =>
{
    // Fetch the connection string from appsettings.json
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    // Return a new SqlConnection using the fetched connection string
    return new Microsoft.Data.SqlClient.SqlConnection(connectionString);
});
// This allows for dynamic DB connection management

// Configure CORS to allow requests from specific origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        policy.WithOrigins("http://localhost:5200", "http://localhost:7291") // Allow requests from the Blazor app
              .AllowAnyHeader() // Allow any header
              .AllowAnyMethod(); // Allow any method (GET, POST, etc.)
    });
});

// Register ComparisonService as a scoped service
builder.Services.AddScoped<ComparisonService>();

// Register HttpClient with a base address for the ComparisonWebAPI
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://ComparisonWebAPI/") });

// Uncomment the following lines to enable Swagger for API documentation
// builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Enable the developer exception page in development environment
    app.UseDeveloperExceptionPage();

    // Uncomment the following lines to enable Swagger UI in development environment
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

// Uncomment the following line to use custom exception middleware
// app.UseMiddleware<CustomExceptionMiddleware>();

// Enable routing
app.UseRouting();

// Use the configured CORS policy
app.UseCors("AllowBlazorApp");

// Map controller routes
app.MapControllers();

// Enable authorization
app.UseAuthorization();

// Run the application
app.Run();
