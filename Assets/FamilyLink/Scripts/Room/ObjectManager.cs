using System.Collections;
using System.Collections.Generic;
using FamilyLink;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

//마이크의 위치, 넘기기 버튼 제어 등등...
//Avatar매니저와 같은 곳에 들어가므로 파괴 로직 생략
public class ObjectManager : MonoBehaviour
{
    public static ObjectManager objectManager;

    [Header("유저 정보")]
    private NetworkUser currentUser;

    [Header("제어 대상 오브젝트")]
    public GameObject Search;
    public Button skipButton;
    public Mic mic;

    void Awake()
    {
        if(objectManager == null) objectManager = this;
        currentUser = SessionManager.sessionManager.currentUser;
        SocketManager.socketManager.OnTurnChanged += SetTurn;
        StartCoroutine(SetupRoutine());
    }

    IEnumerator SetupRoutine()
    {
        string currentTurnId = SessionManager.sessionManager.currentTurnId;

        yield return new WaitUntil(() => 
            AvatarManager.avatarManager.userDict.ContainsKey(currentTurnId) &&
            AvatarManager.avatarManager.userDict[currentTurnId] != null);
        SetTurn(currentTurnId);
    }

    //턴 요소 처리
    public void SetTurn(string uid)
    {
        bool isTurn = currentUser.id == uid;

        skipButton.gameObject.SetActive(isTurn);
        Search.gameObject.SetActive(isTurn);

        //mic.GetComponent<ObjSync>().SetOwner(isTurn);
        var target = AvatarManager.avatarManager.userDict[uid];
        if (isTurn) mic.traceTarget = target.GetComponent<AvatarSync>().xrRight;
        else mic.traceTarget = target.transform.Find("AvatarRoot/Hips/Spine/Chest/Right_Shoulder/Right_UpperArm/Right_ForeArm/RightHand/RightHandIndex1/RightHandIndex2");
    }
}