using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    [Header("Elementos de la UI")]
    [SerializeField] private GameObject panelPantallaCarga;
    [SerializeField] private Slider barraProgreso;
    [SerializeField] private TextMeshProUGUI textoPorcentaje;
    [SerializeField] private TextMeshProUGUI textoConsejo;

    [Header("Tiempos (Segundos)")]
    [SerializeField] private float duracionFade = 1.2f;
    [SerializeField] private float tiempoMinimoCarga = 5.0f;

    [Header("Configuración de Consejos")]
    [TextArea(2, 4)]
    [SerializeField] private List<string> listaConsejos = new List<string>();

    [Header("Nombres de Escenas")]
    [SerializeField] private string escenaNuevaPartida = "Nivel1";
    [SerializeField] private string escenaCargarPartida = "Nivel1";

    [Header("Tus Funciones Antiguas")]
    public UnityEvent alCompletarNuevaPartida;
    public UnityEvent alCompletarCargarPartida;

    private bool estaCargando = false;

    public void BotonIniciarPartida()
    {
        if (estaCargando) return;
        StartCoroutine(ProcesoCompletoDeCarga(escenaNuevaPartida.Trim(), true));
    }

    public void BotonCargarPartida()
    {
        if (estaCargando) return;
        // Limpiamos espacios ocultos y usamos escenaCargarPartida si no hay clave guardada válida
        string guardada = PlayerPrefs.GetString("EscenaGuardada", "");
        string escenaDestino = string.IsNullOrWhiteSpace(guardada) ? escenaCargarPartida.Trim() : guardada.Trim();
        StartCoroutine(ProcesoCompletoDeCarga(escenaDestino, false));
    }

    private IEnumerator ProcesoCompletoDeCarga(string nombreEscena, bool esNuevaPartida)
    {
        estaCargando = true;
        Time.timeScale = 1f; // Asegura que el tiempo del juego no esté pausado

        // 1. Consejo aleatorio
        if (listaConsejos.Count > 0 && textoConsejo != null)
        {
            textoConsejo.text = listaConsejos[Random.Range(0, listaConsejos.Count)];
        }

        // 2. Reiniciar Slider y texto
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

        // 3. Mostrar panel al frente
        panelPantallaCarga.transform.SetAsLastSibling();
        panelPantallaCarga.SetActive(true);

        // FASE 1: Fade In
        Graphic[] graficos = panelPantallaCarga.GetComponentsInChildren<Graphic>(true);
        float[] alfasDestino = new float[graficos.Length];

        for (int i = 0; i < graficos.Length; i++)
        {
            alfasDestino[i] = graficos[i].color.a > 0.05f ? graficos[i].color.a : 1f;
            Color c = graficos[i].color;
            c.a = 0f;
            graficos[i].color = c;
        }

        float relojFade = 0f;
        while (relojFade < duracionFade)
        {
            relojFade += Time.unscaledDeltaTime;
            float factor = Mathf.Clamp01(relojFade / duracionFade);

            for (int i = 0; i < graficos.Length; i++)
            {
                if (graficos[i] != null)
                {
                    Color c = graficos[i].color;
                    c.a = Mathf.Lerp(0f, alfasDestino[i], factor);
                    graficos[i].color = c;
                }
            }
            yield return null;
        }

        for (int i = 0; i < graficos.Length; i++)
        {
            if (graficos[i] != null)
            {
                Color c = graficos[i].color;
                c.a = alfasDestino[i];
                graficos[i].color = c;
            }
        }

        // FASE 2: Llenado garantizado del Slider y carga de escena
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

            // Actualizar Slider de todas las formas posibles
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

        // Asegurar 100% visual
        if (barraProgreso != null) barraProgreso.value = 1f;
        if (textoPorcentaje != null) textoPorcentaje.text = "100%";

        yield return new WaitForSecondsRealtime(0.25f);

        // Ejecutar eventos de guardado/nueva partida de forma segura
        try
        {
            if (esNuevaPartida)
                alCompletarNuevaPartida?.Invoke();
            else
                alCompletarCargarPartida?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Error en evento al completar carga: " + e.Message);
        }

        // Cambio de escena garantizado
        if (operacion != null)
        {
            operacion.allowSceneActivation = true;
        }
        else
        {
            SceneManager.LoadScene(nombreEscena);
        }
    }
}