using System.Reflection;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    // ============================
    // SCORE
    // ============================

    [Header("Score")]
    [SerializeField]
    private TMP_Text scoreText;

    // ============================
    // HEALTH
    // ============================

    [Header("Health")]
    [SerializeField]
    private Image healthBar;

    [SerializeField]
    private TMP_Text healthText;

    // ============================
    // WEAPON HUD
    // ============================

    [Header("Weapon")]
    [SerializeField]
    private TMP_Text weaponText;

    [SerializeField]
    private TMP_Text ammoText;

    [SerializeField]
    private TMP_Text weaponStatusText;

    // ============================
    // MATCH UI
    // ============================

    [Header("Match")]
    [SerializeField]
    private TMP_Text waitingText;

    [SerializeField]
    private TMP_Text victoryText;

    [SerializeField]
    private TMP_Text defeatText;

    [SerializeField]
    private Button restartButton;

    // ============================
    // GAME SCENES
    // ============================

    [Header("Game Scenes")]
    [SerializeField]
    private string deathmatchSceneName =
        "DEATHMATCH";

    // ============================
    // DEATHMATCH START MESSAGE
    // ============================

    [Header("Deathmatch Start Message")]
    [SerializeField]
    private float deathmatchStartedMessageDuration =
        3f;

    // ============================
    // MANAGERS
    // ============================

    [Header("Managers")]
    [SerializeField]
    private NetworkGameManager gameManager;

    [SerializeField]
    private DeathmatchGameManager
        deathmatchGameManager;

    // ============================
    // LOCAL PLAYER
    // ============================

    private NetworkObject localPlayerObject;

    private PHealth localHealth;

    private PTeam localTeam;

    private PWeaponInventory weaponInventory;

    // ============================
    // WEAPON COMPONENTS
    // ============================

    private MonoBehaviour pistolScript;

    private MonoBehaviour shotgunScript;

    private MonoBehaviour assaultRifleScript;

    private MonoBehaviour sniperScript;

    private MonoBehaviour rocketLauncherScript;

    private MonoBehaviour grenadeScript;

    // ============================
    // RUNTIME
    // ============================

    private bool resultShown;

    private bool localPlayerFound;

    private bool previousDeathmatchStarted;

    private bool deathmatchStartMessageActive;

    private float deathmatchStartMessageEndTime;

    // ============================
    // START
    // ============================

    private void Start()
    {
        resultShown =
            false;

        localPlayerFound =
            false;

        previousDeathmatchStarted =
            false;

        deathmatchStartMessageActive =
            false;

        deathmatchStartMessageEndTime =
            0f;

        HideEndGameUI();

        if (waitingText != null)
        {
            waitingText
                .gameObject
                .SetActive(
                    true
                );
        }

        if (weaponStatusText != null)
        {
            weaponStatusText.text =
                "";
        }

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;

        Debug.Log(
            $"HUD READY | MODE: " +
            $"{(IsDeathmatch() ? "DEATHMATCH" : "TEAM VS TEAM")}"
        );
    }

    // ============================
    // UPDATE
    // ============================

    private void Update()
    {
        FindLocalPlayer();

        if (IsDeathmatch())
        {
            UpdateDeathmatchHUD();

            return;
        }

        UpdateTeamVsTeamHUD();
    }

    // ============================
    // MODE
    // ============================

    private bool IsDeathmatch()
    {
        return
            SceneManager
                .GetActiveScene()
                .name ==
            deathmatchSceneName;
    }

    // ============================
    // TEAM VS TEAM HUD
    // ============================

    private void UpdateTeamVsTeamHUD()
    {
        FindGameManager();

        if (gameManager == null)
            return;

        if (!gameManager.IsReady)
            return;

        UpdateTeamScore();

        UpdateTeamWaitingUI();

        if (!localPlayerFound)
            return;

        UpdateHealth();

        UpdateWeaponHUD();

        UpdateTeamMatchResult();
    }

    // ============================
    // DEATHMATCH HUD
    // ============================

    private void UpdateDeathmatchHUD()
    {
        FindDeathmatchGameManager();

        if (deathmatchGameManager == null)
            return;

        if (!deathmatchGameManager.IsReady)
            return;

        UpdateDeathmatchWaitingUI();

        UpdateDeathmatchScore();

        if (!localPlayerFound)
            return;

        UpdateHealth();

        UpdateWeaponHUD();

        UpdateDeathmatchMatchResult();
    }

    // ============================
    // FIND TEAM MANAGER
    // ============================

    private void FindGameManager()
    {
        if (gameManager != null)
            return;

        gameManager =
            FindFirstObjectByType<
                NetworkGameManager
            >();
    }

    // ============================
    // FIND DEATHMATCH MANAGER
    // ============================

    private void FindDeathmatchGameManager()
    {
        if (deathmatchGameManager != null)
            return;

        deathmatchGameManager =
            FindFirstObjectByType<
                DeathmatchGameManager
            >();
    }

    // ============================
    // FIND LOCAL PLAYER
    // ============================

    private void FindLocalPlayer()
    {
        if (localPlayerFound &&
            localPlayerObject != null)
        {
            if (localPlayerObject.IsValid)
                return;
        }

        localPlayerFound =
            false;

        localPlayerObject =
            null;

        localHealth =
            null;

        localTeam =
            null;

        weaponInventory =
            null;

        PHealth[] players =
            FindObjectsByType<PHealth>(
                FindObjectsSortMode.None
            );

        foreach (
            PHealth health
            in players)
        {
            if (health == null)
                continue;

            NetworkObject networkObject =
                health.GetComponent<
                    NetworkObject
                >();

            if (networkObject == null)
                continue;

            if (!networkObject.IsValid)
                continue;

            if (!networkObject
                .HasInputAuthority)
            {
                continue;
            }

            localPlayerObject =
                networkObject;

            localHealth =
                health;

            localTeam =
                networkObject
                    .GetComponent<
                        PTeam
                    >();

            weaponInventory =
                networkObject
                    .GetComponent<
                        PWeaponInventory
                    >();

            CacheWeaponScripts(
                networkObject.gameObject
            );

            localPlayerFound =
                true;

            Debug.Log(
                $"HUD LOCAL PLAYER FOUND | " +
                $"Player: " +
                $"{networkObject.InputAuthority.PlayerId} | " +
                $"Mode: " +
                $"{(IsDeathmatch() ? "DEATHMATCH" : "TEAM VS TEAM")} | " +
                $"StateAuthority: " +
                $"{networkObject.HasStateAuthority} | " +
                $"InputAuthority: " +
                $"{networkObject.HasInputAuthority}"
            );

            return;
        }
    }

    // ============================
    // CACHE WEAPON SCRIPTS
    // ============================

    private void CacheWeaponScripts(
        GameObject player)
    {
        if (player == null)
            return;

        MonoBehaviour[] scripts =
            player.GetComponents<
                MonoBehaviour
            >();

        foreach (
            MonoBehaviour script
            in scripts)
        {
            if (script == null)
                continue;

            string typeName =
                script
                    .GetType()
                    .Name;

            switch (typeName)
            {
                case "PShooting":

                    pistolScript =
                        script;

                    break;

                case "PShotgun":

                    shotgunScript =
                        script;

                    break;

                case "PAssaultRifle":

                    assaultRifleScript =
                        script;

                    break;

                case "PSniper":

                    sniperScript =
                        script;

                    break;

                case "PRocketLauncher":

                    rocketLauncherScript =
                        script;

                    break;

                case "PGrenade":

                    grenadeScript =
                        script;

                    break;
            }
        }
    }

    // ============================
    // TEAM SCORE
    // ============================

    private void UpdateTeamScore()
    {
        if (scoreText == null)
            return;

        if (gameManager == null)
            return;

        scoreText.text =
            $"RED: {gameManager.RedScore}   |   " +
            $"BLUE: {gameManager.BlueScore}";
    }

    // ============================
    // DEATHMATCH SCORE
    // ============================

    private void UpdateDeathmatchScore()
    {
        if (scoreText == null)
            return;

        if (deathmatchGameManager == null)
            return;

        scoreText.text =
            $"TOTAL KILLS: " +
            $"{deathmatchGameManager.TotalKills} / " +
            $"{deathmatchGameManager.KillsToWin}";
    }

    // ============================
    // TEAM WAITING
    // ============================

    private void UpdateTeamWaitingUI()
    {
        if (waitingText == null)
            return;

        if (gameManager == null)
            return;

        bool waiting =
            !gameManager.MatchStarted &&
            !gameManager.GameOver;

        waitingText
            .gameObject
            .SetActive(
                waiting
            );

        if (waiting)
        {
            waitingText.text =
                "WAITING FOR PLAYERS";
        }
    }

    // ============================
    // DEATHMATCH WAITING
    // ============================

    private void UpdateDeathmatchWaitingUI()
    {
        if (waitingText == null)
            return;

        if (deathmatchGameManager == null)
            return;

        bool matchStartedNow =
            deathmatchGameManager.MatchStarted;

        // ============================
        // DETECT MATCH START
        // ============================

        if (matchStartedNow &&
            !previousDeathmatchStarted)
        {
            deathmatchStartMessageActive =
                true;

            deathmatchStartMessageEndTime =
                Time.time +
                deathmatchStartedMessageDuration;

            Debug.Log(
                "HUD: DEATHMATCH START MESSAGE"
            );
        }

        previousDeathmatchStarted =
            matchStartedNow;

        // ============================
        // GAME OVER
        // ============================

        if (deathmatchGameManager.GameOver)
        {
            deathmatchStartMessageActive =
                false;

            waitingText
                .gameObject
                .SetActive(
                    false
                );

            return;
        }

        // ============================
        // WARMUP
        // ============================

        if (deathmatchGameManager.WarmupActive)
        {
            deathmatchStartMessageActive =
                false;

            int seconds =
                deathmatchGameManager
                    .WarmupSecondsRemaining;

            waitingText.text =
                $"DEATHMATCH STARTS IN: {seconds}";

            waitingText
                .gameObject
                .SetActive(
                    true
                );

            return;
        }

        // ============================
        // STARTED MESSAGE
        // ============================

        if (deathmatchStartMessageActive)
        {
            if (Time.time <
                deathmatchStartMessageEndTime)
            {
                waitingText.text =
                    "DEATHMATCH STARTED";

                waitingText
                    .gameObject
                    .SetActive(
                        true
                    );

                return;
            }

            deathmatchStartMessageActive =
                false;
        }

        // ============================
        // WAITING FOR PLAYERS
        // ============================

        if (!deathmatchGameManager.MatchStarted)
        {
            waitingText.text =
                "WAITING FOR PLAYERS";

            waitingText
                .gameObject
                .SetActive(
                    true
                );

            return;
        }

        // ============================
        // MATCH ACTIVE
        // ============================

        waitingText
            .gameObject
            .SetActive(
                false
            );
    }

    // ============================
    // HEALTH
    // ============================

    private void UpdateHealth()
    {
        if (localHealth == null)
            return;

        if (localHealth.Object == null)
            return;

        if (!localHealth.Object.IsValid)
            return;

        if (healthBar != null)
        {
            float maxHealth =
                Mathf.Max(
                    localHealth.MaxHealth,
                    1
                );

            healthBar.fillAmount =
                Mathf.Clamp01(
                    localHealth.Health /
                    maxHealth
                );
        }

        if (healthText != null)
        {
            healthText.text =
                $"{localHealth.Health} / " +
                $"{localHealth.MaxHealth}";
        }
    }

    // ============================
    // WEAPON HUD
    // ============================

    private void UpdateWeaponHUD()
    {
        if (weaponInventory == null)
            return;

        if (weaponInventory.Object == null)
            return;

        if (!weaponInventory.Object.IsValid)
            return;

        if (weaponText != null)
        {
            weaponText.text =
                weaponInventory
                    .CurrentWeaponName;
        }

        UpdateAmmoText();

        UpdateReloadText();
    }

    // ============================
    // AMMO
    // ============================

    private void UpdateAmmoText()
    {
        if (ammoText == null)
            return;

        if (weaponInventory == null)
            return;

        switch (
            weaponInventory.CurrentWeapon)
        {
            case PWeaponInventory
                .WeaponType
                .Pistol:

                int pistolAmmo =
                    GetIntValue(
                        pistolScript,
                        "CurrentAmmo",
                        0
                    );

                int pistolMagazine =
                    GetIntValue(
                        pistolScript,
                        "MagazineSize",
                        6
                    );

                ammoText.text =
                    $"{pistolAmmo} / " +
                    $"{pistolMagazine}";

                break;

            case PWeaponInventory
                .WeaponType
                .Shotgun:

                ammoText.text =
                    $"{weaponInventory.ShotgunLoaded} / " +
                    $"{weaponInventory.ShotgunReserve}";

                break;

            case PWeaponInventory
                .WeaponType
                .AssaultRifle:

                ammoText.text =
                    $"{weaponInventory.AssaultRifleLoaded} / " +
                    $"{weaponInventory.AssaultRifleReserve}";

                break;

            case PWeaponInventory
                .WeaponType
                .Sniper:

                ammoText.text =
                    $"{weaponInventory.SniperLoaded} / " +
                    $"{weaponInventory.SniperReserve}";

                break;

            case PWeaponInventory
                .WeaponType
                .RocketLauncher:

                ammoText.text =
                    $"{weaponInventory.RocketLauncherLoaded} / " +
                    $"{weaponInventory.RocketLauncherReserve}";

                break;

            case PWeaponInventory
                .WeaponType
                .Grenade:

                ammoText.text =
                    $"{weaponInventory.GrenadeLoaded} / " +
                    $"{weaponInventory.GrenadeReserve}";

                break;
        }
    }

    // ============================
    // RELOAD STATUS
    // ============================

    private void UpdateReloadText()
    {
        if (weaponStatusText == null)
            return;

        if (weaponInventory == null)
            return;

        bool reloading =
            false;

        switch (
            weaponInventory.CurrentWeapon)
        {
            case PWeaponInventory
                .WeaponType
                .Pistol:

                reloading =
                    GetBoolValue(
                        pistolScript,
                        "IsReloading"
                    );

                break;

            case PWeaponInventory
                .WeaponType
                .Shotgun:

                reloading =
                    GetBoolValue(
                        shotgunScript,
                        "IsReloading"
                    );

                break;

            case PWeaponInventory
                .WeaponType
                .AssaultRifle:

                reloading =
                    GetBoolValue(
                        assaultRifleScript,
                        "IsReloading"
                    );

                break;

            case PWeaponInventory
                .WeaponType
                .Sniper:

                reloading =
                    GetBoolValue(
                        sniperScript,
                        "IsReloading"
                    );

                break;

            case PWeaponInventory
                .WeaponType
                .RocketLauncher:

                reloading =
                    GetBoolValue(
                        rocketLauncherScript,
                        "IsReloading"
                    );

                break;

            case PWeaponInventory
                .WeaponType
                .Grenade:

                reloading =
                    GetBoolValue(
                        grenadeScript,
                        "IsReloading"
                    );

                break;
        }

        weaponStatusText.text =
            reloading
                ? "RELOADING..."
                : "";
    }

    // ============================
    // TEAM MATCH RESULT
    // ============================

    private void UpdateTeamMatchResult()
    {
        if (gameManager == null)
            return;

        if (!gameManager.GameOver)
        {
            ResetResultUIIfNeeded();

            return;
        }

        if (resultShown)
            return;

        if (!gameManager.HasWinningTeam)
            return;

        if (localTeam == null)
            return;

        if (localTeam.Object == null)
            return;

        if (!localTeam.Object.IsValid)
            return;

        bool localTeamWon =
            localTeam.CurrentTeam ==
            gameManager.WinningTeam;

        if (localTeamWon)
        {
            ShowVictory();
        }
        else
        {
            ShowDefeat();
        }

        resultShown =
            true;

        UnlockCursor();
    }

    // ============================
    // DEATHMATCH MATCH RESULT
    // ============================

    private void UpdateDeathmatchMatchResult()
    {
        if (deathmatchGameManager == null)
            return;

        if (!deathmatchGameManager.GameOver)
        {
            ResetResultUIIfNeeded();

            return;
        }

        if (resultShown)
            return;

        if (!deathmatchGameManager.HasWinner)
            return;

        if (localPlayerObject == null)
            return;

        if (!localPlayerObject.IsValid)
            return;

        PlayerRef localPlayer =
            localPlayerObject.InputAuthority;

        bool localPlayerWon =
            deathmatchGameManager
                .IsWinner(
                    localPlayer
                );

        if (localPlayerWon)
        {
            ShowVictory();

            Debug.Log(
                $"DEATHMATCH HUD RESULT | " +
                $"PLAYER {localPlayer.PlayerId} | " +
                $"VICTORY"
            );
        }
        else
        {
            ShowDefeat();

            Debug.Log(
                $"DEATHMATCH HUD RESULT | " +
                $"PLAYER {localPlayer.PlayerId} | " +
                $"DEFEAT"
            );
        }

        resultShown =
            true;

        UnlockCursor();
    }

    // ============================
    // RESET RESULT UI
    // ============================

    private void ResetResultUIIfNeeded()
    {
        if (!resultShown)
            return;

        resultShown =
            false;

        HideEndGameUI();

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;
    }

    // ============================
    // VICTORY
    // ============================

    private void ShowVictory()
    {
        if (victoryText != null)
        {
            victoryText.text =
                "VICTORY";

            victoryText
                .gameObject
                .SetActive(
                    true
                );
        }

        if (defeatText != null)
        {
            defeatText
                .gameObject
                .SetActive(
                    false
                );
        }

        UpdateRestartButtonVisibility();
    }

    // ============================
    // DEFEAT
    // ============================

    private void ShowDefeat()
    {
        if (victoryText != null)
        {
            victoryText
                .gameObject
                .SetActive(
                    false
                );
        }

        if (defeatText != null)
        {
            defeatText.text =
                "DEFEAT";

            defeatText
                .gameObject
                .SetActive(
                    true
                );
        }

        UpdateRestartButtonVisibility();
    }

    // ============================
    // RESTART BUTTON VISIBILITY
    // ============================

    private void UpdateRestartButtonVisibility()
    {
        if (restartButton == null)
            return;

        bool canRestart =
            false;

        if (IsDeathmatch())
        {
            canRestart =
                deathmatchGameManager != null &&
                deathmatchGameManager.IsReady &&
                deathmatchGameManager
                    .CanLocalRestart;
        }
        else
        {
            canRestart =
                gameManager != null &&
                gameManager.IsReady &&
                gameManager
                    .CanLocalRestart;
        }

        restartButton
            .gameObject
            .SetActive(
                canRestart
            );
    }

    // ============================
    // HIDE END GAME
    // ============================

    public void HideEndGameUI()
    {
        if (victoryText != null)
        {
            victoryText
                .gameObject
                .SetActive(
                    false
                );
        }

        if (defeatText != null)
        {
            defeatText
                .gameObject
                .SetActive(
                    false
                );
        }

        if (restartButton != null)
        {
            restartButton
                .gameObject
                .SetActive(
                    false
                );
        }
    }

    // ============================
    // RESTART
    // ============================

    public void RestartMatch()
    {
        Debug.Log(
            "HUD: RESTART BUTTON PRESSED"
        );

        if (IsDeathmatch())
        {
            RestartDeathmatch();

            return;
        }

        RestartTeamVsTeam();
    }

    // ============================
    // RESTART TEAM VS TEAM
    // ============================

    private void RestartTeamVsTeam()
    {
        FindGameManager();

        if (gameManager == null)
        {
            Debug.LogError(
                "HUD: NETWORK GAME MANAGER IS NULL"
            );

            return;
        }

        if (!gameManager.IsReady)
        {
            Debug.LogError(
                "HUD: NETWORK GAME MANAGER IS NOT READY"
            );

            return;
        }

        if (!gameManager.CanLocalRestart)
        {
            Debug.LogWarning(
                "HUD: RESTART BLOCKED | " +
                "ONLY HOST CAN RESTART"
            );

            return;
        }

        gameManager.RequestRestart();
    }

    // ============================
    // RESTART DEATHMATCH
    // ============================

    private void RestartDeathmatch()
    {
        FindDeathmatchGameManager();

        if (deathmatchGameManager == null)
        {
            Debug.LogError(
                "HUD: DEATHMATCH GAME MANAGER IS NULL"
            );

            return;
        }

        if (!deathmatchGameManager.IsReady)
        {
            Debug.LogError(
                "HUD: DEATHMATCH GAME MANAGER IS NOT READY"
            );

            return;
        }

        if (!deathmatchGameManager
            .CanLocalRestart)
        {
            Debug.LogWarning(
                "HUD: DEATHMATCH RESTART BLOCKED | " +
                "ONLY HOST CAN RESTART"
            );

            return;
        }

        Debug.Log(
            "HUD: SENDING DEATHMATCH RESTART"
        );

        deathmatchGameManager
            .RequestRestart();
    }

    // ============================
    // CURSOR
    // ============================

    private void UnlockCursor()
    {
        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;
    }

    // ============================
    // REFLECTION INT
    // ============================

    private int GetIntValue(
        MonoBehaviour component,
        string propertyName,
        int defaultValue)
    {
        if (component == null)
            return defaultValue;

        PropertyInfo property =
            component
                .GetType()
                .GetProperty(
                    propertyName,
                    BindingFlags.Public |
                    BindingFlags.Instance
                );

        if (property != null &&
            property.PropertyType ==
            typeof(int))
        {
            object value =
                property.GetValue(
                    component
                );

            if (value != null)
            {
                return (int)value;
            }
        }

        FieldInfo field =
            component
                .GetType()
                .GetField(
                    propertyName,
                    BindingFlags.Public |
                    BindingFlags.Instance
                );

        if (field != null &&
            field.FieldType ==
            typeof(int))
        {
            object value =
                field.GetValue(
                    component
                );

            if (value != null)
            {
                return (int)value;
            }
        }

        return defaultValue;
    }

    // ============================
    // REFLECTION BOOL
    // ============================

    private bool GetBoolValue(
        MonoBehaviour component,
        string propertyName)
    {
        if (component == null)
            return false;

        PropertyInfo property =
            component
                .GetType()
                .GetProperty(
                    propertyName,
                    BindingFlags.Public |
                    BindingFlags.Instance
                );

        if (property != null)
        {
            object value =
                property.GetValue(
                    component
                );

            if (value is bool boolValue)
                return boolValue;

            if (value is NetworkBool networkBool)
            {
                return networkBool;
            }
        }

        FieldInfo field =
            component
                .GetType()
                .GetField(
                    propertyName,
                    BindingFlags.Public |
                    BindingFlags.Instance
                );

        if (field != null)
        {
            object value =
                field.GetValue(
                    component
                );

            if (value is bool boolValue)
                return boolValue;

            if (value is NetworkBool networkBool)
            {
                return networkBool;
            }
        }

        return false;
    }
}