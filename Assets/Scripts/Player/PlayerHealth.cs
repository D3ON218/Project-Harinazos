using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Salud (Limpieza del Traje)")]
    public float limpiezaMaxima = 100f;
    public float limpiezaActual = 100f;

    [Header("Efectos Visuales (Manchas)")]
    public GameObject manchaTrajePrefab;

    [Header("Conexión Game Over")]
    public Animator animator;
    public PlayerCombat playerCombat;
    public PlayerController playerController;
    public UIManager uiManager;

    private bool estaMuerto = false;

    public void MancharTraje(float danio = 10f)
    {
        if (estaMuerto) return; // Si ya moriste, ya no te manchan más

        limpiezaActual -= danio;

        if (limpiezaActual <= 0)
        {
            limpiezaActual = 0;
            Morir();
        }

        if (manchaTrajePrefab != null && !estaMuerto)
        {
            Vector3 offsetAleatorio = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.8f, 1.4f), Random.Range(-0.2f, 0.2f));
            GameObject nuevaMancha = Instantiate(manchaTrajePrefab, transform.position + offsetAleatorio, transform.rotation, transform);
            nuevaMancha.transform.Rotate(0, 0, Random.Range(0f, 360f));
        }
    }

    private void Morir()
    {
        estaMuerto = true;

        // 1. Desactivamos los controles
        if (playerCombat != null) playerCombat.enabled = false;
        if (playerController != null) playerController.enabled = false;

        // 2. Disparamos la animación tipo Master Chief
        if (animator != null) animator.SetTrigger("Die");

        // 3. Le avisamos a la UI que inicie la secuencia de derrota
        if (uiManager != null) uiManager.MostrarGameOver();
    }
}