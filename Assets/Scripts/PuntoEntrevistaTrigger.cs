using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abre el panel de entrevista del NPC que entra en el área de este objeto.
/// La ID sale del componente NPCAdoptante de cada NPC (la asigna el spawner con AsignarID).
///
/// No depende de OnTriggerEnter ni de Rigidbody: revisa periódicamente la posición de todos los
/// NPCAdoptante de la escena (y sus colliders, si tienen) para saber si están dentro del área.
/// Si un NPC no tiene ID, no se abre ningún panel y se avisa en la consola.
/// </summary>
public class PuntoEntrevistaTrigger : MonoBehaviour
{
    [Header("Referencias OBLIGATORIAS")]
    [SerializeField] private EntrevistaUIManager entrevistaUIManager;

    [Header("Paneles (opcional: se los pasa al manager al iniciar)")]
    [SerializeField] private GameObject panel3Preguntas;
    [SerializeField] private GameObject panel4Preguntas;

    [Header("Detección del NPC")]
    [Tooltip("Cada cuántos segundos se revisa si hay un NPC dentro del área.")]
    [SerializeField] private float intervaloRevision = 0.1f;
    [Tooltip("Si no está vacío, solo cuentan los NPC con este tag. Déjalo vacío para aceptar cualquier NPCAdoptante.")]
    [SerializeField] private string tagNPC = "NPC";

    private static readonly float[] Alturas = { 0f, 0.5f, 1f, 1.5f };

    private Collider areaCollider;
    private readonly List<NPCAdoptante> npcs = new List<NPCAdoptante>();
    private readonly HashSet<string> avisados = new HashSet<string>();

    private float proximaRevision;
    private float proximoRefresco;
    private int cantidadAnterior = -1;
    private bool tagValido;

    private NPCAdoptante npcActual;
    private string idActual;

    // Los mensajes van a la consola y, si hay Manager, también a la pantalla del juego
    private void Registrar(string mensaje, int nivel)
    {
        if (entrevistaUIManager != null) { entrevistaUIManager.Diagnostico(mensaje, nivel); return; }

        if (nivel == 2) Debug.LogError(mensaje);
        else if (nivel == 1) Debug.LogWarning(mensaje);
        else Debug.Log(mensaje);
    }

    private void Info(string mensaje) { Registrar(mensaje, 0); }
    private void Aviso(string mensaje) { Registrar(mensaje, 1); }
    private void Error(string mensaje) { Registrar(mensaje, 2); }

    private void Start()
    {
        areaCollider = GetComponent<Collider>();
        if (areaCollider == null) areaCollider = GetComponentInChildren<Collider>();

        if (entrevistaUIManager == null) entrevistaUIManager = BuscarManager();

        Info($"[Trigger] Start en '{name}'. " +
             $"Área: {(areaCollider != null ? areaCollider.GetType().Name + (areaCollider.enabled ? "" : " (DESACTIVADO)") : "NO HAY COLLIDER")}. " +
             $"Manager: {entrevistaUIManager != null}.");

        if (areaCollider == null)
            Error("[Trigger] Este objeto necesita un Collider (BoxCollider, por ejemplo) que marque el área de la entrevista.");
        else
            Info($"[Trigger] Área en {areaCollider.bounds.center} con tamaño {areaCollider.bounds.size}.");

        if (entrevistaUIManager == null)
            Error("[Trigger] Falta el EntrevistaUIManager: asígnalo en el Inspector.");

        tagValido = TagExiste(tagNPC);
        if (!string.IsNullOrEmpty(tagNPC) && !tagValido)
            Aviso($"[Trigger] El tag '{tagNPC}' no existe en el proyecto: se ignora el filtro de tag.");

        // Si se asignaron paneles aquí, se le pasan al manager
        if (entrevistaUIManager != null && (panel3Preguntas != null || panel4Preguntas != null))
            entrevistaUIManager.AsignarPaneles(panel3Preguntas, panel4Preguntas);
    }

    private EntrevistaUIManager BuscarManager()
    {
        foreach (EntrevistaUIManager m in Resources.FindObjectsOfTypeAll<EntrevistaUIManager>())
            if (m != null && m.gameObject.scene.IsValid()) return m;
        return null;
    }

    private void Update()
    {
        if (Time.unscaledTime < proximaRevision) return;
        proximaRevision = Time.unscaledTime + Mathf.Max(0.02f, intervaloRevision);

        if (Time.unscaledTime >= proximoRefresco)
        {
            proximoRefresco = Time.unscaledTime + 0.5f;
            RefrescarNPCs();
        }

        RevisarArea();
    }

