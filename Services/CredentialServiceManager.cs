using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleJSON;

namespace UnityBuilderDiscordBot.Services;

public class CredentialServiceManager : IHostedService
{
    private ILogger<CredentialServiceManager> _logger;
    public readonly Dictionary<JSONNode, SshCredentialService> RegisteredSshCredentialServices = new();
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole(); // 添加Console输出提供程序
    });

    public static CredentialServiceManager Instance;

    public CredentialServiceManager(ILogger<CredentialServiceManager> logger)
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
        foreach (var kvp in RegisteredSshCredentialServices)
        {
            RemoveSshCredentialService(kvp.Key);
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// 注册一个Ssh客户端
    /// </summary>
    /// <param name="node"></param>
    public bool RegisterSshCredentialService(JSONNode node)
    {
        var logger = _loggerFactory.CreateLogger<SshCredentialService>();
        _logger.LogInformation($"[{GetType()}] {node["address"]}({node["user"]}) registered!");
        return RegisteredSshCredentialServices.TryAdd(node, new SshCredentialService(logger, node));
    }

    /// <summary>
    /// 反注册一个Ssh客户端
    /// </summary>
    /// <param name="node"></param>
    public void RemoveSshCredentialService(JSONNode node)
    {
        if (!RegisteredSshCredentialServices.TryGetValue(node, out var service)) return;
        service.StopAsync(CancellationToken.None);
        RegisteredSshCredentialServices.Remove(node);
        _logger.LogInformation($"[{GetType()}] {node["address"]}({node["user"]}) removed!");
    }
}