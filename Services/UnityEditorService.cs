using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleJSON;
using UnityBuilderDiscordBot.Models;
using UnityBuilderDiscordBot.Modules;
using UnityBuilderDiscordBot.Utilities;

namespace UnityBuilderDiscordBot.Services;

public class UnityEditorService : IHostedService
{
    // 打包参数
    // -nographics -batchmode -quit -buildWindowsPlayer "${WORKSPACE}\Build\Windows\${BUILD_NUMBER}\output\your_game.exe"

    public Dictionary<string, string> EditorInstallations;
    public List<UnityProjectModel> UnityProjects;

    public readonly Dictionary<UnityProjectModel, Process> RunningProcesses =
        new Dictionary<UnityProjectModel, Process>();

    public const string NecessaryCommandLineArgs = "-nographics -batchmode -quit -logFile - ";

    public readonly ILogger<UnityEditorService> _logger;

    private static UnityEditorService _instance;
    public static UnityEditorService Instance => _instance;

    public UnityEditorService(ILogger<UnityEditorService> logger)
    {
        _logger = logger;
        _instance = this;
    }

    public bool Initialize()
    {
        EditorInstallations = new Dictionary<string, string>();
        foreach (JSONNode node in ConfigurationUtility.Configuration["Unity"].AsArray)
        {
            foreach (var kvp in node)
            {
                EditorInstallations.Add(kvp.Key, kvp.Value.Value);
                _logger.LogInformation($"[{GetType()}] Found a Unity Editor installation! {kvp.Key}, {kvp.Value}");
            }
        }

        UnityProjects = new List<UnityProjectModel>();
        foreach (JSONNode node in ConfigurationUtility.Configuration["Projects"])
        {
            var model = new UnityProjectModel();
            foreach (var kvp in node)
            {
                switch (kvp.Key)
                {
                    case "name":
                        model.name = kvp.Value;
                        break;

                    case "path":
                        model.path = kvp.Value;
                        break;

                    case "unityVersion":
                        model.unityVersion = kvp.Value;
                        break;

                    case "playerBuildOutput":
                        model.playerBuildOutput = kvp.Value;
                        break;

                    case "addressableBuildOutput":
                        model.addressableBuildOutput = kvp.Value;
                        break;
                }
            }

            UnityProjects.Add(model);
            _logger.LogInformation($"[{GetType()}] {model}");
        }

        if (UnityProjects.Count <= 0)
        {
            _logger.LogCritical($"[{GetType()}] There is no project defined in appsettings.json!");
            return false;
        }

        return true;
    }

    public bool TryGetProject(string projectName, out UnityProjectModel project)
    {
        project = UnityProjects.Find(search => search.name == projectName);

        if (project == null)
        {
            _logger.LogError($"[{GetType()}] project {projectName} not exist!");
            return false;
        }

        return true;
    }

    public bool CheckProjectIsRunning(UnityProjectModel project)
    {
        if (!RunningProcesses.ContainsKey(project)) return true;
        _logger.LogWarning($"[{GetType()}] {project.name}({project.path}) is still running!");
        return false;
    }

    public bool TryGetUnityEditor(string version, out string unityEditor)
    {
        if (!EditorInstallations.TryGetValue(version, out var editor))
        {
            _logger.LogCritical(
                $"[{GetType()}] Unity Editor installation {version} not exist! You must define it in appsettings.json!");
            unityEditor = string.Empty;
            return false;
        }

        unityEditor = editor;
        return true;
    }

