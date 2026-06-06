using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ItemEquipper : MonoBehaviour
{
    private MeshRenderer[] renderers;
    void Start()
    {
        // 시작할 때 자기 자신과 자식 오브젝트들에 있는 모든 렌더러를 찾아둡니다.
        renderers = GetComponentsInChildren<MeshRenderer>();
    }
    // XR Interaction Toolkit의 Select Entered 이벤트에서 호출될 함수
    public void OnItemGrabbed(SelectEnterEventArgs args)
    {
        // args.interactorObject가 바로 나를 집어든 '손(컨트롤러)'입니다!
        HandTrigger hand = args.interactorObject.transform.GetComponentInParent<HandTrigger>();
        
        if (hand != null)
        {
            // 그 손의 HandTrigger에 문자열 ID를 전달
            hand.Objselect(gameObject.tag);
            SetRender(false);
        }
    }

    public void OnItemDropped(SelectExitEventArgs args)
    {
        HandTrigger hand = args.interactorObject.transform.GetComponentInParent<HandTrigger>();
        if (hand != null)
        {
            hand.DeselectAll();
            SetRender(true);
        }
    }

    void SetRender(bool state)
    {
        if(renderers == null) return;
         gameObject.GetComponent<Collider>().enabled = state;
        foreach(var r in renderers) r.enabled = state;
    }
}