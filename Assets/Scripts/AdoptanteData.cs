using System.Collections.Generic;
using UnityEngine;

/// Estructura de datos individual de cada Adoptante
[System.Serializable]
public class AdoptanteInfo
{
    [Header("Datos Internos / Editor")]
    public string id;                   // ID solo visible en el Editor
    public GameObject modelPrefab;      // Modelo 3D asignado al adoptante

    [Header("Info General")]
    public string adopterName;          // Nombre en la cápsula central
    public string speciesStat;          // Especie
    public Sprite photo;                // Foto/Retrato para el recuadro blanco

    [Header("Stats de la Carta")]
    public string compositionStat;      // Fila Personas (Composición familiar)
    public string homeStat;             // Fila Casa (Hogar)
    public string timeStat;             // Fila Reloj (Tiempo disponible)
    public string lifestyleStat;        // Fila Árbol (Estilo de vida)
    public string experienceStat;       // Fila Corazón (Experiencia)
    public string commentStat;          // Fila Comentario (Comentario de la familia)
}

/// ScriptableObject que actúa como base de datos general para los Adoptantes
[CreateAssetMenu(fileName = "NewAdoptanteDatabase", menuName = "Adoptantes/Adoptante Database")]
public class AdoptanteData : ScriptableObject
{
    [Header("Lista de Adoptantes")]
    public List<AdoptanteInfo> adoptantesList = new List<AdoptanteInfo>();
}