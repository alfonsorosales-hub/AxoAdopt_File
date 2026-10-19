using UnityEngine;

public class MovNPC : MonoBehaviour
{

    private UnityEngine.AI.NavMeshAgent agente;
    private SkinNPC skin;
    public Transform[] puntosRuta;

    public float cooldown = 3f;
    private float proximoMovimiento = 0f;
    private bool esperando = false;
    private int indiceActual = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agente = GetComponent<UnityEngine.AI.NavMeshAgent>();
        skin = GetComponent<SkinNPC>();

        if(puntosRuta.Length >0)
        {
            SiguienteDestino();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!agente.pathPending && agente.remainingDistance < 0.5f)
        {
            if (!esperando)
            {
                
                esperando = true;
                proximoMovimiento = Time.time + cooldown;
                if (skin != null) skin.CambiarSkin(1);
            }
            else if(Time.time >= proximoMovimiento)
            {
                esperando = false;
                if (skin != null) skin.CambiarSkin(0);
                SiguienteDestino();   
            }
        }
    }

    void SiguienteDestino()
    {
        if(puntosRuta.Length == 0) return;


        agente.destination = puntosRuta[indiceActual].position;

        indiceActual = (indiceActual + 1) % puntosRuta.Length;

    }
}
