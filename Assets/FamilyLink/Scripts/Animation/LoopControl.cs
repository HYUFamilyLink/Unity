using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoopControl : StateMachineBehaviour
{
    int counterStart, counterEnd;
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        counterStart = animator.GetComponent<Avatar>().counter;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        counterEnd = animator.GetComponent<Avatar>().counter;
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(counterStart == counterEnd) animator.SetBool("isLoop", false);
    }
}
