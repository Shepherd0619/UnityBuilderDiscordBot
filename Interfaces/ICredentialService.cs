namespace UnityBuilderDiscordBot.Interfaces;

public interface ICredentialService<T>
{
    public void Login();

    public void Logout();

    public T CredentialInfo { get; set; }
}