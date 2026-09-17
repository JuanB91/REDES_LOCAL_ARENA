using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    [Header("Game UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text waitingText;
    [SerializeField] private TMP_Text victoryText;
    [SerializeField] private TMP_Text defeatText;
    [SerializeField] private Button restartButton;

    [Header("Health UI")]
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthText;

    [Header("Weapon UI")]
    [SerializeField] private TMP_Text weaponText;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text reloadText;

    private NetworkRunner runner;
    private NetworkGameManager gameManager;

    private bool playerLogged = false;
    private bool resultShown = false;
    private bool matchWasStarted = false;

    private void Start()
    {
        if (victoryText != null)
            victoryText.gameObject.SetActive(false);

        if (defeatText != null)
            defeatText.gameObject.SetActive(false);

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);

        if (waitingText != null)
            waitingText.gameObject.SetActive(true);

        if (reloadText != null)
            reloadText.gameObject.SetActive(false);
    }

    private void Update()
    {
        // ============================
        // NETWORK RUNNER
        // ============================

        if (runner == null)
        {
            runner =
                FindFirstObjectByType<NetworkRunner>();

            if (runner == null)
                return;
        }

        // ============================
        // DEBUG PLAYER LOCAL
        // ============================

        if (!playerLogged &&
            runner.IsRunning &&
            runner.LocalPlayer != PlayerRef.None)
        {
            Debug.Log(
                $"SOY PLAYER {runner.LocalPlayer.PlayerId} | " +
                $"FusionRef: {runner.LocalPlayer}"
            );

            playerLogged = true;
        }

        // ============================
        // GAME MANAGER
        // ============================

        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<NetworkGameManager>();

            if (gameManager == null)
                return;
        }

        if (!gameManager.IsReady)
            return;

        // ============================
        // ACTUALIZAR HUD
        // ============================

        UpdateScore();
        UpdateWaitingState();
        UpdateHealth();
        UpdateWeapon();

        // ============================
        // GAME OVER
        // ============================

        if (gameManager.GameOver)
        {
            ShowResult();
        }
        else if (resultShown)
        {
            HideResult();
        }
    }

    // ============================
    // WAITING
    // ============================

    private void UpdateWaitingState()
    {
        bool waiting =
            !gameManager.MatchStarted &&
            !gameManager.GameOver;

        if (waitingText != null)
        {
            waitingText.gameObject.SetActive(
                waiting
            );
        }

        if (waiting)
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;

            matchWasStarted = false;
        }
        else if (gameManager.MatchStarted &&
                 !gameManager.GameOver &&
                 !matchWasStarted)
        {
            matchWasStarted = true;

            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;

            Debug.Log("FIGHT!");
        }
    }

    // ============================
    // VIDA
    // ============================

    private void UpdateHealth()
    {
        if (runner == null ||
            runner.LocalPlayer == PlayerRef.None)
        {
            return;
        }

        NetworkObject playerObject =
            runner.GetPlayerObject(
                runner.LocalPlayer
            );

        if (playerObject == null)
            return;

        PHealth health =
            playerObject.GetComponent<PHealth>();

        if (health == null)
            return;

        float healthPercent =
            (float)health.Health /
            health.MaxHealth;

        healthPercent =
            Mathf.Clamp01(
                healthPercent
            );

        if (healthFill != null)
        {
            healthFill.fillAmount =
                healthPercent;
        }

        if (healthText != null)
        {
            healthText.text =
                $"{health.Health} / {health.MaxHealth}";
        }
    }

    // ============================
    // ARMAS
    // ============================

    private void UpdateWeapon()
    {
        if (runner == null ||
            runner.LocalPlayer == PlayerRef.None)
        {
            return;
        }

        NetworkObject playerObject =
            runner.GetPlayerObject(
                runner.LocalPlayer
            );

        if (playerObject == null)
            return;

        PWeaponInventory inventory =
            playerObject.GetComponent<PWeaponInventory>();

        PShooting pistol =
            playerObject.GetComponent<PShooting>();

        PShotgun shotgun =
            playerObject.GetComponent<PShotgun>();

        PAssaultRifle assaultRifle =
            playerObject.GetComponent<PAssaultRifle>();

        PSniper sniper =
            playerObject.GetComponent<PSniper>();

        PRocketLauncher rocketLauncher =
            playerObject.GetComponent<PRocketLauncher>();

        PGrenade grenade =
            playerObject.GetComponent<PGrenade>();

        if (inventory == null)
            return;

        // ============================
        // NOMBRE DEL ARMA
        // ============================

        if (weaponText != null)
        {
            weaponText.gameObject.SetActive(true);

            weaponText.text =
                inventory.CurrentWeaponName;
        }

        // ============================
        // MUNICIÓN
        // ============================

        if (ammoText != null)
        {
            ammoText.gameObject.SetActive(true);

            switch (inventory.CurrentWeapon)
            {
                // -------------------------
                // PISTOL
                // -------------------------

                case PWeaponInventory.WeaponType.Pistol:

                    if (pistol != null)
                    {
                        ammoText.text =
                            $"{pistol.CurrentAmmo} / " +
                            $"{pistol.MagazineSize}";
                    }

                    break;

                // -------------------------
                // SHOTGUN
                // -------------------------

                case PWeaponInventory.WeaponType.Shotgun:

                    ammoText.text =
                        $"{inventory.ShotgunLoaded} / " +
                        $"{inventory.ShotgunReserve}";

                    break;

                // -------------------------
                // ASSAULT RIFLE
                // -------------------------

                case PWeaponInventory.WeaponType.AssaultRifle:

                    ammoText.text =
                        $"{inventory.AssaultRifleLoaded} / " +
                        $"{inventory.AssaultRifleReserve}";

                    break;

                // -------------------------
                // SNIPER
                // -------------------------

                case PWeaponInventory.WeaponType.Sniper:

                    ammoText.text =
                        $"{inventory.SniperLoaded} / " +
                        $"{inventory.SniperReserve}";

                    break;

                // -------------------------
                // ROCKET LAUNCHER
                // -------------------------

                case PWeaponInventory.WeaponType.RocketLauncher:

                    ammoText.text =
                        $"{inventory.RocketLauncherLoaded} / " +
                        $"{inventory.RocketLauncherReserve}";

                    break;

                // -------------------------
                // GRENADE
                // -------------------------

                case PWeaponInventory.WeaponType.Grenade:

                    ammoText.text =
                        $"{inventory.GrenadeLoaded} / " +
                        $"{inventory.GrenadeReserve}";

                    break;
            }
        }

        // ============================
        // RELOADING
        // ============================

        if (reloadText != null)
        {
            bool showReloading = false;

            // PISTOL
            if (inventory.IsPistolEquipped() &&
                pistol != null)
            {
                showReloading =
                    pistol.IsReloading;
            }

            // SHOTGUN
            else if (inventory.IsShotgunEquipped() &&
                     shotgun != null)
            {
                showReloading =
                    shotgun.IsReloading;
            }

            // ASSAULT RIFLE
            else if (inventory.IsAssaultRifleEquipped() &&
                     assaultRifle != null)
            {
                showReloading =
                    assaultRifle.IsReloading;
            }

            // SNIPER
            else if (inventory.IsSniperEquipped() &&
                     sniper != null)
            {
                showReloading =
                    sniper.IsReloading;
            }

            // ROCKET LAUNCHER
            else if (inventory.IsRocketLauncherEquipped() &&
                     rocketLauncher != null)
            {
                showReloading =
                    rocketLauncher.IsReloading;
            }

            // GRENADE
            else if (inventory.IsGrenadeEquipped() &&
                     grenade != null)
            {
                showReloading =
                    grenade.IsReloading;
            }

            reloadText.gameObject.SetActive(
                showReloading
            );
        }
    }

    // ============================
    // SCORE
    // ============================

    private void UpdateScore()
    {
        int player1Kills = 0;
        int player2Kills = 0;

        foreach (
            PlayerRef player
            in runner.ActivePlayers)
        {
            NetworkObject playerObject =
                runner.GetPlayerObject(player);

            if (playerObject == null)
                continue;

            PHealth health =
                playerObject.GetComponent<PHealth>();

            if (health == null)
                continue;

            if (player.PlayerId == 1)
            {
                player1Kills =
                    health.Kills;
            }
            else if (player.PlayerId == 2)
            {
                player2Kills =
                    health.Kills;
            }
        }

        if (scoreText != null)
        {
            scoreText.text =
                $"P1: {player1Kills}   |   " +
                $"P2: {player2Kills}";
        }
    }

    // ============================
    // RESULTADO
    // ============================

    private void ShowResult()
    {
        if (resultShown)
            return;

        resultShown = true;

        bool localPlayerWon =
            runner.LocalPlayer ==
            gameManager.Winner;

        if (victoryText != null)
        {
            victoryText.gameObject.SetActive(
                localPlayerWon
            );
        }

        if (defeatText != null)
        {
            defeatText.gameObject.SetActive(
                !localPlayerWon
            );
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(
                true
            );
        }

        if (waitingText != null)
        {
            waitingText.gameObject.SetActive(
                false
            );
        }

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        Debug.Log(
            localPlayerWon
                ? "FIN DE PARTIDA: VICTORY"
                : "FIN DE PARTIDA: DEFEAT"
        );
    }

    private void HideResult()
    {
        resultShown = false;

        if (victoryText != null)
            victoryText.gameObject.SetActive(false);

        if (defeatText != null)
            defeatText.gameObject.SetActive(false);

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);

        matchWasStarted = false;
    }

    // ============================
    // RESTART
    // ============================

    public void RestartGame()
    {
        Debug.Log(
            "BOTON RESTART PRESIONADO"
        );

        if (gameManager == null)
            return;

        gameManager.RequestRestart();
    }
}