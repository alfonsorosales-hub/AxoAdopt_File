using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PreguntaData
{
    public string enunciadoPregunta; // Texto del botón / Pregunta del jugador
    [TextArea(2, 5)]
    public string respuestaNPC;      // Diálogo / Respuesta del adoptante
    public bool esEspecial;
}

[System.Serializable]
public class PreguntasAdoptanteGroup
{
    public string idAdoptante;       // ID exacta: "01", "02", ..., "75"
    public bool viveSolo;            // Indicador si el adoptante vive solo
    public List<PreguntaData> preguntas = new List<PreguntaData>();
}

[CreateAssetMenu(fileName = "PreguntasDatabase", menuName = "Adoptantes/Preguntas Database")]
public class PreguntasDatabase : ScriptableObject
{
    [Header("Base de Datos para los 75 Adoptantes")]
    public List<PreguntasAdoptanteGroup> adoptantesPreguntas = new List<PreguntasAdoptanteGroup>();

    // Conjunto de números de adoptantes que viven solos (3, 20, 33, 38, 42, 63, 68)
    private readonly HashSet<int> adoptantesSolos = new HashSet<int> { 3, 20, 33, 38, 42, 63, 68 };

    /// Se ejecuta automáticamente al pulsar "Reset" o crear el asset en Unity
    private void Reset()
    {
        GenerarEstructura75Adoptantes();
    }

    /// Regenera completamente los 75 elementos borrando cualquier dato previo
    [ContextMenu("Regenerar 75 Adoptantes Automático")]
    public void GenerarEstructura75Adoptantes()
    {
        adoptantesPreguntas = new List<PreguntasAdoptanteGroup>();

        for (int i = 0; i < 75; i++)
        {
            int numeroNPC = i + 1;
            bool viveSolo = adoptantesSolos.Contains(numeroNPC);

            // Formato de ID numérico sin prefijos: "01", "02", ..., "75"
            string idLimpia = numeroNPC < 10 ? $"0{numeroNPC}" : $"{numeroNPC}";

            PreguntasAdoptanteGroup grupo = new PreguntasAdoptanteGroup
            {
                idAdoptante = idLimpia,
                viveSolo = viveSolo
            };

            // --- Cargar las 3 Preguntas Generales (Adaptadas si vive solo) ---
            string p1 = viveSolo 
                ? "¿Al vivir solo/a, cuánto tiempo sueles tener disponible durante el día para cuidar al animal?"
                : "¿Cuánto tiempo suelen tener disponible durante el día para cuidar al animal?";

            string p2 = viveSolo
                ? "¿Has tenido antes un animal con necesidades similares haciéndote cargo tú solo/a?"
                : "¿Han tenido antes un animal con necesidades similares?";

            string p3 = viveSolo
                ? "¿Cómo es el espacio donde viviría el animal siendo tu residencia única?"
                : "¿Cómo es el espacio donde viviría el animal?";

            // Agregar las 3 preguntas generales
            grupo.preguntas.Add(new PreguntaData { enunciadoPregunta = p1, respuestaNPC = $"Respuesta P1 del Adoptante {grupo.idAdoptante}", esEspecial = false });
            grupo.preguntas.Add(new PreguntaData { enunciadoPregunta = p2, respuestaNPC = $"Respuesta P2 del Adoptante {grupo.idAdoptante}", esEspecial = false });
            grupo.preguntas.Add(new PreguntaData { enunciadoPregunta = p3, respuestaNPC = $"Respuesta P3 del Adoptante {grupo.idAdoptante}", esEspecial = false });

            // --- Del Adoptante 16 al 75: Añadir la 4ª Pregunta Especial ---
            if (numeroNPC >= 16)
            {
                grupo.preguntas.Add(new PreguntaData
                {
                    enunciadoPregunta = $"[ESPECIAL] Pregunta clave sobre el caso del adoptante {grupo.idAdoptante}",
                    respuestaNPC = $"Respuesta especial del Adoptante {grupo.idAdoptante}.",
                    esEspecial = true
                });
            }

            adoptantesPreguntas.Add(grupo);
        }
    }

    /// Busca las preguntas por la ID ("01" a "75")
    public List<PreguntaData> ObtenerPreguntasPorID(string id)
    {
        if (adoptantesPreguntas == null) return null;

        foreach (var grupo in adoptantesPreguntas)
        {
            if (grupo.idAdoptante == id)
            {
                return grupo.preguntas;
            }
        }
        return null;
    }
}