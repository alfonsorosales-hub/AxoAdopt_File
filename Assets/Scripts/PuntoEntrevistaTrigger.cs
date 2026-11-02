using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Abre el panel de entrevista que corresponde al NPC que entra en el área de este objeto.
/// NO depende de OnTriggerEnter ni de que el NPC tenga collider:
///   - Revisa qué colliders hay dentro del área, y
///   - revisa la POSICIÓN de todos los personajes de la escena (objetos con Animator,
///     CharacterController o el tag NPC) para saber si están dentro del área.
///
/// La ID del NPC se obtiene, en este orden:
///   1) El nombre de algún objeto del NPC coincide con un modelPrefab de AdoptanteData.
///   2) El modelo del database está dentro del NPC.
///   3) Un componente del NPC tiene un campo "id" (1-75).
///   4) Un objeto del NPC tiene un número de 1 a 75 en su nombre (por ejemplo "Adoptante_12").
///   Si no se puede saber la ID, NO se abre ningún panel (solo se avisa en la consola).
/// </summary>
public class PuntoEntrevistaTrigger : MonoBehaviour
{
    [Header("Referencias OBLIGATORIAS")]
    [SerializeField] private EntrevistaUIManager entrevistaUIManager;
    [SerializeField] private AdoptanteData adoptanteDatabase;

    [Header("Paneles (opcional: se los pasa al manager al iniciar)")]
    [SerializeField] private GameObject panel3Preguntas;
    [SerializeField] private GameObject panel4Preguntas;

    [Header("Detección del NPC")]
    [Tooltip("Cada cuántos segundos se revisa qué hay dentro del área. No necesita Rigidbody ni 'Is Trigger'.")]
    [SerializeField] private float intervaloRevision = 0.1f;
    [Tooltip("Capas que se revisan para los colliders (por defecto, todas).")]
    [SerializeField] private LayerMask capas = ~0;
    [Tooltip("Tag de los NPC. Si tu NPC no tiene Animator ni CharacterController, ponle este tag.")]
    [SerializeField] private string tagNPC = "NPC";

    // Quita "(Clone)" y sufijos del editor como " (1)" para comparar nombres de modelo
    private static readonly Regex SufijosNombre = new Regex(@"\s*\(Clone\)|\s*\(\d+\)", RegexOptions.IgnoreCase);

    private const int MaxNodosSubarbol = 600;
    private static readonly float[] AlturasComprobadas = { 0f, 0.5f, 1f, 1.5f };
    private static readonly string[] NombresCampoID = { "id", "idadoptante", "adoptanteid", "idnpc", "npcid", "adopterid", "idadopter" };

    private class Resultado
    {
        public bool ok;
        public Transform raiz;
        public string id;
        public string metodo;
        public float expira;
    }

    private Collider areaCollider;
    private readonly Collider[] buffer = new Collider[64];
    private readonly Dictionary<Collider, Resultado> cache = new Dictionary<Collider, Resultado>();
    private readonly List<Transform> candidatos = new List<Transform>();

    private float proximaRevision;
    private float proximoRefrescoCandidatos;
    private float proximoLogCercania;
    private bool avisoSinCandidatos;

    private Dictionary<string, string> indice;   // nombre de modelo normalizado -> ID
    private float indiceExpira;

    private bool tagNpcValido;
    private bool tagPlayerValido;

    private Transform npcActual;
    private string idActual;
    private string firmaAnterior;

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

    private static string NormalizarNombre(string nombre)
    {
        return SufijosNombre.Replace(nombre ?? "", "").Trim();
    }

