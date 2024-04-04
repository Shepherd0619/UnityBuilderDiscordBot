﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using UnityBuilderDiscordBot.Interfaces;
using UnityBuilderDiscordBot.Models;

namespace UnityBuilderDiscordBot.Services;

public class SftpFileTransferService : IHostedService, IFileTransferService<ConnectionInfo>
{
    private readonly Dictionary<string, IAsyncResult> _downloadAsyncResults = new();

    private readonly ILogger<SftpFileTransferService> _logger;

    private readonly Dictionary<string, IAsyncResult> _uploadAsyncResults = new();

    private SftpClient? _client;

    public SftpFileTransferService(ILogger<SftpFileTransferService> logger)
    {
        _logger = logger;
    }

    public static SftpFileTransferService Instance { get; private set; }

    public IFileTransferService<ConnectionInfo>.ConnectionInfoStruct? ConnectionInfo { get; set; }

    public async Task<ResultMsg> UploadFile(string path, string remotePath)
    {
        var result = new ResultMsg();
        var isFile = File.Exists(path);

        if (isFile)
        {
            _logger.LogInformation(
                $"[{DateTime.Now}][{GetType()}.Upload] Start uploading file {Path.GetFileName(path)}({path}) to {remotePath}");
            if (_uploadAsyncResults.ContainsKey(remotePath))
            {
                _logger.LogWarning(
                    $"[{DateTime.Now}][{GetType()}.Upload] File {Path.GetFileName(path)}({path}) is already uploading!");
                result.Success = false;
                result.Message = "Upload in progress";
                return result;
            }

            // 默认直接覆盖远端文件
            result = await UploadFileAsync(File.OpenRead(path), remotePath, true);
            return result;
        }

        _logger.LogError($"[{DateTime.Now}][{GetType()}.Upload] Invalid path: {path}.");
        result.Success = false;
        result.Message = "Invalid path";

        return result;
    }

    public async Task<ResultMsg> DownloadFile(string remotePath, string path)
    {
        throw new NotImplementedException();
    }

    public void CancelAllDownload()
    {
        foreach (var kvp in _downloadAsyncResults) _client.EndDownloadFile(kvp.Value);

        _downloadAsyncResults.Clear();
    }

    public void CancelAllUpload()
    {
        foreach (var kvp in _uploadAsyncResults) _client.EndUploadFile(kvp.Value);

        _uploadAsyncResults.Clear();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client = new SftpClient(SshCredentialService.Instance.CredentialInfo);
        Instance = this;

        await _client.ConnectAsync(cancellationToken);
        _logger.LogInformation($"[{DateTime.Now}][{GetType()}.StartAsync] Initialized!");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        CancelAllUpload();
        CancelAllDownload();
        _client.Disconnect();
        _client.Dispose();
        _logger.LogInformation($"[{DateTime.Now}][{GetType()}.StopAsync] Stopped!");
        return Task.CompletedTask;
    }

    private async Task<ResultMsg> UploadFileAsync(Stream input, string path, bool canOverride)
    {
        var resultMsg = new ResultMsg();
        try
        {
            var dir = Path.GetDirectoryName(path).Replace(Path.DirectorySeparatorChar, '/');
            if (!_client.Exists(dir)) _client.CreateDirectory(dir);

            var result = _client.BeginUploadFile(input, path, canOverride, null, null);
            _uploadAsyncResults.Add(path, result);
            result.AsyncWaitHandle.WaitOne();
            _client.EndUploadFile(result);

            resultMsg.Success = true;
            resultMsg.Message = string.Empty;
            _uploadAsyncResults.Remove(path);
            return resultMsg;
        }
        catch (Exception ex)
        {
            // 处理可能出现的异常
            _logger.LogError($"[{DateTime.Now}][{GetType()}.UploadFileAsync] ERROR! {ex}");
            resultMsg.Success = false;
            resultMsg.Message = ex.ToString();
            _uploadAsyncResults.Remove(path);
            return resultMsg;
        }
    }
}