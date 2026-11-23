using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla la apertura y cierre del menú de pausa mediante la tecla Escape
/// o mediante botones de la interfaz de usuario (UI).
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Referencias de UI")]
    [Tooltip("Arrastra aquí el panel del menú de pausa (el que empieza desactivado en la jerarquía).")]
    public GameObject MenuPausaUI;

    [Header("Configuración de Escenas")]
    [Tooltip("Nombre exacto de la escena del menú principal para el botón Salir.")]
    [SerializeField] private string escenaMenuPrincipal = "Main Menu";

    // Estado interno para saber si el juego está pausado actualmente
    private bool estaPausado = false;

    private void Start()
    {
        // Verificación de seguridad: avisar si olvidaste asignar el panel en el Inspector
        if (MenuPausaUI == null)
        {
            Debug.LogError("[PauseMenu] ¡Error! No has asignado 'MenuPausaUI' en el Inspector.");
            return;
        }

        // Aviso si pusiste este script dentro del mismo panel que empieza desactivado
        if (MenuPausaUI == gameObject)
        {
            Debug.LogWarning("[PauseMenu] Este script está dentro de 'MenuPausaUI'. Muévelo al Canvas o a un objeto siempre activo para que funcione.");
        }

        // Aseguramos que al iniciar la escena el menú esté oculto y el juego corra normal
        MenuPausaUI.SetActive(false);
        Time.timeScale = 1f;
        estaPausado = false;
    }

    private void Update()
    {
        // Permite abrir o cerrar la pausa presionando la tecla Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            AlternarPausa();
        }
    }

    /// <summary>
    /// Método práctico para conectar en el botón de Pausa de la interfaz:
    /// si está cerrado lo abre, y si está abierto lo cierra.
    /// </summary>
    public void AlternarPausa()
    {
        if (estaPausado)
        {
            Reanudar();
        }
        else
        {
            Pausar();
        }
    }

    /// <summary>
    /// Desactiva el panel de pausa y reanuda el tiempo del juego.
    /// </summary>
    public void Reanudar()
    {
        if (MenuPausaUI == null) return;

        Debug.Log("[PauseMenu] Juego reanudado.");
        MenuPausaUI.SetActive(false);
        Time.timeScale = 1f;
        estaPausado = false;
    }

    /// <summary>
    /// Activa el panel de pausa en pantalla y congela el tiempo del juego.
    /// </summary>
    public void Pausar()
    {
        if (MenuPausaUI == null) return;

        Debug.Log("[PauseMenu] Juego pausado.");

        // Si algún objeto padre del menú estaba apagado, lo encendemos para que el menú sea visible
        if (!MenuPausaUI.transform.parent.gameObject.activeInHierarchy)
        {
            MenuPausaUI.transform.parent.gameObject.SetActive(true);
        }

        MenuPausaUI.SetActive(true);
        Time.timeScale = 0f;
        estaPausado = true;
    }

    /// <summary>
    /// Restaura el tiempo normal y regresa a la escena del Menú Principal.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[PauseMenu] Saliendo al menú principal...");
        Time.timeScale = 1f; // Muy importante restaurar el tiempo antes de cambiar de escena
        estaPausado = false;
        SceneManager.LoadScene(escenaMenuPrincipal);
    }
}