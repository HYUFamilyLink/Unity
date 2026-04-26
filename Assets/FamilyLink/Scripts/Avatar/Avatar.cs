using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UniVRM10;

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
                }
                break;
            }
        }
        if(role == "phone") AvatarManager.avatarManager.SetWebSync(this);
    }


    void Update() {
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
    }
}