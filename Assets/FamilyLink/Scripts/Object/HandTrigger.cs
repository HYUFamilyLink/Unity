using System;
using System.Collections.Generic;
using FamilyLink;
using UnityEngine;

public class HandTrigger : MonoBehaviour
{
    public float speed;
    public float offset;
    public Vector3 lastPos;
    public Collider otherHand;
    public bool isRight;
    Dictionary<string, ReactionMapping> reactiondict => AvatarManager.avatarManager.reactionDict;

    [Header("vr 1인칭용 오브젝트")]
    public GameObject tambourine;
    public GameObject drum;
    public GameObject bell;

    [Header("쿨다운")]
    public float coolDown = 0.2f;
    public float lastTime;

    void Start()
    {
        lastPos = transform.position;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, lastPos);

        speed = distance / Time.deltaTime;
        lastPos = transform.position;

        TambourineShake();
    }

    void TambourineShake()
    {
        if(isRight || !tambourine.activeSelf) return;
        if(speed > offset)
        {
            if(Time.time >= lastTime + coolDown)
            {
                SocketManager.socketManager.socket.Emit("user:reaction", new {reactionId = reactiondict["tambourine"].id, emoji = $"'\'{reactiondict["tambourine"].emoji}" });
                lastTime = Time.time;
                Debug.Log("탬버린 흔들기");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        //손을 움직이는 속도가 너무 느리면 트리거되지 않음
        if(speed < offset) return;
        //다단히트 방어
        if(Time.time < lastTime + coolDown) return;

        ReactionMapping reaction = null;

        Debug.Log($"충돌판정 {other.tag}");
        switch (other.gameObject.tag)
        {
            case "Hand":
                if(isRight) return;
                reaction = reactiondict["clap"];
                break;
            case "Tambourine":
                if(!isRight) return;
                reaction = reactiondict["kick"];
                break;
            case "Bell":
                reaction = reactiondict["bell"];
                break;
            case "Drum":
                reaction = reactiondict["drum"];
                break;
            default:
                return;
        }
        
        SocketManager.socketManager.socket.Emit("user:reaction", new {reactionId = reaction.id, emoji = $"'\'{reaction.emoji}" });
    }

    //액션에 기반해서 오브젝트를 제어하므로
    //이 코드는 본인 팔에 쫒아다니는거만 하면 된다
    public void Objselect(string objTag)
    {
        DeselectAll();
        switch (objTag)
        {
            case "Tambourine":
                if(!isRight) return;
                otherHand.enabled = false;
                tambourine.SetActive(true);
                break;
            case "Bell":
                bell.SetActive(true);
                break;
            case "Drum":
                drum.SetActive(true);
                break;
            default:
                return;
        }
    }

    public void DeselectAll()
    {
        tambourine.SetActive(false);
        bell.SetActive(false);
        drum.SetActive(false);
        otherHand.enabled = true;
    }
}
/*
const REACTION_DATA = [
  { id: 'kick', icon: '🥁'\U0001F941, sounds: ['/sounds/kick1.mp3','/sounds/kick2.mp3'] },
  { id: 'clap', icon: '👏'\U0001F44F, sounds: ['/sounds/clap1.mp3', '/sounds/clap2.mp3'] },
  { id: 'bell', icon: '🛎️'\U0001F6CE, sounds: ['/sounds/chime1.mp3'] },
  { id: 'drum', icon: '🪘'\U0001FA98, sounds: ['/sounds/drum1.mp3','/sounds/drum2.mp3'] }, 
  { id: 'tambourine', icon: '🪇'\U0001FA87, sounds: ['/sounds/tam1.mp3', '/sounds/tam2.mp3'] },
];
*/