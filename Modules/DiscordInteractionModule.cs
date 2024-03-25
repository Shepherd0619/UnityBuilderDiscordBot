using System.Text;
using Discord;
using Discord.Interactions;
using UnityBuilderDiscordBot.Controllers;
using UnityBuilderDiscordBot.Models;
using UnityBuilderDiscordBot.Services;
using UnityBuilderDiscordBot.Utilities;

namespace UnityBuilderDiscordBot.Modules;

public class DiscordInteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("hello-world", "Check whether the bot can say something.")]
    public Task HelloWorld()
    {
        return RespondAsync("Hello World!");
    }

    [SlashCommand("hello-world-private", "Hello world but only visible to you.")]
    public Task HelloWorldPrivate()
    {
        return RespondAsync("Hello World!", ephemeral: true);
    }

    [SlashCommand("say", "Make the bot say something.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task Say(string text)
    {
        Console.WriteLine($"{DateTime.Now,-19} [{GetType()}.Say(\"{text}\")] Invoke.");
        ReplyAsync(text, true);
        return RespondAsync($"Message sent!");
    }

    [SlashCommand("about", "Print the introduction of this bot.")]
    public Task About()
    {
        return RespondAsync("Hi I am Unity Builder Bot, developed by Shepherd Zhu (AKA. Shepherd0619).\n" +
                            "He is a nice Chinese guy and he likes yandere, Black Lagoon, Unity, C#, .NET so much.\n" +
                            "My main job is to **help everyone (no matter whether he or she is programmer or not) build the Unity game executables and hot updates.**.\n" +
                            "If you like this bot, please consider following my master's social media and donate (if possible).\n" +
                            "If you need help, don't hesitate to contact my master.\n" +
                            "https://shepherd0619.github.io/\n\n" +
                            "Anyway, I hope you and me can have a good time!\n"
            , ephemeral: true);
    }

    [SlashCommand("settings", "Print appsettings.json.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task PrintSettings()
    {
        var sb = new StringBuilder();
        sb.Append("```json\n");
        sb.Append(ConfigurationUtility.Configuration.ToString());
        sb.Append("```");
        return RespondAsync(sb.ToString(), ephemeral: true);
    }
    
    public static async Task Notification(string message)
    {
        if (await DiscordStartupService.Discord.GetChannelAsync(
                ConfigurationUtility.Configuration["Discord"]["channel"].AsULong) is not IMessageChannel channel) return;
        
        await channel.SendMessageAsync(message);
    }

    public static async Task LogNotification(string message)
    {
        if (await DiscordStartupService.Discord.GetChannelAsync(
                ConfigurationUtility.Configuration["Discord"]["logChannel"].AsULong) is not IMessageChannel channel) return;
        
        await channel.SendMessageAsync(message);
    }

    [SlashCommand("build-player", "Build a game executable.")]
    public Task BuildPlayer(string projectName, string targetPlatform)
    {
        if (!UnityEditorController.TryGetProject(projectName, out var project))
        {
            return RespondAsync($"Project **{projectName}** not found!");
        }

        if (!UnityEditorController.TryGetUnityEditor(project.unityVersion, out var editor))
        {
            return RespondAsync($"Unity Editor installation **{project.unityVersion} not found!");
        }

        if (!UnityEditorController.CheckProjectIsRunning(project))
        {
            return RespondAsync($"Project **{projectName}** is already running! Please check back another time.");
        }

        var parseResult = Enum.TryParse<TargetPlatform>(targetPlatform, out var target);

        if (!parseResult)
        {
            return RespondAsync($"Unknown targetPlatform!");
        }

        var task = Task.Run(async () => await UnityEditorController.BuildPlayer(projectName, target));
        task.ContinueWith(async t =>
        {
            if (t.Result.Success)
            {
                await Notification($"**{project}** WindowsPlayer64 build completed!");
            }
            else
            {
                await Notification(
                    $"**{project}** WindowsPlayer64 build failed! \n\n{t.Result.Message}");
            }
        });
        return RespondAsync($"**{project}** WindowsPlayer64 build started!", ephemeral: true);
    }
}