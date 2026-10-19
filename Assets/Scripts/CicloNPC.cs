using System.Collections;
using UnityEngine;

public class CicloNPC : MonoBehaviour
{
      [Header("Configuración de Skins (Modelos Hijos)")]
    [Tooltip("Arrastra aquí los diferentes modelos 3D que están dentro del NPC")]
    public GameObject[] skins;
    private int indiceSkinActual = -1;

    [Header("Configuración de Movimiento")]
    [Tooltip("Objeto vacío en la escena que marca hacia dónde debe ir el NPC")]
    public Transform puntoDestino;
    public float velocidad = 3.5f;
    public float VelocidadRotacion = 10f;
    public float distanciaLlegada = 0.05f;

    private Vector3 posicionInicial;
    private bool eventoTerminado = false;

    void Start()
    {
        // Guardamos la posición exacta donde inicia el NPC
        posicionInicial = transform.position;

        // Iniciamos el ciclo secuencial infinito
        if (puntoDestino != null)
        {
            StartCoroutine(RutinaPrincipalNPC());
        }
        else
        {
            Debug.LogError("Falta asignar el Punto Destino en el inspector del NPC.");
        }
    }

    IEnumerator RutinaPrincipalNPC()
    {
        // Bucle infinito para que el ciclo se repita siempre
        while (true)
        {
            // PASO 1: Cambiar la skin o modelado por otro
            CambiarSkin();

            // PASO 2: Desplazarse hasta el punto destino
            yield return StartCoroutine(MoverHacia(puntoDestino.position));

            // PASO 3: Permanecer allí hasta que el evento termine
            eventoTerminado = false;
            Debug.Log("NPC en posición. Esperando a que finalice el evento...");
            yield return new WaitUntil(() => eventoTerminado);

            // PASO 4: Regresar a la posición inicial
            Debug.Log("Evento finalizado. Regresando al punto de origen...");
            yield return StartCoroutine(MoverHacia(posicionInicial));

            // Pequeña pausa opcional antes de volver a cambiar de skin y repetir
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator MoverHacia(Vector3 destino)
    {
        while (Vector3.Distance(transform.position, destino) > distanciaLlegada)
        {
            // Rotar suavemente hacia la dirección de movimiento (solo en el eje horizontal)
            Vector3 direccion = (destino - transform.position).normalized;
            direccion.y = 0; 
            if (direccion != Vector3.zero)
            {
                Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * VelocidadRotacion);
            }

            // Desplazar el NPC hacia el objetivo
            transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);
            
            yield return null; // Esperar al siguiente frame
        }

        // Ajustar a la posición exacta al llegar
        transform.position = destino;
    }

    void CambiarSkin()
    {
        if (skins == null || skins.Length == 0) return;

        // Avanza al siguiente modelo de la lista (0, 1, 2... y vuelve a 0)
        indiceSkinActual = (indiceSkinActual + 1) % skins.Length;

        // Activa solo la skin actual y desactiva todas las demás
        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i] != null)
            {
                skins[i].SetActive(i == indiceSkinActual);
            }
        }
    }

    // --- MÉTODO PÚBLICO PARA ACTIVAR EL REGRESO ---
    // Llama a esta función desde otro script, un botón de UI o un Trigger cuando tu evento acabe.
    [ContextMenu("Simular Fin del Evento")]
    public void NotificarFinDeEvento()
    {
        eventoTerminado = true;
    }
}