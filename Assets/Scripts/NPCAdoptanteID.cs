using UnityEngine;

/// <summary>
/// Se pone en la RAÍZ de cada NPC de la escena (el objeto que se mueve/entra al trigger).
/// Guarda la ID del adoptante ("01" ... "75") directamente en el NPC, así el trigger
/// ya no depende de que el nombre del objeto coincida con el modelPrefab del database.
/// </summary>
public class NpcAdoptanteID : MonoBehaviour
{
    [Tooltip("ID del adoptante tal como está en AdoptanteData y PreguntasDatabase: 01, 02, ... 75")]
    [SerializeField] private string idAdoptante = "";

    public string IdAdoptante => idAdoptante != null ? idAdoptante.Trim() : "";

    // Se ejecuta en el editor al cambiar valores: avisa si se copió el prefab y se olvidó cambiar la ID
    private void OnValidate()
    {
        if (!int.TryParse(IdAdoptante, out int n) || n < 1 || n > 75)
        {
            Debug.LogWarning($"[NpcAdoptanteId] '{name}' tiene una ID inválida ('{idAdoptante}'). Debe ser un número entre 01 y 75.", this);
            return;
        }

        // Normaliza "1" -> "01" para que coincida con el formato de las bases de datos
        string normalizada = n.ToString("00");
        if (idAdoptante != normalizada) idAdoptante = normalizada;
    }
}