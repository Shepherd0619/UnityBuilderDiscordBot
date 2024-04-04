using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnityBuilderDiscordBot.Utilities;

namespace UnityBuilderDiscordBot.Services;

public class DiscordStartupService : IHostedService
{
    private readonly ILogger<DiscordSocketClient> _logger;

    public DiscordStartupService(DiscordSocketClient discord, ILogger<DiscordSocketClient> logger)
    {
        Discord = discord;
        _logger = logger;

        Discord.Log += Program.Log;
    }

    public static DiscordSocketClient Discord { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Discord.LoginAsync(TokenType.Bot, ConfigurationUtility.Configuration["Discord"]["token"]);
        await Discord.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Discord.LogoutAsync();
        await Discord.StopAsync();
    }
}