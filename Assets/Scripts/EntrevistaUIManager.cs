using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EntrevistaUIManager : MonoBehaviour
{
    [Header("Bases de Datos")]
    [SerializeField] private PreguntasDatabase preguntasDB;

    [Header("Paneles de Interfaz")]
    [SerializeField] private GameObject panel3Preguntas;
    [SerializeField] private GameObject panel4Preguntas;

    [Header("Textos Panel 3 Preguntas")]
    [SerializeField] private TextMeshProUGUI[] preguntasTextoPanel3;
    [SerializeField] private TextMeshProUGUI[] respuestasTextoPanel3;

    [Header("Textos Panel 4 Preguntas")]
    [SerializeField] private TextMeshProUGUI[] preguntasTextoPanel4;
    [SerializeField] private TextMeshProUGUI[] respuestasTextoPanel4;

    public void CargarEntrevistaPorID(string idAdoptante)
    {
        Debug.Log($"[UI] CargarEntrevistaPorID('{idAdoptante}')");
        OcultarPaneles();

        if (!int.TryParse(idAdoptante != null ? idAdoptante.Trim() : null, out int idNumerica))
        {
            Debug.LogError($"[UI] La ID '{idAdoptante}' no es numérica.");
            return;
        }

        // Normalizar a 2 dígitos ("1" -> "01"), que es el formato de PreguntasDatabase
        string idFormateada = idNumerica.ToString("00");
        if (idFormateada != idAdoptante)
            Debug.LogWarning($"[UI] ID '{idAdoptante}' normalizada a '{idFormateada}'. Conviene corregirla en AdoptanteData.");

        if (idNumerica >= 1 && idNumerica <= 15)
        {
            MostrarPanel(panel3Preguntas, "Panel 3 preguntas");
            CargarPreguntasYRespuestas(preguntasTextoPanel3, respuestasTextoPanel3, idFormateada);
        }
        else if (idNumerica >= 16 && idNumerica <= 75)
        {
            MostrarPanel(panel4Preguntas, "Panel 4 preguntas");
            CargarPreguntasYRespuestas(preguntasTextoPanel4, respuestasTextoPanel4, idFormateada);
        }
        else
        {
            Debug.LogWarning($"[UI] La ID {idNumerica} está fuera del rango 1-75. No se muestra ningún panel.");
        }
    }

    private void MostrarPanel(GameObject panel, string nombre)
    {
        if (panel == null)
        {
            Debug.LogError($"[UI] '{nombre}' NO está asignado en el Inspector.");
            return;
        }

        panel.SetActive(true);
        Debug.Log($"[UI] '{nombre}' ({panel.name}) activeSelf={panel.activeSelf}, activeInHierarchy={panel.activeInHierarchy}");

        if (!panel.activeInHierarchy)
            Debug.LogWarning($"[UI] '{panel.name}' está activo pero un padre está desactivado, por eso no se ve.");
    }

    private void CargarPreguntasYRespuestas(TextMeshProUGUI[] camposPreguntas, TextMeshProUGUI[] camposRespuestas, string idAdoptante)
    {
        for (int i = 0; i < camposPreguntas.Length; i++)
            if (camposPreguntas[i] != null) camposPreguntas[i].text = "";
        for (int i = 0; i < camposRespuestas.Length; i++)
            if (camposRespuestas[i] != null) camposRespuestas[i].text = "";

        if (preguntasDB == null)
        {
            Debug.LogError("[UI] Falta asignar PreguntasDatabase en el Inspector.");
            return;
        }

        List<PreguntaData> lista = preguntasDB.ObtenerPreguntasPorID(idAdoptante);

        if (lista == null || lista.Count == 0)
        {
            Debug.LogWarning($"[UI] PreguntasDatabase no devolvió preguntas para la ID '{idAdoptante}'. " +
                             $"Revisa que el asset tenga los 75 grupos (menú contextual > Regenerar).");
            return;
        }

        Debug.Log($"[UI] ID '{idAdoptante}': {lista.Count} preguntas.");

        for (int i = 0; i < lista.Count; i++)
        {
            PreguntaData dato = lista[i];
            if (dato == null)
            {
                Debug.LogWarning($"[UI] El elemento {i} de la lista es null.");
                continue;
            }

            if (i < camposPreguntas.Length && camposPreguntas[i] != null)
                camposPreguntas[i].text = dato.enunciadoPregunta;

            if (i < camposRespuestas.Length && camposRespuestas[i] != null)
                camposRespuestas[i].text = dato.respuestaNPC;

            Debug.Log($"[UI] #{i}: '{dato.enunciadoPregunta}'");
        }

        if (lista.Count > camposPreguntas.Length)
            Debug.LogWarning($"[UI] Hay {lista.Count} preguntas pero el panel solo tiene {camposPreguntas.Length} campos.");
    }

    public void OcultarPaneles()
    {
        if (panel3Preguntas != null) panel3Preguntas.SetActive(false);
        if (panel4Preguntas != null) panel4Preguntas.SetActive(false);
    }
}
