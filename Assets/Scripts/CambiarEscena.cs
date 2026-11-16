using UnityEngine;
using UnityEngine.SceneManagement;

public class CambiarEscena : MonoBehaviour
{
    // Llama a este método pasando el nombre exacto de la escena
    public void CargarEscenaPorNombre(string nombreEscena)
    {
        if (string.IsNullOrEmpty(nombreEscena))
        {
            Debug.LogError("[CambiarEscena] El nombre de la escena está vacío.");
            return;
        }

        SceneManager.LoadScene(nombreEscena);
    }

    // Alternativa: cargar por el índice numérico en Build Settings
    public void CargarEscenaPorIndice(int indiceEscena)
    {
        SceneManager.LoadScene(indiceEscena);
    }
}