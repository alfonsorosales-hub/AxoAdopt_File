using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    [Header("Configuración de Escena")]
    #if UNITY_EDITOR
    [Tooltip("Arrastra aquí tu escena de Gameplay desde la carpeta Assets")]
    [SerializeField] private SceneAsset gameplaySceneAsset;
    #endif

    // Se guarda automáticamente el nombre de la escena elegida
    [HideInInspector]
    [SerializeField] private string sceneName;

    [Header("Referencias de UI")]
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject optionsPanel;

    private void OnValidate()
    {
        #if UNITY_EDITOR
        // Actualiza el nombre de la escena de forma automática al asignarla en el Inspector
        if (gameplaySceneAsset != null)
        {
            sceneName = gameplaySceneAsset.name;
        }
        #endif
    }

    private void Start()
    {
        CheckSaveFile();
    }

    private void CheckSaveFile()
    {
        bool hasSave = PlayerPrefs.GetInt("HasSaveFile", 0) == 1;

        if (continueButton != null)
        {
            continueButton.interactable = hasSave;
        }
    }

    // 1. INICIAR PARTIDA
    public void StartNewGame()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("¡No has asignado ninguna escena en el Inspector!");
            return;
        }

        PlayerPrefs.SetInt("IsContinuing", 0);
        PlayerPrefs.Save();

        SceneManager.LoadScene(sceneName);
    }

    // 2. CONTINUAR
    public void ContinueGame()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("¡No has asignado ninguna escena en el Inspector!");
            return;
        }

        PlayerPrefs.SetInt("IsContinuing", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(sceneName);
    }

    // 3. OPCIONES
    public void OpenOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }
    }

    public void CloseOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
    }

    // 4. SALIR
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}