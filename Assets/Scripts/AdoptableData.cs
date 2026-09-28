using System.Collections.Generic;
using UnityEngine;

/// Estructura de datos individual para cada mascota
[System.Serializable]
public class PetInfo
{
    [Header("Info general")]
    public string petName;
    public Sprite photo;

    [Header("Stats")]
    public string ageStat;    // Fila de la edad
    public string loveStat;   // Fila del corazón (Personalidad)
    public string homeStat;   // Fila de la casa (Lo que necesita)
    public string bowlStat;   // Fila del plato (Alimentación)
    public string starStat;   // Fila de la estrella (Rasgo especial)
}

/// ScriptableObject que actúa como base de datos general para tus 35 mascotas
[CreateAssetMenu(fileName = "NewAdoptableDatabase", menuName = "Adoptables/Adoptable Database")]
public class AdoptableData : ScriptableObject
{
    [Header("Mapeo individual (Para compatibilidad previa)")]
    public string petName;
    public Sprite photo;
    public string ageStat;
    public string loveStat;
    public string homeStat;
    public string bowlStat;
    public string starStat;

    [Header("Lista de las 35 Mascotas")]
    public List<PetInfo> petsList = new List<PetInfo>();
}