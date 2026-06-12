using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using InventorySystem.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Point HttpClient at the Server API base address (from launch settings on .Server project)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5211") });

await builder.Build().RunAsync();
