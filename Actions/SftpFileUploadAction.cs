using System.IO.Compression;
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
        if (!File.Exists(LocalPath))
        {
            if (Directory.Exists(LocalPath))
            {
                var result = new ResultMsg();
                // 如果是文件夹，给压缩成zip再上传。
                var zipLocalPath = Path.Combine(new DirectoryInfo(LocalPath).Parent.FullName, $"{Path.GetFileName(LocalPath)}.zip");
                var zipRemotePath = Path.Combine(RemotePath, $"{Path.GetFileName(LocalPath)}.zip")
                    // 根据Linux平台处理斜线、反斜线问题
                    .Replace(Path.DirectorySeparatorChar, '/');

                if (File.Exists(zipLocalPath)) File.Delete(zipLocalPath);

                try
                {
                    ZipFile.CreateFromDirectory(LocalPath, zipLocalPath);
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = $"Failed when creating zip for {LocalPath}\n{ex}";

                    return result;
                }

                var uploadResult = await SftpFileTransferService.Instance.UploadFile(zipLocalPath, zipRemotePath);

                // 远程主机上解压
                if (uploadResult.Success)
                {
                    var unzipResult =
                        await SshCredentialService.Instance.RunCommand(
                            $"unzip -o {zipRemotePath} -d {RemotePath}");

                    if (unzipResult.Success)
                    {
                        result.Success = true;
                        return result;
                    }

                    result.Success = false;
                    result.Message = unzipResult.Message;
                    return result;
                }

                return uploadResult;
            }

            return new ResultMsg()
            {
                Success = false,
                Message = "Invalid localPath"
            };
        }

        return await SftpFileTransferService.Instance.UploadFile(LocalPath, RemotePath);
    }
}