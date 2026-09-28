using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AdoptanteUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Image adopterPhotoUI;
    [SerializeField] private TextMeshProUGUI adopterNameText;
    [SerializeField] private TextMeshProUGUI speciesStatText;

    [Header("Stats Texts")]
    [SerializeField] private TextMeshProUGUI compositionStatText;  // Fila Personas
    [SerializeField] private TextMeshProUGUI homeStatText;         // Fila Casa
    [SerializeField] private TextMeshProUGUI timeStatText;         // Fila Reloj
    [SerializeField] private TextMeshProUGUI lifestyleStatText;    // Fila Árbol
    [SerializeField] private TextMeshProUGUI experienceStatText;   // Fila Corazón
    [SerializeField] private TextMeshProUGUI commentStatText;      // Fila Comentario

    [Header("Datos de Adoptantes")]
    [SerializeField] private AdoptanteData dataBase;

    private int currentIndex = 0;

    private void Start()
    {
        // Muestra el primer adoptante al iniciar si existen elementos en la lista
        if (dataBase != null && dataBase.adoptantesList.Count > 0)
        {
            DisplayAdoptante(dataBase.adoptantesList[currentIndex]);
        }
    }

    /// Muestra en la interfaz los datos de un adoptante específico
    public void DisplayAdoptante(AdoptanteInfo adoptante)
    {
        if (adoptante == null) return;

        // Foto, Nombre y Especie
        if (adopterPhotoUI != null) adopterPhotoUI.sprite = adoptante.photo;
        if (adopterNameText != null) adopterNameText.text = adoptante.adopterName;
        if (speciesStatText != null) speciesStatText.text = adoptante.speciesStat;

        // Filas de las Stats
        if (compositionStatText != null) compositionStatText.text = adoptante.compositionStat;
        if (homeStatText != null) homeStatText.text = adoptante.homeStat;
        if (timeStatText != null) timeStatText.text = adoptante.timeStat;
        if (lifestyleStatText != null) lifestyleStatText.text = adoptante.lifestyleStat;
        if (experienceStatText != null) experienceStatText.text = adoptante.experienceStat;
        if (commentStatText != null) commentStatText.text = adoptante.commentStat;
    }

    /// Devuelve el adoptante seleccionado actualmente (incluye ID y Modelo 3D)
    public AdoptanteInfo GetCurrentAdoptante()
    {
        if (dataBase != null && dataBase.adoptantesList.Count > currentIndex)
        {
            return dataBase.adoptantesList[currentIndex];
        }
        return null;
    }

    /// Avanza al siguiente adoptante (para vincular con el Botón Siguiente)
    public void NextAdoptante()
    {
        if (dataBase == null || dataBase.adoptantesList.Count == 0) return;

        currentIndex = (currentIndex + 1) % dataBase.adoptantesList.Count;
        DisplayAdoptante(dataBase.adoptantesList[currentIndex]);
    }

    /// Retrocede al adoptante anterior (para vincular con el Botón Anterior)
    public void PreviousAdoptante()
    {
        if (dataBase == null || dataBase.adoptantesList.Count == 0) return;

        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = dataBase.adoptantesList.Count - 1;
        }
        DisplayAdoptante(dataBase.adoptantesList[currentIndex]);
    }
}