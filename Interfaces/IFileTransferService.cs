namespace UnityBuilderDiscordBot.Interfaces;

public interface IFileTransferService<T>
{
    public struct ConnectionInfoStruct
    {
        public string Address;
        public ICredentialService<T> CredentialService;
    }
    public ConnectionInfoStruct ConnectionInfo { get; set; }

    public void Connect();

    public void Disconnect();

    public Task<bool> Upload(string path);

    public Task<bool> Download(string path);
}