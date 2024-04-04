using System.Diagnostics;
using UnityBuilderDiscordBot.Interfaces;
using UnityBuilderDiscordBot.Models;

namespace UnityBuilderDiscordBot.Services;

public class PlasticSCMSourceControlService : ISourceControlService<UnityProjectModel>
{
    public UnityProjectModel Project { get; set; }

    public string CurrentBranch { get; set; }

    public string CurrentCommit { get; set; }

    public Process RunningProcess { get; set; }

    public async Task<ResultMsg> Checkout(string branch)
    {
        if (RunningProcess != null && !RunningProcess.HasExited)
            return new ResultMsg { Success = false, Message = "Another process is still running." };

        // Switch to branch
        RunningProcess = new Process();
        RunningProcess.StartInfo.WorkingDirectory = Project.path;
        RunningProcess.StartInfo.FileName = "cm";
        RunningProcess.StartInfo.Arguments = $"switchtobranch {branch}";
        RunningProcess.StartInfo.UseShellExecute = false;
        RunningProcess.StartInfo.RedirectStandardOutput = true;
        RunningProcess.Start();

        var output = await RunningProcess.StandardOutput.ReadToEndAsync();
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
            return new ResultMsg { Success = false, Message = "Another process is still running." };

        // There's no direct equivalent of git reset in Plastic SCM. We can revert the changes instead
        RunningProcess = new Process();
        RunningProcess.StartInfo.WorkingDirectory = Project.path;
        RunningProcess.StartInfo.FileName = "cm";
        RunningProcess.StartInfo.Arguments = "undo -r";
        RunningProcess.StartInfo.UseShellExecute = false;
        RunningProcess.StartInfo.RedirectStandardOutput = true;
        RunningProcess.Start();

        var output = await RunningProcess.StandardOutput.ReadToEndAsync();
        await RunningProcess.WaitForExitAsync();

        return new ResultMsg
        {
            Success = RunningProcess.ExitCode == 0,
            Message = output
        };
    }
}