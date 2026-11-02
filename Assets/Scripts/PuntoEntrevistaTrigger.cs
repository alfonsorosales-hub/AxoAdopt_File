using UnityEngine;

public class PuntoEntrevistaTrigger : MonoBehaviour
{
    [Header("Referencias OBLIGATORIAS")]
    [SerializeField] private EntrevistaUIManager entrevistaUIManager;

    [Header("Configuración de NPC")]
    [SerializeField] private string tagNPC = "NPC";

    private void OnTriggerEnter(Collider other)
    {
        // Buscar el componente en el objeto o en sus padres
        NPCAdoptante npc = other.GetComponentInParent<NPCAdoptante>();
        if (npc == null) return;

        if (!string.IsNullOrEmpty(tagNPC) && !npc.CompareTag(tagNPC)) return;

        if (entrevistaUIManager == null)
        {
            Debug.LogError("[PuntoEntrevistaTrigger] Falta asignar EntrevistaUIManager.");
            return;
        }

        string id = npc.IdAdoptante;

        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError($"[Trigger] El NPC '{npc.name}' no tiene ID asignada.");
            return;
        }

        Debug.Log($"[Trigger] NPC '{npc.name}' entró con ID: {id}");
        entrevistaUIManager.CargarEntrevistaPorID(id);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<NPCAdoptante>() == null) return;

        if (entrevistaUIManager != null)
            entrevistaUIManager.OcultarPaneles();
    }
}