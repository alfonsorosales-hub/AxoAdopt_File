using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Una tarjeta de mascota. Hay una por cada opción (3 en pantalla): cada tarjeta muestra
/// una de las 3 opciones que el selector eligió para el NPC actual (1 mejor + 2 que no lo son).
/// </summary>
public class AdoptableUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Image petPhotoUI;
    [SerializeField] private TextMeshProUGUI petNameText;

    [Header("Stats Texts")]
    [SerializeField] private TextMeshProUGUI ageStatText;   // Fila Edad
    [SerializeField] private TextMeshProUGUI loveStatText;  // Fila Corazón
    [SerializeField] private TextMeshProUGUI homeStatText;  // Fila Casa
    [SerializeField] private TextMeshProUGUI bowlStatText;  // Fila Plato
    [SerializeField] private TextMeshProUGUI starStatText;  // Fila Estrella

    [Header("Datos de Mascotas")]
    [SerializeField] private AdoptableData dataBase;

    [Header("Opciones según el NPC")]
    [Tooltip("Asigna aquí NewAdoptanteDatabase: de ahí se lee lo que busca el adoptante para elegir sus 3 opciones.")]
    [SerializeField] private AdoptanteData adoptantesDataBase;

    [Tooltip("Qué opción muestra esta tarjeta: 0, 1 o 2. Déjalo en -1 para que se calcule solo " +
             "según el orden de las tarjetas dentro de su contenedor (la primera = 0, la segunda = 1...).")]
    [SerializeField] private int posicionTarjeta = -1;

    [Tooltip("Solo para probar esta escena sola (Play directo, sin pasar por la entrevista). Ejemplo: 26. " +
             "Si se llega desde la entrevista, se usa la ID del NPC y esto se ignora.")]
    [SerializeField] private string idDePrueba = "";

    [Header("Al elegir esta mascota")]
    [Tooltip("Se dispara al llamar a ElegirMascotaActual(). Recibe true si esta mascota era la mejor opción para el adoptante.")]
    [SerializeField] private UnityEvent<bool> alElegirMascota;

    [Tooltip("Si está marcado, al elegir la mascota se registra como adoptada en CampaignManager " +
             "(así deja de aparecer en el pool de las siguientes jornadas).")]
    [SerializeField] private bool registrarAdopcionEnCampana = false;

    // Índice (en dataBase.petsList) de la mascota que muestra esta tarjeta
    private int indiceMascota = -1;

    // ID del adoptante para el que se armaron las opciones
    private string idAdoptante;

    // Para no repetir el trabajo de Start cuando se activa el objeto por primera vez
    private bool iniciado;

    private void Start()
    {
        MostrarOpcion();
        iniciado = true;
    }

    // Si el panel se oculta y se vuelve a mostrar (por ejemplo con otro NPC), la tarjeta se actualiza
    private void OnEnable()
    {
        if (iniciado) MostrarOpcion();
    }

    /// Calcula las 3 opciones del NPC actual y muestra en esta tarjeta la que le corresponde
    public void MostrarOpcion()
    {
        if (dataBase == null || dataBase.petsList.Count == 0) return;

        // 1) ID del NPC: la que dejó la escena de la entrevista (o la de prueba si se abrió esta escena sola)
        idAdoptante = SelectorOpcionesAdopcion.IdAdoptanteActual;
        if (string.IsNullOrEmpty(idAdoptante))
        {
            idAdoptante = idDePrueba;
            Debug.LogWarning($"[AdoptableUI] No llegó ninguna ID desde la entrevista. Se usa la ID de prueba '{idDePrueba}'.");
        }

        // 2) Las 3 opciones de ese adoptante. Todas las tarjetas piden lo mismo y reciben las mismas
        //    (el selector las guarda), así que entre las tarjetas no se repite ninguna mascota
        int[] opciones = SelectorOpcionesAdopcion.ObtenerOpciones(dataBase, adoptantesDataBase, idAdoptante);

        // 3) Si no se pudieron calcular (falta asignar la base de adoptantes, por ejemplo),
        //    cada tarjeta muestra una mascota distinta de la lista, para no repetirlas
        if (opciones.Length == 0)
        {
            Debug.LogWarning("[AdoptableUI] No se pudieron calcular las opciones (revisa 'Adoptantes Data Base').");
            opciones = new int[dataBase.petsList.Count];
            for (int i = 0; i < opciones.Length; i++) opciones[i] = i;
        }

        // 4) Qué opción le toca a esta tarjeta
        int posicion = posicionTarjeta >= 0 ? posicionTarjeta : CalcularPosicionAutomatica();
        if (posicion >= opciones.Length)
        {
            Debug.LogWarning($"[AdoptableUI] La tarjeta '{name}' tiene la posición {posicion}, pero solo hay {opciones.Length} opciones.");
            posicion %= opciones.Length;
        }

        indiceMascota = opciones[posicion];
        DisplayPet(dataBase.petsList[indiceMascota]);
    }

    /// Posición de esta tarjeta entre sus hermanas que también tienen AdoptableUI (0 = la primera).
    /// Si cada tarjeta está en un contenedor distinto, escribe la posición a mano en el Inspector.
    private int CalcularPosicionAutomatica()
    {
        Transform padre = transform.parent;
        if (padre == null) return 0;

        int posicion = 0;
        for (int i = 0; i < padre.childCount; i++)
        {
            Transform hermano = padre.GetChild(i);
            if (hermano == transform) return posicion;
            if (hermano.GetComponent<AdoptableUI>() != null) posicion++;
        }

        return 0;
    }

    /// Muestra en la interfaz los datos de una mascota específica
    public void DisplayPet(PetInfo pet)
    {
        if (pet == null) return;

        if (petPhotoUI != null) petPhotoUI.sprite = pet.photo;
        if (petNameText != null) petNameText.text = pet.petName;

        if (ageStatText != null) ageStatText.text = pet.ageStat;
        if (loveStatText != null) loveStatText.text = pet.loveStat;
        if (homeStatText != null) homeStatText.text = pet.homeStat;
        if (bowlStatText != null) bowlStatText.text = pet.bowlStat;
        if (starStatText != null) starStatText.text = pet.starStat;
    }

    /// Método para elegir la mascota de esta tarjeta (conéctalo al botón de la tarjeta en On Click).
    /// Avisa si era la mejor opción; úsalo con el evento 'Al Elegir Mascota'.
    public void ElegirMascotaActual()
    {
        if (dataBase == null || indiceMascota < 0) return;

        bool eraLaMejor = SelectorOpcionesAdopcion.EsLaMejorOpcion(idAdoptante, indiceMascota);

        Debug.Log($"[AdoptableUI] Adoptante '{idAdoptante}' eligió '{dataBase.petsList[indiceMascota].petName}': " +
                  $"{(eraLaMejor ? "era la MEJOR opción" : "no era la mejor opción")}.");

        // La ID del animal en el pool de la campaña es su posición en la lista empezando en 1 ("01", "02"...)
        if (registrarAdopcionEnCampana && CampaignManager.Instance != null)
            CampaignManager.Instance.RegistrarAdopcionAnimal((indiceMascota + 1).ToString("00"));

        alElegirMascota?.Invoke(eraLaMejor);
    }
}