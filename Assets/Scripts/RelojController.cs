using UnityEngine;
using UnityEngine.UI; // Necesario para trabajar con elementos de UI como Image

public class RelojController : MonoBehaviour
{
    [Header("Configuración de Sprites")]
    [Tooltip("Arrastra aquí los 144 sprites recortados en orden (0 a 143)")]
    public Sprite[] framesReloj;

    [Header("Referencias UI")]
    public Image imagenReloj;

    [Header("Tiempos (en segundos)")]
    [Tooltip("Duración de un día completo de juego en segundos del mundo real (24 min = 1440 seg)")]
    public float duracionDiaEnSegundos = 1440f; // 24 minutos reales = 24 horas de juego

    private float duracion12HorasJuego;
    private float tiempoTranscurrido = 0f;

    void Start()
    {
        // Si no se asignó manualmente en el inspector, intenta obtener la Image del mismo GameObject
        if (imagenReloj == null)
            imagenReloj = GetComponent<Image>();

        // 12 horas del juego equivalen a la mitad de la duración total del día (12 minutos reales)
        duracion12HorasJuego = duracionDiaEnSegundos / 2f;
    }

    void Update()
    {
        if (framesReloj == null || framesReloj.Length == 0 || imagenReloj == null) return;

        // Incrementamos el tiempo transcurrido en tiempo real
        tiempoTranscurrido += Time.deltaTime;

        // Calculamos el progreso dentro del ciclo de 12 horas (de 0.0 a 1.0)
        float progresoCiclo = (tiempoTranscurrido % duracion12HorasJuego) / duracion12HorasJuego;

        // Mapeamos el progreso al índice del sprite correspondiente (0 a 143)
        int indiceFrame = Mathf.FloorToInt(progresoCiclo * framesReloj.Length);
        
        // Clampeamos el índice para evitar salirnos del rango por redondeo
        indiceFrame = Mathf.Clamp(indiceFrame, 0, framesReloj.Length - 1);

        // Cambiamos el sprite de la imagen UI
        imagenReloj.sprite = framesReloj[indiceFrame];
    }
}