using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona la pantalla de carga visual con transiciones suaves de entrada (Fade In)
/// y salida (Fade Out), carga asíncrona de escenas, barra de progreso y consejos aleatorios.
/// </summary>
public class LoadingManager : MonoBehaviour
{
    [Header("Elementos de la UI")]
    [Tooltip("Panel principal que contiene toda la interfaz de la pantalla de carga.")]
    [SerializeField] private GameObject panelPantallaCarga;

    [Tooltip("Slider que muestra el avance visual de la carga (0 a 1).")]
    [SerializeField] private Slider barraProgreso;

    [Tooltip("Texto TMP que muestra el porcentaje numérico (ej. 0% - 100%).")]
    [SerializeField] private TextMeshProUGUI textoPorcentaje;

    [Tooltip("Texto TMP donde se mostrará un consejo aleatorio de la lista.")]
    [SerializeField] private TextMeshProUGUI textoConsejo;

    [Header("Tiempos de Transición y Carga (Segundos)")]
    [Tooltip("Tiempo que tarda la pantalla de carga en aparecer suavemente al hacer clic.")]
    [SerializeField] private float duracionFadeEntrada = 1.2f;

    [Tooltip("Tiempo mínimo que dura la barra llenándose para permitir leer los consejos.")]
    [SerializeField] private float tiempoMinimoCarga = 5.0f;

    [Tooltip("Tiempo que tarda la pantalla de carga en desvanecerse al entrar en la nueva escena.")]
    [SerializeField] private float duracionFadeSalida = 1.0f;

    [Header("Configuración de Consejos")]
    [TextArea(2, 4)]
    [Tooltip("Lista de consejos o curiosidades que aparecerán al azar durante la carga.")]
    [SerializeField] private List<string> listaConsejos = new List<string>();

    [Header("Nombres de Escenas")]
    [Tooltip("Nombre exacto de la escena para Iniciar Partida (debe estar en Build Settings).")]
    [SerializeField] private string escenaNuevaPartida = "Nivel1";

    [Tooltip("Nombre exacto de la escena por defecto al Cargar Partida guardada.")]
    [SerializeField] private string escenaCargarPartida = "Nivel1";

    [Header("Eventos de Lógica Antigua")]
    [Tooltip("Funciones que se ejecutarán al terminar de cargar una Nueva Partida.")]
    public UnityEvent alCompletarNuevaPartida;

    [Tooltip("Funciones que se ejecutarán al terminar de cargar una Partida Guardada.")]
    public UnityEvent alCompletarCargarPartida;

    // Bandera interna para evitar que el jugador haga múltiples clics mientras ya está cargando
    private bool estaCargando = false;

    /// <summary>
    /// Método público para asignar al evento OnClick() del botón "Iniciar Partida".
    /// </summary>
    public void BotonIniciarPartida()
    {
        if (estaCargando) return;
        StartCoroutine(ProcesoCompletoDeCarga(escenaNuevaPartida.Trim(), true));
    }

    /// <summary>
    /// Método público para asignar al evento OnClick() del botón "Cargar / Continuar Partida".
    /// </summary>
    public void BotonCargarPartida()
    {
        if (estaCargando) return;

        // Busca si existe una escena guardada en PlayerPrefs; si no, usa la escena por defecto
        string guardada = PlayerPrefs.GetString("EscenaGuardada", "");
        string escenaDestino = string.IsNullOrWhiteSpace(guardada) ? escenaCargarPartida.Trim() : guardada.Trim();

        StartCoroutine(ProcesoCompletoDeCarga(escenaDestino, false));
    }

