using UnityEngine;

public class NPCController : MonoBehaviour
{
     public NPCIdentity miIdentidad;

    public void Interactuar()
    {
        RelacionType relacion = ReputacionManager.Instance.ObtenerTipoRelacion(miIdentidad);

        switch (relacion)
        {
            case RelacionType.Aliado:
                DispararDialogo("¡Qué alegría verte de nuevo, amigo!");
                OfrecerDescuento();
                break;
            case RelacionType.Enemigo:
                DispararDialogo("Lárgate de aquí. No eres bienvenido.");
                AtacarOIgnorar();
                break;
            case RelacionType.Neutral:
                DispararDialogo("Hola viajero. ¿Qué necesitas?");
                break;
        }
    }

    // Ejemplo de cómo una acción del jugador afecta a este NPC específico
    public void AyudarA_NPC()
    {
        ReputacionManager.Instance.ModificarReputacion(miIdentidad, 15);
    }

    private void DispararDialogo(string texto) => Debug.Log($"{miIdentidad.nombreNPC}: {texto}");
    private void OfrecerDescuento() { }
    private void AtacarOIgnorar() { }
   
}
