using UnityEngine;
using UnityEngine.SceneManagement; 

public class IntroManager : MonoBehaviour
{
    public float velocidad = 20f;
    public string nombreEscenaSiguiente = "Player"; 

    void Update()
    {
        transform.position += Vector3.up * velocidad * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            CambiarEscena();
        }

        if (transform.position.y > 800f)
        {
            CambiarEscena();
        }
    }

    void CambiarEscena()
    {
        SceneManager.LoadScene(nombreEscenaSiguiente);
    }
}