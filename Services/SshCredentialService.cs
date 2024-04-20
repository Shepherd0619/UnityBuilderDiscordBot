using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using SimpleJSON;
using UnityBuilderDiscordBot.Interfaces;
using UnityBuilderDiscordBot.Models;
using UnityBuilderDiscordBot.Utilities;

namespace UnityBuilderDiscordBot.Services;

public class SshCredentialService : ICredentialService<ConnectionInfo>
{
    private readonly ILogger<SshCredentialService> _logger;
    private readonly CancellationTokenSource _loginCancellationTokenSource = new();
    private SshClient? _client;

    private List<string>? _expectedFingerprints;

    /// <summary>
    /// 此处会影响命令执行的角色。
    /// 若为true，则会在登录的时候执行sudo su来以root身份执行，
    /// 否则则以当前账号身份执行。
    /// </summary>
    private bool needSudo = false;

    public SshCredentialService(ILogger<SshCredentialService> logger, JSONNode node)
    {
        _logger = logger;
        _ = StartAsync(CancellationToken.None, node);
    }

    public async Task<ResultMsg> Login()
    {
        var result = new ResultMsg();
        if (CredentialInfo == null)
        {
            result.Success = false;
            result.Message = "CredentialInfo is null.";
            _logger.LogError(
                $"[{DateTime.Now}][{CredentialInfo?.Host}:{CredentialInfo?.Port}({CredentialInfo?.Username})][{GetType()}.Login] ERROR! {result.Message}");
            return result;
        }

        if (_client != null) _client.Dispose();

        _client = new SshClient(CredentialInfo);

        var tcs = new TaskCompletionSource<ResultMsg>(); // 创建一个用于等待结果的TaskCompletionSource

        // SSH事件注册
        _client.HostKeyReceived += (sender, args) =>
        {
            _logger.LogWarning(
                $"[{DateTime.Now}][{CredentialInfo?.Host}:{CredentialInfo?.Port}({CredentialInfo?.Username})][{GetType()}.Login] Host Key received! \nHost fingerprint SHA256: {args.FingerPrintSHA256}");
            if (_expectedFingerprints == null || _expectedFingerprints.Count <= 0)
            {
                _logger.LogError(
                    $"[{DateTime.Now}][{CredentialInfo?.Host}:{CredentialInfo?.Port}({CredentialInfo?.Username})][{GetType()}.Login] expectedFingerprints not defined! Abort the login");
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
            _logger.LogError(
                $"[{DateTime.Now}][{CredentialInfo?.Host}:{CredentialInfo?.Port}({CredentialInfo?.Username})][{GetType()}] SshClient ErrorOccurred! {args.Exception}");
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
            if (needSudo)
            {
                _logger.LogWarning($"[{GetType()}.Login] sudo needed!");
                // var sudoResult = await RunCommand("sudo su");
                //
                // if (!sudoResult.Success)
                // {
                //     _logger.LogError($"[{GetType()}.Login] \"sudo su\" failed! {sudoResult.Message}");
                // }
                // else
                // {
                //     _logger.LogInformation($"[{GetType()}.Login] \"sudo su\" success!");
                // }
            }

            result.Success = true;
            result.Message = string.Empty;
            tcs.SetResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"[{DateTime.Now}][{CredentialInfo?.Host}:{CredentialInfo?.Port}({CredentialInfo?.Username})][{GetType()}.Login] Error when SshClient connect!\n{ex}");
            result.Success = false;
            result.Message = $"SshClient error! {ex}";
            tcs.SetResult(result);
        }

        var success = await tcs.Task;
        if (success.Success)
            _logger.LogInformation(
                $"[{DateTime.Now}][{CredentialInfo?.Host}:{CredentialInfo?.Port}({CredentialInfo?.Username})][{GetType()}.Login] Login success!");
        else
            _logger.LogError(
                $"[{DateTime.Now}][{CredentialInfo?.Host}:{CredentialInfo?.Port}({CredentialInfo?.Username})][{GetType()}.Login] login failed! {result.Message}");

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

    public async Task StartAsync(CancellationToken cancellationToken, JSONNode node)
    {
        // var node = ConfigurationUtility.Configuration["Ssh"];
        _expectedFingerprints = new List<string>(node["expectedFingerprints"].Count);
        for (var i = 0; i < node["expectedFingerprints"].Count; i++)
        {
            _expectedFingerprints.Add(node["expectedFingerprints"][i]);
            _logger.LogInformation(
                $"[{DateTime.Now}][{GetType()}] SHA256 fingerprint {node["expectedFingerprints"][i]} added!");
        }

        CredentialInfo = new ConnectionInfo(node["address"].Value, node["user"].Value,
            new PasswordAuthenticationMethod(node["user"].Value, node["password"].Value),
            new PrivateKeyAuthenticationMethod(node["user"].Value, new PrivateKeyFile(node["privateKeyPath"].Value)));

        needSudo = node["needSudo"];

        await Login();
        _logger.LogInformation(
            $"[{DateTime.Now}][{CredentialInfo?.Host}:{CredentialInfo?.Port}({CredentialInfo?.Username})][{GetType()}.StartAsync] Initialized!");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Logout();
        _logger.LogInformation(
            $"[{DateTime.Now}][{CredentialInfo?.Host}:{CredentialInfo?.Port}({CredentialInfo?.Username})][{GetType()}.StopAsync] Stopped!");
    }

    public async Task<ResultMsg> RunCommand(string command)
    {
        if (!_client.IsConnected)
        {
            await _client.ConnectAsync(_loginCancellationTokenSource.Token);
        }
        
        if (needSudo)
        {
            command = $"sudo {command}";
        }

        var result = new ResultMsg();
        var sshCommand = _client.CreateCommand(command);
        try
        {
            await Task.Factory.FromAsync(sshCommand.BeginExecute, sshCommand.EndExecute, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"[{DateTime.Now}][{CredentialInfo?.Host}:{CredentialInfo?.Port}({CredentialInfo?.Username})][{GetType()}] Error when executing {command}! {ex}");
            result.Message = ex.ToString();
            result.Success = false;
        }

        result.Success = true;
        result.Message = sshCommand.Result;
        return result;
    }
}