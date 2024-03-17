using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UnityBuilderDiscordBot.Services;

public class DiscordStartupService : IHostedService
{
    private readonly DiscordSocketClient _discord;
    private readonly ILogger<DiscordSocketClient> _logger;

    public DiscordStartupService(DiscordSocketClient discord, ILogger<DiscordSocketClient> logger)
    {
        _discord = discord;
        _logger = logger;

        _discord.Log += Program.Log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _discord.LoginAsync(TokenType.Bot, await File.ReadAllTextAsync("DiscordToken.txt", cancellationToken));
        await _discord.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _discord.LogoutAsync();
        await _discord.StopAsync();
    }
}