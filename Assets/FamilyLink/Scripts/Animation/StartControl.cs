using UnityEngine;

public class StartControl : StateMachineBehaviour
{
    public string reactionId; // 인스펙터에 "drum" 등 입력

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 1. Any State 무한 루프 방지를 위해 들어오자마자 남아있는 트리거 리셋
        animator.ResetTrigger(reactionId);

        Avatar avatar = animator.GetComponent<Avatar>();
        if (avatar == null) return;

        // 2. 오브젝트 켜기
        avatar.ReactionObjShow(reactionId);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("isLoop", true);
        animator.GetComponent<Avatar>().PlaySound(reactionId);
    }
}