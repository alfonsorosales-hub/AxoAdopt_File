using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EntrevistaUIManager : MonoBehaviour
{
    [Header("Bases de Datos")]
    [SerializeField] private PreguntasDatabase preguntasDB;

    [Header("Paneles de Interfaz")]
    [SerializeField] private GameObject panel3Preguntas;
    [SerializeField] private GameObject panel4Preguntas;

    [Header("Panel 3 Preguntas (opcional: si se dejan vacíos se buscan dentro del panel)")]
    [SerializeField] private Button[] botonesPreguntasPanel3;
    [SerializeField] private TextMeshProUGUI[] textosPreguntasPanel3;   // el TextMeshPro de cada botón, en el mismo orden
    [SerializeField] private TextMeshProUGUI respuestaUnicaPanel3;

    [Header("Panel 4 Preguntas (opcional: si se dejan vacíos se buscan dentro del panel)")]
    [SerializeField] private Button[] botonesPreguntasPanel4;
    [SerializeField] private TextMeshProUGUI[] textosPreguntasPanel4;   // el TextMeshPro de cada botón, en el mismo orden
    [SerializeField] private TextMeshProUGUI respuestaUnicaPanel4;

    // Elementos de UI ya resueltos de un panel (asignados en el Inspector o detectados solos)
    private class PanelUI
    {
        public string nombre;
        public GameObject panel;
        public int esperado;
        public Button[] botones = new Button[0];
        public TextMeshProUGUI[] textosPregunta = new TextMeshProUGUI[0];   // el TextMeshPro dentro de cada botón
        public TextMeshProUGUI respuesta;                                   // un único campo de respuesta
    }

    private PanelUI ui3;
    private PanelUI ui4;
    private PanelUI uiActivo;
    private List<PreguntaData> listaActual = new List<PreguntaData>();
    private bool resuelto;

    private void Awake()
    {
        ResolverPaneles();
        OcultarPaneles();
    }

    private void ResolverPaneles()
    {
        if (resuelto) return;
        resuelto = true;

        ui3 = Resolver("Panel 3", panel3Preguntas, 3, botonesPreguntasPanel3, textosPreguntasPanel3, respuestaUnicaPanel3);
        ui4 = Resolver("Panel 4", panel4Preguntas, 4, botonesPreguntasPanel4, textosPreguntasPanel4, respuestaUnicaPanel4);
    }

    // =====================================================================
    // CARGA DE UNA ENTREVISTA
    // =====================================================================
    public void CargarEntrevistaPorID(string idAdoptante)
    {
        Debug.Log($"[UI] CargarEntrevistaPorID('{idAdoptante}')");
        ResolverPaneles();
        OcultarPaneles();

        if (!int.TryParse(idAdoptante != null ? idAdoptante.Trim() : null, out int idNumerica))
        {
            Debug.LogError($"[UI] La ID '{idAdoptante}' no es numérica.");
            return;
        }

        if (idNumerica < 1 || idNumerica > 75)
        {
            Debug.LogWarning($"[UI] La ID {idNumerica} está fuera del rango 1-75. No se muestra ningún panel.");
            return;
        }

        // Formato de PreguntasDatabase: "01" ... "75"
        string idFormateada = idNumerica.ToString("00");
        if (idFormateada != idAdoptante)
            Debug.LogWarning($"[UI] ID '{idAdoptante}' normalizada a '{idFormateada}'. Conviene corregirla en AdoptanteData.");

        // 1) El PANEL depende solo de la ID: 1-15 -> panel de 3, 16-75 -> panel de 4.
        //    Se activa SIEMPRE, antes de consultar la base de datos.
        bool usaPanel4 = idNumerica >= 16;
        uiActivo = usaPanel4 ? ui4 : ui3;

        Debug.Log($"[UI] ID '{idFormateada}' -> {uiActivo.nombre}.");
        MostrarPanel(uiActivo.panel, uiActivo.nombre);

        // 2) Preguntas (en los botones) y respuesta (campo único) se cargan después
        if (uiActivo.respuesta != null) uiActivo.respuesta.text = "";
        LimpiarBotones(uiActivo);

        if (preguntasDB == null)
        {
            Debug.LogError("[UI] Falta asignar PreguntasDatabase en el Inspector.");
            return;
        }

        List<PreguntaData> lista = preguntasDB.ObtenerPreguntasPorID(idFormateada);

        if (lista == null || lista.Count == 0)
        {
            Debug.LogWarning($"[UI] PreguntasDatabase no devolvió preguntas para la ID '{idFormateada}'. " +
                             "El panel se muestra vacío. Revisa que el asset tenga los 75 grupos (menú contextual > Regenerar).");
            return;
        }

        if ((lista.Count >= 4) != usaPanel4)
            Debug.LogWarning($"[UI] La ID {idNumerica} tiene {lista.Count} preguntas, pero su panel es el de {(usaPanel4 ? 4 : 3)}. " +
                             "Regenera el asset PreguntasDatabase.");

        listaActual = new List<PreguntaData>(lista);
        CargarPreguntas(uiActivo, listaActual, idFormateada);
    }

    private void CargarPreguntas(PanelUI ui, List<PreguntaData> lista, string idAdoptante)
    {
        for (int i = 0; i < ui.botones.Length; i++)
        {
            Button boton = ui.botones[i];
            if (boton == null) continue;

            boton.onClick.RemoveAllListeners();

            if (i >= lista.Count || lista[i] == null)
            {
                boton.gameObject.SetActive(false);   // sobra un botón para esta ID
                continue;
            }

            PreguntaData dato = lista[i];
            boton.gameObject.SetActive(true);

            TextMeshProUGUI texto = ui.textosPregunta[i];
            if (texto != null)
            {
                texto.gameObject.SetActive(true);
                texto.text = dato.enunciadoPregunta;
            }
            else
            {
                Debug.LogWarning($"[UI] El botón '{boton.name}' no tiene un TextMeshPro dentro para mostrar la pregunta.");
            }

            int indice = i;
            boton.onClick.AddListener(() => MostrarRespuesta(indice));

            Debug.Log($"[UI] ID {idAdoptante} botón #{i}: '{dato.enunciadoPregunta}' -> '{dato.respuestaNPC}'");
        }

        if (lista.Count > ui.botones.Length)
            Debug.LogWarning($"[UI] Hay {lista.Count} preguntas pero el {ui.nombre} solo tiene {ui.botones.Length} botones.");
    }

    /// <summary>Muestra en el campo único la respuesta de la pregunta pulsada.</summary>
    public void MostrarRespuesta(int indice)
    {
        if (uiActivo == null || indice < 0 || indice >= listaActual.Count) return;

        if (uiActivo.respuesta == null)
        {
            Debug.LogError($"[UI] {uiActivo.nombre}: no hay un TextMeshPro de respuesta asignado.");
            return;
        }

        uiActivo.respuesta.gameObject.SetActive(true);
        uiActivo.respuesta.text = listaActual[indice].respuestaNPC;
    }

    private void LimpiarBotones(PanelUI ui)
    {
        for (int i = 0; i < ui.textosPregunta.Length; i++)
            if (ui.textosPregunta[i] != null) ui.textosPregunta[i].text = "";
    }

    /// <summary>Activa el panel y también los padres que estén desactivados.</summary>
    private void MostrarPanel(GameObject panel, string nombre)
    {
        if (panel == null)
        {
            Debug.LogError($"[UI] '{nombre}' NO está asignado en el Inspector.");
            return;
        }

        for (Transform t = panel.transform; t != null; t = t.parent)
        {
            if (!t.gameObject.activeSelf)
            {
                t.gameObject.SetActive(true);
                if (t.gameObject != panel)
                    Debug.LogWarning($"[UI] El padre '{t.name}' de '{panel.name}' estaba desactivado; se activó para mostrar el panel.");
            }
        }

        Debug.Log($"[UI] '{nombre}' ({panel.name}) activeInHierarchy={panel.activeInHierarchy}");
    }

    // =====================================================================
    // RESOLUCIÓN DE ELEMENTOS: Inspector primero; si está vacío, se busca dentro del panel
    // =====================================================================
    private PanelUI Resolver(string nombre, GameObject panel, int esperado, Button[] botonesInspector,
                             TextMeshProUGUI[] textosInspector, TextMeshProUGUI respuestaInspector)
    {
        var ui = new PanelUI { nombre = nombre, panel = panel, esperado = esperado };

        if (panel == null)
        {
            Debug.LogError($"[UI] {nombre}: el panel NO está asignado en el Inspector.");
            return ui;
        }

        bool botonesAsignados = botonesInspector != null && botonesInspector.Length > 0;
        ui.botones = botonesAsignados ? botonesInspector : DetectarBotones(ui);

        // Texto de cada pregunta: el asignado en el Inspector; si falta, el TextMeshPro hijo del botón
        ui.textosPregunta = new TextMeshProUGUI[ui.botones.Length];
        for (int i = 0; i < ui.botones.Length; i++)
        {
            TextMeshProUGUI texto = (textosInspector != null && i < textosInspector.Length) ? textosInspector[i] : null;

            if (texto == null && ui.botones[i] != null)
                texto = ui.botones[i].GetComponentInChildren<TextMeshProUGUI>(true);

            ui.textosPregunta[i] = texto;
        }

        ui.respuesta = respuestaInspector != null ? respuestaInspector : DetectarRespuesta(ui);

        Validar(ui);
        return ui;
    }

    /// <summary>Busca los botones de pregunta dentro del panel.</summary>
    private Button[] DetectarBotones(PanelUI ui)
    {
        var candidatos = new List<Button>();
        var nombrados = new List<Button>();

        foreach (Button b in ui.panel.GetComponentsInChildren<Button>(true))
        {
            TextMeshProUGUI t = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t == null || EsBotonDeAccion(b, t)) continue;

            candidatos.Add(b);
            if (Contiene(b.name, "pregunta") || Contiene(t.name, "pregunta")) nombrados.Add(b);
        }

        // Si algunos se llaman "pregunta", se usan solo esos
        if (nombrados.Count > 0) candidatos = nombrados;

        if (candidatos.Count > ui.esperado)
        {
            Debug.LogWarning($"[UI] {ui.nombre}: se encontraron {candidatos.Count} botones y se usarán los primeros {ui.esperado}. " +
                             "Si no son los correctos, asígnalos en el Inspector.");
            candidatos = candidatos.GetRange(0, ui.esperado);
        }

        return candidatos.ToArray();
    }

    /// <summary>Busca el TextMeshPro único de respuesta: el que no está dentro de un botón.</summary>
    private TextMeshProUGUI DetectarRespuesta(PanelUI ui)
    {
        // Todo texto que esté dentro de cualquier botón queda excluido
        var excluidos = new HashSet<TextMeshProUGUI>();
        foreach (Button b in ui.panel.GetComponentsInChildren<Button>(true))
            foreach (TextMeshProUGUI t in b.GetComponentsInChildren<TextMeshProUGUI>(true))
                excluidos.Add(t);

        var porNombre = new List<TextMeshProUGUI>();
        var restantes = new List<TextMeshProUGUI>();

        foreach (TextMeshProUGUI t in ui.panel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (excluidos.Contains(t)) continue;
            if (Contiene(t.name, "respuesta") || Contiene(t.name, "answer")) porNombre.Add(t);
            else restantes.Add(t);
        }

        if (porNombre.Count >= 1)
        {
            if (porNombre.Count > 1)
                Debug.LogWarning($"[UI] {ui.nombre}: hay {porNombre.Count} textos de respuesta; se usará '{porNombre[0].name}'. Asígnalo en el Inspector si no es el correcto.");
            return porNombre[0];
        }

        if (restantes.Count == 1) return restantes[0];   // único texto fuera de los botones

        return null;   // ambiguo: Validar() lo informa
    }

    private static bool EsBotonDeAccion(Button b, TextMeshProUGUI texto)
    {
        string[] palabras = { "cerrar", "close", "salir", "exit", "volver", "back", "adoptar", "rechazar", "aceptar", "decision" };
        foreach (string p in palabras)
            if (Contiene(b.name, p) || Contiene(texto.text, p)) return true;
        return false;
    }

    private static bool Contiene(string texto, string palabra)
    {
        return !string.IsNullOrEmpty(texto) && texto.IndexOf(palabra, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // ---------------------------------------------------------------------
    // Validación: solo escribe en la consola; nunca impide que un panel se muestre.
    // ---------------------------------------------------------------------
    private void Validar(PanelUI ui)
    {
        int botonesValidos = 0;
        foreach (Button b in ui.botones) if (b != null) botonesValidos++;

        if (botonesValidos < ui.esperado || ui.respuesta == null)
        {
            var sb = new StringBuilder();
            sb.Append($"[UI] {ui.nombre}: encontré {botonesValidos} botones de pregunta (se necesitan {ui.esperado}) ");
            sb.Append(ui.respuesta != null ? "y el texto de respuesta. " : "y NO encontré el texto de respuesta. ");
            sb.Append("Asígnalos en el Inspector de EntrevistaUIManager (botones y respuesta única). ");

            sb.Append("TextMeshPro dentro del panel: ");
            foreach (TextMeshProUGUI t in ui.panel.GetComponentsInChildren<TextMeshProUGUI>(true))
                sb.Append($"'{t.name}', ");

            sb.Append("| Botones: ");
            foreach (Button b in ui.panel.GetComponentsInChildren<Button>(true))
                sb.Append($"'{b.name}', ");

            Debug.LogError(sb.ToString());
            return;
        }

        Debug.Log($"[UI] {ui.nombre} listo: {botonesValidos} botones de pregunta y respuesta en '{ui.respuesta.name}'.");
    }

    public void OcultarPaneles()
    {
        listaActual.Clear();
        if (panel3Preguntas != null) panel3Preguntas.SetActive(false);
        if (panel4Preguntas != null) panel4Preguntas.SetActive(false);
    }
}