using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GestorEscenasSinReinicio : MonoBehaviour
{
    // Guarda los objetos raíz activos de la Escena 1 para ocultarlos sin destruirlos
    private static List<GameObject> objetosEscenaPrincipal = new List<GameObject>();

    // Guarda el nombre de la Escena 1
    private static string nombreEscenaPrincipal = "";

    // Guarda el nombre de la Escena 2 que se abrió encima
    private static string nombreEscenaSecundariaActual = "";

    /// <summary>
    /// BOTÓN ESCENA 1: Abre la Escena 2 encima sin reiniciar la Escena 1.
    /// </summary>
    public void AbrirEscenaSecundaria(string nombreEscenaSecundaria)
    {
        Debug.Log($"<color=cyan>[GestorEscenas] Clic recibido para abrir: '{nombreEscenaSecundaria}'</color>");

        string destino = nombreEscenaSecundaria.Trim();
        if (!Application.CanStreamedLevelBeLoaded(destino))
        {
            Debug.LogError($"[GestorEscenas] La escena '{destino}' no está en File -> Build Profiles.");
            return;
        }

        StartCoroutine(ProcesoAbrirSecundaria(destino));
    }

    /// <summary>
    /// BOTÓN ESCENA 2 (Sin parámetros): Cierra la Escena 2 y vuelve a la Escena 1.
    /// </summary>
    public void VolverAEscenaPrincipal()
    {
        Debug.Log("<color=cyan>[GestorEscenas] Clic recibido en VolverAEscenaPrincipal()</color>");
        StartCoroutine(ProcesoVolverPrincipal(""));
    }

    /// <summary>
    /// BOTÓN ESCENA 2 (Con nombre de respaldo): Cierra la Escena 2 y vuelve a la Escena 1.
    /// </summary>
    public void VolverAEscenaPrincipal(string nombreEscenaRespaldo)
    {
        Debug.Log($"<color=cyan>[GestorEscenas] Clic recibido en VolverAEscenaPrincipal('{nombreEscenaRespaldo}')</color>");
        StartCoroutine(ProcesoVolverPrincipal(nombreEscenaRespaldo.Trim()));
    }

    private IEnumerator ProcesoAbrirSecundaria(string nombreEscenaSecundaria)
    {
        // 1. Guardamos la Escena 1 actual
        Scene escena1 = SceneManager.GetActiveScene();
        nombreEscenaPrincipal = escena1.name;
        nombreEscenaSecundariaActual = nombreEscenaSecundaria;

        // 2. Apagamos los objetos raíz de la Escena 1 sin destruirlos
        objetosEscenaPrincipal.Clear();
        foreach (GameObject obj in escena1.GetRootGameObjects())
        {
            if (obj.activeSelf)
            {
                objetosEscenaPrincipal.Add(obj);
                obj.SetActive(false);
            }
        }

        // 3. Cargamos la Escena 2 en modo Aditivo
        AsyncOperation carga = SceneManager.LoadSceneAsync(nombreEscenaSecundaria, LoadSceneMode.Additive);
        while (carga != null && !carga.isDone)
        {
            yield return null;
        }

        // 4. Activamos la Escena 2 y reactivamos su EventSystem para que los botones respondan
        Scene escena2 = SceneManager.GetSceneByName(nombreEscenaSecundaria);
        if (escena2.IsValid() && escena2.isLoaded)
        {
            SceneManager.SetActiveScene(escena2);
            ActivarEventSystemDeEscena(escena2);
        }
    }

    private IEnumerator ProcesoVolverPrincipal(string nombreRespaldo)
    {
        // CASO 1: Si diste Play directamente en la Escena 2 (solo hay 1 escena abierta)
        if (SceneManager.sceneCount <= 1)
        {
            string destino = !string.IsNullOrEmpty(nombreEscenaPrincipal) ? nombreEscenaPrincipal : nombreRespaldo;

            if (!string.IsNullOrEmpty(destino) && Application.CanStreamedLevelBeLoaded(destino))
            {
                Debug.Log($"[GestorEscenas] Solo hay 1 escena abierta. Cargando '{destino}' directamente...");
                SceneManager.LoadScene(destino, LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError($"[GestorEscenas] No se pudo volver: escribe el nombre exacto de tu Escena 1 en el botón de la Escena 2.");
            }
            yield break;
        }

        // CASO 2: Ambas escenas están en memoria (venimos desde la Escena 1)
        // 1. Reactivamos todos los objetos de la Escena 1 tal como estaban
        foreach (GameObject obj in objetosEscenaPrincipal)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        objetosEscenaPrincipal.Clear();

        // 2. Devolvemos el foco activo a la Escena 1
        string nombreEscena1 = !string.IsNullOrEmpty(nombreEscenaPrincipal) ? nombreEscenaPrincipal : nombreRespaldo;
        Scene escena1 = SceneManager.GetSceneByName(nombreEscena1);

        if (escena1.IsValid() && escena1.isLoaded)
        {
            SceneManager.SetActiveScene(escena1);
            ActivarEventSystemDeEscena(escena1);
        }

        // 3. Descargamos la Escena 2 de la memoria
        Scene escena2 = !string.IsNullOrEmpty(nombreEscenaSecundariaActual)
            ? SceneManager.GetSceneByName(nombreEscenaSecundariaActual)
            : gameObject.scene;

        if (escena2.IsValid() && escena2.isLoaded && escena2 != escena1)
        {
            AsyncOperation descarga = SceneManager.UnloadSceneAsync(escena2);
            while (descarga != null && !descarga.isDone)
            {
                yield return null;
            }
        }

        nombreEscenaSecundariaActual = "";
        Debug.Log("<color=green>[GestorEscenas] Regreso a Escena 1 completado con éxito.</color>");
    }

    /// <summary>
    /// Asegura que la escena destino tenga su EventSystem y su módulo de entrada activos.
    /// </summary>
    private void ActivarEventSystemDeEscena(Scene escenaDestino)
    {
        EventSystem[] todos = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool encontroEnDestino = false;

        foreach (EventSystem es in todos)
        {
            if (es.gameObject.scene == escenaDestino)
            {
                es.gameObject.SetActive(true);
                es.enabled = true;
                encontroEnDestino = true;
            }
            else
            {
                es.gameObject.SetActive(false);
            }
        }

        if (!encontroEnDestino)
        {
            GameObject nuevoES = new GameObject("EventSystem_Escena");
            SceneManager.MoveGameObjectToScene(nuevoES, escenaDestino);
            nuevoES.AddComponent<EventSystem>();
            nuevoES.AddComponent<StandaloneInputModule>();
        }
    }
}