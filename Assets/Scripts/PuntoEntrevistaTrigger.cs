using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class PuntoEntrevistaTrigger : MonoBehaviour
{
    [Header("Referencias OBLIGATORIAS")]
    [SerializeField] private EntrevistaUIManager entrevistaUIManager;
    [SerializeField] private AdoptanteData adoptanteDatabase;

    // Quita "(Clone)" y sufijos del editor como " (1)" para comparar nombres de modelo
    private static readonly Regex SufijosNombre = new Regex(@"\s*\(Clone\)|\s*\(\d+\)", RegexOptions.IgnoreCase);

    // nombre normalizado del modelPrefab -> ID del adoptante
    private Dictionary<string, string> idPorModelo;

    private Transform npcActual;
    private int collidersDentro = 0;

    private static string NormalizarNombre(string nombre)
    {
        return SufijosNombre.Replace(nombre ?? "", "").Trim();
    }

    // =====================================================================
    // INICIO
    // =====================================================================
    private void Start()
    {
        var col = GetComponent<Collider>();
        Debug.Log($"[Trigger] Start en '{name}'. " +
                  $"Collider: {(col != null ? "isTrigger=" + col.isTrigger : "NO HAY COLLIDER")}. " +
                  $"Manager: {entrevistaUIManager != null}. Database: {adoptanteDatabase != null}");

        ConstruirIndice();
    }

    /// <summary>
    /// Arma la tabla modelo -> ID y avisa si un mismo modelo está asignado a varias IDs
    /// (en ese caso el trigger no puede saber cuál de ellas es el NPC).
    /// </summary>
    private void ConstruirIndice()
    {
        idPorModelo = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        if (adoptanteDatabase == null) return;

        var idsPorModelo = new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);

        foreach (var adoptante in adoptanteDatabase.adoptantesList)
        {
            if (adoptante == null || adoptante.modelPrefab == null) continue;

            string modelo = NormalizarNombre(adoptante.modelPrefab.name);
            if (!idsPorModelo.TryGetValue(modelo, out List<string> ids))
            {
                ids = new List<string>();
                idsPorModelo[modelo] = ids;
            }
            if (!ids.Contains(adoptante.id)) ids.Add(adoptante.id);
        }

        foreach (var par in idsPorModelo)
        {
            idPorModelo[par.Key] = par.Value[0];

            if (par.Value.Count > 1)
            {
                Debug.LogError($"[Trigger] El modelo '{par.Key}' está asignado a varias IDs ({string.Join(", ", par.Value)}). " +
                               $"El trigger siempre usará '{par.Value[0]}', por eso verás preguntas que no corresponden. " +
                               $"Cada ID necesita su propio modelPrefab en AdoptanteData.");
            }
        }

        Debug.Log($"[Trigger] Índice de modelos listo: {idPorModelo.Count} modelos distintos.");
    }

    private void Update()
    {
        // Si el NPC se destruye o se desactiva dentro del trigger, Unity no siempre llama a OnTriggerExit
        if (collidersDentro > 0 && (npcActual == null || !npcActual.gameObject.activeInHierarchy))
        {
            CerrarEntrevista("El NPC desapareció o se desactivó dentro del trigger.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Trigger] OnTriggerEnter con '{other.name}' (root '{other.transform.root.name}')");

        if (entrevistaUIManager == null || adoptanteDatabase == null)
        {
            Debug.LogError("[Trigger] Faltan asignaciones en el Inspector (Manager o Database).");
            return;
        }

        if (!EncontrarNPC(other.transform, out Transform npcRoot, out string id))
        {
            Debug.LogWarning($"[Trigger] Ignorado '{other.name}': ningún objeto de su jerarquía coincide con un modelPrefab del database.");
            return;
        }

        if (npcRoot == npcActual)
        {
            collidersDentro++;
            Debug.Log($"[Trigger] Otro collider de '{npcRoot.name}' entró (total: {collidersDentro}).");
            return;
        }

        // NPC distinto al que estaba (o ninguno): se reinicia el conteo y se carga su entrevista
        npcActual = npcRoot;
        collidersDentro = 1;
        Debug.Log($"[Trigger] NPC '{npcRoot.name}' -> ID '{id}'. Cargando entrevista...");
        entrevistaUIManager.CargarEntrevistaPorID(id);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!EncontrarNPC(other.transform, out Transform npcRoot, out string id)) return;

        if (npcRoot != npcActual)
        {
            Debug.Log($"[Trigger] Exit ignorado: '{npcRoot.name}' no es el NPC actual ('{(npcActual != null ? npcActual.name : "ninguno")}').");
            return;
        }

        collidersDentro--;

        if (collidersDentro > 0)
        {
            Debug.Log($"[Trigger] Salió un collider de '{npcRoot.name}', pero quedan {collidersDentro} dentro. No se oculta.");
            return;
        }

        CerrarEntrevista($"'{npcRoot.name}' salió por completo.");
    }

    private void CerrarEntrevista(string motivo)
    {
        Info($"[Trigger] Cerrando entrevista. {motivo}");
        npcActual = null;
        idActual = null;

        if (entrevistaUIManager != null) entrevistaUIManager.OcultarPaneles();
        else Error("[Trigger] No se pueden ocultar los paneles: falta el Manager.");
    }

    /// <summary>
    /// Sube por la jerarquía desde el collider hasta encontrar un objeto
    /// cuyo nombre coincida con el modelPrefab de algún adoptante.
    /// </summary>
    private bool EncontrarNPC(Transform desde, out Transform npcRoot, out string id)
    {
        npcRoot = null;
        id = null;

        if (adoptanteDatabase == null) return false;

        // Se consulta la lista en vivo (sin copia en caché): si el database se llena o cambia
        // en tiempo de ejecución, el trigger siempre ve los datos actuales.
        for (Transform t = desde; t != null; t = t.parent)
        {
            string nombre = NormalizarNombre(t.name);

            foreach (var adoptante in adoptanteDatabase.adoptantesList)
            {
                if (adoptante == null || adoptante.modelPrefab == null) continue;

                if (string.Equals(NormalizarNombre(adoptante.modelPrefab.name), nombre, System.StringComparison.OrdinalIgnoreCase))
                {
                    npcRoot = t;
                    id = adoptante.id;
                    return true;
                }
            }
        }

        return false;
    }
}