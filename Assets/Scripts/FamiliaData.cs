using UnityEngine;

[CreateAssetMenu(fileName = "NuevaFamilia", menuName = "AXO-ADOPT/Familia Data")]
public class FamiliaData : ScriptableObject
{
    public int id; // ID 01 a 75
    public string nombreFamilia;
    public string modeloBase;
    
    [Header("Respuestas Visibles (P1, P2, P3)")]
    public string p1TiempoTexto;
    public int tiempoDisponibleVal; // 0: Bajo, 1: Medio, 2: Alto
    public string p2ExperienciaTexto;
    public int experienciaVal;      // 0: Poca, 1: Media, 2: Alta/Buena
    public string p3EntornoTexto;
    public bool tieneEspacioAmplio;

    [Header("Información Oculta (P4)")]
    public string p4PreguntaEspecial;
    public string p4RespuestaOculta;
    public TipoHabilidad habilidadRequerida = TipoHabilidad.Ninguna;
    public int nivelHabilidadRequerido = 0; // 1 o 2

    [Header("Condiciones del Hogar")]
    public bool esHogarRuidoso;
    public bool ignoraCuidadosReales;
}

public enum TipoHabilidad { Ninguna, Investigacion, Empatia, Persuasion }