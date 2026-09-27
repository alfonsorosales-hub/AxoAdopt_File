using UnityEngine;

public class ReputationAndSkillManager : MonoBehaviour
{
    [Header("Estado del Centro")]
    public int reputacion = 50; // Inicia en 50/100
    public int puntosHabilidad = 0;

    [Header("Niveles de Habilidad Permanentes")]
    public int nivelInvestigacion = 0; // Max 2
    public int nivelEmpatia = 0;       // Max 2
    public int nivelPersuasion = 0;    // Max 2

    private int mejorasCompradasEstaSemana = 0;

    public void ModificarReputacion(int cambio)
    {
        reputacion = Mathf.Clamp(reputacion + cambio, 0, 100);
        Debug.Log($"[AXO-ADOPT] Reputación actual: {reputacion}");

        if (reputacion <= 0)
        {
            Debug.LogError("[AXO-ADOPT] DESPIDO: La reputación llegó a 0. Samy ha sido despedido.");
            // Invocar pantalla de derrota
        }
    }

    public void ModificarPH(int cambio)
    {
        puntosHabilidad = Mathf.Max(0, puntosHabilidad + cambio);
        Debug.Log($"[AXO-ADOPT] PH actuales: {puntosHabilidad}");
    }

    public bool ComprarMejora(TipoHabilidad habilidad)
    {
        if (mejorasCompradasEstaSemana >= 1)
        {
            Debug.LogWarning("Regla de balance: Máximo 1 mejora por semana.");
            return false;
        }

        switch (habilidad)
        {
            case TipoHabilidad.Investigacion:
                if (nivelInvestigacion == 0 && puntosHabilidad >= 5)
                {
                    puntosHabilidad -= 5; nivelInvestigacion = 1; finalizarCompra(); return true;
                }
                if (nivelInvestigacion == 1 && puntosHabilidad >= 8)
                {
                    puntosHabilidad -= 8; nivelInvestigacion = 2; finalizarCompra(); return true;
                }
                break;

            case TipoHabilidad.Empatia:
                if (nivelEmpatia == 0 && puntosHabilidad >= 5)
                {
                    puntosHabilidad -= 5; nivelEmpatia = 1; finalizarCompra(); return true;
                }
                if (nivelEmpatia == 1 && puntosHabilidad >= 8)
                {
                    puntosHabilidad -= 8; nivelEmpatia = 2; finalizarCompra(); return true;
                }
                break;

            case TipoHabilidad.Persuasion:
                if (nivelPersuasion == 0 && puntosHabilidad >= 5)
                {
                    puntosHabilidad -= 5; nivelPersuasion = 1; finalizarCompra(); return true;
                }
                if (nivelPersuasion == 1 && puntosHabilidad >= 8)
                {
                    puntosHabilidad -= 8; nivelPersuasion = 2; finalizarCompra(); return true;
                }
                break;
        }

        return false;
    }

    private void finalizarCompra()
    {
        mejorasCompradasEstaSemana++;
    }

    public void ReiniciarConteoSemanal()
    {
        mejorasCompradasEstaSemana = 0;
    }

    public bool TieneHabilidad(TipoHabilidad habilidad, int nivelRequerido)
    {
        return habilidad switch
        {
            TipoHabilidad.Investigacion => nivelInvestigacion >= nivelRequerido,
            TipoHabilidad.Empatia => nivelEmpatia >= nivelRequerido,
            TipoHabilidad.Persuasion => nivelPersuasion >= nivelRequerido,
            _ => true
        };
    }
}