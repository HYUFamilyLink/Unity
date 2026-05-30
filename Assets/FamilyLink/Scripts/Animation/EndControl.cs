using Unity.VisualScripting;
using UnityEngine;

public class EndControl : StateMachineBehaviour
{
    public string reactionName; // 인스펙터에 "drum" 등 입력

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Avatar avatar = animator.GetComponent<Avatar>();
        if (avatar == null) return;

        // 1. 오브젝트 끄기
        if (reactionName == "tambourine" && avatar.tambourine != null)
            avatar.tambourine.SetActive(false);
        else if (reactionName == "drum" && avatar.drum != null)
            avatar.drum.SetActive(false);

        // 2. ★ 핵심: 아바타 잠금 해제 ★
        // 애니메이션이 무사히 다 끝났으므로, 다시 트리거를 받을 수 있게 비워줍니다.
        if (avatar.currentReaction == reactionName)
        {
            avatar.currentReaction = "";
            avatar.counter = 0;
        }
    }
}