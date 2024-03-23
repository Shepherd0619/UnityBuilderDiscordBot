using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using UnityBuilderDiscordBot.Interfaces;
using UnityBuilderDiscordBot.Models;
using UnityBuilderDiscordBot.Utilities;

namespace UnityBuilderDiscordBot.Services;

public class SshCredentialService : ICredentialService<ConnectionInfo>, IHostedService
{
    private SshClient? _client;
    private readonly CancellationTokenSource _loginCancellationTokenSource = new CancellationTokenSource();
    
    public async Task<ResultMsg> Login()
    {
        var result = new ResultMsg();
        if (CredentialInfo == null)
        {
            result.Success = false;
            result.Message = "CredentialInfo is null.";
            return result;
        }

        if (_client != null)
        {
            _client.Dispose();
        }

        _client = new SshClient(CredentialInfo);
        
        var tcs = new TaskCompletionSource<ResultMsg>(); // 创建一个用于等待结果的TaskCompletionSource
        
        // SSH事件注册
        _client.HostKeyReceived += (sender, args) =>
        {
            _logger.LogWarning(
                $"[{DateTime.Now}][{GetType()}.Login] Host Key received! \nHost fingerprint SHA256: {args.FingerPrintSHA256}");
            if (_expectedFingerprints == null || _expectedFingerprints.Count <= 0)
            {
                _logger.LogError(
                    $"[{DateTime.Now}][{GetType()}.Login] expectedFingerprints not defined! Abort the login");
                args.CanTrust = false;
                _client.Disconnect();
                result.Success = false;
                result.Message = "Untrusted host";
                tcs.SetResult(result); // 设置TaskCompletionSource的结果
            }
            else
            {
                args.CanTrust = _expectedFingerprints.Contains(args.FingerPrintSHA256);
            }
        };
        _client.ErrorOccurred += (sender, args) =>
        {
            _logger.LogError($"[{DateTime.Now}][{GetType()}] SshClient ErrorOccurred! {args.Exception}");
        };
        
        // 保持SSH会话
        try
        {
            _client.KeepAliveInterval = TimeSpan.Parse(ConfigurationUtility.Configuration["Ssh"]["keepAlive"].Value);
        }
        catch (Exception ex)
        {
            _client.KeepAliveInterval = new TimeSpan(0,0,-1);
        }
        
        // 连接
        try
        {
            await _client.ConnectAsync(_loginCancellationTokenSource.Token);
            result.Success = true;
            result.Message = string.Empty;
            tcs.SetResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[{DateTime.Now}][{GetType()}.Login] Error when SshClient connect!\n{ex}");
            result.Success = false;
            result.Message = $"SshClient error! {ex}";
            tcs.SetResult(result);
        }
        
        return await tcs.Task;
    }

    public Task<ResultMsg> Logout()
    {
        var result = new ResultMsg();
        if (_client == null)
        {
            result.Success = false;
            result.Message = "SshClient is null.";

            return Task.FromResult(result);
        }
        
        _loginCancellationTokenSource.Cancel();
        _client.Disconnect();
        _client.Dispose();

        result.Success = true;
        result.Message = string.Empty;

        return Task.FromResult(result);
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