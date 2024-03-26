using UnityBuilderDiscordBot.Models;

namespace UnityBuilderDiscordBot.Interfaces;

public interface IFileTransferService<T>
{
    public struct ConnectionInfoStruct
    {
        public string Address;
        public ICredentialService<T> CredentialService;
    }
    public ConnectionInfoStruct? ConnectionInfo { get; set; }

    public Task<ResultMsg> UploadFile(string path, string remotePath);

    public Task<ResultMsg> DownloadFile(string remotePath, string path);

    public void CancelAllDownload();
    
    public void CancelAllUpload();
}