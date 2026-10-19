using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject MenuPausaUI;
    private bool estaPausado = false;
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (estaPausado)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    public void Reanudar()
    {
        Debug.Log("Reanudar llamado");
        MenuPausaUI.SetActive(false);
        Time.timeScale = 1f;
        estaPausado = false;
    }

    public void Pausar()
    {
        Debug.Log("Pausar llamado");
        MenuPausaUI.SetActive(true);
        Time.timeScale = 0f;
        estaPausado = true;
    }

    public void QuitGame()
    {
        Debug.Log("Quitar juego llamado");
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }
}