    // =====================================================================
    // INICIO
    // =====================================================================
    private void Start()
    {
        areaCollider = GetComponent<Collider>();
        if (areaCollider == null) areaCollider = GetComponentInChildren<Collider>();

        if (entrevistaUIManager == null) entrevistaUIManager = BuscarManager();

        Info($"[Trigger] Start en '{name}'. " +
             $"Área: {(areaCollider != null ? areaCollider.GetType().Name + (areaCollider.enabled ? "" : " (DESACTIVADO)") : "NO HAY COLLIDER")}. " +
             $"Manager: {entrevistaUIManager != null}. Database: {adoptanteDatabase != null}.");

        if (areaCollider == null)
            Error("[Trigger] Este objeto necesita un Collider (BoxCollider, por ejemplo) que marque el área de la entrevista.");
        else
            Info($"[Trigger] Área en {areaCollider.bounds.center} con tamaño {areaCollider.bounds.size}.");

        if (entrevistaUIManager == null)
            Error("[Trigger] Falta el EntrevistaUIManager: asígnalo en el Inspector.");
        if (adoptanteDatabase == null)
            Error("[Trigger] Falta el AdoptanteData: asígnalo en el Inspector.");

        tagNpcValido = TagExiste(tagNPC);
        tagPlayerValido = TagExiste("Player");

        ConstruirIndice();

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

    /// <summary>
    /// Avisa si un mismo modelo está asignado a varias IDs y cuántos adoptantes tienen modelPrefab.
    /// </summary>
    private void ConstruirIndice()
    {
        if (adoptanteDatabase == null) return;

        var idsPorModelo = new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);
        int total = 0;
        var conModelo = new List<string>();

        foreach (var adoptante in adoptanteDatabase.adoptantesList)
        {
            if (adoptante == null) continue;
            total++;
            if (adoptante.modelPrefab == null) continue;
            conModelo.Add(adoptante.id);

            string modelo = NormalizarNombre(adoptante.modelPrefab.name);
            if (!idsPorModelo.TryGetValue(modelo, out List<string> ids))
            {
                ids = new List<string>();
                idsPorModelo[modelo] = ids;
            }
            if (!ids.Contains(adoptante.id)) ids.Add(adoptante.id);
        }

        var ejemplos = new StringBuilder();
        int mostrados = 0;

        foreach (var par in idsPorModelo)
        {
            if (mostrados < 5)
            {
                ejemplos.Append($"'{par.Key}' (ID {par.Value[0]}), ");
                mostrados++;
            }

            if (par.Value.Count > 1)
            {
                Error($"[Trigger] El modelo '{par.Key}' está asignado a varias IDs ({string.Join(", ", par.Value)}). " +
                      $"El trigger siempre usará '{par.Value[0]}', por eso verás preguntas que no corresponden. " +
                      "Cada ID necesita su propio modelPrefab en AdoptanteData.");
            }
        }

        Info($"[Trigger] Database: {idsPorModelo.Count} modelos distintos. Ejemplos: {ejemplos}");

        if (conModelo.Count < total)
            Aviso($"[Trigger] Solo {conModelo.Count} de {total} adoptantes tienen modelPrefab asignado en AdoptanteData (IDs: {string.Join(", ", conModelo)}). " +
                  "Un NPC con otra ID se reconoce por un campo 'id' en alguno de sus componentes " +
                  "o por el número de 1 a 75 en su nombre (por ejemplo 'Adoptante_12').");
    }

    /// <summary>Tabla nombre de modelo -> ID, renovada cada segundo (por si el database cambia en ejecución).</summary>
    private Dictionary<string, string> ObtenerIndice()
    {
        if (indice != null && Time.unscaledTime < indiceExpira) return indice;

        indice = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        indiceExpira = Time.unscaledTime + 1f;

        if (adoptanteDatabase == null) return indice;

        foreach (var adoptante in adoptanteDatabase.adoptantesList)
        {
            if (adoptante == null || adoptante.modelPrefab == null) continue;

            string modelo = NormalizarNombre(adoptante.modelPrefab.name);
            if (!indice.ContainsKey(modelo)) indice[modelo] = adoptante.id;
        }

        return indice;
    }

    // =====================================================================
    // REVISIÓN PERIÓDICA
    // =====================================================================
    private void Update()
    {
        if (Time.unscaledTime < proximaRevision) return;
        proximaRevision = Time.unscaledTime + Mathf.Max(0.02f, intervaloRevision);

        if (Time.unscaledTime >= proximoRefrescoCandidatos)
        {
            proximoRefrescoCandidatos = Time.unscaledTime + 1f;
            RefrescarCandidatos();
        }

        RevisarArea();
    }

