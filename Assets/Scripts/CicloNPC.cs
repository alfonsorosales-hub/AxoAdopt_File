using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CicloNPC : MonoBehaviour
{
    [Header("Configuración de Skins (Modelos Hijos)")]
    [Tooltip("Arrastra aquí los modelos 3D que están DENTRO del NPC en la escena (hijos del NPC, no archivos de Project).")]
    public GameObject[] skins;
    [Tooltip("Si está activo, al cambiar de skin se desactivan TODOS los hijos del NPC que tengan renderer y no estén en la lista. " +
             "Úsalo si ves 2 modelos a la vez porque hay un modelo hijo que no está en la lista 'Skins'.")]
    [SerializeField] private bool desactivarModelosNoListados = false;
    private int indiceSkinActual = -1;

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

    void Awake()
    {
        Diagnosticar();
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

    void CambiarSkin()
    {
        if (skins == null || skins.Length == 0) return;

        // Avanza al siguiente modelo de la lista (0, 1, 2... y vuelve a 0)
        indiceSkinActual = (indiceSkinActual + 1) % skins.Length;

        // Activa solo la skin actual y desactiva todas las demás
        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i] != null)
            {
                skins[i].SetActive(i == indiceSkinActual);
            }
        }

        if (desactivarModelosNoListados) DesactivarModelosNoListados();

        Debug.Log($"[CicloNPC] '{name}': skin {indiceSkinActual + 1}/{skins.Length} -> " +
                  $"'{(skins[indiceSkinActual] != null ? skins[indiceSkinActual].name : "VACÍA")}'", this);
    }

    /// <summary>Apaga los hijos con Renderer que no estén en la lista de skins ni sean parte de la skin activa.</summary>
    void DesactivarModelosNoListados()
    {
        GameObject activa = skins[indiceSkinActual];

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform hijo = transform.GetChild(i);
            bool enLista = System.Array.IndexOf(skins, hijo.gameObject) >= 0;
            if (enLista) continue;
            if (activa != null && (hijo == activa.transform || activa.transform.IsChildOf(hijo))) continue;

            if (hijo.GetComponentInChildren<Renderer>(true) != null && hijo.gameObject.activeSelf)
            {
                Debug.Log($"[CicloNPC] Se desactiva '{hijo.name}' porque tiene modelo y no está en la lista 'Skins'.", this);
                hijo.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>Avisa de los problemas más comunes (2 modelos a la vez, skin que no cambia, NPC duplicado).</summary>
    void Diagnosticar()
    {
        // 1) Más de un NPC con este script en la escena = "aparecen 2 prefab"
        CicloNPC[] todos = FindObjectsByType<CicloNPC>(FindObjectsSortMode.None);
        if (todos.Length > 1)
        {
            var nombres = new List<string>();
            foreach (CicloNPC n in todos) nombres.Add(n.name);
            Debug.LogWarning($"[CicloNPC] Hay {todos.Length} objetos con CicloNPC en la escena ({string.Join(", ", nombres)}). " +
                             "Si solo debe haber un NPC, borra o desactiva los demás.", this);
        }

        if (skins == null || skins.Length == 0)
        {
            Debug.LogWarning("[CicloNPC] La lista 'Skins' está vacía: no se cambiará de modelo.", this);
            return;
        }

        if (skins.Length < 2)
            Debug.LogWarning("[CicloNPC] 'Skins' tiene un solo modelo: nunca va a cambiar. Agrega los demás.", this);

        var vistos = new HashSet<GameObject>();
        for (int i = 0; i < skins.Length; i++)
        {
            GameObject s = skins[i];
            if (s == null) { Debug.LogWarning($"[CicloNPC] Skins[{i}] está vacío.", this); continue; }

            // 2) Archivos de Project (prefabs sin instanciar): SetActive no los muestra ni los oculta
            if (!s.scene.IsValid())
                Debug.LogError($"[CicloNPC] Skins[{i}] ('{s.name}') es un prefab de Project, no un objeto de la escena. " +
                               "Arrastra el hijo del NPC desde la Hierarchy.", this);
            // 3) No es hijo de este NPC
            else if (!s.transform.IsChildOf(transform))
                Debug.LogWarning($"[CicloNPC] Skins[{i}] ('{s.name}') no es hijo de '{name}'.", this);

            // 4) Repetida: el modelo "no cambia" porque dos posiciones son el mismo objeto
            if (!vistos.Add(s))
                Debug.LogWarning($"[CicloNPC] Skins[{i}] ('{s.name}') está repetida en la lista.", this);
        }

        // 5) Hijos con modelo que están activos y no están en la lista = segundo modelo visible
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform hijo = transform.GetChild(i);
            if (System.Array.IndexOf(skins, hijo.gameObject) >= 0) continue;
            if (hijo.gameObject.activeSelf && hijo.GetComponentInChildren<Renderer>(true) != null)
                Debug.LogWarning($"[CicloNPC] El hijo '{hijo.name}' tiene modelo, está activo y NO está en 'Skins': " +
                                 "se verá junto a la skin activa. Agrégalo a la lista o activa 'Desactivar Modelos No Listados'.", this);
        }
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