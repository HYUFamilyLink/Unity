using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;
using UnityEngine.UIElements;
using System;

//p2p 통신간 아바타 동기화를 담당하는 코드
//ubiq로 움직임 동기화가 필요한 모든 아바타는 이 스크립트를 포함해야 한다.
public class AvatarSync : MonoBehaviour, INetworkSpawnable
{
    [Header("Avatar Transform")]
    public Transform leftHand;
    private P2PTransform left => new P2PTransform{pos = leftHand.position, rot = leftHand.rotation};
    public Transform rightHand;
    private P2PTransform right => new P2PTransform{pos = rightHand.position, rot = rightHand.rotation};
    public Transform Head;
    private P2PTransform head => new P2PTransform{pos = Head.position, rot = Head.rotation};

    [Header("XR Transform")]
    public Transform xrLeft;
    public Transform xrRight;
    public Transform xrHead;

    [Header("Network")]
    private NetworkContext context;
    public bool isOwner = false;
    private NetworkId _networkId;
    private string socketId;
    private bool isMyAvatar = false;

    [Header("SpawnPoint")]
    public int pointIdx;
    public void SetPointIndex(int idx) {pointIdx = idx;}

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
        public P2PTransform leftHand;
        public P2PTransform rightHand;
        public P2PTransform head;
        public string id;
        public int pointIdx;
    }

    // 소유권 설정 (예: 로컬 플레이어가 생성한 경우 true)
    public void SetOwner(bool _isOwner)
    {
        isOwner = _isOwner;
    }

    public void SetMine()
    {
        isMyAvatar = true;
    }
    public void SetTransform()
    {
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        xrOrigin.transform.position = gameObject.transform.position;
        xrOrigin.transform.rotation = gameObject.transform.rotation;
        Debug.Log("완료0");
        Debug.Log(xrOrigin.transform);

        xrHead = xrOrigin.transform.Find("Camera Offset/Main Camera");
        xrLeft = xrOrigin.transform.Find("Camera Offset/Left Controller");
        xrRight = xrOrigin.transform.Find("Camera Offset/Right Controller");
        Debug.Log("완료1");
        Debug.Log(xrLeft);

        xrLeft.position = leftHand.position;
        Debug.Log("완료2");

        xrRight.position = rightHand.position;
        Debug.Log("완료3");

        xrHead.position = Head.position;
        Debug.Log("완료4");
    }

    void Update()
    {
        if (isOwner && context.Id != NetworkId.Null)
        {
            //본인 아바타일 경우 컨트롤러 위치 찾아서 지정
            if (isMyAvatar)
            {
                leftHand.position = xrLeft.position;
                leftHand.rotation = xrLeft.rotation * Quaternion.Euler(0, 90f, 0);

                rightHand.position = xrRight.position;
                rightHand.rotation = xrRight.rotation * Quaternion.Euler(0, -90f, 0);

                Head.position = xrHead.position + xrHead.right * 0.094f;
                Head.rotation = xrHead.rotation;
            }

            // 소유자라면 자신의 위치 정보를 다른 클라이언트에게 전송
            context.Send(JsonUtility.ToJson(new Message
            {
                leftHand = left,
                rightHand = right,
                head = head,
                id = gameObject.GetComponent<Avatar>().id,
                pointIdx = pointIdx
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
            leftHand.position = m.leftHand.pos;
            leftHand.rotation = m.leftHand.rot;
            rightHand.position = m.rightHand.pos;
            rightHand.rotation = m.rightHand.rot;
            Head.position = m.head.pos;
            Head.rotation = m.head.rot;
            pointIdx = m.pointIdx;
            SpawnPointManager.spawnPointManager.SetPoint(pointIdx, this.GetComponent<Avatar>());
            var avatar = gameObject.GetComponent<Avatar>();
            avatar.SetID(m.id);
            AvatarManager.avatarManager.ReigsterAvatar(m.id, avatar);
        }
    }
}

[Serializable]
public struct P2PTransform
{
    public Vector3 pos;
    public Quaternion rot;
}