using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;

//p2p 통신간 오브젝트 동기화를 담당하는 코드
//ubiq로 움직임 동기화가 필요한 모든 오브젝트는 이 스크립트를 포함해야 한다.
public class ObjSync : MonoBehaviour, INetworkSpawnable
{
    private NetworkContext context;
    public bool isOwner = false;

    private NetworkId _networkId;
    public NetworkId NetworkId
    {
        get => _networkId;
        set
        {
            _networkId = value;
            if(context.Id == NetworkId.Null) context = NetworkScene.Register(this, _networkId);
        }
    }

    // 동기화할 데이터 구조체
    struct Message
    {
        public Vector3 position;
        public Quaternion rotation;
        public string id;
    }

    // 소유권 설정 (예: 로컬 플레이어가 생성한 경우 true)
    public void SetOwner(bool _isOwner)
    {
        isOwner = _isOwner;
    }

    void Update()
    {
        if (isOwner && context.Id != NetworkId.Null)
        {
            // 소유자라면 자신의 위치 정보를 다른 클라이언트에게 전송
            context.Send(JsonUtility.ToJson(new Message
            {
                position = transform.position,
                rotation = transform.rotation,
                id = gameObject.GetComponent<Avatar>().id
            }));
        }
    }

    // 네트워크를 통해 메시지를 받았을 때 실행되는 함수
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        if (!isOwner)
        {
            // 소유자가 아니라면 받은 데이터를 바탕으로 위치/회전 업데이트
            var m = JsonUtility.FromJson<Message>(message.ToString());
            transform.position = m.position;
            transform.rotation = m.rotation;

            var avatar = gameObject.GetComponent<Avatar>();
            avatar.SetID(m.id);
            AvatarManager.avatarManager.ReigsterAvatar(m.id, avatar);
        }
    }
}