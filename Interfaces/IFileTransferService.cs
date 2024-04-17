using UnityBuilderDiscordBot.Models;

namespace UnityBuilderDiscordBot.Interfaces;

public interface IFileTransferService<T>
{
    public T? CredentialInfo { get; set; }

    public Task<ResultMsg> UploadFile(string path, string remotePath);

    public Task<ResultMsg> DownloadFile(string remotePath, string path);

    public void CancelAllDownload();

    public void CancelAllUpload();
}