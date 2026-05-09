using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("충돌판정");
        if (other.gameObject.CompareTag("Hand"))
        {
            SocketManager.socketManager.socket.Emit("user:reaction", new {reactionId = "clap", emoji = "👏"});
            Debug.Log("전송");
        }
    }
}
