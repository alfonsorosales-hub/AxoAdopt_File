using UnityEngine;

public class PuntoEntrevistaTrigger : MonoBehaviour
{
    [Header("Referencias OBLIGATORIAS")]
    [SerializeField] private EntrevistaUIManager entrevistaUIManager;
    [SerializeField] private AdoptanteData adoptanteDatabase;

    private Transform npcActual;
    private int collidersDentro = 0;

    private void Start()
    {
        var col = GetComponent<Collider>();
        Debug.Log($"[Trigger] Start en '{name}'. " +
                  $"Collider: {(col != null ? "isTrigger=" + col.isTrigger : "NO HAY COLLIDER")}. " +
                  $"Manager: {entrevistaUIManager != null}. Database: {adoptanteDatabase != null}");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Log ANTES de cualquier validación
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

        collidersDentro++;

        if (npcActual == npcRoot)
        {
            Debug.Log($"[Trigger] '{npcRoot.name}' ya estaba registrado, otro collider suyo entró (total: {collidersDentro}).");
            return;
        }

        npcActual = npcRoot;
        Debug.Log($"[Trigger] NPC '{npcRoot.name}' -> ID '{id}'. Llamando CargarEntrevistaPorID...");
        entrevistaUIManager.CargarEntrevistaPorID(id);
        Debug.Log("[Trigger] CargarEntrevistaPorID terminó sin errores.");
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[Trigger] OnTriggerExit con '{other.name}'");

        if (!EncontrarNPC(other.transform, out Transform npcRoot, out string id))
        {
            Debug.Log($"[Trigger] Exit ignorado: '{other.name}' no es un NPC del database.");
            return;
        }

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

        Debug.Log($"[Trigger] '{npcRoot.name}' salió por completo. Ocultando paneles.");
        npcActual = null;
        collidersDentro = 0;

        if (entrevistaUIManager == null)
        {
            Debug.LogError("[Trigger] No se pueden ocultar los paneles: falta el Manager.");
            return;
        }

        entrevistaUIManager.OcultarPaneles();
    }

    /// Sube por la jerarquía desde el collider hasta encontrar un objeto
    /// cuyo nombre coincida con el modelPrefab de algún adoptante.
    private bool EncontrarNPC(Transform desde, out Transform npcRoot, out string id)
    {
        npcRoot = null;
        id = null;

        for (Transform t = desde; t != null; t = t.parent)
        {
            string nombre = t.name.Replace("(Clone)", "").Trim();

            foreach (var adoptante in adoptanteDatabase.adoptantesList)
            {
                if (adoptante == null || adoptante.modelPrefab == null) continue;

                if (adoptante.modelPrefab.name == nombre)
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


