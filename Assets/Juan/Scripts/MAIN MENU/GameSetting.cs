public static class GameSettings
{
    // ============================
    // GAME MODE
    // ============================

    public enum GameMode
    {
        TeamVsTeam,
        Deathmatch
    }

    public static GameMode SelectedGameMode =
        GameMode.TeamVsTeam;

    // ============================
    // FRIENDLY FIRE
    // ============================

    public static bool FriendlyFireEnabled =
        false;
}