using UnityEngine;
using System.Collections;

public class EnemyDummy : MonoBehaviour
{
    [Header("Estadísticas")]
    public int saludHarina = 3; // Harinazos necesarios para un K.O. sin patada
    public bool isCoughing = false;
    public float tiempoTos = 4f; // Cuántos segundos dura aturdido

    private Renderer render;
    private Color colorOriginal;

    private void Awake()
    {
        render = GetComponent<Renderer>();
        if (render != null)
        {
            colorOriginal = render.material.color;
        }
    }

    // --- FUNCIÓN 1: EL IMPACTO DEL PROYECTIL ---
    // Esta función la mandaremos llamar desde el jugador cuando le atinemos con la harina
    public void RecibirHarinazo()
    {
        saludHarina--;

        // Si su salud llega a cero a puros harinazos, cae seco
        if (saludHarina <= 0)
        {
            CaerNoqueado();
        }
        // Si no, y todavía no estaba tosiendo, lo aturdimos
        else if (!isCoughing)
        {
            StartCoroutine(EstadoVulnerable());
        }
    }

    // --- FUNCIÓN 2: LA PATADA DE LEON ---
    // Para el remate de frente o el sigilo por la espalda
    public void RecibirPatada()
    {
        CaerNoqueado();
    }

    // --- EL ESTADO VULNERABLE ---
    private IEnumerator EstadoVulnerable()
    {
        isCoughing = true;

        // Feedback visual: Se pone AMARILLO para avisarte que puedes patearlo
        if (render != null) render.material.color = Color.yellow;
        Debug.Log("¡Enemigo tosiendo! Vulnerable a remate.");

        yield return new WaitForSeconds(tiempoTos);

        // Si no lo pateaste a tiempo, se recupera
        isCoughing = false;
        if (render != null) render.material.color = colorOriginal;
        Debug.Log("El enemigo se recuperó de la tos.");
    }

    // --- EL K.O. DEFINITIVO ---
    private void CaerNoqueado()
    {
        StopAllCoroutines();
        isCoughing = false;

        // Feedback visual: Se pone GRIS de que ya fue
        if (render != null) render.material.color = Color.gray;

        // Lo acostamos 90 grados en el piso
        transform.rotation = Quaternion.Euler(90, 0, 0);

        // Desactivamos su caja de colisión para que no estorbe al caminar
        GetComponent<Collider>().enabled = false;

        // Apagamos este script para que ya no reciba daño
        this.enabled = false;

        Debug.Log("¡Enemigo Noqueado!");
    }
}