    private void RevisarArea()
    {
        if (areaCollider == null || entrevistaUIManager == null || adoptanteDatabase == null) return;

        var npcsDentro = new List<Transform>();
        var idsDentro = new List<string>();
        var metodos = new List<string>();
        var ajenos = new List<string>();

        // --- A) Colliders que se superponen con el área ---
        int cantidad = ColliderEnArea();

        for (int i = 0; i < cantidad; i++)
        {
            Collider c = buffer[i];
            if (c == null || c == areaCollider) continue;

            if (ResolverNPC(c, out Transform raiz, out string id, out string metodo))
            {
                if (!Representado(raiz, npcsDentro))
                {
                    npcsDentro.Add(raiz);
                    idsDentro.Add(id);
                    metodos.Add(metodo);
                }
            }
            else
            {
                string descripcion = $"'{RutaDe(c.transform)}'";
                if (!ajenos.Contains(descripcion)) ajenos.Add(descripcion);
            }
        }

        // --- B) Posición de los personajes de la escena (no necesita colliders) ---
        foreach (Transform personaje in candidatos)
        {
            if (personaje == null || !personaje.gameObject.activeInHierarchy) continue;
            if (!PersonajeEnArea(personaje)) continue;
            if (Representado(personaje, npcsDentro)) continue;

            Transform raizPersonaje = RaizDelPersonaje(personaje);
            if (Representado(raizPersonaje, npcsDentro)) continue;

            if (ResolverIDDePersonaje(personaje, raizPersonaje, out string idP, out string metodoP))
            {
                npcsDentro.Add(raizPersonaje);
                idsDentro.Add(idP);
                metodos.Add(metodoP + ", por posición");
            }
            else
            {
                // Está dentro del área pero no se sabe quién es: no se abre ningún panel, solo se avisa
                string aviso = $"'{RutaDe(raizPersonaje)}' (personaje SIN ID: ponle un número de 1 a 75 en el nombre, " +
                               "por ejemplo 'Adoptante_12', o asigna su modelPrefab en AdoptanteData)";
                if (!ajenos.Contains(aviso)) ajenos.Add(aviso);
            }
        }

        InformarCambios(npcsDentro, idsDentro, metodos, ajenos);

        if (npcsDentro.Count == 0) DiagnosticoCercania();

        // ¿El NPC actual sigue dentro del área?
        if (idActual != null)
        {
            bool sigue = npcActual != null && Representado(npcActual, npcsDentro);
            if (!sigue)
                CerrarEntrevista(npcActual == null ? "El NPC desapareció." : $"'{npcActual.name}' salió del área.");
        }

        // ¿Entró un NPC nuevo?
        if (idActual == null && npcsDentro.Count > 0)
        {
            npcActual = npcsDentro[0];
            idActual = idsDentro[0];

            Info($"[Trigger] NPC '{npcActual.name}' -> ID '{idActual}' (por {metodos[0]}). Cargando entrevista...");
            entrevistaUIManager.CargarEntrevistaPorID(idActual);
        }
    }

    /// <summary>¿Ese transform ya está en la lista, o es padre/hijo de uno de ellos (mismo NPC)?</summary>
    private static bool Representado(Transform t, List<Transform> lista)
    {
        if (t == null) return false;

        foreach (Transform r in lista)
        {
            if (r == null) continue;
            if (t == r || t.IsChildOf(r) || r.IsChildOf(t)) return true;
        }
        return false;
    }

