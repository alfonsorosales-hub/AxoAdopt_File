using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AdoptantePreguntasManager : MonoBehaviour
{
    [Header("Bases de Datos")]
    [SerializeField] private AdoptanteData adoptanteDB;
    [SerializeField] private PreguntasDatabase preguntasDB;

    [Header("UI Diálogo NPC")]
    [SerializeField] private TextMeshProUGUI textoRespuestaNPC; // Cuadro de diálogo del NPC

    [Header("--- DISEÑO 1: Standard (3 Preguntas) ---")]
    [SerializeField] private GameObject layoutStandard3; 
    [SerializeField] private Button[] botonesStandard;          // Array de 3 botones
    [SerializeField] private TextMeshProUGUI[] textosBotonesStandard; // Array de 3 textos para botones

    [Header("--- DISEÑO 2: Especial (4 Preguntas) ---")]
    [SerializeField] private GameObject layoutEspecial4; 
    [SerializeField] private Button[] botonesEspeciales3;        // Array de 3 botones normales
    [SerializeField] private TextMeshProUGUI[] textosBotonesEspeciales3;
    [SerializeField] private Button botonCuartaEspecial;         // Botón destacado (4ª pregunta)
    [SerializeField] private TextMeshProUGUI textoBotonCuartaEspecial;

    private List<PreguntaData> preguntasActuales;

    /// Carga las opciones de preguntas correspondientes al adoptante actual (por su índice de 0 a 74)
    public void CargarPreguntasParaAdoptante(int index)
    {
        if (adoptanteDB == null || preguntasDB == null)
        {
            Debug.LogWarning("Por favor asigna AdoptanteData y PreguntasDatabase en el Inspector.");
            return;
        }

        if (index < 0 || index >= adoptanteDB.adoptantesList.Count) return;

        AdoptanteInfo adoptanteActual = adoptanteDB.adoptantesList[index];
        
        // Buscar por ID del adoptante
        preguntasActuales = preguntasDB.ObtenerPreguntasPorID(adoptanteActual.id);

        // Si no lo encuentra por ID, busca por índice de la lista (0 a 74)
        if (preguntasActuales == null && index < preguntasDB.adoptantesPreguntas.Count)
        {
            preguntasActuales = preguntasDB.adoptantesPreguntas[index].preguntas;
        }

        if (preguntasActuales == null) return;

        // Limpia el cuadro de respuesta del NPC al cambiar de adoptante
        if (textoRespuestaNPC != null)
        {
            textoRespuestaNPC.text = "...";
        }

        // Cargar el diseño correspondiente
        if (preguntasActuales.Count == 3)
        {
            ActivarLayoutStandard();
        }
        else if (preguntasActuales.Count >= 4)
        {
            ActivarLayoutEspecial();
        }
    }

    /// Configura el diseño de 3 preguntas (Adoptantes 1 al 15)
    private void ActivarLayoutStandard()
    {
        if (layoutStandard3 != null) layoutStandard3.SetActive(true);
        if (layoutEspecial4 != null) layoutEspecial4.SetActive(false);

        for (int i = 0; i < botonesStandard.Length && i < preguntasActuales.Count; i++)
        {
            int indexPregunta = i; // Copia para la expresión lambda

            if (textosBotonesStandard[i] != null)
            {
                textosBotonesStandard[i].text = preguntasActuales[i].enunciadoPregunta;
            }

            if (botonesStandard[i] != null)
            {
                botonesStandard[i].onClick.RemoveAllListeners();
                botonesStandard[i].onClick.AddListener(() => ResponderNPC(indexPregunta));
            }
        }
    }

    /// Configura el diseño de 4 preguntas (Adoptantes 16 al 75)
    private void ActivarLayoutEspecial()
    {
        if (layoutStandard3 != null) layoutStandard3.SetActive(false);
        if (layoutEspecial4 != null) layoutEspecial4.SetActive(true);

        // Asignar los primeros 3 botones
        for (int i = 0; i < 3 && i < preguntasActuales.Count; i++)
        {
            int indexPregunta = i;

            if (textosBotonesEspeciales3[i] != null)
            {
                textosBotonesEspeciales3[i].text = preguntasActuales[i].enunciadoPregunta;
            }

            if (botonesEspeciales3[i] != null)
            {
                botonesEspeciales3[i].onClick.RemoveAllListeners();
                botonesEspeciales3[i].onClick.AddListener(() => ResponderNPC(indexPregunta));
            }
        }

        // Asignar el 4º botón especial
        if (preguntasActuales.Count >= 4)
        {
            if (textoBotonCuartaEspecial != null)
            {
                textoBotonCuartaEspecial.text = preguntasActuales[3].enunciadoPregunta;
            }

            if (botonCuartaEspecial != null)
            {
                botonCuartaEspecial.onClick.RemoveAllListeners();
                botonCuartaEspecial.onClick.AddListener(() => ResponderNPC(3));
            }
        }
    }

    /// Muestra en pantalla la respuesta que da el NPC al pulsar una pregunta
    public void ResponderNPC(int indexPregunta)
    {
        if (preguntasActuales != null && indexPregunta < preguntasActuales.Count)
        {
            if (textoRespuestaNPC != null)
            {
                textoRespuestaNPC.text = preguntasActuales[indexPregunta].respuestaNPC;
            }
        }
    }
}