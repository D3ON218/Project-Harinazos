using UnityEngine;

public class MusicaPersistente : MonoBehaviour
{
    private void Awake()
    {
        // Buscamos si ya existe una música en la escena anterior
        GameObject[] musicas = GameObject.FindGameObjectsWithTag("Musica");

        // Si ya hay música sonando de otra escena, destruimos esta para no duplicarla
        if (musicas.Length > 1)
        {
            Destroy(this.gameObject);
        }
        else
        {
            // Si es la primera vez, marcamos este objeto para que NO se destruya al cambiar de escena
            DontDestroyOnLoad(this.gameObject);
        }
    }
}