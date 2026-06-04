using UnityEngine;

public class SelectorAleatorioBaile : StateMachineBehaviour
{
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int baileAleatorio = Random.Range(0, 3);
        animator.SetInteger("DanceID", baileAleatorio);
    }
}