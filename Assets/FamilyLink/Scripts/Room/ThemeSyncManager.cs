using UnityEngine;
using Ubiq.Messaging;
using System.Collections;

public class ThemeSyncManager : MonoBehaviour
{
    public static ThemeSyncManager instance;
    private NetworkContext context;
    private int localCurrentIndex = 0; // 로컬에서 기억하는 현재 테마 인덱스 (기본형: 0)

    // 메시지 타입을 구별하기 위한 구조체
    [System.Serializable]
    struct ThemeMessage
    {
        public string type; // "Request" (테마 물어보기) 또는 "State" (테마 알려주기)
        public int themeIndex;
    }

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // 방 안의 모든 VR 클라이언트가 공유할 고유 주파수 ID 설정
        NetworkId themeNetId = new NetworkId(77778888); 
        context = NetworkScene.Register(this, themeNetId);

        // 방 접속이 안정화된 직후 기존 유저들에게 테마 상태를 요청합니다.
        StartCoroutine(RequestInitialThemeRoutine());
    }

    private IEnumerator RequestInitialThemeRoutine()
    {
        // P2P 네트워크 및 룸 입장이 완료될 때까지 안전하게 1.5초~2초 정도 대기합니다.
        yield return new WaitForSeconds(1.5f);

        if (context.Id != NetworkId.Null)
        {
            Debug.Log("[ThemeSyncManager] 기존 방 참가자들에게 현재 테마 상태 요청 전송");
            context.Send(JsonUtility.ToJson(new ThemeMessage
            {
                type = "Request",
                themeIndex = -1
            }));
        }
    }

    // UI 드롭다운(Dropdown)의 OnValueChanged 이벤트에서 이 함수를 호출하게 설정합니다.
    public void BroadcastThemeChange(int index)
    {
        localCurrentIndex = index;
        
        // 1. 내 로컬 테마 먼저 변경
        ThemeManager.themeManager.ChangeThemeByIndex(index);

        // 2. 다른 방 사람들에게 새로운 테마 전송
        SendThemeState(index);
    }

    // 현재 테마 정보를 P2P 채널에 실어 보내는 함수
    private void SendThemeState(int index)
    {
        if (context.Id != NetworkId.Null)
            {
            context.Send(JsonUtility.ToJson(new ThemeMessage
            {
                type = "State",
                themeIndex = index
            }));
        }
    }

    // Ubiq 고정 주파수(77778888) 채널로 들어오는 모든 메시지를 처리하는 함수
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var m = JsonUtility.FromJson<ThemeMessage>(message.ToString());

        if (m.type == "Request")
            {
            // 누군가 새로 들어와서 방의 테마를 물어봤다면, 이미 방에 있던 내가 현재 테마를 답변해 줍니다.
            Debug.Log($"[ThemeSyncManager] 새 입조자의 요청 확인. 현재 테마 인덱스({localCurrentIndex}) 응답 전송");
            SendThemeState(localCurrentIndex);
            }
        else if (m.type == "State")
        {
            // 테마 변경 이벤트 신호 혹은 신입 유저로서 요청에 대한 응답을 수신한 경우
            localCurrentIndex = m.themeIndex;
            Debug.Log($"[ThemeSyncManager] 테마 동기화 데이터 수신 완료: 인덱스 {m.themeIndex}");
            
            // 로컬 테마 변경 실행 (이미 같은 테마면 ThemeManager 내부 방어 코드가 작동하여 무시됨)
            ThemeManager.themeManager.ChangeThemeByIndex(m.themeIndex);
        }
    }
}