    /// <summary>Lista de todos los NPCAdoptante activos de la escena.</summary>
    private void RefrescarNPCs()
    {
        npcs.Clear();

        foreach (NPCAdoptante n in Resources.FindObjectsOfTypeAll<NPCAdoptante>())
            if (n != null && n.gameObject.scene.IsValid() && n.gameObject.activeInHierarchy)
                npcs.Add(n);

        if (npcs.Count != cantidadAnterior)
        {
            cantidadAnterior = npcs.Count;

            if (npcs.Count == 0)
                Aviso("[Trigger] No hay ningún NPC con el componente NPCAdoptante en la escena. " +
                      "El spawner debe agregarlo al instanciar el NPC y llamar a AsignarID(id).");
            else
                Info($"[Trigger] NPC con NPCAdoptante en la escena: {npcs.Count}.");
        }
    }

    private void RevisarArea()
    {
        if (areaCollider == null || entrevistaUIManager == null) return;

        // ¿El NPC de la entrevista abierta sigue dentro?
        if (idActual != null)
        {
            bool sigue = npcActual != null && npcActual.isActiveAndEnabled && NpcEnArea(npcActual);
            if (sigue) return;

            CerrarEntrevista(npcActual == null ? "El NPC desapareció." : $"'{npcActual.name}' salió del área.");
        }

        // Buscar un NPC nuevo dentro del área (el más cercano al centro)
        NPCAdoptante elegido = null;
        float mejor = float.MaxValue;
        Vector3 centro = areaCollider.bounds.center;

        foreach (NPCAdoptante npc in npcs)
        {
            if (npc == null || !npc.isActiveAndEnabled) continue;

            if (!TagCorrecto(npc))
            {
                AvisarUnaVez(npc, "tag", $"[Trigger] '{npc.name}' tiene NPCAdoptante pero su tag es '{npc.tag}' y se esperaba '{tagNPC}': se ignora. " +
                                         "Cámbiale el tag o vacía el campo 'Tag NPC' del trigger.");
                continue;
            }

            if (!NpcEnArea(npc)) continue;

            float d = (npc.transform.position - centro).sqrMagnitude;
            if (d < mejor)
            {
                mejor = d;
                elegido = npc;
            }
        }

        if (elegido == null) return;

        string id = elegido.IdAdoptante;

        if (string.IsNullOrEmpty(id))
        {
            AvisarUnaVez(elegido, "sinid", $"[Trigger] El NPC '{elegido.name}' entró al área pero no tiene ID asignada. " +
                                           "El spawner debe llamar a NPCAdoptante.AsignarID(id) al crearlo.");
            return;
        }

        npcActual = elegido;
        idActual = id;

        Info($"[Trigger] NPC '{elegido.name}' entró con ID '{id}'. Cargando entrevista...");
        entrevistaUIManager.CargarEntrevistaPorID(id);
    }

    private void CerrarEntrevista(string motivo)
    {
        Info($"[Trigger] Cerrando entrevista. {motivo}");
        npcActual = null;
        idActual = null;

        if (entrevistaUIManager != null) entrevistaUIManager.OcultarPaneles();
        else Error("[Trigger] No se pueden ocultar los paneles: falta el Manager.");
    }

    private void AvisarUnaVez(NPCAdoptante npc, string clave, string mensaje)
    {
        if (avisados.Add($"{npc.GetInstanceID()}:{clave}")) Aviso(mensaje);
    }

    // =====================================================================
    // ¿ESTÁ EL NPC DENTRO DEL ÁREA?
    // =====================================================================
    private bool TagCorrecto(NPCAdoptante npc)
    {
        return string.IsNullOrEmpty(tagNPC) || !tagValido || npc.CompareTag(tagNPC);
    }

    /// <summary>Dentro si su posición (a varias alturas) está en el área, o si alguno de sus colliders la toca.</summary>
    private bool NpcEnArea(NPCAdoptante npc)
    {
        Vector3 pos = npc.transform.position;

        foreach (float altura in Alturas)
            if (PuntoEnArea(pos + Vector3.up * altura)) return true;

        Bounds area = areaCollider.bounds;
        if (area.SqrDistance(pos) > 25f) return false;   // a más de 5 m no hace falta mirar sus colliders

        foreach (Collider c in npc.GetComponentsInChildren<Collider>())
            if (c != null && c.enabled && c != areaCollider && c.bounds.Intersects(area)) return true;

        return false;
    }

    private bool PuntoEnArea(Vector3 punto)
    {
        MeshCollider malla = areaCollider as MeshCollider;
        if (malla != null && !malla.convex) return areaCollider.bounds.Contains(punto);

        Vector3 masCercano = areaCollider.ClosestPoint(punto);
        return (masCercano - punto).sqrMagnitude < 0.0025f;
    }

    /// <summary>Comprueba una sola vez que el tag exista en el proyecto (CompareTag falla si no existe).</summary>
    private static bool TagExiste(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;

        try { GameObject.FindWithTag(tag); return true; }
        catch (UnityException) { return false; }
    }
}