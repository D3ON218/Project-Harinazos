using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("ENTRO AL CARRO: " + other.name);

        if(other.CompareTag("Player"))
        {
            SceneManager.LoadScene("Final");
        }
    }
}