using System.Collections.Generic;
using UnityEngine;

public class GameLoopManager : MonoBehaviour
{
    [Header("Referencias de Sistema")]
    public ReputationAndSkillManager repManager;
    public DeferredConsequencesManager consequenceManager;

    [Header("Estado de la Sesión")]
    public int jornadaActual = 1;
    public FamiliaData familiaActual;
    public bool infoOcultaRevelada = false;

    public List<AnimalData> poolAnimalesDisponibles = new List<AnimalData>();

    // Acción al pulsar botón de Adopción
    public void ProcesarAdopcion(AnimalData animalSeleccionado)
    {
        int puntaje = CompatibilityEvaluator.CalcularPuntaje(familiaActual, animalSeleccionado, infoOcultaRevelada);
        ResultadoCompatibilidad res = CompatibilityEvaluator.EvaluarResultado(puntaje);

        switch (res)
        {
            case ResultadoCompatibilidad.Compatible:
                if (infoOcultaRevelada)
                {
                    repManager.ModificarReputacion(+4); // Adopción excepcional
                }
                else
                {
                    repManager.ModificarReputacion(+3); // Adopción compatible
                }
                repManager.ModificarPH(+2);
                if (infoOcultaRevelada) repManager.ModificarPH(+1);

                // Quitar animal adoptado del pool
                poolAnimalesDisponibles.Remove(animalSeleccionado);
                MostrarFeedback("Buena decisión. El hogar es compatible con las necesidades del animal.");
                break;

            case ResultadoCompatibilidad.CompatibleConCondiciones:
                repManager.ModificarReputacion(+2);
                repManager.ModificarPH(+2);
                poolAnimalesDisponibles.Remove(animalSeleccionado);
                MostrarFeedback("La adopción puede funcionar, pero había una condición importante que debías considerar.");
                break;

            case ResultadoCompatibilidad.Incompatible:
                repManager.ModificarReputacion(-5); // Adopción con incompatibilidad importante
                repManager.ModificarPH(0);

                // Programar consecuencia diferida (Devolución tras 1-2 jornadas)
                consequenceManager.RegistrarConsecuencia(
                    jornadaActual, 
                    Random.Range(1, 3), 
                    -3, 
                    $"Devolución: La familia {familiaActual.nombreFamilia} devolvió al animal.", 
                    animalSeleccionado
                );

                MostrarFeedback("Esta adopción presenta una incompatibilidad importante.");
                break;
        }
    }

    // Acción al pulsar botón de Rechazar
    public void ProcesarRechazo(AnimalData animalComparado = null)
    {
        bool rechazoJustificado = false;

        if (animalComparado != null)
        {
            int puntaje = CompatibilityEvaluator.CalcularPuntaje(familiaActual, animalComparado, infoOcultaRevelada);
            if (CompatibilityEvaluator.EvaluarResultado(puntaje) == ResultadoCompatibilidad.Incompatible)
            {
                rechazoJustificado = true;
            }
        }

        if (rechazoJustificado)
        {
            repManager.ModificarReputacion(-1); // Rechazo justificado
            repManager.ModificarPH(+1);
            MostrarFeedback("Rechazo justificado. Las condiciones no permiten una adopción segura.");
        }
        else
        {
            repManager.ModificarReputacion(-3); // Rechazo injustificado
            repManager.ModificarPH(0);
            MostrarFeedback("El rechazo fue innecesario. La familia podía ofrecer un hogar adecuado.");
        }
    }

    private void MostrarFeedback(string mensaje)
    {
        Debug.Log($"[HUD Feedback]: {mensaje}");
    }
}