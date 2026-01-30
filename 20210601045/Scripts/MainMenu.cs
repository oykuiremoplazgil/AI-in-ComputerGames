using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Settings")]
    public string gameSceneName = "SampleScene"; 
    
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
  
    void Start()
    {
     
        ShowMainMenu();
    }
    
    public void PlayGame()
    {
        Debug.Log(" Oyun başlatılıyor...");
        SceneManager.LoadScene(gameSceneName);
    }

    
    
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
       
    }
    
    public void QuitGame()
    {
        Debug.Log(" Oyundan çıkılıyor...");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
}