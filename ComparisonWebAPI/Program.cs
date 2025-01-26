using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
using System.Data;

var builder = WebApplication.CreateBuilder(args);
// Add services to the DI container
builder.Services.AddControllers(); // This registers all controller-related services
// Add services to DI container
builder.Services.AddScoped<IDbConnection>(provider =>
{
    // Fetch the connection string from appsettings.json
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    return new Microsoft.Data.SqlClient.SqlConnection(connectionString);
});
//for dynamic DB connection


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        policy.WithOrigins("http://localhost:5200", "http://localhost:7291") // Allow requests from the Blazor app
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register LogsRepository and LogsService
builder.Services.AddScoped<ComparisonService>();


builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://ComparisonWebAPI/") });



//builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

//app.UseMiddleware<CustomExceptionMiddleware>();
app.UseRouting();

// Use CORS policy
app.UseCors("AllowBlazorApp");

app.MapControllers();
app.UseAuthorization();
app.Run();
