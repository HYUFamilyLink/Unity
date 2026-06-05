using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ItemEquipper : MonoBehaviour
{

    // XR Interaction Toolkit의 Select Entered 이벤트에서 호출될 함수
    public void OnItemGrabbed(SelectEnterEventArgs args)
    {
        // args.interactorObject가 바로 나를 집어든 '손(컨트롤러)'입니다!
        HandTrigger hand = args.interactorObject.transform.GetComponentInParent<HandTrigger>();
        
        if (hand != null)
        {
            // 그 손의 HandTrigger에 문자열 ID를 전달
            hand.Objselect(gameObject.tag);
        }
    }
}