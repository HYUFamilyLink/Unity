using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HandTrigger : MonoBehaviour
{
    private Vector3 lastPos;
    public float speed;
    public float offset;

    void Start()
    {
        lastPos = transform.position;
    }
    void Update()
    {
        float distance = Vector3.Distance(transform.position, lastPos);

        speed = distance / Time.deltaTime;
        lastPos = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        //손을 움직이는 속도가 너무 느리면 트리거되지 않음
        if(speed < offset) return;
        string reactionId = "";
        string emoji = "";

        Debug.Log("충돌판정");
        switch (other.gameObject.tag)
        {
            case "Hand":
                reactionId = "clap";
                emoji = "👏";
                break;
            case "Tambourine":
                reactionId = "tambourine";
                emoji = "🪇";
                break;
            case "Bell":
                reactionId = "bell";
                emoji = "🛎️";
                break;
            case "Drum":
                reactionId = "drum";
                emoji = "🪘";
                break;
            default:
                return;
        }
        SocketManager.socketManager.socket.Emit("user:reaction", new {reactionId = reactionId, emoji = emoji});
    }
}

/*
const REACTION_DATA = [
  { id: 'kick', icon: '🥁', sounds: ['/sounds/kick1.mp3','/sounds/kick2.mp3'] },
  { id: 'clap', icon: '👏', sounds: ['/sounds/clap1.mp3', '/sounds/clap2.mp3'] },
  { id: 'bell', icon: '🛎️', sounds: ['/sounds/chime1.mp3'] },
  { id: 'drum', icon: '🪘', sounds: ['/sounds/drum1.mp3','/sounds/drum2.mp3'] }, 
  { id: 'tambourine', icon: '🪇', sounds: ['/sounds/tam1.mp3', '/sounds/tam2.mp3'] },
];
*/