using System.Reflection;
using Fusion;
using TMPro;
using UnityEngine;
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
    // REFERENCES
    // ============================

    [Header("References")]
    [SerializeField]
    private NetworkGameManager gameManager;

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

    // ============================
    // START
    // ============================

    private void Start()
    {
        HideEndGameUI();

        if (waitingText != null)
        {
            waitingText.gameObject.SetActive(
                true
            );
        }

        if (weaponStatusText != null)
        {
            weaponStatusText.text = "";
        }

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;
    }

    // ============================
    // UPDATE
    // ============================

    private void Update()
    {
        FindGameManager();

        if (gameManager == null)
            return;

        if (!gameManager.IsReady)
            return;

        FindLocalPlayer();

        UpdateScore();
        UpdateWaitingUI();

        if (!localPlayerFound)
            return;

        UpdateHealth();
        UpdateWeaponHUD();
        UpdateMatchResult();
    }

    // ============================
    // FIND GAME MANAGER
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
    // FIND LOCAL PLAYER
    // ============================

    private void FindLocalPlayer()
    {
        if (localPlayerFound &&
            localPlayerObject != null)
        {
            return;
        }

        PHealth[] players =
            FindObjectsByType<PHealth>(
                FindObjectsSortMode.None
            );

        foreach (PHealth health in players)
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

            if (!networkObject.HasInputAuthority)
                continue;

            localPlayerObject =
                networkObject;

            localHealth =
                health;

            localTeam =
                networkObject.GetComponent<
                    PTeam
                >();

            weaponInventory =
                networkObject.GetComponent<
                    PWeaponInventory
                >();

            CacheWeaponScripts(
                networkObject.gameObject
            );

            localPlayerFound =
                true;

            return;
        }
    }

    // ============================
    // CACHE WEAPON SCRIPTS
    // ============================

    private void CacheWeaponScripts(
        GameObject player)
    {
        MonoBehaviour[] scripts =
            player.GetComponents<
                MonoBehaviour
            >();

        foreach (MonoBehaviour script in scripts)
        {
            if (script == null)
                continue;

            string typeName =
                script.GetType().Name;

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
    // SCORE
    // ============================

    private void UpdateScore()
    {
        if (scoreText == null)
            return;

        scoreText.text =
            $"RED: {gameManager.RedScore}   |   " +
            $"BLUE: {gameManager.BlueScore}";
    }

    // ============================
    // WAITING
    // ============================

    private void UpdateWaitingUI()
    {
        if (waitingText == null)
            return;

        bool waiting =
            !gameManager.MatchStarted &&
            !gameManager.GameOver;

        waitingText.gameObject.SetActive(
            waiting
        );

        if (waiting)
        {
            waitingText.text =
                "WAITING FOR PLAYERS";
        }
    }

    // ============================
    // HEALTH
    // ============================

    private void UpdateHealth()
    {
        if (localHealth == null)
            return;

        if (healthBar != null)
        {
            healthBar.fillAmount =
                (float)localHealth.Health /
                localHealth.MaxHealth;
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

        switch (
            weaponInventory.CurrentWeapon
        )
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
                        "MagSize",
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

        bool reloading = false;

        switch (
            weaponInventory.CurrentWeapon
        )
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
    // MATCH RESULT
    // ============================

    private void UpdateMatchResult()
    {
        if (!gameManager.GameOver)
        {
            if (resultShown)
            {
                resultShown =
                    false;

                HideEndGameUI();

                Cursor.lockState =
                    CursorLockMode.Locked;

                Cursor.visible =
                    false;
            }

            return;
        }

        if (resultShown)
            return;

        if (!gameManager.HasWinningTeam)
            return;

        if (localTeam == null)
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

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;
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

            victoryText.gameObject.SetActive(
                true
            );
        }

        if (defeatText != null)
        {
            defeatText.gameObject.SetActive(
                false
            );
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(
                true
            );
        }
    }

    // ============================
    // DEFEAT
    // ============================

    private void ShowDefeat()
    {
        if (victoryText != null)
        {
            victoryText.gameObject.SetActive(
                false
            );
        }

        if (defeatText != null)
        {
            defeatText.text =
                "DEFEAT";

            defeatText.gameObject.SetActive(
                true
            );
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(
                true
            );
        }
    }

    // ============================
    // HIDE END GAME
    // ============================

    public void HideEndGameUI()
    {
        if (victoryText != null)
        {
            victoryText.gameObject.SetActive(
                false
            );
        }

        if (defeatText != null)
        {
            defeatText.gameObject.SetActive(
                false
            );
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(
                false
            );
        }
    }

    // ============================
    // RESTART
    // ============================

    public void RestartMatch()
    {
        Debug.Log("HUD: RESTART BUTTON PRESSED");

        if (gameManager == null)
        {
            Debug.LogError(
                "HUD: GAME MANAGER ES NULL"
            );

            return;
        }

        if (!gameManager.IsReady)
        {
            Debug.LogError(
                "HUD: GAME MANAGER TODAVIA NO ESTA READY"
            );

            return;
        }

        Debug.Log(
            "HUD: ENVIANDO REQUEST RESTART"
        );

        gameManager.RequestRestart();
    }

    // ============================
    // REFLECTION HELPERS
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

        if (property != null &&
            property.PropertyType ==
            typeof(bool))
        {
            object value =
                property.GetValue(
                    component
                );

            if (value != null)
            {
                return (bool)value;
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
            typeof(bool))
        {
            object value =
                field.GetValue(
                    component
                );

            if (value != null)
            {
                return (bool)value;
            }
        }

        return false;
    }
}