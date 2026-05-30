using System.Collections;
using System.Collections.Generic;
using FamilyLink;
using UniGLTF.SpringBoneJobs;
using Unity.VisualScripting;
using UnityEngine;

public class HandTrigger : MonoBehaviour
{
    public float speed;
    public float offset;
    public Collider left;
    Dictionary<string, ReactionMapping> reactiondict => AvatarManager.avatarManager.reactionDict;

    [Header("vr 1인칭용 오브젝트")]
    public GameObject tambourine;
    public GameObject drum;

    void Update()
    {
        float distance = Vector3.Distance(transform.position, left.transform.position);

        speed = distance / Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        //손을 움직이는 속도가 너무 느리면 트리거되지 않음
        if(speed < offset) return;
        ReactionMapping reaction = null;

        Debug.Log($"충돌판정 {other.tag}");
        switch (other.gameObject.tag)
        {
            case "Hand":
                reaction = reactiondict["clap"];
                break;
            case "Tambourine":
                reaction = reactiondict["tambourine"];
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
    public void Objselect(GameObject obj)
    {
        left.enabled = false;
        obj.SetActive(true);
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