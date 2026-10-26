using Fusion;

public static class NetworkLaunchSettings
{
   
    public static GameMode SelectedGameMode =
        GameMode.Host;

    
    public static void SetHost()
    {
        SelectedGameMode =
            GameMode.Host;
    }

    
    public static void SetClient()
    {
        SelectedGameMode =
            GameMode.Client;
    }
}