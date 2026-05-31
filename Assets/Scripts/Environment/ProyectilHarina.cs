using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ProyectilHarina : MonoBehaviour
{
    public float tiempoVida = 4f;

    [Header("Efectos Visuales")]
    public GameObject nubePolvoPrefab;
    public GameObject decalPrefab; // Aquí arrastrarás tu nuevo Prefab "DecalMancha"

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

        EnemyDummy enemigo = collision.gameObject.GetComponent<EnemyDummy>();
        if (enemigo != null)
        {
            enemigo.RecibirHarinazo();
        }

        // --- SISTEMA DE PROYECCIÓN DE CALCOMANÍAS ---
        if (decalPrefab != null)
        {
            // El proyector necesita nacer un poco separado para proyectar hacia adentro
            Vector3 posicionDecal = puntoImpacto + (direccionPared * 0.1f);

            // Lo rotamos para que apunte directamente a la superficie
            GameObject decal = Instantiate(decalPrefab, posicionDecal, Quaternion.LookRotation(-direccionPared));

            // Importante: Lo hacemos hijo para que se mueva con el enemigo o pared
            decal.transform.SetParent(collision.transform);
        }

        Destroy(gameObject);
    }
}