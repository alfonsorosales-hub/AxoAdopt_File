using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIJornada : MonoBehaviour
{
  [Header("Textos")]
    public TMP_Text textoJornada;
    public TMP_Text textoReputacion;
    public TMP_Text textoRespuesta;      // texto dentro del recuadro blanco

    [Header("Botones")]
    public Button[] botonesPregunta;     // los 3 botones, en orden

    [Header("Datos de cada pregunta")]
    [TextArea] public string[] respuestas;      // respuesta de cada pregunta
    public int[] cambioReputacion;              // cuánto suma o resta cada una

    private int jornada = 1;
    private int reputacion = 50;
    private const int reputacionMax = 100;

    void Start()
    {
        // Conecta cada botón con su índice
        for (int i = 0; i < botonesPregunta.Length; i++)
        {
            int indice = i; // copia necesaria para que el lambda funcione bien
            botonesPregunta[i].onClick.AddListener(() => AlPresionarPregunta(indice));
        }

        ActualizarUI();
        textoRespuesta.text = "";
    }

    void AlPresionarPregunta(int indice)
    {
        if (indice < respuestas.Length)
            textoRespuesta.text = respuestas[indice];

        if (indice < cambioReputacion.Length)
            CambiarReputacion(cambioReputacion[indice]);
    }

    public void CambiarReputacion(int cantidad)
    {
        reputacion = Mathf.Clamp(reputacion + cantidad, 0, reputacionMax);
        ActualizarUI();
    }

    public void SiguienteJornada()
    {
        jornada++;
        ActualizarUI();
    }

    void ActualizarUI()
    {
        textoJornada.text = "Jornada " + jornada;
        textoReputacion.text = "Reputacion: " + reputacion + "/" + reputacionMax;
    }
}
