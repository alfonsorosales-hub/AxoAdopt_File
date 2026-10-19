using UnityEngine;

public class SkinNPC : MonoBehaviour
{
     public GameObject[] modelos;   // un modelo 3D por cada skin
    public int skinInicial = 0;

    void Start()
    {
        CambiarSkin(skinInicial);
    }

    public void CambiarSkin(int indice)
    {
        if (indice < 0 || indice >= modelos.Length) return;

        for (int i = 0; i < modelos.Length; i++)
        {
            modelos[i].SetActive(i == indice);
        }
    }
}
