using UnityBuilderDiscordBot.Models;

namespace UnityBuilderDiscordBot.Interfaces;

public interface ICredentialService<T>
{
    public T? CredentialInfo { get; set; }
    public Task<ResultMsg> Login();

    public Task<ResultMsg> Logout();
}