using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private int currentIndex = 0;

    private void Start()
    {
        // Mostrar la primera mascota al iniciar si hay elementos en la lista
        if (dataBase != null && dataBase.petsList.Count > 0)
        {
            DisplayPet(dataBase.petsList[currentIndex]);
        }
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

    /// Método para avanzar a la siguiente mascota (Botón Siguiente)
    public void NextPet()
    {
        if (dataBase == null || dataBase.petsList.Count == 0) return;

        currentIndex = (currentIndex + 1) % dataBase.petsList.Count;
        DisplayPet(dataBase.petsList[currentIndex]);
    }

    /// Método para retroceder a la mascota anterior (Botón Anterior)
    public void PreviousPet()
    {
        if (dataBase == null || dataBase.petsList.Count == 0) return;

        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = dataBase.petsList.Count - 1;
        }
        DisplayPet(dataBase.petsList[currentIndex]);
    }
}