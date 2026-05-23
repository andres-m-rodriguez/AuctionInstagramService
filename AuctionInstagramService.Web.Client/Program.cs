using AuctionInstagramService.Application.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuctionAppClients(new Uri(builder.HostEnvironment.BaseAddress));

await builder.Build().RunAsync();
