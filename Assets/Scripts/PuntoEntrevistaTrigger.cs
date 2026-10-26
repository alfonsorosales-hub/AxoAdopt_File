using UnityEngine;
using System.Text.RegularExpressions;

public class PuntoEntrevistaTrigger : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private EntrevistaUIManager uiManager;

    [Header("Configuración de Filtro")]
    [SerializeField] private string tagNPC = "NPC";

    private Transform npcActual;

    private void Awake()
    {
        if (uiManager == null)
            uiManager = UnityEngine.Object.FindFirstObjectByType<EntrevistaUIManager>(FindObjectsInactive.Include);

        if (uiManager == null)
            Debug.LogError("[TRIGGER] No hay EntrevistaUIManager asignado en la escena.");
    }

    private void OnTriggerEnter(Collider other)
    {
        Transform raiz = ObtenerRaizNPC(other.transform);
        if (raiz == null) return;          // No es un NPC
        if (raiz == npcActual) return;     // Mismo NPC ya procesado

        // Obtener la ID del NPC
        string idAdoptante = ObtenerIDDesdeNPC(raiz) ?? ObtenerIDDesdeNPC(other.transform);

        if (string.IsNullOrEmpty(idAdoptante))
        {
            Debug.LogError($"[TRIGGER] No se encontró una ID válida de 2 dígitos (01-75) para '{raiz.name}'.");
            return;
        }

        npcActual = raiz;
        Debug.Log($"<color=green>[TRIGGER] ¡ID leída con éxito! ID: '{idAdoptante}'</color>");

        if (uiManager != null) 
        {
            uiManager.CargarEntrevistaPorID(idAdoptante);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Transform raiz = ObtenerRaizNPC(other.transform);
        if (raiz == null || raiz != npcActual) return;

        npcActual = null;
        if (uiManager != null) uiManager.OcultarPaneles();
    }

    private Transform ObtenerRaizNPC(Transform t)
    {
        Transform encontrado = null;
        while (t != null)
        {
            if (t.CompareTag(tagNPC)) encontrado = t;
            t = t.parent;
        }
        return encontrado;
    }

    /// <summary>
    /// Escanea los datos adjuntos en la jerarquía del NPC para obtener su ID numérica (01 a 75).
    /// </summary>
    private string ObtenerIDDesdeNPC(Transform objetivo)
    {
        if (objetivo == null) return null;

        // 1. Probar en los componentes adjuntos
        Component[] componentes = objetivo.GetComponentsInChildren<Component>(true);

        for (int i = 0; i < componentes.Length; i++)
        {
            Component comp = componentes[i];
            if (comp == null || comp == this) continue;

            // Extraer ID si el nombre del componente o sus datos guardan el número
            string idValida = ExtraerNumeroID(comp.ToString());
            if (!string.IsNullOrEmpty(idValida)) return idValida;
        }

        // 2. Probar en el nombre del objeto raíz del NPC instanciado en escena
        string idRaiz = ExtraerNumeroID(objetivo.name);
        if (!string.IsNullOrEmpty(idRaiz)) return idRaiz;

        return null;
    }

    private string ExtraerNumeroID(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return null;

        // Omitir nombres que contengan palabras reservadas de los assets si no son la ID
        string limpio = texto.Replace("(Clone)", "").Trim();

        MatchCollection coincidencias = Regex.Matches(limpio, @"\d+");

        for (int i = coincidencias.Count - 1; i >= 0; i--)
        {
            if (int.TryParse(coincidencias[i].Value, out int num) && num >= 1 && num <= 75)
            {
                return num.ToString("00"); // Asegura formato "01" a "75"
            }
        }

        return null;
    }
}