using UnityEngine;
using TMPro; // TextMeshPro를 사용한다면 필수

public class RoomManager : MonoBehaviour
{
    [SerializeField] private GameObject[] themeWalls;

    // 드롭다운 전용 함수 (인자값이 int로 들어옵니다)
    public void OnDropdownValueChanged(int index)
    {
        // 모든 테마 비활성화
        for (int i = 0; i < themeWalls.Length; i++)
        {
            themeWalls[i].SetActive(false);
        }

        // 선택한 번호만 활성화
        if (index >= 0 && index < themeWalls.Length)
        {
            themeWalls[index].SetActive(true);
        }
    }
}