    /// <summary>Escribe un mensaje solo cuando cambia lo que hay dentro del área (para no llenar la consola).</summary>
    private void InformarCambios(List<Transform> npcs, List<string> ids, List<string> metodos, List<string> ajenos)
    {
        var sb = new StringBuilder();

        sb.Append("NPC reconocidos: ");
        if (npcs.Count == 0) sb.Append("ninguno");
        for (int i = 0; i < npcs.Count; i++) sb.Append($"'{npcs[i].name}' (ID {ids[i]}, por {metodos[i]}) ");

        sb.Append("| Otros objetos (ruta): ");
        if (ajenos.Count == 0) sb.Append("ninguno");
        for (int i = 0; i < ajenos.Count && i < 6; i++) sb.Append(ajenos[i]).Append(' ');

        string firma = sb.ToString();
        if (firma == firmaAnterior) return;

        firmaAnterior = firma;
        Info($"[Trigger] Dentro del área -> {firma}");
    }

    /// <summary>Mientras no hay nadie dentro, dice qué personaje está más cerca y a qué distancia.</summary>
    private void DiagnosticoCercania()
    {
        if (candidatos.Count == 0)
        {
            if (!avisoSinCandidatos)
            {
                avisoSinCandidatos = true;
                Aviso("[Trigger] No encontré ningún personaje en la escena (nada con Animator, CharacterController o el tag '" + tagNPC +
                      "'). Si tu NPC no tiene ninguno, ponle el tag NPC.");
            }
            return;
        }

        avisoSinCandidatos = false;

        if (Time.unscaledTime < proximoLogCercania) return;
        proximoLogCercania = Time.unscaledTime + 2f;

        Bounds area = areaCollider.bounds;
        Transform masCercano = null;
        float mejor = float.MaxValue;

        foreach (Transform c in candidatos)
        {
            if (c == null) continue;
            float d = area.SqrDistance(c.position);
            if (d < mejor) { mejor = d; masCercano = c; }
        }

        if (masCercano != null && mejor < 400f)   // a menos de 20 m
            Info($"[Trigger] Personaje más cercano: '{RutaDe(masCercano)}' a {Mathf.Sqrt(mejor):0.0} m del área " +
                 $"(personajes en la escena: {candidatos.Count}).");
    }

    private void CerrarEntrevista(string motivo)
    {
        Info($"[Trigger] Cerrando entrevista. {motivo}");
        npcActual = null;
        idActual = null;

        if (entrevistaUIManager != null) entrevistaUIManager.OcultarPaneles();
        else Error("[Trigger] No se pueden ocultar los paneles: falta el Manager.");
    }

    private static string RutaDe(Transform t)
    {
        var partes = new List<string>();
        for (; t != null; t = t.parent) partes.Insert(0, t.name);
        return string.Join("/", partes);
    }

    // =====================================================================
    // DETECCIÓN POR COLLIDERS
    // =====================================================================

    /// <summary>Colliders que se superponen con el área de este objeto (exacto para BoxCollider).</summary>
    private int ColliderEnArea()
    {
        BoxCollider caja = areaCollider as BoxCollider;
        if (caja != null)
        {
            Transform t = caja.transform;
            Vector3 escala = t.lossyScale;
            Vector3 mitad = new Vector3(
                Mathf.Abs(escala.x * caja.size.x),
                Mathf.Abs(escala.y * caja.size.y),
                Mathf.Abs(escala.z * caja.size.z)) * 0.5f;

            return Physics.OverlapBoxNonAlloc(t.TransformPoint(caja.center), mitad, buffer, t.rotation,
                                              capas, QueryTriggerInteraction.Collide);
        }

        Bounds b = areaCollider.bounds;
        return Physics.OverlapBoxNonAlloc(b.center, b.extents, buffer, Quaternion.identity,
                                          capas, QueryTriggerInteraction.Collide);
    }

    /// <summary>Igual que EncontrarNPC, pero guarda el resultado unos segundos por collider.</summary>
    private bool ResolverNPC(Collider c, out Transform raiz, out string id, out string metodo)
    {
        if (cache.TryGetValue(c, out Resultado r) && Time.unscaledTime < r.expira && (!r.ok || r.raiz != null))
        {
            raiz = r.raiz;
            id = r.id;
            metodo = r.metodo;
            return r.ok;
        }

        bool ok = EncontrarNPC(c, out raiz, out id, out metodo);

        if (cache.Count > 300) cache.Clear();
        cache[c] = new Resultado
        {
            ok = ok,
            raiz = raiz,
            id = id,
            metodo = metodo,
            expira = Time.unscaledTime + (ok ? 5f : 2f)
        };

        return ok;
    }

