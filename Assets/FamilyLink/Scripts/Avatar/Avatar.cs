using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using FamilyLink;
using NUnit.Framework.Constraints;
using UniGLTF.SpringBoneJobs;
using Unity.VisualScripting;
using UnityEngine;
using UniVRM10;

//아바타 고유 정보 등을 처리 및 보유하는 스크립트
public class Avatar : MonoBehaviour
{
    public string id;
    public uint agoraUid;
    public string role = "phone";
    public void SetID(string _id) {id = _id; return;}

    private bool isMyAvatar = false;
    public Transform head;

    [Header("Animation Setting")]
    private Coroutine animeRoutine;
    private Animator animator => gameObject.GetComponent<Animator>();
    public float animeTime;
    private string nowAnime = "";

    [Header("Reaction Objects")]
    public GameObject tambourine;
    public GameObject drumStick;

    public void SetMine()
    {
        isMyAvatar = true;
        AgoraManager.agoraManager.SetAttenuation(agoraUid, 0f);
    }

    private uint GetAgoraUid(string strId)
    {
        if (string.IsNullOrEmpty(strId)) return 0;

        // 1. 초기값 설정 (JS와 동일하게 5381)
        uint hash = 5381;

        // 2. 문자열을 순회하며 해시 계산
        foreach (char c in strId)
        {
            // 3. 산술 오버플로우 발생 시 자동으로 하위 비트만 남기도록 unchecked 사용
            // (JS의 비트 연산과 동일한 효과를 냄)
            unchecked
            {
                // (hash << 5) + hash 는 수학적으로 hash * 33 과 동일함
                hash = ((hash << 5) + hash) + (uint)c;
            }
        }

        return hash;
    }
    void Start()
    {
        string networkId = gameObject.GetComponent<AvatarSync>().NetworkId.ToString().Split('.')[0];
        var room = AvatarManager.avatarManager.spawnManager.roomClient.Room;

        foreach(var entry in room)
        {
            if (entry.Key.Contains(networkId))
            {
                var data = JsonUtility.FromJson<ExtendedSpawnMessage>(entry.Value);
                if(!string.IsNullOrEmpty(data.targetSocketId))
                {
                    id = data.targetSocketId;
                    AvatarManager.avatarManager.ReigsterAvatar(id, this);
                }
                break;
            }
        }
        if(role == "phone") AvatarManager.avatarManager.SetWebSync(this);
        else gameObject.GetComponent<Animator>().enabled = false;
        agoraUid = GetAgoraUid(id);
        AgoraManager.agoraManager.SetAttenuation(agoraUid, 0.3f);
    }


    void Update() {
        if (!isMyAvatar)
        {
            // 아래 화살표 누르면 blink 표정 적용
            if (Input.GetKeyDown(KeyCode.DownArrow)) {
                Debug.Log("asdfsafd");
                var vrm10 = GetComponent<Vrm10Instance>(); // [cite: 99, 113]
                if (vrm10 != null) {
                    // UI가 없어도 코드로는 강제 실행됩니다
                    vrm10.Runtime.Expression.SetWeight(ExpressionKey.Blink, 1.0f); // [cite: 111, 128]
                    Debug.Log("표정 강제 변경 완료!");
                }
            }
            if(AgoraManager.agoraManager != null && agoraUid != 0)
            {
                AgoraManager.agoraManager.UpdateRemotePosition(
                    agoraUid,
                    head.position,
                    head.forward
                );
            }
        }
        else
        {
            if(AgoraManager.agoraManager != null && agoraUid != 0)
            {
                AgoraManager.agoraManager.UpdateSelfPosition(
                    head.position,
                    head.forward,
                    head.right,
                    head.up
                );
            }
        }
    }

    public void PlayReaction(string reactionId)
    {
        Debug.Log(id + "가 리액션 :" + reactionId);

        HideAllReactionObj();

        switch (reactionId)
        {
            case "tambourine":
                if(tambourine != null) tambourine.SetActive(true);
                break;
            case "kick":
                break;
            case "drum":
                if(drumStick != null) drumStick.SetActive(true);
                break;
            case "clap":
                break;
        }

        PlayAnimation(reactionId);

        //리액션에 따른 활성화와 애니매이션 작동 코드

        //(현재 재생중인 것과 다를 경우)
        //기존 애니메이션 중단
        //오브젝트 비활성화

        //대상 오브젝트 활성화
        //애니메이션 n초간 재생
    }

    public void PlayAnimation(string reactionId)
    {

        if(animeRoutine != null)
        {
            StopCoroutine(animeRoutine);
        }

        if(nowAnime != "" && nowAnime != reactionId)
        {
            if(animator != null && animator.enabled)
            {
                animator.SetBool(reactionId, false);
            }
        }

        animeRoutine = StartCoroutine(AnimeTimerRoutine(reactionId));
    }

    private IEnumerator AnimeTimerRoutine(string reactionId)
    {
        nowAnime = reactionId;

        if(animator != null && animator.enabled)
        {
            animator.SetBool(reactionId, true);
        }

        yield return new WaitForSeconds(animeTime);

        if(animator != null && animator.enabled)
        {
            animator.SetBool(reactionId, false);
        }

        HideAllReactionObj();
        animeRoutine = null;
        nowAnime = "";
    }

    private void HideAllReactionObj()
    {
        if(tambourine != null) tambourine.SetActive(false);
        if(drumStick != null) drumStick.SetActive(false);
    }
}