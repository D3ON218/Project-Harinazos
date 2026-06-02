using UnityEngine;

public class SelectorAleatorioBaile : StateMachineBehaviour
{
    // Cambiamos a OnStateEnter: Se ejecuta en cuanto el enemigo regresa o arranca en Idle
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Dejamos el número del siguiente baile ya calculado y listo en la recámara
        int baileAleatorio = Random.Range(0, 3);

        // Se lo asignamos al parámetro del Animator inmediatamente
        animator.SetInteger("DanceID", baileAleatorio);

        Debug.Log("Próximo baile preparado en la recámara. ID seleccionado: " + baileAleatorio);
    }
}