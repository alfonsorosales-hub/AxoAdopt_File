using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class EntrevistaUIManager : MonoBehaviour
{
    [Header("Base de Datos Oficial")]
    [SerializeField] private PreguntasDatabase preguntasDB;

    [Header("Paneles de Interfaz")]
    [SerializeField] private GameObject panel3Preguntas;
    [SerializeField] private GameObject panel4Preguntas;

    [Header("Panel 3 Preguntas")]
    [SerializeField] private Button[] botonesPreguntasPanel3;
    [SerializeField] private TextMeshProUGUI respuestaUnicaPanel3;

    [Header("Panel 4 Preguntas")]
    [SerializeField] private Button[] botonesPreguntasPanel4;
    [SerializeField] private TextMeshProUGUI respuestaUnicaPanel4;

    private List<PreguntaData> listaPreguntasActuales = new List<PreguntaData>();
    private TextMeshProUGUI campoRespuestaActivo = null;

    private void Awake()
    {
        ApagarAmbosPaneles();
    }

    /// <summary>
    /// Invoca la entrevista al entrar en contacto con el Trigger pasando la ID del NPC
    /// </summary>
    public void CargarEntrevistaPorID(string idAdoptante)
    {
        Debug.Log($"[Entrevista] Solicitud recibida con ID: '{idAdoptante}'");

        ApagarAmbosPaneles();

        if (preguntasDB == null)
        {
            Debug.LogError("[Entrevista] preguntasDB no está asignada en el Inspector.");
            return;
        }

        if (string.IsNullOrEmpty(idAdoptante))
        {
            Debug.LogWarning("[Entrevista] La ID recibida está vacía.");
            return;
        }

        List<PreguntaData> preguntasObtenidas = BuscarPreguntas(idAdoptante);

        if (preguntasObtenidas == null || preguntasObtenidas.Count == 0)
        {
            Debug.LogWarning($"[Entrevista] No se encontraron preguntas para la ID '{idAdoptante}'. Revisa cómo está escrita la ID en PreguntasDatabase.");
            return;
        }

        listaPreguntasActuales = new List<PreguntaData>(preguntasObtenidas);
        int totalPreguntas = listaPreguntasActuales.Count;
        Debug.Log($"[Entrevista] {totalPreguntas} preguntas encontradas.");

        // 4 o más preguntas -> panel de 4; 1 a 3 preguntas -> panel de 3
        if (totalPreguntas >= 4)
        {
            ActivarConPadres(panel4Preguntas);
            campoRespuestaActivo = respuestaUnicaPanel4;
            ConfigurarPanelDirecto(botonesPreguntasPanel4, respuestaUnicaPanel4);
        }
        else
        {
            ActivarConPadres(panel3Preguntas);
            campoRespuestaActivo = respuestaUnicaPanel3;
            ConfigurarPanelDirecto(botonesPreguntasPanel3, respuestaUnicaPanel3);
        }
    }

    /// <summary>
    /// Prueba varios formatos de ID ("1", "01", tal cual) hasta encontrar preguntas.
    /// </summary>
    private List<PreguntaData> BuscarPreguntas(string idOriginal)
    {
        string idLimpia = idOriginal.Trim();
        List<string> candidatos = new List<string> { idLimpia };

        if (int.TryParse(idLimpia, out int numID))
        {
            candidatos.Add(numID.ToString("00")); // "01"
            candidatos.Add(numID.ToString());     // "1"
        }

        foreach (string candidato in candidatos)
        {
            List<PreguntaData> resultado = preguntasDB.ObtenerPreguntasPorID(candidato);
            if (resultado != null && resultado.Count > 0)
            {
                Debug.Log($"[Entrevista] ID encontrada con el formato '{candidato}'.");
                return resultado;
            }
        }

        return null;
    }

    /// <summary>
    /// Activa el objeto y todos sus padres, por si alguno quedó desactivado.
    /// </summary>
    private void ActivarConPadres(GameObject objetivo)
    {
        if (objetivo == null)
        {
            Debug.LogError("[Entrevista] Falta asignar un panel en el Inspector.");
            return;
        }

        Transform t = objetivo.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    private void ConfigurarPanelDirecto(Button[] botones, TextMeshProUGUI campoRespuesta)
    {
        if (campoRespuesta != null)
        {
            campoRespuesta.gameObject.SetActive(true);
            campoRespuesta.text = "";
        }

        if (botones == null || botones.Length == 0)
        {
            Debug.LogError("[Entrevista] El arreglo de botones del panel está vacío en el Inspector.");
            return;
        }

        // Resetear listeners y estado de todos los botones
        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] != null)
            {
                botones[i].onClick.RemoveAllListeners();
                botones[i].gameObject.SetActive(false);
            }
        }

        // Asignación de preguntas a los botones
        for (int i = 0; i < listaPreguntasActuales.Count; i++)
        {
            if (i >= botones.Length)
            {
                Debug.LogWarning($"[Entrevista] Hay más preguntas ({listaPreguntasActuales.Count}) que botones ({botones.Length}).");
                break;
            }

            if (botones[i] == null) continue;

            int indiceFijo = i;
            PreguntaData data = listaPreguntasActuales[i];

            botones[i].gameObject.SetActive(true);

            TextMeshProUGUI textoInterno = botones[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if (textoInterno != null)
            {
                textoInterno.gameObject.SetActive(true);
                textoInterno.text = data.enunciadoPregunta;
            }
            else
            {
                Debug.LogWarning($"[Entrevista] El botón '{botones[i].name}' no tiene un TextMeshProUGUI hijo.");
            }

            botones[i].onClick.AddListener(() => MostrarRespuesta(indiceFijo));
        }
    }

    public void MostrarRespuesta(int indice)
    {
        if (indice < 0 || indice >= listaPreguntasActuales.Count) return;

        if (campoRespuestaActivo != null)
        {
            campoRespuestaActivo.gameObject.SetActive(true);
            campoRespuestaActivo.text = listaPreguntasActuales[indice].respuestaNPC;
        }
    }

    public void OcultarPaneles()
    {
        ApagarAmbosPaneles();
    }

    private void ApagarAmbosPaneles()
    {
        listaPreguntasActuales.Clear();
        campoRespuestaActivo = null;

        if (respuestaUnicaPanel3 != null) respuestaUnicaPanel3.text = "";
        if (respuestaUnicaPanel4 != null) respuestaUnicaPanel4.text = "";

        if (panel3Preguntas != null) panel3Preguntas.SetActive(false);
        if (panel4Preguntas != null) panel4Preguntas.SetActive(false);
    }
}