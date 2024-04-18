using System.Diagnostics;
using Microsoft.Extensions.Logging;
using UnityBuilderDiscordBot.Interfaces;
using UnityBuilderDiscordBot.Models;

namespace UnityBuilderDiscordBot.Services;

public class GitSourceControlService : ISourceControlService<UnityProjectModel>
{
    public UnityProjectModel Project { get; set; }

    public string CurrentBranch { get; set; }

    public string CurrentCommit { get; set; }

    public Process? RunningProcess { get; set; }

    public ILogger<GitSourceControlService> Logger { get; set; }

    public async Task<ResultMsg> Checkout(string branch)
    {
        if (RunningProcess != null && !RunningProcess.HasExited)
            return new ResultMsg { Success = false, Message = "Another process is still running." };

        // Fetch updates from remote
        RunningProcess = new Process();
        RunningProcess.StartInfo.WorkingDirectory = Project.path;
        RunningProcess.StartInfo.FileName = "git";
        RunningProcess.StartInfo.Arguments = "fetch";
        RunningProcess.StartInfo.UseShellExecute = false;
        RunningProcess.StartInfo.RedirectStandardOutput = true;
        RunningProcess.Start();

        var message = await RunningProcess.StandardOutput.ReadToEndAsync();
        string output = string.Empty;
        output += message;
        Logger.LogInformation($"[{GetType()}] {message}");
        await RunningProcess.WaitForExitAsync();

        if (RunningProcess.ExitCode != 0)
            return new ResultMsg
            {
                Success = false,
                Message = output
            };

        // Checkout branch
        RunningProcess = new Process();
        RunningProcess.StartInfo.WorkingDirectory = Project.path;
        RunningProcess.StartInfo.FileName = "git";
        RunningProcess.StartInfo.Arguments = $"checkout {branch}";
        RunningProcess.StartInfo.UseShellExecute = false;
        RunningProcess.StartInfo.RedirectStandardOutput = true;
        RunningProcess.Start();

        message = await RunningProcess.StandardOutput.ReadToEndAsync();
        output += message;
        Logger.LogInformation($"[{GetType()}] {message}");
        await RunningProcess.WaitForExitAsync();

        CurrentBranch = branch;

        if (RunningProcess.ExitCode != 0)
            return new ResultMsg
            {
                Success = false,
                Message = output
            };

        RunningProcess = new Process();
        RunningProcess.StartInfo.WorkingDirectory = Project.path;
        RunningProcess.StartInfo.FileName = "git";
        RunningProcess.StartInfo.Arguments = "pull";
        RunningProcess.StartInfo.UseShellExecute = false;
        RunningProcess.StartInfo.RedirectStandardOutput = true;
        RunningProcess.Start();

        message = await RunningProcess.StandardOutput.ReadToEndAsync();
        output += message;
        Logger.LogInformation($"[{GetType()}] {message}");
        await RunningProcess.WaitForExitAsync();

        return new ResultMsg
        {
            Success = RunningProcess.ExitCode == 0,
            Message = output
        };
    }

    public async Task<ResultMsg> Reset(bool hard)
    {
        if (RunningProcess != null && !RunningProcess.HasExited)
            return new ResultMsg { Success = false, Message = "Another process is still running." };

        RunningProcess = new Process();
        RunningProcess.StartInfo.WorkingDirectory = Project.path;
        RunningProcess.StartInfo.FileName = "git";
        RunningProcess.StartInfo.Arguments = $"reset {(hard ? "--hard" : "")}";
        RunningProcess.StartInfo.UseShellExecute = false;
        RunningProcess.StartInfo.RedirectStandardOutput = true;
        RunningProcess.Start();

        var message = await RunningProcess.StandardOutput.ReadToEndAsync();
        string output = string.Empty;
        output += message;
        Logger.LogInformation($"[{GetType()}] {message}");
        await RunningProcess.WaitForExitAsync();

        return new ResultMsg
        {
            Success = RunningProcess.ExitCode == 0,
            Message = output
        };
    }
}