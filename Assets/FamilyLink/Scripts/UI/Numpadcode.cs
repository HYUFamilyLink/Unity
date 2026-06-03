using UnityEngine;
using TMPro; // TextMeshPro 필수

public class NumpadController : MonoBehaviour
{
    [Header("연결할 대상")]
    public TMP_InputField targetInputField; // 기존 로그인 화면의 4자리 비밀번호 입력창
    public GameObject numpadPanel;          // 이 키보드 패널

    private int maxDigits = 4; // 4자리 제한

    // 숫자 버튼(0~9)을 누를 때
    public void OnNumberPressed(string number)
    {
        // 타겟 입력창이 연결되어 있고, 4자리 미만일 때만 글자 추가
        if (targetInputField != null && targetInputField.text.Length < maxDigits)
        {
            targetInputField.text += number;
        }
    }

    // 지우기(←) 버튼을 누를 때 실행
    public void OnBackspacePressed()
    {
        if (targetInputField != null && targetInputField.text.Length > 0)
        {
            // 맨 마지막 글자 하나를 지움
            targetInputField.text = targetInputField.text.Substring(0, targetInputField.text.Length - 1);
        }
    }

    // 엔터(Enter/확인) 버튼을 누를 때 실행 (기존 'C' 버튼 대체)
    public void OnEnterPressed()
    {
        if (targetInputField != null)
        {
            if (targetInputField.text.Length == maxDigits)
            {
                // 4자리가 꽉 찼다면 넘패드(키보드)를 화면에서 숨김
                numpadPanel.SetActive(false);
            }
            else
            {
                Debug.Log("비밀번호 4자리를 모두 입력해주세요.");
            }
        }
    }

    //기존 로그인 입력창을 클릭했을 때 키보드를 띄우기 위한 함수
    public void OpenNumpad()
    {
        // 입력창을 열 때마다 기존 텍스트를 비우고 싶다면 아래 주석 해제
        // if (targetInputField != null) targetInputField.text = "";

        numpadPanel.SetActive(true);
    }
}