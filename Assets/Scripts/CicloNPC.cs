using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class CicloNPC : MonoBehaviour
{
    [Header("Configuración de Skins (Modelos Hijos)")]
    [Tooltip("Los modelos 3D del NPC. Pueden ser los hijos de la escena o los prefabs de Project: " +
             "si son prefabs, el script usa el hijo que tenga el mismo nombre (o lo crea si no existe).")]
    public GameObject[] skins;
    private int indiceSkinActual = -1;

    // Los modelos reales de la escena (hijos de este NPC), en el mismo orden que 'skins'
    private GameObject[] modelos = new GameObject[0];

    // Quita "(Clone)" y sufijos del editor como " (1)" para comparar nombres
    private static readonly Regex Sufijos = new Regex(@"\s*\(Clone\)|\s*\(\d+\)", RegexOptions.IgnoreCase);

    [Header("Configuración de Movimiento")]
    [Tooltip("Objeto vacío en la escena que marca hacia dónde debe ir el NPC")]
    public Transform puntoDestino;
    public float velocidad = 3.5f;
    public float VelocidadRotacion = 10f;
    public float distanciaLlegada = 0.05f;

    private Vector3 posicionInicial;
    private bool eventoTerminado = false;
    private bool esperandoEvento = false;

    /// <summary>True mientras el NPC está en el punto destino esperando la decisión final.</summary>
    public bool EsperandoEvento => esperandoEvento;

    private static string Normalizar(string nombre)
    {
        return Sufijos.Replace(nombre ?? "", "").Trim();
    }

    void Awake()
    {
        DesactivarSkinNPC();
        ResolverSkins();
        AvisarNPCDuplicados();
    }

    void Start()
    {
        // Guardamos la posición exacta donde inicia el NPC
        posicionInicial = transform.position;

        // Iniciamos el ciclo secuencial infinito
        if (puntoDestino != null)
        {
            StartCoroutine(RutinaPrincipalNPC());
        }
        else
        {
            Debug.LogError("Falta asignar el Punto Destino en el inspector del NPC.", this);
        }
    }

    // =====================================================================
    // SKINS
    // =====================================================================

    /// <summary>
    /// SkinNPC también activa/desactiva modelos con su propia lista y deja un segundo modelo encendido.
    /// Este script se encarga de las skins, así que SkinNPC se apaga antes de que corra su Start.
    /// </summary>
    void DesactivarSkinNPC()
    {
        foreach (SkinNPC otro in GetComponentsInChildren<SkinNPC>(true))
        {
            if (otro != null && otro.enabled)
            {
                otro.enabled = false;
                Debug.Log($"[CicloNPC] Se desactivó SkinNPC en '{otro.name}': CicloNPC controla las skins.", this);
            }
        }
    }

    /// <summary>
    /// Convierte la lista 'skins' en los hijos reales del NPC. Si un elemento es un prefab de Project
    /// (o un objeto de otro lado), se busca entre los hijos el que se llame igual; si no existe, se crea.
    /// </summary>
    void ResolverSkins()
    {
        if (skins == null || skins.Length == 0)
        {
            Debug.LogWarning("[CicloNPC] La lista 'Skins' está vacía: no se cambiará de modelo.", this);
            modelos = new GameObject[0];
            return;
        }

        if (skins.Length < 2)
            Debug.LogWarning("[CicloNPC] 'Skins' tiene un solo modelo: nunca va a cambiar. Agrega los demás.", this);

        modelos = new GameObject[skins.Length];
        var usados = new HashSet<GameObject>();
        Transform[] descendientes = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < skins.Length; i++)
        {
            GameObject s = skins[i];
            if (s == null)
            {
                Debug.LogWarning($"[CicloNPC] Skins[{i}] está vacío.", this);
                continue;
            }

            GameObject resuelto = null;

            if (s != gameObject && s.scene.IsValid() && s.transform.IsChildOf(transform))
            {
                resuelto = s; // ya es un hijo de la escena
            }
            else
            {
                string buscado = Normalizar(s.name);
                foreach (Transform t in descendientes)
                {
                    if (t == transform || usados.Contains(t.gameObject)) continue;

                    if (string.Equals(Normalizar(t.name), buscado, System.StringComparison.OrdinalIgnoreCase))
                    {
                        resuelto = t.gameObject;
                        break;
                    }
                }

                if (resuelto != null)
                {
                    Debug.Log($"[CicloNPC] Skins[{i}] ('{s.name}') apuntaba a un prefab: se usa el hijo '{resuelto.name}' de la escena.", this);
                }
                else if (!s.scene.IsValid())
                {
                    resuelto = Instantiate(s, transform);
                    resuelto.name = s.name;
                    Debug.LogWarning($"[CicloNPC] Skins[{i}] ('{s.name}'): no había ningún hijo con ese nombre, se creó uno nuevo dentro del NPC.", this);
                }
                else
                {
                    resuelto = s;
                    Debug.LogWarning($"[CicloNPC] Skins[{i}] ('{s.name}') no es hijo de '{name}'.", this);
                }
            }

            if (!usados.Add(resuelto))
                Debug.LogWarning($"[CicloNPC] Skins[{i}] ('{resuelto.name}') está repetida: nunca se verá un cambio entre esas posiciones.", this);

            modelos[i] = resuelto;
        }
    }

    void CambiarSkin()
    {
        if (modelos == null || modelos.Length == 0) return;

        // Avanza al siguiente modelo de la lista (0, 1, 2... y vuelve a 0)
        indiceSkinActual = (indiceSkinActual + 1) % modelos.Length;

        // Activa solo la skin actual y desactiva todas las demás
        for (int i = 0; i < modelos.Length; i++)
        {
            if (modelos[i] != null)
            {
                modelos[i].SetActive(i == indiceSkinActual);
            }
        }

        ApagarModelosNoListados();

        GameObject activa = modelos[indiceSkinActual];
        Debug.Log($"[CicloNPC] '{name}': skin {indiceSkinActual + 1}/{modelos.Length} -> '{(activa != null ? activa.name : "VACÍA")}'", this);
    }

    /// <summary>Apaga cualquier otro hijo con modelo que no esté en la lista (así nunca se ven 2 a la vez).</summary>
    void ApagarModelosNoListados()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform hijo = transform.GetChild(i);
            if (!hijo.gameObject.activeSelf) continue;

            bool esSkinOContieneUna = false;
            foreach (GameObject m in modelos)
            {
                if (m != null && (m == hijo.gameObject || m.transform.IsChildOf(hijo)))
                {
                    esSkinOContieneUna = true;
                    break;
                }
            }
            if (esSkinOContieneUna) continue;

            if (hijo.GetComponentInChildren<Renderer>(true) != null)
            {
                Debug.Log($"[CicloNPC] Se apaga '{hijo.name}': tiene modelo y no está en la lista 'Skins'.", this);
                hijo.gameObject.SetActive(false);
            }
        }
    }

    void AvisarNPCDuplicados()
    {
        CicloNPC[] todos = FindObjectsByType<CicloNPC>(FindObjectsSortMode.None);
        if (todos.Length > 1)
        {
            var nombres = new List<string>();
            foreach (CicloNPC n in todos) nombres.Add(n.name);
            Debug.LogWarning($"[CicloNPC] Hay {todos.Length} objetos con CicloNPC en la escena ({string.Join(", ", nombres)}). " +
                             "Si solo debe haber un NPC, borra o desactiva los demás.", this);
        }
    }

    // =====================================================================
    // CICLO
    // =====================================================================
    IEnumerator RutinaPrincipalNPC()
    {
        // Bucle infinito para que el ciclo se repita siempre
        while (true)
        {
            // La señal de fin de evento se limpia AL EMPEZAR el ciclo, no al llegar:
            // así una señal enviada mientras el NPC aún camina no se pierde.
            eventoTerminado = false;

            // PASO 1: Cambiar la skin o modelado por otro
            CambiarSkin();

            // PASO 2: Desplazarse hasta el punto destino
            yield return StartCoroutine(MoverHacia(puntoDestino.position));

            // PASO 3: Permanecer allí hasta que el evento termine
            esperandoEvento = true;
            Debug.Log($"[CicloNPC] '{name}' en posición. Esperando NotificarFinDeEvento()...", this);
            yield return new WaitUntil(() => eventoTerminado);
            esperandoEvento = false;

            // PASO 4: Regresar a la posición inicial
            Debug.Log($"[CicloNPC] '{name}': evento finalizado. Regresando al punto de origen...", this);
            yield return StartCoroutine(MoverHacia(posicionInicial));

            // Pequeña pausa opcional antes de volver a cambiar de skin y repetir
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator MoverHacia(Vector3 destino)
    {
        while (Vector3.Distance(transform.position, destino) > distanciaLlegada)
        {
            // Rotar suavemente hacia la dirección de movimiento (solo en el eje horizontal)
            Vector3 direccion = (destino - transform.position).normalized;
            direccion.y = 0;
            if (direccion != Vector3.zero)
            {
                //Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
                // mirar hacia donde se camina
                //transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * VelocidadRotacion);
            }

            // Desplazar el NPC hacia el objetivo
            transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);

            yield return null; // Esperar al siguiente frame
        }

        // Ajustar a la posición exacta al llegar
        transform.position = destino;
    }

    // --- MÉTODO PÚBLICO PARA ACTIVAR EL REGRESO ---
    // Llama a esta función desde otro script, un botón de UI o un Trigger cuando tu evento acabe.
    [ContextMenu("Simular Fin del Evento")]
    public void NotificarFinDeEvento()
    {
        if (!esperandoEvento)
            Debug.Log($"[CicloNPC] NotificarFinDeEvento() en '{name}' llegó antes de que el NPC esté en el destino: se guarda y se usará al llegar.", this);

        eventoTerminado = true;
    }
}