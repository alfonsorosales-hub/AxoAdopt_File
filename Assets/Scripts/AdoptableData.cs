using UnityEngine;

/// Datos de una mascota adoptable. Puedes crear estos como ScriptableObject
/// para tener una "base de datos" de mascotas, o llenarlos en runtime.
[CreateAssetMenu(fileName = "NewAdoptable", menuName = "Adoptables/Adoptable Data")]
public class AdoptableData : ScriptableObject
{
    [Header("Info general")]
    public string petName;
    public Sprite photo;

    [Header("Stats (texto libre, ej: '3/5', 'Grande', 'Le gusta el pollo')")]
    public string loveStat;   // Fila del corazón
    public string homeStat;   // Fila de la casa
    public string bowlStat;   // Fila del plato
    public string starStat;   // Fila de la estrella
}