    private bool EncontrarNPC(Collider colision, out Transform npcRoot, out string id, out string metodo)
    {
        Transform desde = colision.transform;

        npcRoot = null;
        id = null;
        metodo = null;

        if (adoptanteDatabase == null) return false;

        Dictionary<string, string> tabla = ObtenerIndice();

        // 1) El collider o algún padre se llama como un modelPrefab
        for (Transform t = desde; t != null; t = t.parent)
        {
            if (tabla.TryGetValue(NormalizarNombre(t.name), out string idNombre))
            {
                npcRoot = t;
                id = idNombre;
                metodo = "nombre del modelo";
                return true;
            }
        }

        // 2) El modelo es un hijo: el ancestro más cercano cuyo contenido tenga exactamente un modelo
        for (Transform t = desde; t != null; t = t.parent)
        {
            int resultado = BuscarModeloEnSubarbol(t, tabla, out string idSub);

            if (resultado == 1)
            {
                npcRoot = t;
                id = idSub;
                metodo = "modelo dentro del objeto";
                return true;
            }

            if (resultado != 0) break;   // varios modelos (contenedor) o demasiado grande: no se puede decidir
        }

        // 3) Algo con aspecto de NPC: la ID sale de un componente o del nombre
        if (PareceNPC(colision, out Transform personaje))
        {
            for (Transform t = desde; t != null; t = t.parent)
            {
                string idComponente = IDDesdeComponentes(t);
                if (idComponente != null)
                {
                    npcRoot = personaje;
                    id = idComponente;
                    metodo = $"campo id de un componente de '{t.name}'";
                    return true;
                }
                if (t == personaje) break;
            }

            for (Transform t = desde; t != null; t = t.parent)
            {
                string idNumero = ExtraerID(t.name);
                if (idNumero != null)
                {
                    npcRoot = personaje;
                    id = idNumero;
                    metodo = $"número en el nombre '{t.name}'";
                    return true;
                }
                if (t == personaje) break;
            }
        }

        id = null;
        return false;
    }

    /// <summary>
    /// Cuenta cuántas IDs distintas de modelo hay bajo 'raiz'.
    /// Devuelve 0 = ninguna, 1 = una (idUnico), 2 = varias, -1 = subárbol demasiado grande.
    /// </summary>
    private int BuscarModeloEnSubarbol(Transform raiz, Dictionary<string, string> tabla, out string idUnico)
    {
        idUnico = null;

        var pila = new Stack<Transform>();
        pila.Push(raiz);
        int visitados = 0;

        while (pila.Count > 0)
        {
            Transform t = pila.Pop();

            visitados++;
            if (visitados > MaxNodosSubarbol)
            {
                idUnico = null;
                return -1;
            }

            if (tabla.TryGetValue(NormalizarNombre(t.name), out string idHallada))
            {
                if (idUnico == null) idUnico = idHallada;
                else if (idUnico != idHallada) return 2;
            }

            for (int i = 0; i < t.childCount; i++) pila.Push(t.GetChild(i));
        }

        return idUnico != null ? 1 : 0;
    }

    // =====================================================================
    // DETECCIÓN POR POSICIÓN (personajes sin collider)
    // =====================================================================

    /// <summary>Lista de personajes de la escena: objetos con Animator, CharacterController o el tag NPC.</summary>
    private void RefrescarCandidatos()
    {
        candidatos.Clear();
        var vistos = new HashSet<Transform>();

        foreach (Animator a in Resources.FindObjectsOfTypeAll<Animator>())
            if (a != null) AgregarCandidato(a.transform, vistos);

        foreach (CharacterController cc in Resources.FindObjectsOfTypeAll<CharacterController>())
            if (cc != null) AgregarCandidato(cc.transform, vistos);

        if (tagNpcValido && !string.IsNullOrEmpty(tagNPC))
        {
            foreach (GameObject go in GameObject.FindGameObjectsWithTag(tagNPC))
                if (go != null) AgregarCandidato(go.transform, vistos);
        }
    }

