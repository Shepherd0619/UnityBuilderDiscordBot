using Microsoft.Extensions.Hosting;
using Renci.SshNet;
using UnityBuilderDiscordBot.Interfaces;
using UnityBuilderDiscordBot.Models;

namespace UnityBuilderDiscordBot.Services;

public class SftpFileTransferService : IHostedService, IFileTransferService<ConnectionInfo>
{
    private SftpClient? _client;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IFileTransferService<ConnectionInfo>.ConnectionInfoStruct? ConnectionInfo { get; set; }
    public void Connect()
    {
        throw new NotImplementedException();
    }

    public void Disconnect()
    {
        throw new NotImplementedException();
    }

    public async Task<ResultMsg> Upload(string path)
    {
        throw new NotImplementedException();
    }

    public async Task<ResultMsg> Download(string path)
    {
        throw new NotImplementedException();
    }
}