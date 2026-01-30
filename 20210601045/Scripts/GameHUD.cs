using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("UI Elements")]
    public Image healthBarFill;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI enemyCountText;

    [Header("Win/Lose Panels")]
    public GameObject winPanel;
    public GameObject losePanel;
    
    [Header("Scene Settings")]
    public string menuSceneName = "MenuScene";

    [Header("Debug")]
    public bool showDebugLogs = true;

    private AgentFSM activeAgentFSM;
    private AgentWeaponSystem activeWeapon;
    private bool gameEnded = false;

    void Start()
    {
        if (winPanel != null) 
            winPanel.SetActive(false);
        if (losePanel != null)
            losePanel.SetActive(false);

        FindReferences();
    }

    void FindReferences()
    {
        AgentFSM[] allFSMs = FindObjectsOfType<AgentFSM>();
        AgentWeaponSystem[] allWeapons = FindObjectsOfType<AgentWeaponSystem>();

        if (showDebugLogs)
        {
            Debug.Log($"[HUD] {allFSMs.Length} AgentFSM, {allWeapons.Length} WeaponSystem bulundu");
        }

        foreach (AgentFSM fsm in allFSMs)
        {
            if (fsm.currentHealth > 0)
            {
                activeAgentFSM = fsm;
                if (showDebugLogs)
                    Debug.Log($"[HUD] AgentFSM bulundu: {fsm.gameObject.name} (Health: {fsm.currentHealth})");
                break;
            }
        }

        foreach (AgentWeaponSystem weapon in allWeapons)
        {
            if (weapon.GetCurrentAmmo() > 0 || weapon.GetMaxAmmo() > 0)
            {
                activeWeapon = weapon;
                if (showDebugLogs)
                    Debug.Log($"[HUD] WeaponSystem bulundu: {weapon.gameObject.name} (Ammo: {weapon.GetCurrentAmmo()})");
                break;
            }
        }

        Invoke("InitialUpdate", 0.2f);
    }

    void InitialUpdate()
    {
        UpdateUI();
    }

    void Update()
    {
        if (gameEnded) return;

        if (activeAgentFSM == null || activeWeapon == null)
        {
            FindReferences();
            return;
        }

        UpdateUI();
        CheckAgentDeath();
        CheckWinCondition();
    }

    void UpdateUI()
    {
        if (activeAgentFSM == null || activeWeapon == null) return;

        if (ammoText != null)
        {
            if (activeWeapon.IsReloading())
            {
                ammoText.text = "RELOADING...";
                ammoText.color = Color.yellow;
            }
            else
            {
                int currentAmmo = activeWeapon.GetCurrentAmmo();
                int maxAmmo = activeWeapon.GetMaxAmmo();
                ammoText.text = $"{currentAmmo} / {maxAmmo}";

                if (currentAmmo <= 5)
                    ammoText.color = Color.red;
                else if (currentAmmo <= 10)
                    ammoText.color = Color.yellow;
                else
                    ammoText.color = Color.white;
            }
        }

        if (healthBarFill != null)
        {
            float healthPercent = activeAgentFSM.currentHealth / activeAgentFSM.maxHealth;
            healthBarFill.fillAmount = healthPercent;

            if (healthPercent > 0.6f)
                healthBarFill.color = Color.green;
            else if (healthPercent > 0.3f)
                healthBarFill.color = Color.yellow;
            else
                healthBarFill.color = Color.red;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(activeAgentFSM.currentHealth)} / {activeAgentFSM.maxHealth}";
        }

        if (enemyCountText != null)
        {
            int aliveEnemies = CountAliveEnemies();
            enemyCountText.text = $"Enemies: {aliveEnemies}";

            if (aliveEnemies == 0)
                enemyCountText.color = Color.green;
            else
                enemyCountText.color = Color.white;
        }
    }

    int CountAliveEnemies()
    {
        int count = 0;
        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();
        
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead())
            {
                count++;
            }
        }
        
        return count;
    }

    void CheckAgentDeath()
    {
        if (activeAgentFSM != null && activeAgentFSM.currentHealth <= 0)
        {
            ShowLoseScreen();
        }
    }

    void CheckWinCondition()
    {
        int aliveEnemies = CountAliveEnemies();
        
        if (aliveEnemies == 0 && activeAgentFSM != null)
        {
            DungeonGenerator dungeon = FindObjectOfType<DungeonGenerator>();
            if (dungeon != null)
            {
                float distanceToGoal = Vector3.Distance(
                    activeAgentFSM.transform.position, 
                    dungeon.GetGoalPosition()
                );

                if (distanceToGoal < 2f)
                {
                    ShowWinScreen();
                }
            }
        }
    }

    public void ShowWinScreen()
    {
        if (gameEnded) return;
        gameEnded = true;

        if (winPanel != null)
        {
            winPanel.SetActive(true);
            Time.timeScale = 0f;
            
            if (showDebugLogs)
                Debug.Log("WIN!");
        }
    }

    public void ShowLoseScreen()
    {
        if (gameEnded) return;
        gameEnded = true;

        if (losePanel != null)
        {
            losePanel.SetActive(true);
            Time.timeScale = 0f;
            
            if (showDebugLogs)
                Debug.Log("LOSE!");
        }
    }

    public void RestartGame()
    {
        if (showDebugLogs)
            Debug.Log("Restarting game...");
        
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void QuitToMenu()
    {
        if (showDebugLogs)
            Debug.Log($"Going to menu: {menuSceneName}");
        
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
    
    public void QuitToDesktop()
    {
        if (showDebugLogs)
            Debug.Log("Quitting game...");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}