    private void AgregarCandidato(Transform t, HashSet<Transform> vistos)
    {
        if (t == null || vistos.Contains(t)) return;
        if (!t.gameObject.scene.IsValid() || !t.gameObject.activeInHierarchy) return;   // prefabs/assets o apagados
        if (t is RectTransform) return;                                                  // elementos de UI con Animator
        if (t == transform || t.IsChildOf(transform)) return;                            // objetos de este mismo trigger
        if (EsJugador(t)) return;

        vistos.Add(t);
        candidatos.Add(t);
    }

    private bool EsJugador(Transform t)
    {
        if (!tagPlayerValido) return false;

        for (; t != null; t = t.parent)
            if (t.CompareTag("Player")) return true;
        return false;
    }

    /// <summary>¿Está el personaje dentro del área? Se prueban varias alturas desde su pivote.</summary>
    private bool PersonajeEnArea(Transform personaje)
    {
        Vector3 basePos = personaje.position;

        foreach (float altura in AlturasComprobadas)
            if (PuntoEnArea(basePos + Vector3.up * altura)) return true;

        return false;
    }

    private bool PuntoEnArea(Vector3 punto)
    {
        MeshCollider malla = areaCollider as MeshCollider;
        if (malla != null && !malla.convex) return areaCollider.bounds.Contains(punto);

        Vector3 masCercano = areaCollider.ClosestPoint(punto);
        return (masCercano - punto).sqrMagnitude < 0.0025f;
    }

    /// <summary>Sube desde el Animator mientras el padre contenga solo a ese personaje (máximo 4 niveles).</summary>
    private Transform RaizDelPersonaje(Transform t)
    {
        if (t.GetComponentInChildren<Animator>(true) == null) return t;

        Transform raiz = t;
        for (int nivel = 0; nivel < 4 && raiz.parent != null; nivel++)
        {
            Transform padre = raiz.parent;
            if (padre.GetComponentsInChildren<Animator>(true).Length != 1) break;   // contenedor con varios personajes
            raiz = padre;
        }
        return raiz;
    }

    /// <summary>ID de un personaje detectado por posición (mismas fuentes que por collider).</summary>
    private bool ResolverIDDePersonaje(Transform personaje, Transform raiz, out string id, out string metodo)
    {
        id = null;
        metodo = null;

        Dictionary<string, string> tabla = ObtenerIndice();

        // 1) Nombre de modelo en la cadena personaje -> raíz
        for (Transform t = personaje; t != null; t = t.parent)
        {
            if (tabla.TryGetValue(NormalizarNombre(t.name), out string idNombre))
            {
                id = idNombre;
                metodo = "nombre del modelo";
                return true;
            }
            if (t == raiz) break;
        }

        // 2) Modelo del database dentro del NPC
        if (BuscarModeloEnSubarbol(raiz, tabla, out string idSub) == 1)
        {
            id = idSub;
            metodo = "modelo dentro del NPC";
            return true;
        }

        // 3) Campo "id" en algún componente
        for (Transform t = personaje; t != null; t = t.parent)
        {
            string idComponente = IDDesdeComponentes(t);
            if (idComponente != null)
            {
                id = idComponente;
                metodo = $"campo id de un componente de '{t.name}'";
                return true;
            }
            if (t == raiz) break;
        }

        // 4) Número de 1 a 75 en el nombre
        for (Transform t = personaje; t != null; t = t.parent)
        {
            string idNumero = ExtraerID(t.name);
            if (idNumero != null)
            {
                id = idNumero;
                metodo = $"número en el nombre '{t.name}'";
                return true;
            }
            if (t == raiz) break;
        }

        return false;
    }

