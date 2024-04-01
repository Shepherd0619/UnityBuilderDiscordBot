using UnityBuilderDiscordBot.Models;

namespace UnityBuilderDiscordBot.Interfaces;

public interface ICredentialService<T>
{
    public Task<ResultMsg> Login();

    public Task<ResultMsg> Logout();

    public T? CredentialInfo { get; set; }
}