    /// <summary>
    /// Corrutina principal que controla toda la secuencia:
    /// 1. Preparación de Canvas persistente -> 2. Fade In -> 3. Carga de escena -> 4. Fade Out -> 5. Limpieza.
    /// </summary>
    private IEnumerator ProcesoCompletoDeCarga(string nombreEscena, bool esNuevaPartida)
    {
        // Verificación de seguridad por si falta asignar el panel en el Inspector
        if (panelPantallaCarga == null)
        {
            Debug.LogError("[LoadingManager] Falta asignar 'Panel Pantalla Carga' en el Inspector.");
            yield break;
        }

        estaCargando = true;
        Time.timeScale = 1f; // Garantiza que el tiempo del juego no esté pausado

        // --- PASO 1: CREAR UN CANVAS PERSISTENTE EXCLUSIVO PARA LA CARGA ---
        // Creamos un objeto raíz limpio para que DontDestroyOnLoad nunca falle ni toque tu Canvas del menú
        GameObject objetoCanvasCarga = new GameObject("Canvas_TransicionCarga");
        Canvas canvasCarga = objetoCanvasCarga.AddComponent<Canvas>();
        canvasCarga.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasCarga.sortingOrder = 9999; // Asegura que se dibuje por encima de absolutamente todo

        // Copiamos la configuración de escalado de tu Canvas original para que no cambie el tamaño de la UI
        CanvasScaler scalerOriginal = panelPantallaCarga.GetComponentInParent<CanvasScaler>();
        CanvasScaler nuevoScaler = objetoCanvasCarga.AddComponent<CanvasScaler>();
        if (scalerOriginal != null)
        {
            nuevoScaler.uiScaleMode = scalerOriginal.uiScaleMode;
            nuevoScaler.referenceResolution = scalerOriginal.referenceResolution;
            nuevoScaler.screenMatchMode = scalerOriginal.screenMatchMode;
            nuevoScaler.matchWidthOrHeight = scalerOriginal.matchWidthOrHeight;
        }

        objetoCanvasCarga.AddComponent<GraphicRaycaster>();

        // Movemos únicamente el panel de carga dentro de este nuevo Canvas persistente
        panelPantallaCarga.transform.SetParent(objetoCanvasCarga.transform, false);

        // Hacemos persistentes tanto el Canvas temporal como este LoadingManager (asegurando que sea raíz)
        transform.SetParent(null);
        DontDestroyOnLoad(objetoCanvasCarga);
        DontDestroyOnLoad(gameObject);

        // --- PASO 2: PREPARAR INTERFAZ Y CONSEJO ALEATORIO ---
        if (listaConsejos.Count > 0 && textoConsejo != null)
        {
            textoConsejo.text = listaConsejos[Random.Range(0, listaConsejos.Count)];
        }

        if (barraProgreso != null)
        {
            barraProgreso.minValue = 0f;
            barraProgreso.maxValue = 1f;
            barraProgreso.value = 0f;
        }

        if (textoPorcentaje != null)
        {
            textoPorcentaje.text = "0%";
        }

        // Activamos el panel ya listo dentro de su propio Canvas
        panelPantallaCarga.SetActive(true);

        // --- PASO 3: TRANSICIÓN DE ENTRADA (FADE IN) ---
        Graphic[] graficos = panelPantallaCarga.GetComponentsInChildren<Graphic>(true);
        float[] alfasOriginales = new float[graficos.Length];

        // Guardamos su opacidad deseada y los ponemos completamente transparentes (alfa = 0)
        for (int i = 0; i < graficos.Length; i++)
        {
            bool esParteDelSlider = barraProgreso != null && graficos[i].transform.IsChildOf(barraProgreso.transform);
            alfasOriginales[i] = esParteDelSlider ? 1f : (graficos[i].color.a > 0.05f ? graficos[i].color.a : 1f);

            Color c = graficos[i].color;
            c.a = 0f;
            graficos[i].color = c;
        }

        // Aumentamos gradualmente la opacidad desde 0 hasta su valor original
        float relojFadeIn = 0f;
        while (relojFadeIn < duracionFadeEntrada)
        {
            relojFadeIn += Time.unscaledDeltaTime;
            float factor = Mathf.Clamp01(relojFadeIn / duracionFadeEntrada);

            for (int i = 0; i < graficos.Length; i++)
            {
                if (graficos[i] != null)
                {
                    Color c = graficos[i].color;
                    c.a = Mathf.Lerp(0f, alfasOriginales[i], factor);
                    graficos[i].color = c;
                }
            }
            yield return null;
        }

        // Aseguramos que todos queden en su opacidad exacta al finalizar el Fade In
        for (int i = 0; i < graficos.Length; i++)
        {
            if (graficos[i] != null)
            {
                Color c = graficos[i].color;
                c.a = alfasOriginales[i];
                graficos[i].color = c;
            }
        }

        // --- PASO 4: CARGA ASÍNCRONA DE LA ESCENA Y BARRA DE PROGRESO ---
        AsyncOperation operacion = SceneManager.LoadSceneAsync(nombreEscena);
        if (operacion != null)
        {
            operacion.allowSceneActivation = false;
        }

        float relojCarga = 0f;
        float duracion = Mathf.Max(tiempoMinimoCarga, 0.5f);

        while (relojCarga < duracion || (operacion != null && operacion.progress < 0.9f))
        {
            relojCarga += Time.unscaledDeltaTime;

            float progresoTiempo = Mathf.Clamp01(relojCarga / duracion);
            float progresoUnity = (operacion != null) ? Mathf.Clamp01(operacion.progress / 0.9f) : 1f;
            float progresoVisual = Mathf.Min(progresoTiempo, progresoUnity);

            if (barraProgreso != null)
            {
                barraProgreso.value = progresoVisual;
                if (barraProgreso.fillRect != null)
                {
                    barraProgreso.fillRect.anchorMin = Vector2.zero;
                    barraProgreso.fillRect.anchorMax = new Vector2(progresoVisual, 1f);
                    barraProgreso.fillRect.offsetMin = Vector2.zero;
                    barraProgreso.fillRect.offsetMax = Vector2.zero;
                }
            }

            if (textoPorcentaje != null)
            {
                textoPorcentaje.text = Mathf.RoundToInt(progresoVisual * 100f) + "%";
            }

            yield return null;
        }

        // --- PASO 5: COMPLETAR AL 100% Y ACTIVAR LA NUEVA ESCENA ---
        if (barraProgreso != null) barraProgreso.value = 1f;
        if (textoPorcentaje != null) textoPorcentaje.text = "100%";

        yield return new WaitForSecondsRealtime(0.25f);

        // Ejecutamos tus eventos personalizados (reiniciar stats o cargar guardado)
        try
        {
            if (esNuevaPartida)
                alCompletarNuevaPartida?.Invoke();
            else
                alCompletarCargarPartida?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[LoadingManager] Aviso en evento al completar carga: " + e.Message);
        }

        // Activamos la nueva escena y esperamos a que termine de cargar
        if (operacion != null)
        {
            operacion.allowSceneActivation = true;
            while (!operacion.isDone)
            {
                yield return null;
            }
        }
        else
        {
            SceneManager.LoadScene(nombreEscena);
            yield return null;
        }

        // --- PASO 6: TRANSICIÓN FINAL DE SALIDA (FADE OUT) EN LA NUEVA ESCENA ---
        // Desactivamos el bloqueo de clics para que el jugador ya pueda interactuar mientras desaparece
        for (int i = 0; i < graficos.Length; i++)
        {
            if (graficos[i] != null) graficos[i].raycastTarget = false;
        }

        float relojFadeOut = 0f;
        float duracionSalida = Mathf.Max(duracionFadeSalida, 0.1f);

        while (relojFadeOut < duracionSalida)
        {
            relojFadeOut += Time.unscaledDeltaTime;
            float factor = Mathf.Clamp01(relojFadeOut / duracionSalida);

            for (int i = 0; i < graficos.Length; i++)
            {
                if (graficos[i] != null)
                {
                    Color c = graficos[i].color;
                    c.a = Mathf.Lerp(alfasOriginales[i], 0f, factor);
                    graficos[i].color = c;
                }
            }
            yield return null;
        }

        // --- PASO 7: LIMPIEZA DE OBJETOS TEMPORALES ---
        // Destruimos el Canvas temporal y este LoadingManager al terminar el desvanecimiento
        Destroy(objetoCanvasCarga);
        Destroy(gameObject);
    }
}