using UnityEngine;

public class PuntoEntrevistaTrigger : MonoBehaviour
{
    [Header("Referencias OBLIGATORIAS")]
    [SerializeField] private EntrevistaUIManager entrevistaUIManager;
    [SerializeField] private AdoptanteData adoptanteDatabase; // Arrastra aquí tu ScriptableObject AdoptanteData

    [Header("Configuración de NPC")]
    [SerializeField] private string tagNPC = "NPC";

    private void OnTriggerEnter(Collider other)
    {
        // 1. Validar que lo que entra es el NPC
        if (!string.IsNullOrEmpty(tagNPC) && !other.CompareTag(tagNPC) && (other.transform.root == null || !other.transform.root.CompareTag(tagNPC)))
        {
            return;
        }

        // 2. Validar referencias
        if (entrevistaUIManager == null || adoptanteDatabase == null)
        {
            Debug.LogError("[PuntoEntrevistaTrigger] Faltan asignaciones en el Inspector.");
            return;
        }

        // 3. Obtener la ID directamente de la base de datos AdoptanteData
        string idParaCargar = ObtenerIDDesdeDatabase();

        if (!string.IsNullOrEmpty(idParaCargar))
        {
            Debug.Log($"[Trigger] Activando panel para la ID de AdoptanteData: {idParaCargar}");
            entrevistaUIManager.CargarEntrevistaPorID(idParaCargar);
        }
        else
        {
            Debug.LogError("[Trigger] No se pudo obtener una ID válida desde AdoptanteData.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(tagNPC) || (other.transform.root != null && other.transform.root.CompareTag(tagNPC)))
        {
            if (entrevistaUIManager != null)
            {
                entrevistaUIManager.OcultarPaneles();
            }
        }
    }

    /// Lee la ID del adoptante directamente desde AdoptanteData
    private string ObtenerIDDesdeDatabase()
    {
        if (adoptanteDatabase == null || adoptanteDatabase.adoptantesList == null || adoptanteDatabase.adoptantesList.Count == 0)
        {
            return null;
        }

        // Si tu AdoptanteData guarda el índice del NPC actual que camina (ej. un contador o índice actual)
        // o si los busca en la lista, aquí lee el primer valor disponible o el elemento actual:
        var adoptanteActual = adoptanteDatabase.adoptantesList[0]; // Lee el objeto actual de la lista

        if (adoptanteActual != null && !string.IsNullOrEmpty(adoptanteActual.id))
        {
            return adoptanteActual.id;
        }

        return null;
    }
}