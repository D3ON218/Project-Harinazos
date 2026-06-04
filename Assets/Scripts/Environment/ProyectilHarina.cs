using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ProyectilHarina : MonoBehaviour
{
    public float tiempoVida = 4f;

    [Header("Efectos Visuales")]
    public GameObject nubePolvoPrefab;
    public GameObject decalPrefab;

    private void Start()
    {
        Destroy(gameObject, tiempoVida);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contacto = collision.GetContact(0);
        Vector3 puntoImpacto = contacto.point;
        Vector3 direccionPared = contacto.normal;

        if (nubePolvoPrefab != null)
        {
            Instantiate(nubePolvoPrefab, puntoImpacto, Quaternion.identity);
        }

        // Si le pega a un enemigo...
        EnemyDummy enemigo = collision.gameObject.GetComponent<EnemyDummy>();
        if (enemigo != null)
        {
            enemigo.RecibirHarinazo();
        }
        // CONEXIÓN AL NUEVO SISTEMA DE SALUD: Si el proyectil le pega a tu personaje...
        else if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.MancharTraje(10f);
            }
        }

        if (decalPrefab != null)
        {
            Vector3 posicionDecal = puntoImpacto + (direccionPared * 0.1f);
            GameObject decal = Instantiate(decalPrefab, posicionDecal, Quaternion.LookRotation(-direccionPared));
            decal.transform.SetParent(collision.transform);
        }

        Destroy(gameObject);
    }
}