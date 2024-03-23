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

    public void Upload(string path);

    public void Download(string path);
}