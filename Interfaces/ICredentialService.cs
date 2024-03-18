namespace UnityBuilderDiscordBot.Interfaces;

public interface ICredentialService
{
    public void Login();

    public void Logout();

    public void SetCredential();

    public void GetCredential();
}