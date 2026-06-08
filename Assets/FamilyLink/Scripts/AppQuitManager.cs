using UnityEngine;

public class AppQuitManager : MonoBehaviour
{
    public static AppQuitManager instance;

    private void Awake()
    {
        // 싱글톤 패턴 적용 및 씬 전환 시 파괴 방지
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 애플리케이션이 종료될 때 자동으로 호출되는 유니티 콜백
    private void OnApplicationQuit()
    {
        PerformCleanup();
    }

    private void PerformCleanup()
    {
        Debug.Log("[AppQuitManager] 강제 종료 감지, 클린업 루틴을 시작합니다.");

        // 1. 아고라(음성 채팅) 통신 해제
        if (AgoraManager.agoraManager != null)
        {
            AgoraManager.agoraManager.QuitChannel();
        }

        // 2. 소켓 연결 종료 및 방 퇴장 이벤트 처리
        // AvatarManager의 Despawn 루틴은 코루틴이므로 강제 종료 시 완료를 보장할 수 없습니다.
        // 따라서 SocketManager의 기능들을 동기적으로 직접 호출합니다.
        if (SocketManager.socketManager != null)
        {
            // room:leave 전송 및 이벤트 리스너 해제
            SocketManager.socketManager.LeftEvenet();
            
            // 소켓 연결 자체를 즉시 차단
            if (SocketManager.socketManager.socket != null)
            {
                SocketManager.socketManager.socket.Disconnect();
            }
        }

        // 3. 세션 및 룸 데이터 정보 초기화
        if (SessionManager.sessionManager != null)
        {
            SessionManager.sessionManager.ClearSession();
        }

        // Ubiq(P2P 네트워크 및 오브젝트 스폰)의 자원들은 유니티 프로세스 종료와 함께 
        // 하위 객체들의 OnDestroy()가 불리며 자동으로 해제되므로 강제 종료 시 별도의 
        // 수동 코루틴 호출(예: DespawnMyAvatarRoutine)은 생략하는 것이 안전합니다.
    }
}