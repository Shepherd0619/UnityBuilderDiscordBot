using System.Diagnostics;
using System.Text;
using SimpleJSON;
using UnityBuilderDiscordBot.Models;
using UnityBuilderDiscordBot.Modules;
using UnityBuilderDiscordBot.Utilities;

namespace UnityBuilderDiscordBot.Controllers;

public static class UnityEditorController
{
    // 打包参数
    // -nographics -batchmode -quit -buildWindowsPlayer "${WORKSPACE}\Build\Windows\${BUILD_NUMBER}\output\your_game.exe"

    public static Dictionary<string, string> EditorInstallations;
    public static List<UnityProjectModel> UnityProjects;
    public static readonly Dictionary<UnityProjectModel, Process> RunningProcesses = new Dictionary<UnityProjectModel, Process>();
    public const string NecessaryCommandLineArgs = "-nographics -batchmode -quit -logFile - ";
    
    public static bool Initialize()
    {
        EditorInstallations = new Dictionary<string, string>();
        foreach (JSONNode node in ConfigurationUtility.Configuration["Unity"].AsArray)
        {
            foreach (var kvp in node)
            {
                EditorInstallations.Add(kvp.Key, kvp.Value.Value);
                Console.WriteLine($"[UnityEditorController] Found a Unity Editor installation! {kvp.Key}, {kvp.Value}");
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
            Console.WriteLine($"[UnityEditorController] {model}");
        }

        if (UnityProjects.Count <= 0)
        {
            Console.WriteLine($"[UnityEditorController] There is no project defined in appsettings.json!");
            return false;
        }
        
        return true;
    }

    public static bool TryGetProject(string projectName, out UnityProjectModel project)
    {
        project = UnityProjects.Find(search => search.name == projectName);

        if (project == null)
        {
            Console.WriteLine($"[UnityEditorController] project {projectName} not exist!");
            return false;
        }

        return true;
    }

    public static bool CheckProjectIsRunning(UnityProjectModel project)
    {
        if (!RunningProcesses.ContainsKey(project)) return true;
        Console.WriteLine($"[UnityEditorController] {project.name}({project.path}) is still running!");
        return false;
    }

    public static bool TryGetUnityEditor(string version, out string unityEditor)
    {
        if (!EditorInstallations.TryGetValue(version, out var editor))
        {
            Console.WriteLine(
                $"[UnityEditorController] Unity Editor installation {version} not exist! You must define it in appsettings.json!");
            unityEditor = string.Empty;
            return false;
        }

        unityEditor = editor;
        return true;
    }
    
    public static async Task<ResultMsg> BuildPlayer(string projectName, TargetPlatform targetPlatform)
    {
        var result = new ResultMsg();
        if (!TryGetProject(projectName, out var project))
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

        if (!TryGetUnityEditor(project.unityVersion, out var editor))
        {
            result.Success = false;
            result.Message = $"Unity Editor Installation ({project.unityVersion}) invalid";
            return result;
        }

        Process process = new Process();
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
                Console.WriteLine($"[UnityEditorController] Unsupported targetPlatform {targetPlatform.ToString()}");
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
        
        sb.Append($"\"{Path.Combine(project.playerBuildOutput, $"{targetPlatform.ToString()}/{timestamp}/{project.name}{fileExtension}")}\"");
        
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
                Console.WriteLine(log);
                DiscordInteractionModule.LogNotification(log);
            }
        };
        if (!process.Start())
        {
            Console.WriteLine($"[UnityEditorController] Failed to start process for {projectName}.");
            result.Success = false;
            result.Message = $"Failed to start process for {projectName}";
            return result;
        }
        process.BeginOutputReadLine();
        RunningProcesses.Add(project, process);
        
        var buildStartLog =
            $"[UnityEditorController] Start building {targetPlatform.ToString()} player for {project.name} ({project.path}). CommandLineArgs: {sb}";
        output.Append(buildStartLog);
        Console.WriteLine(buildStartLog);
        await DiscordInteractionModule.Notification(buildStartLog);
        
        await process.WaitForExitAsync();
        RunningProcesses.Remove(project);
        
        var buildExitLog =
            $"\n[UnityEditorController] {project.name}({project.path}) has exited on {process.ExitTime} with code {process.ExitCode}.";
        output.Append(buildExitLog);
        Console.WriteLine(buildExitLog);
        await DiscordInteractionModule.Notification(buildExitLog);
        var logPath = $"logs/{projectName}_WindowsPlayer64_{timestamp}.log";
        var logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        if (!Directory.Exists(logFolder))
        {
            Directory.CreateDirectory(logFolder);
        }
        await File.WriteAllTextAsync(logPath, output.ToString());
        Console.WriteLine(
            $"[UnityEditorController] build log of {projectName} can be found in {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logPath)}.");

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
    public static async Task<ResultMsg> BuildHotUpdate(string projectName, TargetPlatform targetPlatform)
    {
        var result = new ResultMsg();
        if (!TryGetProject(projectName, out var project))
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

        if (!TryGetUnityEditor(project.unityVersion, out var editor))
        {
            result.Success = false;
            result.Message = $"Unity Editor Installation ({project.unityVersion}) invalid";
            return result;
        }
        
        Process process = new Process();
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
                Console.WriteLine($"[UnityEditorController] Unsupported targetPlatform {targetPlatform.ToString()}");
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
                Console.WriteLine(log);
                DiscordInteractionModule.LogNotification(log);
            }
        };
        if (!process.Start())
        {
            Console.WriteLine($"[UnityEditorController] Failed to start process for {projectName}.");
            result.Success = false;
            result.Message = $"Failed to start process for {projectName}";
            return result;
        }
        process.BeginOutputReadLine();
        RunningProcesses.Add(project, process);
        
        var buildStartLog =
            $"[UnityEditorController] Start building {targetPlatform.ToString()} hot update for {project.name} ({project.path}). CommandLineArgs: {sb}";
        output.Append(buildStartLog);
        Console.WriteLine(buildStartLog);
        await DiscordInteractionModule.Notification(buildStartLog);
        
        await process.WaitForExitAsync();
        RunningProcesses.Remove(project);
        
        var buildExitLog =
            $"\n[UnityEditorController] {project.name}({project.path}) has exited on {process.ExitTime} with code {process.ExitCode}.";
        output.Append(buildExitLog);
        Console.WriteLine(buildExitLog);
        await DiscordInteractionModule.Notification(buildExitLog);
        var logPath = $"logs/{projectName}_WindowsPlayer64_{timestamp}.log";
        var logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        if (!Directory.Exists(logFolder))
        {
            Directory.CreateDirectory(logFolder);
        }
        await File.WriteAllTextAsync(logPath, output.ToString());
        Console.WriteLine(
            $"[UnityEditorController] build log of {projectName} can be found in {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logPath)}.");

        result.Success = true;
        
        return result;
    }
}