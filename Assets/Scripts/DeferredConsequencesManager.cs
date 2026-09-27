using System.Collections.Generic;
using UnityEngine;

public class DeferredConsequencesManager : MonoBehaviour
{
    public ReputationAndSkillManager repManager;

    public struct ConsecuenciaPendiente
    {
        public int jornadaEjecucion;
        public int cambioReputacion;
        public string mensajeFeedback;
        public AnimalData animalADevolver; // Null si no aplica
    }

    private List<ConsecuenciaPendiente> colaConsecuencias = new List<ConsecuenciaPendiente>();

    public void RegistrarConsecuencia(int jornadaActual, int retrasoJornadas, int cambioRep, string mensaje, AnimalData animal = null)
    {
        ConsecuenciaPendiente nueva = new ConsecuenciaPendiente
        {
            jornadaEjecucion = jornadaActual + retrasoJornadas,
            cambioReputacion = cambioRep,
            mensajeFeedback = mensaje,
            animalADevolver = animal
        };
        colaConsecuencias.Add(nueva);
    }

    public void ProcesarJornada(int jornadaActual, List<AnimalData> poolAnimalesActivo)
    {
        for (int i = colaConsecuencias.Count - 1; i >= 0; i--)
        {
            if (colaConsecuencias[i].jornadaEjecucion == jornadaActual)
            {
                var cons = colaConsecuencias[i];
                repManager.ModificarReputacion(cons.cambioReputacion);
                
                if (cons.animalADevolver != null && !poolAnimalesActivo.Contains(cons.animalADevolver))
                {
                    poolAnimalesActivo.Add(cons.animalADevolver);
                    Debug.Log($"[Devolución] El animal {cons.animalADevolver.nombreEspecie} regresó al centro.");
                }

                Debug.Log($"[Consecuencia Diferida]: {cons.mensajeFeedback}");
                colaConsecuencias.RemoveAt(i);
            }
        }
    }
}