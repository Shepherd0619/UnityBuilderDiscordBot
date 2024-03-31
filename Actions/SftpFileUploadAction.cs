using UnityBuilderDiscordBot.Interfaces;
using UnityBuilderDiscordBot.Models;
using UnityBuilderDiscordBot.Services;

namespace UnityBuilderDiscordBot.Actions;

public class SftpFileUploadAction : IAction
{
    public string LocalPath { get; set; }
    public string RemotePath { get; set; }
    
    public async Task<ResultMsg> Invoke()
    {
        return await SftpFileTransferService.Instance.UploadFile(LocalPath, RemotePath);
    }
}