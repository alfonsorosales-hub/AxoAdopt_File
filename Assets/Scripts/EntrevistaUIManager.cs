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
        OcultarPaneles();

        if (!int.TryParse(idAdoptante, out int idNumerica))
        {
            Debug.LogError($"[EntrevistaUIManager] La ID '{idAdoptante}' no es válida.");
            return;
        }

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
            Debug.LogError("[EntrevistaUIManager] Falta asignar PreguntasDatabase.");
            return;
        }

        var datosEntrevista = preguntasDB.ObtenerPreguntasPorID(idAdoptante);

        if (datosEntrevista != null)
        {
            for (int i = 0; i < datosEntrevista.Count; i++)
            {
                var item = datosEntrevista[i];
                if (item == null) continue;

                // Asignar Texto de Pregunta
                if (i < camposPreguntas.Length && camposPreguntas[i] != null)
                {
                    camposPreguntas[i].text = ObtenerTextoPregunta(item);
                }

                // Asignar Texto de Respuesta
                if (i < camposRespuestas.Length && camposRespuestas[i] != null)
                {
                    camposRespuestas[i].text = ObtenerTextoRespuesta(item);
                }
            }
        }
    }

    /// Lee de forma segura el texto de la pregunta sin importar el nombre exacto del campo
    private string ObtenerTextoPregunta(PreguntaData item)
    {
        var tipo = item.GetType();
        var campoPregunta = tipo.GetField("pregunta") ?? tipo.GetField("textoPregunta") ?? tipo.GetField("Pregunta");
        if (campoPregunta != null) return campoPregunta.GetValue(item)?.ToString() ?? "";

        var propPregunta = tipo.GetProperty("pregunta") ?? tipo.GetProperty("textoPregunta") ?? tipo.GetProperty("Pregunta");
        if (propPregunta != null) return propPregunta.GetValue(item)?.ToString() ?? "";

        return "";
    }

    /// Lee de forma segura la respuesta sin importar el nombre exacto del campo
    private string ObtenerTextoRespuesta(PreguntaData item)
    {
        var tipo = item.GetType();

        // Probar nombres comunes de campos de respuesta
        var campoRespuesta = tipo.GetField("respuesta") ?? tipo.GetField("textoRespuesta") ?? tipo.GetField("respuestaCorrecta") ?? tipo.GetField("Respuesta");
        if (campoRespuesta != null) return campoRespuesta.GetValue(item)?.ToString() ?? "";

        var propRespuesta = tipo.GetProperty("respuesta") ?? tipo.GetProperty("textoRespuesta") ?? tipo.GetProperty("respuestaCorrecta") ?? tipo.GetProperty("Respuesta");
        if (propRespuesta != null) return propRespuesta.GetValue(item)?.ToString() ?? "";

        // Si es una lista interna de respuestas dentro de PreguntaData
        var campoListaRespuestas = tipo.GetField("respuestas") ?? tipo.GetField("Respuestas");
        if (campoListaRespuestas != null)
        {
            var lista = campoListaRespuestas.GetValue(item) as System.Collections.IList;
            if (lista != null && lista.Count > 0) return lista[0]?.ToString() ?? "";
        }

        return "";
    }

    public void OcultarPaneles()
    {
        if (panel3Preguntas != null) panel3Preguntas.SetActive(false);
        if (panel4Preguntas != null) panel4Preguntas.SetActive(false);
    }
}