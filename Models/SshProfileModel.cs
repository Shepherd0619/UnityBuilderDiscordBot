namespace UnityBuilderDiscordBot.Models;

public class SshProfileModel
{
    public string address { get; set; }
    public string user { get; set; }
    public string password { get; set; }
    public string privateKeyPath { get; set; }
    public string keepAliveInterval { get; set; }
    public List<string> expectedFingerprints { get; set; }
}