using UnityBuilderDiscordBot.Interfaces;
using UnityBuilderDiscordBot.Models;
using UnityBuilderDiscordBot.Services;

namespace UnityBuilderDiscordBot.Actions;

public class SftpFileUploadAction : IAction
{
    // 所有路径都可以使用诸如“{projectPath}"来简化输入
    public string LocalPath { get; set; }
    public string RemotePath { get; set; }
    
    public async Task<ResultMsg> Invoke()
    {
        return await SftpFileTransferService.Instance.UploadFile(LocalPath, RemotePath);
    }
}