using UnityEngine;

public class NPCAdoptante : MonoBehaviour
{
    [SerializeField] private string idAdoptante;
    public string IdAdoptante => idAdoptante;

    // Lo llamas desde tu spawner al instanciar el NPC
    public void AsignarID(string id)
    {
        idAdoptante = id;
    }
}