using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Salud (Limpieza del Traje)")]
    public float limpiezaMaxima = 100f;
    public float limpiezaActual = 100f;

    [Header("Efectos Visuales (Manchas)")]
    public GameObject manchaTrajePrefab;

    public void MancharTraje(float danio = 10f)
    {
        limpiezaActual -= danio;

        if (limpiezaActual <= 0)
        {
            limpiezaActual = 0;
            Debug.Log("NOOOOOOOOOOOO MI TRAJE");
        }

        if (manchaTrajePrefab != null)
        {
            Vector3 offsetAleatorio = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.8f, 1.4f), Random.Range(-0.2f, 0.2f));
            GameObject nuevaMancha = Instantiate(manchaTrajePrefab, transform.position + offsetAleatorio, transform.rotation, transform);
            nuevaMancha.transform.Rotate(0, 0, Random.Range(0f, 360f));
        }
    }
}