    public async Task<ResultMsg> BuildPlayer(string projectName, TargetPlatform targetPlatform)
    {
        var result = PrepareEditorProcess(projectName, out var project, out var editor, out var process);
        if (!result.Success)
        {
            return result;
        }

        var sb = new StringBuilder();
        var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        sb.Append(NecessaryCommandLineArgs);
        sb.Append($"-projectPath \"{project.path}\" ");
        switch (targetPlatform)
        {
            case TargetPlatform.Windows:
                sb.Append("-buildWindowsPlayer ");
                break;

            case TargetPlatform.Windows64:
                sb.Append("-buildWindows64Player ");
                break;

            case TargetPlatform.Linux:
                sb.Append("-buildLinux64Player ");
                break;

            case TargetPlatform.Mac:
                sb.Append("-buildOSXUniversalPlayer ");
                break;

            default:
                _logger.LogError($"[{GetType()}] Unsupported targetPlatform {targetPlatform.ToString()}");
                break;
        }

        string fileExtension = string.Empty;
        switch (targetPlatform)
        {
            case TargetPlatform.Mac:
                fileExtension = ".app";
                break;

            case TargetPlatform.Windows:
                fileExtension = ".exe";
                break;

            case TargetPlatform.Windows64:
                fileExtension = ".exe";
                break;

            case TargetPlatform.WindowsServer:
                fileExtension = ".exe";
                break;
        }

        sb.Append(
            $"\"{Path.Combine(project.playerBuildOutput, $"{targetPlatform.ToString()}/{timestamp}/{project.name}{fileExtension}")}\"");

        process.StartInfo.FileName = editor;
        process.StartInfo.Arguments = sb.ToString();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        var output = new StringBuilder();
        int lineCount = 0;
        process.OutputDataReceived += (sender, args) =>
        {
            // Prepend line numbers to each line of the output.
            if (!String.IsNullOrEmpty(args.Data))
            {
                lineCount++;
                var log = $"[{lineCount}][{DateTime.Now}]: {args.Data}";
                output.Append($"\n{log}");
                _logger.LogInformation(log);
                DiscordInteractionModule.LogNotification(log);
            }
        };
        if (!process.Start())
        {
            _logger.LogError($"[{GetType()}] Failed to start process for {projectName}.");
            result.Success = false;
            result.Message = $"Failed to start process for {projectName}";
            return result;
        }

        process.BeginOutputReadLine();
        RunningProcesses.Add(project, process);

        var buildStartLog =
            $"[{GetType()}] Start building {targetPlatform.ToString()} player for {project.name} ({project.path}). CommandLineArgs: {sb}";
        output.Append(buildStartLog);
        _logger.LogInformation(buildStartLog);
        await DiscordInteractionModule.Notification(buildStartLog);

        await process.WaitForExitAsync();
        RunningProcesses.Remove(project);

        var buildExitLog =
            $"\n[{GetType()}] {project.name}({project.path}) has exited on {process.ExitTime} with code {process.ExitCode}.";
        output.Append(buildExitLog);
        _logger.LogWarning(buildExitLog);
        await DiscordInteractionModule.Notification(buildExitLog);
        var logPath = $"logs/{projectName}_WindowsPlayer64_{timestamp}.log";
        var logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        if (!Directory.Exists(logFolder))
        {
            Directory.CreateDirectory(logFolder);
        }

        await File.WriteAllTextAsync(logPath, output.ToString());
        _logger.LogInformation(
            $"[{GetType()}] build log of {projectName} can be found in {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logPath)}.");

        result.Success = true;

        return result;
    }

