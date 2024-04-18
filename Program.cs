using System.Net;
using Discord;
using Discord.Interactions;
using Discord.Net.Rest;
using Discord.Net.WebSockets;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnityBuilderDiscordBot.Services;
using UnityBuilderDiscordBot.Utilities;

namespace UnityBuilderDiscordBot;

internal class Program
{
    public static Task<int> Main(string[] args)
    {
        return new Program().MainAsync();
    }

    public async Task<int> MainAsync()
    {
        var title = "Unity Builder Discord Bot";

        // 计算装饰框的长度
        var boxLength = title.Length + 4;

        // 输出顶部边框
        Console.WriteLine(new string('*', boxLength));

        // 输出标题行
        Console.WriteLine("* " + title + " *");
        Console.WriteLine("* by Shepherd Zhu *");

        // 输出底部边框
        Console.WriteLine(new string('*', boxLength));

        try
        {
            ConfigurationUtility.Initialize();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load appsettings.json! \n{ex}");
            return -1;
        }

        var hostBuilder = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddLogging(builder => builder.AddConsole());
                services.AddSingleton(serviceProvider =>
                {
                    var config = new DiscordSocketConfig
                    {
                        RestClientProvider = DefaultRestClientProvider.Create(true),
                        WebSocketProvider = DefaultWebSocketProvider.Create(WebRequest.DefaultWebProxy),
                        GatewayIntents = GatewayIntents.All
                    };
                    return new DiscordSocketClient(config);
                }); // Add the discord client to services
                services.AddSingleton<InteractionService>(); // Add the interaction service to services
                services.AddHostedService<UnityEditorService>(); // Add the Unity Editor service
                services.AddHostedService<InteractionHandlingService>(); // Add the slash command handler
                services.AddHostedService<DiscordStartupService>(); // Add the discord startup service
                // services.AddHostedService<SshCredentialService>(); // Add the SSH 
                services.AddHostedService<CredentialServiceManager>();
                // services.AddHostedService<SftpFileTransferService>(); // Add the SFTP
                services.AddHostedService<FileTransferServiceManager>();
            });

        await hostBuilder.RunConsoleAsync();

        return 0;
    }

    // Example of a logging handler. This can be re-used by addons
    // that ask for a Func<LogMessage, Task>.
    public static Task Log(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Critical:
            case LogSeverity.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case LogSeverity.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case LogSeverity.Info:
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case LogSeverity.Verbose:
            case LogSeverity.Debug:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                break;
        }

        Console.WriteLine(
            $"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
        Console.ResetColor();

        // If you get an error saying 'CompletedTask' doesn't exist,
        // your project is targeting .NET 4.5.2 or lower. You'll need
        // to adjust your project's target framework to 4.6 or higher
        // (instructions for this are easily Googled).
        // If you *need* to run on .NET 4.5 for compat/other reasons,
        // the alternative is to 'return Task.Delay(0);' instead.
        return Task.CompletedTask;
    }
}