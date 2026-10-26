using UnityEngine;

public class NPCAdoptante : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
      [SerializeField] private string idAdoptante;
    public string IdAdoptante => idAdoptante;

    // Lo llamas desde tu spawner al instanciar el NPC
    public void AsignarID(string id)
    {
        idAdoptante = id;
    }
}