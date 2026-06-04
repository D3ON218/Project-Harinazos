using UnityEngine;
using UnityEngine.SceneManagement;

public class ZonaVictoria : MonoBehaviour
{
    // Asegúrate de que tu carro tenga el tag "Player" o el tag que uses para identificarlo
    public string tagDelJugador = "Player";

    private void OnTriggerEnter(Collider other)
    {
        // Si lo que toca la zona tiene el tag del jugador...
        if (other.CompareTag(tagDelJugador))
        {
            // ...cargamos la escena final
            SceneManager.LoadScene("Final");
        }
    }
}