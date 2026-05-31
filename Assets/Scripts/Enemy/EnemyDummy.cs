using UnityEngine;
using System.Collections;

public class EnemyDummy : MonoBehaviour
{
    [Header("Estadísticas")]
    public int saludHarina = 3;
    public bool isCoughing = false;
    public float tiempoTos = 4f;

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

    public void RecibirHarinazo()
    {
        saludHarina--;

        if (saludHarina <= 0)
        {
            CaerNoqueado();
        }
        else if (!isCoughing)
        {
            StartCoroutine(EstadoVulnerable());
        }
    }

    public void RecibirPatada()
    {
        CaerNoqueado();
    }

    private IEnumerator EstadoVulnerable()
    {
        isCoughing = true;
        if (render != null) render.material.color = Color.yellow;
        Debug.Log("¡Enemigo tosiendo! Vulnerable a remate.");

        // NUEVO: Mientras tose, le apagamos su IA para que deje de dispararte y caminar
        EnemigoLanzador cerebro = GetComponent<EnemigoLanzador>();
        if (cerebro != null) cerebro.enabled = false;

        yield return new WaitForSeconds(tiempoTos);

        // Si sobrevive a la tos, le devolvemos su color y le volvemos a encender la IA
        isCoughing = false;
        if (render != null) render.material.color = colorOriginal;

        if (cerebro != null) cerebro.enabled = true;

        Debug.Log("El enemigo se recuperó de la tos.");
    }

    private void CaerNoqueado()
    {
        StopAllCoroutines();
        isCoughing = false;

        // Feedback visual: Se pone GRIS de que ya fue
        if (render != null) render.material.color = Color.gray;

        // Lo acostamos y lo pegamos al piso
        transform.rotation = Quaternion.Euler(90f, transform.rotation.eulerAngles.y, 0f);
        transform.position = new Vector3(transform.position.x, 0.5f, transform.position.z);

        // --- EL ARREGLO PARA QUE NO CAIGA AL VACÍO ---
        // 1. Le apagamos la física para que no se hunda en el piso
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero; // Lo frenamos en seco
            rb.isKinematic = true;      // Le quitamos la gravedad
        }

        // 2. Lo hacemos "fantasma" (Trigger) para que lo puedas atravesar sin tropezarte
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        // ----------------------------------------------

        // APAGADO TOTAL DE IA
        EnemigoLanzador cerebroLanzador = GetComponent<EnemigoLanzador>();
        if (cerebroLanzador != null)
        {
            cerebroLanzador.enabled = false;
        }

        // Apagamos este script
        this.enabled = false;

        Debug.Log("¡Enemigo Noqueado, tirado en la calle!");
    }
}