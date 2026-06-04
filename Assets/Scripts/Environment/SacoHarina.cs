using UnityEngine;

public class SacoHarina : MonoBehaviour
{
    public void Recoger(PlayerCombat combate)
    {
        int tirosExtra = Random.Range(3, 6);

        combate.AgregarMunicion(tirosExtra);

        Destroy(gameObject); 
    }
}