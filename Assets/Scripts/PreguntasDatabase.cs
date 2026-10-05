using System.Collections.Generic;
using UnityEngine;

/// Estructura para almacenar la pregunta que hace el jugador y la respuesta que da el NPC
[System.Serializable]
public class PreguntaData
{
    public string enunciadoPregunta; // Texto del botón (lo que pregunta el jugador)
    [TextArea(2, 5)]
    public string respuestaNPC;      // Texto de diálogo (lo que responde el NPC)
    public bool esEspecial;         // Identifica si es la 4ª pregunta especial
}

/// Grupo de preguntas y respuestas para un Adoptante específico
[System.Serializable]
public class PreguntasAdoptanteGroup
{
    public string idAdoptante; // ID del NPC (ej: "ADOPT_01")
    [HideInInspector] public string etiquetaEditor;
    public List<PreguntaData> preguntas = new List<PreguntaData>();
}

/// ScriptableObject que actúa como Base de Datos para los 75 Adoptantes NPCs
[CreateAssetMenu(fileName = "PreguntasDatabase", menuName = "Adoptantes/Preguntas Database")]
public class PreguntasDatabase : ScriptableObject
{
    [Header("Base de Datos para los 75 Adoptantes (NPCs)")]
    public List<PreguntasAdoptanteGroup> adoptantesPreguntas = new List<PreguntasAdoptanteGroup>();

    private void OnValidate()
    {
        if (adoptantesPreguntas.Count != 75)
        {
            GenerarEstructura75Adoptantes();
        }
    }

    [ContextMenu("Regenerar 75 Adoptantes")]
    public void GenerarEstructura75Adoptantes()
    {
        adoptantesPreguntas.Clear();

        for (int i = 0; i < 75; i++)
        {
            int numeroNPC = i + 1;
            PreguntasAdoptanteGroup grupo = new PreguntasAdoptanteGroup();
            grupo.idAdoptante = $"ADOPT_{numeroNPC:D2}"; // IDs: ADOPT_01, ADOPT_02 ... ADOPT_75

            // Adoptantes 1 al 15 -> 3 preguntas
            if (numeroNPC <= 15)
            {
                grupo.etiquetaEditor = $"Adoptante NPC #{numeroNPC} (3 Preguntas)";
                for (int p = 0; p < 3; p++)
                {
                    grupo.preguntas.Add(new PreguntaData 
                    { 
                        enunciadoPregunta = $"¿Pregunta {p + 1} para el adoptante?", 
                        respuestaNPC = $"Respuesta del Adoptante #{numeroNPC} a la pregunta {p + 1}.",
                        esEspecial = false 
                    });
                }
            }
            // Adoptantes 16 al 75 -> 4 preguntas (la 4ª es especial)
            else
            {
                grupo.etiquetaEditor = $"Adoptante NPC #{numeroNPC} (4 Preguntas - Especial)";
                for (int p = 0; p < 3; p++)
                {
                    grupo.preguntas.Add(new PreguntaData 
                    { 
                        enunciadoPregunta = $"¿Pregunta {p + 1} para el adoptante?", 
                        respuestaNPC = $"Respuesta del Adoptante #{numeroNPC} a la pregunta {p + 1}.",
                        esEspecial = false 
                    });
                }
                
                // 4ª Pregunta Especial
                grupo.preguntas.Add(new PreguntaData 
                { 
                    enunciadoPregunta = $"[PREGUNTA ESPECIAL] ¿Caso o situación clave?", 
                    respuestaNPC = $"Respuesta especial del Adoptante #{numeroNPC} sobre la pregunta clave.",
                    esEspecial = true 
                });
            }

            adoptantesPreguntas.Add(grupo);
        }
    }

    /// Obtiene la lista de preguntas/respuestas buscando por ID de adoptante
    public List<PreguntaData> ObtenerPreguntasPorID(string id)
    {
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