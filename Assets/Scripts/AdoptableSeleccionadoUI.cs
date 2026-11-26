using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Muestra en un TextMeshProUGUI el nombre del adoptable que el jugador escogió para el NPC actual.
/// - Al iniciar muestra "ninguno".
/// - Al elegir una de las 3 opciones (botón 0, 1 o 2), actualiza el texto con el nombre de NewAdoptableDatabase.
/// - Cuando CicloNPC finaliza el evento (NotificarFinDeEvento), reinicia el texto a "ninguno".
/// </summary>
public class AdoptableSeleccionadoUI : MonoBehaviour
{
    // Guarda la selección globalmente para sobrevivir entre escenas aditivas o paneles distintos
    public static string UltimoAdoptableElegido { get; private set; } = "";

    [Header("Referencias UI")]
    [Tooltip("Arrastra aquí el TextMeshProUGUI donde se mostrará el adoptable elegido.")]
    [SerializeField] private TextMeshProUGUI textoAdoptableSeleccionado;

    [Header("Base de Datos de Adoptables")]
    [Tooltip("Arrastra aquí 'NewAdoptableDatabase' (AdoptableData).")]
    [SerializeField] private AdoptableData adoptablesDataBase;

    [Header("Botones de Selección (Opcional - Conexión Automática)")]
    [Tooltip("Si los 3 botones están en esta misma escena, arrástralos aquí en orden (0, 1 y 2) " +
             "y el script los conectará automáticamente sin tener que configurar OnClick() a mano.")]
    [SerializeField] private Button[] botonesOpciones;

    [Header("Referencia al Ciclo del NPC")]
    [Tooltip("Opcional: arrastra el NPC con CicloNPC. Si lo dejas vacío, se buscará automáticamente en la escena.")]
    [SerializeField] private CicloNPC cicloNPC;

    [Header("Configuración de Texto")]
    [Tooltip("Texto que se mostrará al inicio y al finalizar cada evento.")]
    [SerializeField] private string textoPorDefecto = "ninguno";

    private bool estabaEsperandoEvento = false;

    private void Awake()
    {
        // Si asignaste los 3 botones en el array 'Botones Opciones', los conectamos automáticamente
        if (botonesOpciones != null)
        {
            for (int i = 0; i < botonesOpciones.Length; i++)
            {
                if (botonesOpciones[i] != null)
                {
                    int indiceBoton = i; // Copia local indispensable para el listener
                    botonesOpciones[i].onClick.AddListener(() => SeleccionarOpcionTarjeta(indiceBoton));
                }
            }
        }
    }

    private void Start()
    {
        if (cicloNPC == null)
        {
            cicloNPC = FindFirstObjectByType<CicloNPC>();
        }

        // Si ya había una selección hecha desde otra escena aditiva, la mostramos; si no, "ninguno"
        if (!string.IsNullOrEmpty(UltimoAdoptableElegido))
        {
            ActualizarTexto(UltimoAdoptableElegido);
        }
        else
        {
            ReiniciarSeleccion();
        }
    }

    private void OnEnable()
    {
        // Al volver a la escena principal tras cerrar la escena de selección, sincronizamos el texto
        if (!string.IsNullOrEmpty(UltimoAdoptableElegido))
        {
            ActualizarTexto(UltimoAdoptableElegido);
        }
    }

    private void Update()
    {
        // Si otra instancia (por ejemplo en una escena aditiva) cambió la selección, actualizamos este texto
        if (!string.IsNullOrEmpty(UltimoAdoptableElegido) &&
            textoAdoptableSeleccionado != null &&
            textoAdoptableSeleccionado.text != UltimoAdoptableElegido)
        {
            ActualizarTexto(UltimoAdoptableElegido);
        }

        // Detecta cuando CicloNPC.NotificarFinDeEvento() libera al NPC (EsperandoEvento pasa de true a false)
        if (cicloNPC != null)
        {
            if (estabaEsperandoEvento && !cicloNPC.EsperandoEvento)
            {
                ReiniciarSeleccion();
            }

            estabaEsperandoEvento = cicloNPC.EsperandoEvento;
        }
    }

    /// <summary>
    /// Llama a este método al hacer clic en una de las 3 opciones (0 = primera, 1 = segunda, 2 = tercera).
    /// </summary>
    public void SeleccionarOpcionTarjeta(int posicionTarjeta)
    {
        Debug.Log($"<color=yellow>[AdoptableSeleccionadoUI] ¡Clic recibido en opción {posicionTarjeta}!</color>");

        if (adoptablesDataBase == null || adoptablesDataBase.petsList == null || adoptablesDataBase.petsList.Count == 0)
        {
            Debug.LogError("[AdoptableSeleccionadoUI] ERROR: Falta asignar 'NewAdoptableDatabase' en el Inspector.");
            return;
        }

        // Obtenemos las 3 opciones del adoptante actual desde SelectorOpcionesAdopcion
        AdoptanteData[] basesAdoptantes = Resources.FindObjectsOfTypeAll<AdoptanteData>();
        AdoptanteData refAdoptantes = basesAdoptantes.Length > 0
            ? basesAdoptantes[0]
            : ScriptableObject.CreateInstance<AdoptanteData>();

        int[] opciones = SelectorOpcionesAdopcion.ObtenerOpcionesActuales(adoptablesDataBase, refAdoptantes);

        int indiceEnDatabase = posicionTarjeta;
        if (opciones != null && opciones.Length > 0 && posicionTarjeta >= 0 && posicionTarjeta < opciones.Length)
        {
            indiceEnDatabase = opciones[posicionTarjeta];
        }

        SeleccionarPorIndiceDatabase(indiceEnDatabase);
    }

    /// <summary>
    /// Busca la mascota por su índice en NewAdoptableDatabase y actualiza el texto en pantalla.
    /// </summary>
    public void SeleccionarPorIndiceDatabase(int indiceEnPetsList)
    {
        if (adoptablesDataBase == null || adoptablesDataBase.petsList == null) return;

        if (indiceEnPetsList >= 0 && indiceEnPetsList < adoptablesDataBase.petsList.Count)
        {
            string nombreMascota = adoptablesDataBase.petsList[indiceEnPetsList].petName;
            UltimoAdoptableElegido = nombreMascota;
            Debug.Log($"<color=green>[AdoptableSeleccionadoUI] Texto actualizado a: '{nombreMascota}'</color>");
            ActualizarTexto(nombreMascota);
        }
        else
        {
            Debug.LogWarning($"[AdoptableSeleccionadoUI] Índice {indiceEnPetsList} fuera de rango en NewAdoptableDatabase.");
        }
    }

    /// <summary>
    /// Reinicia la selección actual y vuelve a mostrar "ninguno".
    /// </summary>
    public void ReiniciarSeleccion()
    {
        UltimoAdoptableElegido = "";
        ActualizarTexto(textoPorDefecto);
    }

    private void ActualizarTexto(string nuevoTexto)
    {
        if (textoAdoptableSeleccionado == null)
        {
            Debug.LogError("[AdoptableSeleccionadoUI] ERROR: No has asignado el TextMeshProUGUI en el Inspector.");
            return;
        }

        if (!textoAdoptableSeleccionado.gameObject.activeSelf)
        {
            textoAdoptableSeleccionado.gameObject.SetActive(true);
        }

        textoAdoptableSeleccionado.text = nuevoTexto;
        textoAdoptableSeleccionado.ForceMeshUpdate();
    }
}