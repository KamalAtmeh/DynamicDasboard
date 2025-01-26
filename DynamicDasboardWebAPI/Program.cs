using DynamicDasboardWebAPI.Repositories;
using DynamicDasboardWebAPI.Services;
using DynamicDasboardWebAPI.Utilities;
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
builder.Services.AddScoped<DbConnectionFactory>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new DbConnectionFactory(configuration);
});

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
builder.Services.AddScoped<TestRepository>();
builder.Services.AddScoped<LogsRepository>();
builder.Services.AddScoped<TableRepository>();
builder.Services.AddScoped<ColumnRepository>();
builder.Services.AddScoped<RelationshipRepository>();
builder.Services.AddScoped<ILogsService, LogsService>();
builder.Services.AddScoped<QueryRepository>();
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


builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://dynamicdashboardAPIs/") });



builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseMiddleware<CustomExceptionMiddleware>();
app.UseRouting();

// Use CORS policy
app.UseCors("AllowBlazorApp");

app.MapControllers();
app.UseAuthorization();
app.Run();
