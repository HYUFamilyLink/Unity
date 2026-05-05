using UnityEngine;
using UnityEngine.Android;

[RequireComponent(typeof(AudioSource))]
public class MicInputControler : MonoBehaviour
{
    private string micName;
    private AudioClip micClip;
    private AudioSource audioSource;

    void Start()
    {
        //권한 요청
        #if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            Debug.Log("🎤 마이크 권한을 요청했습니다.");
        }
        #endif

        // 내 목소리를 다시 듣기 위한 AudioSource 컴포넌트 가져오기
        audioSource = GetComponent<AudioSource>();

        // 1. 연결된 마이크 하드웨어 찾기
        if (Microphone.devices.Length > 0)
        {
            // 보통 0번이 시스템 기본 마이크입니다.
            micName = Microphone.devices[0]; 
            Debug.Log("🎤 인풋을 받을 마이크: " + micName);

            // 2. 마이크 인풋 받기 시작
            // 파라미터: (마이크 이름, 루프 여부, 녹음 길이(초), 샘플링 레이트)
            micClip = Microphone.Start(micName, true, 1, 44100);

            // 3. 받은 인풋을 AudioSource에 연결해서 귀로 확인하기
            audioSource.clip = micClip;
            audioSource.loop = true;
            
            // 마이크가 데이터를 실제로 버퍼에 담을 때까지 아주 잠깐 대기
            while (!(Microphone.GetPosition(micName) > 0)) { } 
            
            // 내 목소리 스피커로 출력
            audioSource.Play();
        }
        else
        {
            Debug.LogError("❌ 연결된 마이크를 찾을 수 없습니다!");
        }
    }

    void OnDestroy()
    {
        // 오브젝트가 파괴되거나 씬이 넘어갈 때는 반드시 마이크 입력을 꺼주어야 합니다.
        if (micName != null)
        {
            Microphone.End(micName);
        }
    }
}