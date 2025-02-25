using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TwoStackManager : MonoBehaviour
{
    [SerializeField] private Transform _arrowTransform;

    // 입력을 저장할 큐 (크기 2 유지, 0=Null, 1=Alt, 2=F4)
    private List<int> inputQueue = new List<int> { 0, 0 };

    [SerializeField] private TMP_Text[] tmp_Text = new TMP_Text[2];

    // 플레이어 이동 방향 (0: 오른쪽, 1: 아래, 2: 왼쪽, 3: 위쪽)
    private int direction = 0;

    // 현재 입력을 저장하는 인덱스
    private int stack = 0;

    // 게임 오버 여부
    private bool _isGameOver = false;

    void Update()
    {
        if (_isGameOver is false)
        {
            // Alt 키 입력 처리 (회전)
            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                if (stack >= 2)
                {
                    ResetQueue();
                }

                HandleInput(1); // ALT 입력 (1)
                direction = (direction + 1) % 4; // 시계 방향 90도 회전
                RotateArrow();

                Debug.Log($"Alt 입력됨 → 현재 방향: {DirectionToString()}");
            }

            // F4 키 입력 처리 (이동 및 게임오버 체크)
            if (Input.GetKeyDown(KeyCode.F4))
            {
                if (stack >= 2)
                {
                    ResetQueue();
                }

                HandleInput(2); // F4 입력 (2)
                MovePlayer(); // 현재 방향으로 한 칸 이동
                Debug.Log($"F4 입력됨 → {DirectionToString()} 방향으로 이동");
            }
        }
    }

    // 📌 화살표 회전 함수
    void RotateArrow()
    {
        float angle = direction switch
        {
            0 => 0f,    // ➡ 오른쪽 (0도)
            1 => 270f,  // ⬇ 아래 (270도, 시계방향)
            2 => 180f,  // ⬅ 왼쪽 (180도)
            3 => 90f,   // ⬆ 위 (90도)
            _ => 0f
        };

        _arrowTransform.rotation = Quaternion.Euler(0, 0, angle); // 🔄 Z축 회전
    }

    // 📌 입력을 리스트에 추가하고 UI 업데이트하는 함수
    void HandleInput(int keyCode)
    {
        inputQueue[stack] = keyCode; // 입력값 저장
        tmp_Text[stack].text = ConvertIntToString(keyCode); // UI 업데이트
        stack++;

        // Alt → F4 입력이 감지되면 즉시 캐릭터 소멸
        if (CheckGameOver())
        {
            Debug.Log("💀 게임 오버! 오브 파괴됨!");
            _isGameOver = true;
            //Destroy(gameObject); // 플레이어 삭제
        }
    }

    // 📌 숫자(int) → 문자열(string) 변환 함수
    string ConvertIntToString(int input)
    {
        return input switch
        {
            1 => "ALT",
            2 => "F4",
            _ => "" // 0일 경우 빈 문자열
        };
    }

    // 📌 Alt → F4 입력이 감지되었는지 확인 (2칸 스택에 맞게 수정)
    bool CheckGameOver()
    {
        return (inputQueue[0] == 1 && inputQueue[1] == 2); // [Alt, F4] 조합 확인
    }

    // 📌 리스트 초기화 (새로운 입력을 받을 준비)
    void ResetQueue()
    {
        inputQueue = new List<int> { 0, 0 }; // 입력 리스트 초기화
        stack = 0;

        // UI 텍스트도 초기화
        for (int i = 0; i < 2; i++)
        {
            tmp_Text[i].text = "";
        }

        Debug.Log("Succeed to Reset inputQueue.");
    }

    // 📌 플레이어 이동 처리
    void MovePlayer()
    {
        Vector3 moveDirection = direction switch
        {
            0 => Vector3.right,  // 오른쪽
            1 => Vector3.down,   // 아래쪽
            2 => Vector3.left,   // 왼쪽
            3 => Vector3.up,     // 위쪽
            _ => Vector3.zero
        };

        transform.position += moveDirection; // 실제 이동 적용
    }

    // 📌 현재 방향을 문자열로 변환 (디버깅용)
    string DirectionToString()
    {
        return direction switch
        {
            0 => "오른쪽",
            1 => "아래쪽",
            2 => "왼쪽",
            3 => "위쪽",
            _ => "알 수 없음"
        };
    }
}
