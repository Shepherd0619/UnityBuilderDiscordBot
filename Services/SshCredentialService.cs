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

    public static SshCredentialService Instance => _instance;
    private static SshCredentialService _instance;
    
    public async Task<ResultMsg> Login()
    {
        var result = new ResultMsg();
        if (CredentialInfo == null)
        {
            result.Success = false;
            result.Message = "CredentialInfo is null.";
            _logger.LogError($"[{DateTime.Now}][{GetType()}.Login] ERROR! {result.Message}");
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
            _client.KeepAliveInterval = new TimeSpan(-1);
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

        var success = await tcs.Task;
        if(success.Success)
            _logger.LogInformation($"[{DateTime.Now}][{GetType()}.Login] Login success!");
        else
        {
            _logger.LogError($"[{DateTime.Now}][{GetType()}.Login] login failed! {result.Message}");
        }

        return success;
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

    private readonly List<string>? _expectedFingerprints;

    private readonly ILogger<SshCredentialService> _logger;

    public SshCredentialService(ILogger<SshCredentialService> logger)
    {
        _logger = logger;
        
        var node = ConfigurationUtility.Configuration["Ssh"];
        _expectedFingerprints = new List<string>(node["expectedFingerprints"].Count);
        for (int i = 0; i < node["expectedFingerprints"].Count; i++)
        {
            _expectedFingerprints.Add(node["expectedFingerprints"][i]);
            _logger.LogInformation(
                $"[{DateTime.Now}][{GetType()}] SHA256 fingerprint {node["expectedFingerprints"][i]} added!");
        }

        CredentialInfo = new ConnectionInfo(node["address"].Value, node["user"].Value,
            new PasswordAuthenticationMethod(node["user"].Value, node["password"].Value),
            new PrivateKeyAuthenticationMethod(node["user"].Value, new PrivateKeyFile(node["privateKeyPath"].Value)));
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _instance = this;
        await Login();
        _logger.LogInformation($"[{DateTime.Now}][{GetType()}.StartAsync] Initialized!");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Logout();
        _logger.LogInformation($"[{DateTime.Now}][{GetType()}.StopAsync] Stopped!");
    }

    public async Task<ResultMsg> RunCommand(string command)
    {
        var result = new ResultMsg();
        var sshCommand = _client.CreateCommand(command);
        try
        {
            await Task.FromResult(_client.CreateCommand(command).BeginExecute());
        }
        catch (Exception ex)
        {
            _logger.LogError($"[{DateTime.Now}][{GetType()}] Error when executing {command}! {ex}");
            result.Message = ex.ToString();
            result.Success = false;
        }

        result.Success = true;
        result.Message = sshCommand.Result;
        return result;
    }
}