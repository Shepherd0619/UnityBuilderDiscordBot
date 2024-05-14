using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using UnityBuilderDiscordBot.Models;
using UnityBuilderDiscordBot.Services;
using UnityBuilderDiscordBot.Utilities;
using Newtonsoft.Json;

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
        return RespondAsync("Message sent!");
    }

    [SlashCommand("show-channel-id", "Show a channel's id.")]
    public Task ShowChannelId(SocketTextChannel channel)
    {
        return RespondAsync($"{channel.Name} id is {channel.Id}", ephemeral:true);
    }

    [SlashCommand("about", "Print the introduction of this bot.")]
    public Task About()
    {
        return RespondAsync("Hi I am Unity Builder Bot, developed by Shepherd Zhu (AKA. Shepherd0619).\n" +
                            "He is a nice Chinese guy and he likes yandere, Black Lagoon, Unity, C#, .NET so much.\n" +
                            "My main job is to **help everyone (no matter whether he or she is programmer or not) build the Unity game executables and hot updates.**\n" +
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
        sb.Append("\n```");
        return RespondAsync(sb.ToString(), ephemeral: true);
    }

    [SlashCommand("print-project-settings", "Print a specific project's settings from appsettings.json.")]
    public Task PrintProjectSettings(string name)
    {
        var project = UnityEditorService.Instance.UnityProjects.Find(search => search.name == name);
        if (project == null)
        {
            return RespondAsync($"No project called {name}.", ephemeral: true);
        }

        var sb = new StringBuilder();
        sb.Append("```json\n");
        sb.Append(JsonConvert.SerializeObject(project));
        sb.Append("\n```");
        return RespondAsync(sb.ToString(), ephemeral: true);
    }

    #region 通知类
    /// <summary>
    /// 向预设好的频道发送信息
    /// </summary>
    /// <param name="message"></param>
    public static async Task Notification(string message)
    {
        if (await DiscordStartupService.Discord.GetChannelAsync(
                ConfigurationUtility.Configuration["Discord"]["channel"].AsULong) is not IMessageChannel
            channel) return;

        await channel.SendMessageAsync(message);
    }

    /// <summary>
    /// 向用户自定义的频道发送信息
    /// </summary>
    /// <param name="message"></param>
    /// <param name="channelId"></param>
    public static async Task Notification(string message, ulong channelId)
    {
        if (await DiscordStartupService.Discord.GetChannelAsync(channelId) is not IMessageChannel channel) 
            return;

        await channel.SendMessageAsync(message);
    }

    public static async Task Notification(string message, UnityProjectModel project)
    {
        if (string.IsNullOrWhiteSpace(project.notificationChannel))
        {
            await Notification(message);
            return;
        }
        
        if (await DiscordStartupService.Discord.GetChannelAsync(ulong.Parse(project.notificationChannel)) is not
            IMessageChannel channel)
        {
            // 若没有配置或者无效，则fallback到默认的频道
            await Notification(message);
            return;
        }

        await channel.SendMessageAsync(message);
    }

    public static async Task LogNotification(string message)
    {
        if (await DiscordStartupService.Discord.GetChannelAsync(
                ConfigurationUtility.Configuration["Discord"]["logChannel"].AsULong) is not IMessageChannel
            channel) return;

        await channel.SendMessageAsync(message);
    }
    #endregion
    
    #region 打包指令
    [SlashCommand("build-player", "Build a game executable.")]
    public Task BuildPlayer(string projectName, string targetPlatform)
    {
        if (!UnityEditorService.Instance.TryGetProject(projectName, out var project))
            return RespondAsync($"Project **{projectName}** not found!");

        if (!UnityEditorService.Instance.TryGetUnityEditor(project.unityVersion, out var editor))
            return RespondAsync($"Unity Editor installation **{project.unityVersion} not found!");

        if (!UnityEditorService.Instance.CheckProjectIsRunning(project))
            return RespondAsync($"Project **{projectName}** is already running! Please check back another time.");

        var parseResult = Enum.TryParse<TargetPlatform>(targetPlatform, out var target);

        if (!parseResult) return RespondAsync("Unknown targetPlatform!");

        var task = Task.Run(async () => await UnityEditorService.Instance.BuildPlayer(projectName, target));
        task.ContinueWith(async t =>
        {
            if (t.Result.Success)
                await Notification($"**{project}** {targetPlatform} build completed!", project);
            else
                await Notification(
                    $"**{project}** {targetPlatform} build failed! \n\n{t.Result.Message}", project);
        });
        
        var respondMsg = $"**{project}** {targetPlatform} build started!";
        Notification(respondMsg, project);
        return RespondAsync(respondMsg);
    }

    [SlashCommand("build-hot-update", "Build a hot update.")]
    public Task BuildHotUpdate(string projectName, string targetPlatform)
    {
        if (!UnityEditorService.Instance.TryGetProject(projectName, out var project))
            return RespondAsync($"Project **{projectName}** not found!");

        if (!UnityEditorService.Instance.TryGetUnityEditor(project.unityVersion, out var editor))
            return RespondAsync($"Unity Editor installation **{project.unityVersion} not found!");

        if (!UnityEditorService.Instance.CheckProjectIsRunning(project))
            return RespondAsync($"Project **{projectName}** is already running! Please check back another time.");

        var parseResult = Enum.TryParse<TargetPlatform>(targetPlatform, out var target);

        if (!parseResult) return RespondAsync("Unknown targetPlatform!");

        var task = Task.Run(async () => await UnityEditorService.Instance.BuildHotUpdate(projectName, target));
        task.ContinueWith(async t =>
        {
            if (t.Result.Success)
                await Notification($"**{project}** {targetPlatform} hot update build completed!", project);
            else
                await Notification(
                    $"**{project}** {targetPlatform} hot update build failed! \n\n{t.Result.Message}", project);
        });

        var respondMsg = $"**{project}** {targetPlatform} hot update build started!";
        Notification(respondMsg, project);
        return RespondAsync(respondMsg);
    }
    #endregion
}