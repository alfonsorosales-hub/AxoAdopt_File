using UnityEngine;
using TMPro;

public class AlternarObjeto : MonoBehaviour
{
    [SerializeField] private GameObject objetivo;
    [SerializeField] private TextMeshProUGUI textoBoton;

    [SerializeField] private string textoParaActivar = "Analizar Documento";
    [SerializeField] private string textoParaDesactivar = "Guardar Documento";

    private void Start()
    {
        ActualizarTexto();
    }

    public void Alternar()
    {
        objetivo.SetActive(!objetivo.activeSelf);
        ActualizarTexto();
    }

    private void ActualizarTexto()
    {
        if (textoBoton == null) return;

        // Imagen visible -> el botón sirve para guardar. Imagen oculta -> sirve para analizar.
        textoBoton.text = objetivo.activeSelf ? textoParaDesactivar : textoParaActivar;
    }
}