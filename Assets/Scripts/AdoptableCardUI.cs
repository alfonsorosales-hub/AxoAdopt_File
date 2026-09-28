using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Controla la tarjeta de adopción: asigna las referencias en el Inspector
/// siguiendo el layout de la plantilla (foto, nombre, y 4 filas de stats).
public class AdoptableCardUI : MonoBehaviour
{
    [Header("Foto y nombre")]
    [SerializeField] private Image petPhotoImage;
    [SerializeField] private TMP_Text petNameText;

    [Header("Filas de stats (en orden: corazón, casa, plato, estrella)")]
    [SerializeField] private TMP_Text loveValueText;
    [SerializeField] private TMP_Text homeValueText;
    [SerializeField] private TMP_Text bowlValueText;
    [SerializeField] private TMP_Text starValueText;

    [Header("Opcional: placeholder si la mascota no tiene foto")]
    [SerializeField] private Sprite defaultPhoto;
    /// Llena la tarjeta con los datos de un ScriptableObject.
    public void SetData(AdoptableData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[AdoptableCardUI] SetData recibió datos nulos.");
            return;
        }

        if (petNameText != null)
            petNameText.text = data.petName;

        if (petPhotoImage != null)
            petPhotoImage.sprite = data.photo != null ? data.photo : defaultPhoto;

        if (loveValueText != null) loveValueText.text = data.loveStat;
        if (homeValueText != null) homeValueText.text = data.homeStat;
        if (bowlValueText != null) bowlValueText.text = data.bowlStat;
        if (starValueText != null) starValueText.text = data.starStat;
    }
    /// Sobrecarga rápida para llenar la tarjeta dinámicamente (ej: desde un JSON o API).
    public void SetData(string name, Sprite photo, string love, string home, string bowl, string star)
    {
        if (petNameText != null) petNameText.text = name;
        if (petPhotoImage != null) petPhotoImage.sprite = photo != null ? photo : defaultPhoto;
        if (loveValueText != null) loveValueText.text = love;
        if (homeValueText != null) homeValueText.text = home;
        if (bowlValueText != null) bowlValueText.text = bowl;
        if (starValueText != null) starValueText.text = star;
    }
    /// Limpia la tarjeta (útil para un estado vacío).
    public void Clear()
    {
        if (petNameText != null) petNameText.text = string.Empty;
        if (petPhotoImage != null) petPhotoImage.sprite = defaultPhoto;
        if (loveValueText != null) loveValueText.text = string.Empty;
        if (homeValueText != null) homeValueText.text = string.Empty;
        if (bowlValueText != null) bowlValueText.text = string.Empty;
        if (starValueText != null) starValueText.text = string.Empty;
    }
}