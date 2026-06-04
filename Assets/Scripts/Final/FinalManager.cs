using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalManager : MonoBehaviour
{
    void Update()
    {
        if (Input.anyKeyDown)
        {
            SceneManager.LoadScene("MenuPrincipal");
        }
    }
}