using System.Collections.Generic;
using UnityEngine;

public class ReputacionManager : MonoBehaviour
{
    public static ReputacionManager Instance { get; private set;}

     private Dictionary<NPCIdentity, int> reputacionDatabase = new System.Collections.Generic.Dictionary<NPCIdentity, int>();

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void ModificarReputacion(NPCIdentity npc, int cantidad)
    {
        if (!reputacionDatabase.ContainsKey(npc))
        {
            reputacionDatabase.Add(npc, 0);
        }

        reputacionDatabase[npc] += cantidad;
        
        // Opcional: Limitar los valores entre -100 y 100
        reputacionDatabase[npc] = Mathf.Clamp(reputacionDatabase[npc], -100, 100);

        Debug.Log($"Reputacion con {npc.nombreNPC} ahora es: {reputacionDatabase[npc]}");
    }

    // Obtener la reputación actual de un NPC
    public int ObtenerReputacion(NPCIdentity npc)
    {
        if (reputacionDatabase.TryGetValue(npc, out int reputacion))
        {
            return reputacion;
        }
        return 0; // Reputacion por defecto si no lo conoce
    }

    // Evaluar el estado de la relación
    public RelacionType ObtenerTipoRelacion(NPCIdentity npc)
    {
        int rep = ObtenerReputacion(npc);
        if (rep >= 50) return RelacionType.Aliado;
        if (rep <= -50) return RelacionType.Enemigo;
        return RelacionType.Neutral;
    }
}

public enum RelacionType { Enemigo, Neutral, Aliado }

