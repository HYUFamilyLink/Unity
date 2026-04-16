using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//아바타 고유 정보 등을 처리 및 보유하는 스크립트
public class Avatar : MonoBehaviour
{
    public string id;
    public string role = "phone";

    public void SetID(string _id) { id = _id; return;}

    void Start()
    {
        string networkId = gameObject.GetComponent<ObjSync>().NetworkId.ToString().Split('.')[0];
        var room = AvatarManager.avatarManager.spawnManager.roomClient.Room;

        foreach(var entry in room)
        {
            if (entry.Key.Contains(networkId))
            {
                var data = JsonUtility.FromJson<ExtendedSpawnMessage>(entry.Value);
                if(!string.IsNullOrEmpty(data.targetSocketId))
                {
                    AvatarManager.avatarManager.ReigsterAvatar(data.targetSocketId, this);
                    id = data.targetSocketId;
                    Debug.Log("아바타.cs");
                }
                break;
            }
        }
        if(role == "phone") AvatarManager.avatarManager.SetWebSync(this);
    }
}
