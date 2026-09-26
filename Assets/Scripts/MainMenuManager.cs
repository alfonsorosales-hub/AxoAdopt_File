using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    [Tooltip("Nombre exacto de la escena del juego principal")]
    [SerializeField] private string gameSceneName = "GameScene";

    // 1. Iniciar Juego (Empieza una nueva partida)
    public void StartNewGame()
    {
        // Opcional: Borrar datos previas si se quiere empezar totalmente de cero
        // PlayerPrefs.DeleteAll(); 

        SceneManager.LoadScene(gameSceneName);
    }

    // 2. Continuar Partida (Carga el juego existente)
    public void ContinueGame()
    {
        // Verifica si existe algún guardado antes de intentar cargar
        if (PlayerPrefs.HasKey("SavedLevel") || PlayerPrefs.HasKey("SaveData"))
        {
            // Carga la escena del juego (o la escena guardada específica)
            string sceneToLoad = PlayerPrefs.GetString("SavedLevel", gameSceneName);
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("No se encontró ninguna partida guardada.");

        }
    }

    // 3. Salir del Juego
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        // Cierra la aplicación (solo funciona en builds ejecutables, no en el editor)
        Application.Quit();
    }
}