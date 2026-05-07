using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FamilyLink;
using UnityEngine;

using UniVRM10;

//아바타 고유 정보 등을 처리 및 보유하는 스크립트
public class Avatar : MonoBehaviour
{
    public string id;
    public uint agoraUid;
    public string role = "phone";
    public void SetID(string _id) {id = _id; return;}

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
        agoraUid = GetAgoraUid(id);
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

    public void PlayReaction(string emoji)
    {
        int codePoint = char.ConvertToUtf32(emoji, 0);
        Debug.Log(id + "가 리액션 :" + codePoint);
    }
}