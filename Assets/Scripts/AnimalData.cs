using UnityEngine;

[CreateAssetMenu(fileName = "NuevoAnimal", menuName = "AXO-ADOPT/Animal Data")]
public class AnimalData : ScriptableObject
{
    public int id;
    public string nombreEspecie; // Ej: "01. PERRO BEBÉ"
    public string edad;
    public string personalidad;
    [TextArea(2, 4)] public string necesidades;
    public string alimentacion;
    public string rasgoEspecial;
    public Sprite artworkPixelArt;

    [Header("Requisitos Mínimos para Algoritmo")]
    [Range(0, 3)] public int tiempoRequerido; // 0: Bajo, 1: Medio, 2: Alto
    public bool requiereEspacioAmplio;
    public bool sensibleAlRuido;
    public bool requiereExperienciaPrevia;
}