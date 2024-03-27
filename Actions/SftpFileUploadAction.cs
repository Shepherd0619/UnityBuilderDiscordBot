using UnityBuilderDiscordBot.Interfaces;
using UnityBuilderDiscordBot.Models;
using UnityBuilderDiscordBot.Services;

namespace UnityBuilderDiscordBot.Actions;

public class SftpFileUploadAction : IAction
{
    public string LocalPath { get; set; }
    public string RemotePath { get; set; }
    
    public Task<ResultMsg> Invoke()
    {
        return SftpFileTransferService.Instance.UploadFile(LocalPath, RemotePath);
    }
}