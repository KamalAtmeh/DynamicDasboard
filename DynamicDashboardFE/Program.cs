using Blazored.Toast;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

namespace DynamicDashboardFE
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Create a new WebAssemblyHostBuilder instance with default settings.
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            // Blazored Toast (Existing)
            builder.Services.AddBlazoredToast();

            // MudBlazor Services (NEW)
            builder.Services.AddMudServices();

            // Add the root component App to the HTML element with id "app".
            builder.RootComponents.Add<App>("#app");

            // Add the HeadOutlet component to the HTML head element.
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // Configure the HttpClient service with the base address of the backend API.
            // ORIGINAL CONFIGURATION - DO NOT CHANGE
            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5000/"),
                Timeout = TimeSpan.FromMinutes(5)
            });

            // Build and run the WebAssembly host.
            await builder.Build().RunAsync();
        }
    }
}