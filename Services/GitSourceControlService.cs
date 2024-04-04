using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using UnityBuilderDiscordBot.Interfaces;
using UnityBuilderDiscordBot.Models;

namespace UnityBuilderDiscordBot.Services;

public class GitSourceControlService : ISourceControlService<UnityProjectModel>
{
    public UnityProjectModel Project { get; set; }

    public string CurrentBranch { get; set; }

    public string CurrentCommit { get; set; }

    public Process RunningProcess { get; set; }

    public async Task<ResultMsg> Checkout(string branch)
    {
        if (RunningProcess != null && !RunningProcess.HasExited)
        {
            return new ResultMsg { Success = false, Message = "Another process is still running." };
        }

        // Fetch updates from remote
        RunningProcess = new Process();
        RunningProcess.StartInfo.WorkingDirectory = Project.path;
        RunningProcess.StartInfo.FileName = "git";
        RunningProcess.StartInfo.Arguments = $"fetch";
        RunningProcess.StartInfo.UseShellExecute = false;
        RunningProcess.StartInfo.RedirectStandardOutput = true;
        RunningProcess.Start();

        string output = await RunningProcess.StandardOutput.ReadToEndAsync();
        await RunningProcess.WaitForExitAsync();

        if (RunningProcess.ExitCode != 0)
        {
            return new ResultMsg
            {
                Success = false,
                Message = output
            };
        }

        // Checkout branch
        RunningProcess = new Process();
        RunningProcess.StartInfo.WorkingDirectory = Project.path;
        RunningProcess.StartInfo.FileName = "git";
        RunningProcess.StartInfo.Arguments = $"checkout {branch}";
        RunningProcess.StartInfo.UseShellExecute = false;
        RunningProcess.StartInfo.RedirectStandardOutput = true;
        RunningProcess.Start();

        output += await RunningProcess.StandardOutput.ReadToEndAsync();
        await RunningProcess.WaitForExitAsync();

        CurrentBranch = branch;
        return new ResultMsg
        {
            Success = RunningProcess.ExitCode == 0,
            Message = output
        };
    }

    public async Task<ResultMsg> Reset(bool hard)
    {
        if (RunningProcess != null && !RunningProcess.HasExited)
        {
            return new ResultMsg { Success = false, Message = "Another process is still running." };
        }

        RunningProcess = new Process();
        RunningProcess.StartInfo.WorkingDirectory = Project.path;
        RunningProcess.StartInfo.FileName = "git";
        RunningProcess.StartInfo.Arguments = $"reset {(hard ? "--hard" : "")}";
        RunningProcess.StartInfo.UseShellExecute = false;
        RunningProcess.StartInfo.RedirectStandardOutput = true;
        RunningProcess.Start();

        string output = await RunningProcess.StandardOutput.ReadToEndAsync();
        await RunningProcess.WaitForExitAsync();

        return new ResultMsg
        {
            Success = RunningProcess.ExitCode == 0,
            Message = output
        };
    }
}

