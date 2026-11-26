using UnityEngine;

/// <summary>
/// Trigger del punto de entrevista.
/// Cuando un NPC entra, lee SU ID (componente NpcAdoptanteID) y le pide al
/// EntrevistaUIManager que cargue las preguntas/respuestas de esa ID.
/// Cuando el NPC sale, oculta los paneles (y el cuadro de respuesta).
///
/// CAMBIO IMPORTANTE: antes la ID se adivinaba comparando el nombre del objeto con el
/// "modelPrefab" del database. Como solo 5 adoptantes (23, 26, 30, 32, 40) tienen modelPrefab,
/// los demás NPC caían siempre en la misma ID y se repetían las preguntas.
/// Ahora la ID se lee directamente del NPC.
/// </summary>
public class PuntoEntrevistaTrigger : MonoBehaviour
{
    [Header("Referencias OBLIGATORIAS")]
    [SerializeField] private EntrevistaUIManager entrevistaUIManager;

    // Ya no es necesario para identificar al NPC; se deja por si lo tenías asignado (puede quedar vacío)
    [SerializeField] private AdoptanteData adoptanteDatabase;

    // NPC que está actualmente dentro del trigger y su ID
    private Transform npcActual;
    private string idActual;

    // Cantidad de colliders del NPC actual que están dentro (un NPC puede tener varios)
    private int collidersDentro = 0;

    private void Start()
    {
        var col = GetComponent<Collider>();
        Debug.Log($"[Trigger] Start en '{name}'. " +
                  $"Collider: {(col != null ? "isTrigger=" + col.isTrigger : "NO HAY COLLIDER")}. " +
                  $"Manager: {entrevistaUIManager != null}");
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
        if (entrevistaUIManager == null)
        {
            Debug.LogError("[Trigger] Falta asignar el EntrevistaUIManager en el Inspector.");
            return;
        }

        if (!BuscarIdNPC(other.transform, out Transform npcRoot, out string id))
        {
            Debug.LogWarning($"[Trigger] Ignorado '{other.name}': no tiene el componente NpcAdoptanteID (ni en sus padres ni en sus hijos activos).");
            return;
        }

        // Mismo NPC y misma ID: solo es otro collider suyo entrando
        if (npcRoot == npcActual && id == idActual)
        {
            collidersDentro++;
            return;
        }

        // NPC distinto (o el mismo objeto pero con otra ID, por ejemplo si cambió de skin):
        // se reinicia el conteo y se carga SU entrevista
        npcActual = npcRoot;
        idActual = id;
        collidersDentro = 1;

        Debug.Log($"[Trigger] NPC '{npcRoot.name}' -> ID '{id}'. Cargando entrevista...");
        entrevistaUIManager.CargarEntrevistaPorID(id);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!BuscarIdNPC(other.transform, out Transform npcRoot, out string id)) return;

        // Ignora la salida de objetos que no son el NPC actual
        if (npcRoot != npcActual) return;

        collidersDentro--;

        // Aún quedan colliders del NPC dentro: no se oculta nada
        if (collidersDentro > 0) return;

        CerrarEntrevista($"'{npcRoot.name}' salió por completo.");
    }

    /// <summary>
    /// Oculta los paneles y el cuadro de respuesta, y olvida al NPC actual.
    /// </summary>
    private void CerrarEntrevista(string motivo)
    {
        Debug.Log($"[Trigger] Cerrando entrevista. {motivo}");
        npcActual = null;
        idActual = null;
        collidersDentro = 0;

        if (entrevistaUIManager != null) entrevistaUIManager.OcultarPaneles();
        else Debug.LogError("[Trigger] No se pueden ocultar los paneles: falta el Manager.");
    }

    /// <summary>
    /// Busca el componente NpcAdoptanteID:
    /// 1) hacia arriba desde el collider (padres), y si no está,
    /// 2) hacia abajo desde la raíz, solo entre hijos ACTIVOS (sirve si el NPC cambia de skin
    ///    y cada skin lleva su propia ID).
    /// </summary>
    private bool BuscarIdNPC(Transform desde, out Transform npcRoot, out string id)
    {
        npcRoot = null;
        id = null;

        NpcAdoptanteID comp = desde.GetComponentInParent<NpcAdoptanteID>();

        if (comp == null)
            comp = desde.root.GetComponentInChildren<NpcAdoptanteID>(false); // false = ignora hijos desactivados

        if (comp == null || string.IsNullOrEmpty(comp.IdAdoptante)) return false;

        // La raíz del NPC es el objeto más alto con movimiento; se usa la raíz de la jerarquía
        npcRoot = desde.root;
        id = comp.IdAdoptante;
        return true;
    }
}