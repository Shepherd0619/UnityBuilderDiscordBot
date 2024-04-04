using System.Diagnostics;
using UnityBuilderDiscordBot.Models;

namespace UnityBuilderDiscordBot.Interfaces;

public interface ISourceControlService<T>
{
    public T Project { get; set; }

    public string CurrentBranch { get; set; }

    public string CurrentCommit { get; set; }

    public Process RunningProcess { get; set; }

    public Task<ResultMsg> Checkout(string branch);

    public Task<ResultMsg> Reset(bool hard);
}