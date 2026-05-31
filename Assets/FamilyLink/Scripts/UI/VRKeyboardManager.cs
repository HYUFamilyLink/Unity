using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

public class VRKeyboardManager : MonoBehaviour
{
    public static VRKeyboardManager Instance;

    [Header("Keyboard Settings")]
    [Tooltip("임포트한 Spatial Keyboard 프리팹을 할당하세요.")]
    public GameObject spatialKeyboard; 

    [Header("Transform Settings")]
    public float distance = 0.6f;     // 카메라로부터의 거리
    public float heightOffset = -0.6f; // 시선보다 살짝 아래로 위치

    private void Awake()
    {
        // 싱글톤 및 DontDestroyOnLoad 처리
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 키보드 객체도 씬 전환 시 파괴되지 않도록 매니저의 자식으로 편입 후 비활성화
            if (spatialKeyboard != null)
            {
                if (spatialKeyboard.scene.rootCount == 0) 
                {
                    spatialKeyboard = Instantiate(spatialKeyboard, gameObject.transform);
                    spatialKeyboard.SetActive(false);
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // STTInputController 등에서 호출할 키보드 열기 함수
    public void OpenKeyboard(TMP_InputField targetField)
    {
        if (spatialKeyboard == null) return;

        // 1. 카메라 자동 탐색 (호출될 때마다 현재 씬의 메인 카메라를 찾음)
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = FindObjectOfType<Camera>(); // MainCamera 태그가 없을 경우를 대비한 2차 탐색
            if (cam == null)
            {
                Debug.LogWarning("[VRKeyboardManager] 씬에서 카메라를 찾을 수 없습니다.");
                return;
            }
        }

        // 2. 플레이어 시야 앞쪽으로 위치 및 회전 계산
        Vector3 spawnPos = cam.transform.position + (cam.transform.forward * distance);
        spawnPos.y += heightOffset;
        spatialKeyboard.transform.position = spawnPos;
        
        // 키보드가 유저를 바라보도록 Y축 기준 회전
        Vector3 lookTarget = new Vector3(cam.transform.position.x, spatialKeyboard.transform.position.y, cam.transform.position.z);
        spatialKeyboard.transform.LookAt(lookTarget);
        spatialKeyboard.transform.Rotate(-40, 0, 0); // 프리팹 축에 따라 키보드가 뒤집혀 보이면 이 줄의 각도를 조정(또는 삭제)하세요.

        // 3. 입력 필드 자동 할당 (인스펙터 이미지 참고)
        // ※ 네임스페이스 에러가 날 경우 해당 스크립트를 열어 네임스페이스를 확인하고 상단에 using으로 추가해주세요.
        var keyboardDisplay = spatialKeyboard.GetComponentInChildren<XRKeyboardDisplay>(); 
        if (keyboardDisplay != null)
        {
            keyboardDisplay.inputField = targetField;
        }
        else
        {
            Debug.LogWarning("Spatial Keyboard에 XRKeyboardDisplay 스크립트가 없습니다.");
        }

        // 4. 키보드 활성화
        spatialKeyboard.SetActive(true);
    }

    public void CloseKeyboard()
    {
        if (spatialKeyboard != null)
        {
            spatialKeyboard.SetActive(false);
        }
    }
}