using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleJSON;

namespace UnityBuilderDiscordBot.Services;

public class FileTransferServiceManager : IHostedService
{
    private ILogger<FileTransferServiceManager> _logger;
    public readonly Dictionary<JSONNode, SftpFileTransferService> RegisteredSftpFileTransferServices = new();
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole(); // 添加Console输出提供程序
    });

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
            RemoveSftpFileTransferService(kvp.Key);
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
        _logger.LogInformation($"[{GetType()}] {node["address"]}({node["user"]}) registered!");
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
        _logger.LogInformation($"[{GetType()}] {node["address"]}({node["user"]}) removed!");
    }
}