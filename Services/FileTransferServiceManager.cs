﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleJSON;

namespace UnityBuilderDiscordBot.Services;

public class FileTransferServiceManager : IHostedService
{
    private ILogger<FileTransferServiceManager> _logger;
    public readonly Dictionary<JSONNode, SftpFileTransferService> RegisteredSftpFileTransferServices = new();
    private readonly LoggerFactory _loggerFactory = new();

    public static FileTransferServiceManager Instance;

    public FileTransferServiceManager(ILogger<FileTransferServiceManager> logger)
    {
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Instance = this;
        _logger.LogInformation($"[{GetType()}] Standing by.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var kvp in RegisteredSftpFileTransferServices)
        {
            
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 注册一个Sftp客户端
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public bool RegisterSftpFileTransferService(JSONNode node)
    {
        var logger = _loggerFactory.CreateLogger<SftpFileTransferService>();
        return RegisteredSftpFileTransferServices.TryAdd(node, new SftpFileTransferService(logger, node));
    }

    /// <summary>
    /// 反注册一个Sftp客户端
    /// </summary>
    /// <param name="node"></param>
    public void RemoveSftpFileTransferService(JSONNode node)
    {
        if (!RegisteredSftpFileTransferServices.TryGetValue(node, out var service)) return;
        service.StopAsync(CancellationToken.None);
        RegisteredSftpFileTransferServices.Remove(node);
    }
}