    // =====================================================================
    // AYUDAS
    // =====================================================================
    private bool TieneTag(Transform t)
    {
        return tagNpcValido && !string.IsNullOrEmpty(tagNPC) && t.CompareTag(tagNPC);
    }

    /// <summary>Comprueba una sola vez que el tag exista en el proyecto (CompareTag falla si no existe).</summary>
    private static bool TagExiste(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;

        try { GameObject.FindWithTag(tag); return true; }
        catch (UnityException) { return false; }
    }

    /// <summary>
    /// Algo que parece un NPC aunque no se reconozca su modelo: tiene el tag NPC, un Animator,
    /// un Rigidbody o un CharacterController. El Player no cuenta.
    /// </summary>
    private bool PareceNPC(Collider c, out Transform raiz)
    {
        raiz = null;

        if (EsJugador(c.transform)) return false;

        // Objeto con el tag NPC (se toma el más alto de la jerarquía)
        for (Transform t = c.transform; t != null; t = t.parent)
            if (TieneTag(t)) raiz = t;
        if (raiz != null) return true;

        Animator animador = c.GetComponentInParent<Animator>();
        if (animador != null) { raiz = animador.transform; return true; }

        if (c.attachedRigidbody != null) { raiz = c.attachedRigidbody.transform; return true; }

        CharacterController personaje = c.GetComponentInParent<CharacterController>();
        if (personaje != null) { raiz = personaje.transform; return true; }

        return false;
    }

    /// <summary>Busca en los componentes de un objeto un campo o propiedad llamado id/idAdoptante/... con un valor de 1 a 75.</summary>
    private string IDDesdeComponentes(Transform t)
    {
        foreach (MonoBehaviour mb in t.GetComponents<MonoBehaviour>())
        {
            if (mb == null || mb == this || mb is EntrevistaUIManager) continue;

            System.Type tipo = mb.GetType();
            string espacio = tipo.Namespace ?? "";
            if (espacio.StartsWith("UnityEngine") || espacio.StartsWith("Unity.") || espacio.StartsWith("TMPro")) continue;

            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

            try
            {
                foreach (System.Reflection.FieldInfo campo in tipo.GetFields(flags))
                {
                    if (!EsNombreDeID(campo.Name)) continue;
                    string idCampo = ValorComoID(campo.GetValue(mb));
                    if (idCampo != null) return idCampo;
                }

                foreach (System.Reflection.PropertyInfo prop in tipo.GetProperties(flags))
                {
                    if (!prop.CanRead || prop.GetIndexParameters().Length > 0 || !EsNombreDeID(prop.Name)) continue;
                    string idProp = ValorComoID(prop.GetValue(mb, null));
                    if (idProp != null) return idProp;
                }
            }
            catch (System.Exception) { /* un componente que no se puede leer se ignora */ }
        }

        return null;
    }

    private static bool EsNombreDeID(string nombre)
    {
        string n = (nombre ?? "").TrimStart('_').ToLowerInvariant();
        return System.Array.IndexOf(NombresCampoID, n) >= 0;
    }

    private static string ValorComoID(object valor)
    {
        int numero;

        if (valor is int entero) numero = entero;
        else if (valor is string texto && int.TryParse(texto.Trim(), out int parseado)) numero = parseado;
        else return null;

        return (numero >= 1 && numero <= 75) ? numero.ToString("00") : null;
    }

    /// <summary>Número de 1 a 75 dentro de un nombre ("Adoptante_05" -> "05"); prefiere los de 2 dígitos.</summary>
    private static string ExtraerID(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return null;

        MatchCollection coincidencias = Regex.Matches(nombre, @"\d+");

        foreach (Match m in coincidencias)
            if (m.Value.Length == 2 && int.TryParse(m.Value, out int dosDigitos) && dosDigitos >= 1 && dosDigitos <= 75)
                return dosDigitos.ToString("00");

        foreach (Match m in coincidencias)
            if (int.TryParse(m.Value, out int cualquiera) && cualquiera >= 1 && cualquiera <= 75)
                return cualquiera.ToString("00");

        return null;
    }
}