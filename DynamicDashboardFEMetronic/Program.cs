using Starterkit.Components;
using Starterkit._keenthemes;
using Starterkit._keenthemes.libs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IKTTheme, KTTheme>();
builder.Services.AddSingleton<IBootstrapBase, BootstrapBase>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
// Register HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5000/") });

var app = builder.Build();

IConfiguration themeConfiguration = new ConfigurationBuilder()
                            .AddJsonFile("_keenthemes/config/themesettings.json")
                            .Build();

IConfiguration iconsConfiguration = new ConfigurationBuilder()
                            .AddJsonFile("_keenthemes/config/icons.json")
                            .Build();

KTThemeSettings.init(themeConfiguration);
KTIconsSettings.init(iconsConfiguration);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();
//app.UseRouting();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
