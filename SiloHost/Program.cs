using GameContracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

// Build host and silo
var host = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.UseLocalhostClustering()
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "dev";
                options.ServiceId = "PokerPlatform";
            })
            .AddMemoryGrainStorage("Default")
            .ConfigureLogging(logging => logging.AddConsole());
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .Build();

await host.RunAsync();

// Test interaction with Grain
// await host.StartAsync(); 
// await Task.Run(async () =>
// {
//     var grainFactory = host.Services.GetRequiredService<IGrainFactory>();
//     var room = grainFactory.GetGrain<IRoomGrain>("test-room");
//
//     var p1 = new PlayerInfo {PlayerId = "p1", Nickname = "Alice"};
//     await room.JoinAsync(p1);
//     await room.JoinAsync(new PlayerInfo { PlayerId = "p2", Nickname = "Bob" });
//
//     var players = await room.GetPlayersAsync();
//     Console.WriteLine("Players in test-room:");
//     foreach (var p in players)
//     {
//         Console.WriteLine($"- {p.PlayerId} ({p.Chips} chips)");
//     }
// });
//
// await host.WaitForShutdownAsync(); 