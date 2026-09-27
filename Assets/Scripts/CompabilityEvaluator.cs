using UnityEngine;

public static class CompatibilityEvaluator
{
    public static int CalcularPuntaje(FamiliaData familia, AnimalData animal, bool infoOcultaDescubierta)
    {
        float puntaje = 0f;

        // 1. Tiempo Disponible (25%)
        float factorTiempo = 1f;
        if (familia.tiempoDisponibleVal < animal.tiempoRequerido)
            factorTiempo = 0.2f;
        else if (familia.tiempoDisponibleVal == animal.tiempoRequerido)
            factorTiempo = 0.8f;
        puntaje += factorTiempo * 25f;

        // 2. Entorno (25%)
        float factorEntorno = 1f;
        if (animal.requiereEspacioAmplio && !familia.tieneEspacioAmplio)
            factorEntorno = 0.1f;
        if (animal.sensibleAlRuido && familia.esHogarRuidoso)
            factorEntorno *= 0.3f;
        puntaje += factorEntorno * 25f;

        // 3. Experiencia (15%)
        float factorExp = 1f;
        if (animal.requiereExperienciaPrevia && familia.experienciaVal == 0)
            factorExp = 0.2f;
        puntaje += factorExp * 15f;

        // 4. Estilo de Vida y Actividad (15%)
        float factorEstilo = 0.85f;
        puntaje += factorEstilo * 15f;

        // 5. Motivación / Expectativas (10%)
        float factorMotivacion = familia.ignoraCuidadosReales ? 0.2f : 1.0f;
        puntaje += factorMotivacion * 10f;

        // 6. Información Oculta Descubierta (10%)
        float factorOculto = infoOcultaDescubierta ? 1.0f : 0.0f;
        puntaje += factorOculto * 10f;

        // Regla de penalización severa por necesidad crítica
        if (animal.requiereEspacioAmplio && !familia.tieneEspacioAmplio) return Mathf.Min((int)puntaje, 40);
        if (animal.sensibleAlRuido && familia.esHogarRuidoso) return Mathf.Min((int)puntaje, 45);

        return Mathf.Clamp((int)puntaje, 0, 100);
    }

    public static ResultadoCompatibilidad EvaluarResultado(int puntaje)
    {
        if (puntaje >= 80) return ResultadoCompatibilidad.Compatible;
        if (puntaje >= 60) return ResultadoCompatibilidad.CompatibleConCondiciones;
        return ResultadoCompatibilidad.Incompatible;
    }
}

public enum ResultadoCompatibilidad
{
    Compatible,
    CompatibleConCondiciones,
    Incompatible
}