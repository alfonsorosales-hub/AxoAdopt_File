using UnityEngine;

public class PuntoEntrevistaTrigger : MonoBehaviour
{
    [Header("Referencias OBLIGATORIAS")]
    [SerializeField] private EntrevistaUIManager entrevistaUIManager;

    [Header("Configuración de NPC")]
    [SerializeField] private string tagNPC = "NPC";

    private void OnTriggerEnter(Collider other)
    {
        bool esNPC = string.IsNullOrEmpty(tagNPC) || 
                     other.CompareTag(tagNPC) || 
                     (other.transform.root != null && other.transform.root.CompareTag(tagNPC));

        if (!esNPC) return;

        if (entrevistaUIManager == null)
        {
            Debug.LogError("[PuntoEntrevistaTrigger] Falta asignar EntrevistaUIManager en el Inspector.");
            return;
        }

        string idDetectada = ExtraerIDNumerica(other.gameObject);

        if (!string.IsNullOrEmpty(idDetectada))
        {
            Debug.Log($"<color=green>[Trigger] NPC detectado con ID: '{idDetectada}'</color>");
            entrevistaUIManager.CargarEntrevistaPorID(idDetectada);
        }
        else
        {
            Debug.LogWarning($"[Trigger] El objeto '{other.gameObject.name}' no tiene una ID válida (01 a 75).");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        bool esNPC = string.IsNullOrEmpty(tagNPC) || 
                     other.CompareTag(tagNPC) || 
                     (other.transform.root != null && other.transform.root.CompareTag(tagNPC));

        if (esNPC && entrevistaUIManager != null)
        {
            entrevistaUIManager.OcultarPaneles();
        }
    }

    private string ExtraerIDNumerica(GameObject obj)
    {
        string nombreObjeto = obj.name.Trim();
        if (int.TryParse(nombreObjeto, out int numObjeto) && numObjeto >= 1 && numObjeto <= 75)
        {
            return numObjeto < 10 ? $"0{numObjeto}" : $"{numObjeto}";
        }

        if (obj.transform.root != null)
        {
            string nombreRaiz = obj.transform.root.name.Trim();
            if (int.TryParse(nombreRaiz, out int numRaiz) && numRaiz >= 1 && numRaiz <= 75)
            {
                return numRaiz < 10 ? $"0{numRaiz}" : $"{numRaiz}";
            }
        }

        return null;
    }
}