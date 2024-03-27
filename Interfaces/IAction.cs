using UnityBuilderDiscordBot.Models;

namespace UnityBuilderDiscordBot.Interfaces;

public interface IAction
{
    public Task<ResultMsg> Invoke();
}