    /// <summary>
    /// 打热更包。具体热更打包逻辑得在Unity侧实现。
    /// https://github.com/Shepherd0619/JenkinsBuildUnity.git
    /// </summary>
    /// <param name="projectName"></param>
    /// <param name="targetPlatform"></param>
    /// <returns></returns>
    public async Task<ResultMsg> BuildHotUpdate(string projectName, TargetPlatform targetPlatform)
    {
        var result = PrepareEditorProcess(projectName, out var project, out var editor, out var process);
        if (!result.Success)
        {
            return result;
        }

        var sb = new StringBuilder();
        var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        sb.Append(NecessaryCommandLineArgs);
        sb.Append($"-projectPath \"{project.path}\" ");
        switch (targetPlatform)
        {
            case TargetPlatform.Windows64:
                sb.Append("-executeMethod JenkinsBuild.BuildHotUpdateForWindows64");
                break;

            case TargetPlatform.iOS:
                sb.Append("-executeMethod JenkinsBuild.BuildHotUpdateForiOS");
                break;

            case TargetPlatform.Android:
                sb.Append("-executeMethod JenkinsBuild.BuildHotUpdateForAndroid");
                break;

            case TargetPlatform.Linux:
                sb.Append("-executeMethod JenkinsBuild.BuildHotUpdateForLinux");
                break;

            default:
                _logger.LogError($"[{GetType()}] Unsupported targetPlatform {targetPlatform.ToString()}");
                break;
        }

        process.StartInfo.FileName = editor;
        process.StartInfo.Arguments = sb.ToString();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        var output = new StringBuilder();
        int lineCount = 0;
        process.OutputDataReceived += (sender, args) =>
        {
            // Prepend line numbers to each line of the output.
            if (!String.IsNullOrEmpty(args.Data))
            {
                lineCount++;
                var log = $"[{lineCount}][{DateTime.Now}]: {args.Data}";
                output.Append($"\n{log}");
                _logger.LogInformation(log);
                DiscordInteractionModule.LogNotification(log);
            }
        };
        if (!process.Start())
        {
            _logger.LogError($"[{GetType()}] Failed to start process for {projectName}.");
            result.Success = false;
            result.Message = $"Failed to start process for {projectName}";
            return result;
        }

        process.BeginOutputReadLine();
        RunningProcesses.Add(project, process);

        var buildStartLog =
            $"[{GetType()}] Start building {targetPlatform.ToString()} hot update for {project.name} ({project.path}). CommandLineArgs: {sb}";
        output.Append(buildStartLog);
        _logger.LogInformation(buildStartLog);
        await DiscordInteractionModule.Notification(buildStartLog);

        await process.WaitForExitAsync();
        RunningProcesses.Remove(project);

        var buildExitLog =
            $"\n[{GetType()}] {project.name}({project.path}) has exited on {process.ExitTime} with code {process.ExitCode}.";
        output.Append(buildExitLog);
        _logger.LogWarning(buildExitLog);
        await DiscordInteractionModule.Notification(buildExitLog);
        var logPath = $"logs/{projectName}_WindowsPlayer64_{timestamp}.log";
        var logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        if (!Directory.Exists(logFolder))
        {
            Directory.CreateDirectory(logFolder);
        }

        await File.WriteAllTextAsync(logPath, output.ToString());
        _logger.LogInformation(
            $"[{GetType()}] build log of {projectName} can be found in {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logPath)}.");

        result.Success = true;

        return result;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!Initialize())
        {
            _logger.LogCritical($"[{DateTime.Now}][{GetType()}.StartAsync] Initialize failed!");
            Environment.Exit(-1);
        }
        else
        {
            _logger.LogInformation($"[{DateTime.Now}][{GetType()}.StartAsync] Initialized!");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var kvp in RunningProcesses)
        {
            kvp.Value.Kill(true);
        }

        RunningProcesses.Clear();
        _logger.LogInformation($"[{DateTime.Now}][{GetType()}.StopAsync] Stopped!");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 准备编辑器进程，若为success，直接从out参数拿所需的参数即可。
    /// </summary>
    private ResultMsg PrepareEditorProcess(string projectName, out UnityProjectModel project, out string editor,
        out Process process)
    {
        var result = new ResultMsg();
        process = null;
        editor = string.Empty;

        if (!TryGetProject(projectName, out project))
        {
            result.Success = false;
            result.Message = "Project invalid";
            return result;
        }

        if (!CheckProjectIsRunning(project))
        {
            result.Success = false;
            result.Message = "Project is running";
            return result;
        }

        if (!TryGetUnityEditor(project.unityVersion, out editor))
        {
            result.Success = false;
            result.Message = $"Unity Editor Installation ({project.unityVersion}) invalid";
            return result;
        }

        process = new Process();

        result.Success = true;

        return result;
    }
}