using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

public class FinalManager : MonoBehaviour
{
    // Asegúrate de que el nombre aquí sea EXACTAMENTE igual al nombre de tu escena en el proyecto
    public string nombreMenu = "MenuPrincipal";

    void Update()
    {
        // Detectamos si el jugador presiona ESPACIO
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RegresarAlMenu();
        }
    }

    void RegresarAlMenu()
    {
        SceneManager.LoadScene(nombreMenu);
    }
}