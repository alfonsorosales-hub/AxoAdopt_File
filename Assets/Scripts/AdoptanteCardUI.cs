using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AdoptanteCardUI : MonoBehaviour
{
    [Header("Referencias Generales UI")]
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

    /// Aplica la información del adoptante a los elementos de la interfaz
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
}