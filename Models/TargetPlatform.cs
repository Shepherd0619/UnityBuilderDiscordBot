namespace UnityBuilderDiscordBot.Models;

public enum TargetPlatform
{
    Windows,
    Windows64,
    Linux,
    Mac,
    WindowsServer,
    LinuxServer,
    Android,
    iOS
}

public enum UnityTargetPlatform
{
    StandaloneWindows,
    StandaloneWindows64,
    StandaloneLinux64,
    StandaloneOSX,
    Android,
    iOS
}

public static class TargetPlatformEnumConverter
{
    public static UnityTargetPlatform ConvertToUnityTargetPlatform(TargetPlatform targetPlatform)
    {
        switch (targetPlatform)
        {
            case TargetPlatform.Windows:
                return UnityTargetPlatform.StandaloneWindows;
            case TargetPlatform.Windows64:
                return UnityTargetPlatform.StandaloneWindows64;
            case TargetPlatform.Linux:
                return UnityTargetPlatform.StandaloneLinux64;
            case TargetPlatform.Mac:
                return UnityTargetPlatform.StandaloneOSX;
            case TargetPlatform.WindowsServer:
                return UnityTargetPlatform.StandaloneWindows;
            case TargetPlatform.LinuxServer:
                return UnityTargetPlatform.StandaloneLinux64;
            case TargetPlatform.Android:
                return UnityTargetPlatform.Android;
            case TargetPlatform.iOS:
                return UnityTargetPlatform.iOS;
            default:
                throw new ArgumentException("Unsupported TargetPlatform");
        }
    }

    public static TargetPlatform ConvertToTargetPlatform(UnityTargetPlatform unityTargetPlatform)
    {
        switch (unityTargetPlatform)
        {
            case UnityTargetPlatform.StandaloneWindows:
                return TargetPlatform.Windows;
            case UnityTargetPlatform.StandaloneWindows64:
                return TargetPlatform.Windows64;
            case UnityTargetPlatform.StandaloneLinux64:
                return TargetPlatform.Linux;
            case UnityTargetPlatform.StandaloneOSX:
                return TargetPlatform.Mac;
            case UnityTargetPlatform.Android:
                return TargetPlatform.Android;
            case UnityTargetPlatform.iOS:
                return TargetPlatform.iOS;
            default:
                throw new ArgumentException("Unsupported UnityTargetPlatform");
        }
    }
}