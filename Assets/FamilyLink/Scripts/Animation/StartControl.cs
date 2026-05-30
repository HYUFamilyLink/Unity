using UnityEngine;

public class StartControl : StateMachineBehaviour
{
    public string reactionName; // 인스펙터에 "drum" 등 입력

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 1. Any State 무한 루프 방지를 위해 들어오자마자 남아있는 트리거 리셋
        animator.ResetTrigger(reactionName);

        Avatar avatar = animator.GetComponent<Avatar>();
        if (avatar == null) return;

        // 2. 오브젝트 켜기
        if (reactionName == "tambourine" && avatar.tambourine != null)
            avatar.tambourine.SetActive(true);
        else if (reactionName == "drum" && avatar.drum != null)
            avatar.drum.SetActive(true);
    }
}