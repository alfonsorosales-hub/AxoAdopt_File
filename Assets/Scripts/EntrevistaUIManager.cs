using UnityEngine;
using TMPro; // Si usas el Text clásico de Unity, cambia 'TextMeshProUGUI' por 'Text'

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
        OcultarPaneles();

        if (!int.TryParse(idAdoptante, out int idNumerica))
        {
            Debug.LogError($"[EntrevistaUIManager] La ID '{idAdoptante}' no es válida.");
            return;
        }

        // Seleccionar panel según el rango de ID (01-15: 3 preguntas, 16-75: 4 preguntas)
        if (idNumerica >= 1 && idNumerica <= 15)
        {
            if (panel3Preguntas != null) panel3Preguntas.SetActive(true);
            CargarPreguntasYRespuestas(preguntasTextoPanel3, respuestasTextoPanel3, idAdoptante);
        }
        else if (idNumerica >= 16 && idNumerica <= 75)
        {
            if (panel4Preguntas != null) panel4Preguntas.SetActive(true);
            CargarPreguntasYRespuestas(preguntasTextoPanel4, respuestasTextoPanel4, idAdoptante);
        }
    }

    private void CargarPreguntasYRespuestas(TextMeshProUGUI[] camposPreguntas, TextMeshProUGUI[] camposRespuestas, string idAdoptante)
    {
        // 1. Limpiar los textos en la interfaz
        for (int i = 0; i < camposPreguntas.Length; i++)
        {
            if (camposPreguntas[i] != null) camposPreguntas[i].text = "";
        }
        for (int i = 0; i < camposRespuestas.Length; i++)
        {
            if (camposRespuestas[i] != null) camposRespuestas[i].text = "";
        }

        if (preguntasDB == null)
        {
            Debug.LogError("[EntrevistaUIManager] Falta asignar PreguntasDatabase en el Inspector.");
            return;
        }

        // 2. Cargar los datos desde PreguntasDatabase usando la ID
        var datosEntrevista = preguntasDB.ObtenerPreguntasPorID(idAdoptante);

        if (datosEntrevista != null)
        {
            // Asignar las preguntas a los botones
            if (datosEntrevista.preguntas != null)
            {
                for (int i = 0; i < camposPreguntas.Length && i < datosEntrevista.preguntas.Count; i++)
                {
                    if (camposPreguntas[i] != null)
                    {
                        camposPreguntas[i].text = datosEntrevista.preguntas[i];
                    }
                }
            }

            // Asignar las respuestas correspondientes
            if (datosEntrevista.respuestas != null)
            {
                for (int i = 0; i < camposRespuestas.Length && i < datosEntrevista.respuestas.Count; i++)
                {
                    if (camposRespuestas[i] != null)
                    {
                        camposRespuestas[i].text = datosEntrevista.respuestas[i];
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"[EntrevistaUIManager] No se encontraron preguntas registradas en PreguntasDatabase para la ID: '{idAdoptante}'.");
        }
    }

    public void OcultarPaneles()
    {
        if (panel3Preguntas != null) panel3Preguntas.SetActive(false);
        if (panel4Preguntas != null) panel4Preguntas.SetActive(false);
    }
}