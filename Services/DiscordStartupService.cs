using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnityBuilderDiscordBot.Utilities;

namespace UnityBuilderDiscordBot.Services;

public class DiscordStartupService : IHostedService
{
    public static DiscordSocketClient Discord => _discord;
    private static DiscordSocketClient _discord;
    private readonly ILogger<DiscordSocketClient> _logger;

    public DiscordStartupService(DiscordSocketClient discord, ILogger<DiscordSocketClient> logger)
    {
        _discord = discord;
        _logger = logger;

        _discord.Log += Program.Log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _discord.LoginAsync(TokenType.Bot, ConfigurationUtility.Configuration["Discord"]["token"]);
        await _discord.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _discord.LogoutAsync();
        await _discord.StopAsync();
    }
}