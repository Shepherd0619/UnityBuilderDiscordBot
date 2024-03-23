using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using UnityBuilderDiscordBot.Interfaces;
using UnityBuilderDiscordBot.Models;
using UnityBuilderDiscordBot.Utilities;

namespace UnityBuilderDiscordBot.Services;

public class SshCredentialService : ICredentialService<ConnectionInfo>, IHostedService
{
    private SshClient? client;
    
    public Task<ResultMsg> Login()
    {
        var result = new ResultMsg();
        if (CredentialInfo == null)
        {
            result.Success = false;
            result.Message = "CredentialInfo is null.";
            return Task.FromResult(result);
        }

        if (client != null)
        {
            client.Dispose();
        }

        client = new SshClient(CredentialInfo);
        
        // SSH事件注册
        client.HostKeyReceived += (sender, args) =>
        {
            _logger.LogWarning(
                $"[{DateTime.Now}][{GetType()}.Login] Host Key received! \nHost fingerprint SHA256: {args.FingerPrintSHA256}");
            if (_expectedFingerprints == null || _expectedFingerprints.Count <= 0)
            {
                _logger.LogError(
                    $"[{DateTime.Now}][{GetType()}.Login] expectedFingerprints not defined! Abort the login");
                args.CanTrust = false;
                client.Disconnect();
                result.Success = false;
                result.Message = "Untrusted host";
            }
            else
            {
                args.CanTrust = _expectedFingerprints.Contains(args.FingerPrintSHA256);
            }
        };
        client.ErrorOccurred += (sender, args) =>
        {
            _logger.LogError($"[{DateTime.Now}][{GetType()}] SshClient ErrorOccurred! {args.Exception}");
        };
        
        // 保持SSH会话
        try
        {
            client.KeepAliveInterval = TimeSpan.Parse(ConfigurationUtility.Configuration["Ssh"]["keepAlive"].Value);
        }
        catch (Exception ex)
        {
            client.KeepAliveInterval = new TimeSpan(0,0,-1);
        }
        
        // 连接
        try
        {
            client.Connect();
            result.Success = true;
            result.Message = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[{DateTime.Now}][{GetType()}.Login] Error when SshClient connect!\n{ex}");
            result.Success = false;
            result.Message = $"SshClient error! {ex}";
        }
        
        return Task.FromResult(result);
    }

    public Task<ResultMsg> Logout()
    {
        throw new NotImplementedException();
    }

    public ConnectionInfo? CredentialInfo { get; set; }

    private List<string>? _expectedFingerprints;

    private readonly ILogger<SshCredentialService> _logger;

    public SshCredentialService(ILogger<SshCredentialService> logger)
    {
        _logger = logger;
        _expectedFingerprints = new List<string>(ConfigurationUtility.Configuration["Ssh"].Count);
        for (int i = 0; i < ConfigurationUtility.Configuration["Ssh"].Count; i++)
        {
            _expectedFingerprints.Add(ConfigurationUtility.Configuration["Ssh"][i]);
            _logger.LogInformation(
                $"[{DateTime.Now}][{GetType()}] SHA256 fingerprint {ConfigurationUtility.Configuration["Ssh"][i]} added!");
        }
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Logout();
    }
}