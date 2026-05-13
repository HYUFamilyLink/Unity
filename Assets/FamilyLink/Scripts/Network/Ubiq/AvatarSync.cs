using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;
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
    public Transform Body;
    private P2PTransform body => new P2PTransform{pos = Body.position, rot = Body.rotation};
    private Vector3 initialBodyPos;
    private Quaternion initialBodyRot;
    


    [Header("XR Transform")]
    public Transform xrLeft;
    public Transform xrRight;
    public Transform xrHead;
    public Transform xrOrigin;

    [Header("Network")]
    private NetworkContext context;
    public bool isOwner = false;
    private NetworkId _networkId;
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
        public P2PTransform body;
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
        Transform[] allChildren = gameObject.GetComponentsInChildren<Transform>(true);
        foreach(var child in allChildren) child.gameObject.layer = 3;
        xrOrigin = GameObject.Find("XR Origin (XR Rig)").transform;
        xrHead = xrOrigin.Find("Camera Offset/Main Camera");
        xrLeft = xrOrigin.Find("Camera Offset/Left Controller");
        xrRight = xrOrigin.Find("Camera Offset/Right Controller");

        xrOrigin.position = this.transform.position; 
        xrOrigin.rotation = this.transform.rotation;
        
        initialBodyPos = Body.position; 
        initialBodyRot = Body.rotation;

        Debug.Log("XR 트랜스폼 매핑 완료");
    }

    void LateUpdate()
    {
        if (isOwner && context.Id != NetworkId.Null)
        {
            //본인 아바타일 경우 컨트롤러 위치 찾아서 지정
            if (isMyAvatar)
            {
                // 1. 머리 회전만 동기화 (위치는 고정하여 아바타가 소파를 절대 벗어나지 않게 함)
                Head.rotation = xrHead.rotation;

                // 2. 핵심 로직: 실제 내 머리(HMD)와 손(Controller) 사이의 물리적 벡터 거리(Offset)만 계산
                Vector3 leftOffset = xrLeft.position - xrHead.position;
                Vector3 rightOffset = xrRight.position - xrHead.position;

                // 3. 가상 아바타 머리를 기준으로 똑같은 거리만큼 손 위치를 적용
                leftHand.position = Head.position + leftOffset;
                leftHand.rotation = xrLeft.rotation * Quaternion.Euler(0, 90f, 0);

                rightHand.position = Head.position + rightOffset;
                rightHand.rotation = xrRight.rotation * Quaternion.Euler(0, -90f, 0);

                // 🚨 주의: Body(골반) 위치를 고정하는 코드는 완전히 삭제했습니다!
                // 이렇게 놔두면 유니티 애니메이터가 알아서 다리를 쫙 펴고 예쁘게 서있게/앉아있게 만듭니다.
            }

            // 소유자라면 자신의 위치 정보를 다른 클라이언트에게 전송
            context.Send(JsonUtility.ToJson(new Message
            {
                leftHand = left,
                rightHand = right,
                head = head,
                body = body,
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
            Body.position = m.body.pos;
            Body.rotation = m.body.rot;
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