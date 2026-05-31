using UnityEngine;

public class SacoHarina : MonoBehaviour
{
    // Esta función la mandará llamar el jugador al presionar la tecla E
    public void Recoger(PlayerCombat combate)
    {
        int tirosExtra = Random.Range(3, 6);

        combate.AgregarMunicion(tirosExtra);
        Debug.Log("¡Interactuaste con el saco! Tiros ganados: " + tirosExtra);

        Destroy(gameObject); // Desaparece el saco
    }
}