using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using FamilyLink;
using NUnit.Framework.Constraints;
using Org.BouncyCastle.Crypto.Agreement.JPake;
using UniGLTF.SpringBoneJobs;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Scripting;
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

    private Animator animator => gameObject.GetComponent<Animator>();
    public string currentReaction = "";
    public int counter;
    Coroutine hideTimer;
    public float objShowTime = 1f;

    [Header("Reaction Objects")]
    AudioSource audioSource;
    public bool isSinging = false;
    public GameObject tambourine;
    public GameObject drum;
    public GameObject bell;

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
        audioSource = this.GetComponent<AudioSource>();
        if(audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

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
        if(role == "phone")
        {
            AvatarManager.avatarManager.SetWebSync(this);
            gameObject.GetComponent<RigBuilder>().enabled = false;
        }
        else gameObject.GetComponent<Animator>().SetBool("isVR",true);
        agoraUid = GetAgoraUid(id);
        AgoraManager.agoraManager.SetAttenuation(agoraUid, 0.3f);
    }

    void Update() {
        if (!isMyAvatar)
        {
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

    //웹앱 유저용
    public void PlayReaction(string reactionId)
    {
        if(isSinging)
        {
            PlaySound(reactionId);
            return;
        }

        Debug.Log(id + "가 리액션 :" + reactionId);

        // 1. 지금 재생 중인 행동과 똑같은 신호가 오면 무시 (연타 완벽 방어)
        if (currentReaction == reactionId)
        {
            counter++;
            PlaySound(reactionId);
            return;
        }

        // 2. 다른 리액션으로 바뀌는 거라면 이전 오브젝트 싹 다 끄기
        HideAllReactionObj();

        // 3. 상태 잠금 및 애니메이터 트리거 발동
        currentReaction = reactionId;
        animator.SetTrigger(reactionId);
    }
    public void HideAllReactionObj()
    {
        if (tambourine != null) tambourine.SetActive(false);
        if (drum != null) drum.SetActive(false);
        if (bell != null) bell.SetActive(false);
    }

    //vr 유저용
    //기본 1초간 들고 있는다
    //다른 리액션을 하거나 1초가 지나면 비활성화
    public void PlayReactionVr(string reactionId)
    {
        PlaySound(reactionId);

        if(isSinging) return;

        if(hideTimer != null)
        {
            StopCoroutine(hideTimer);
        }

        if (currentReaction == reactionId)
        {
            
            hideTimer = StartCoroutine(HideObjTimer());
            return;
        }

        HideAllReactionObj();
        ReactionObjShow(reactionId);
        hideTimer = StartCoroutine(HideObjTimer());
    }

    IEnumerator HideObjTimer()
    {
        yield return new WaitForSeconds(objShowTime);

        HideAllReactionObj();
        hideTimer = null;
    }

    public void ReactionObjShow(string reactionId)
    {
        switch (reactionId)
        {
            case "tambourine":
                tambourine.SetActive(true);
                return;
            case "drum":
                drum.SetActive(true);
                return;
            case "bell":
                bell.SetActive(true);
                return;
            default:
                return;
        }
    }

    public void PlaySound(string reactionId)
    {
        ReactionMapping reaction = AvatarManager.avatarManager.reactionDict[reactionId];
        AudioClip clip = reaction.sound[Random.Range(0, reaction.sound.Count)];
        audioSource.PlayOneShot(clip);
    }
}