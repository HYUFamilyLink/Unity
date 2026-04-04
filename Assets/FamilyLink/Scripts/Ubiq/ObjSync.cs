using UnityEngine;
using Ubiq.Messaging;
using UnityEngine.Rendering.Universal;

public class ObsSync : MonoBehaviour
{
    private NetworkContext context;
    public bool owner = false;

    // 동기화할 데이터 구조체
    struct Message
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    void Start()
    {
        // Ubiq 네트워크 컨텍스트 초기화
        context = NetworkScene.Register(this);
        SetOwner(true);
    }

    // 소유권 설정 (예: 로컬 플레이어가 생성한 경우 true)
    public void SetOwner(bool isOwner)
    {
        owner = isOwner;
    }

    void Update()
    {
        if (owner)
        {
            // 소유자라면 자신의 위치 정보를 다른 클라이언트에게 전송
            context.Send(JsonUtility.ToJson(new Message
            {
                position = transform.position,
                rotation = transform.rotation
            }));
        }
    }

    // 네트워크를 통해 메시지를 받았을 때 실행되는 함수
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        if (!owner)
        {
            // 소유자가 아니라면 받은 데이터를 바탕으로 위치/회전 업데이트
            var m = JsonUtility.FromJson<Message>(message.ToString());
            transform.position = m.position;
            transform.rotation = m.rotation;
        }
    }
}