using System.Collections.Generic;
using UnityEngine;

public class NoResetStackManager : MonoBehaviour
{
    // 입력을 저장할 큐 (크기 3 유지, 0=Null, 1=Alt, 2=F4)
    private List<int> inputQueue = new List<int> { 0, 0, 0 };

    // 플레이어 이동 방향 (0: 오른쪽, 1: 아래, 2: 왼쪽, 3: 위쪽)
    private int direction = 0;

    void Update()
    {
        // Alt 키 입력 처리 (회전)
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            EnqueueInput(1); // Alt 입력 추가
            direction = (direction + 1) % 4; // 시계 방향 90도 회전
            Debug.Log($" Alt 입력됨 → 현재 방향: {DirectionToString()}");
        }

        // F4 키 입력 처리 (이동 및 게임오버 체크)
        if (Input.GetKeyDown(KeyCode.F4))
        {
            EnqueueInput(2); // F4 입력 추가
            MovePlayer(); // 현재 방향으로 한 칸 이동
            Debug.Log($" F4 입력됨 → 현재 방향으로 이동");

            // Alt → F4 입력이 감지되면 즉시 캐릭터 소멸
            if (CheckGameOver())
            {
                Debug.Log(" 게임 오버! 오브 파괴됨!");
                Destroy(gameObject); // 플레이어 삭제
            }
        }
    }

    // 📌 입력을 리스트(큐)에 추가하는 함수 (최대 크기 3 유지)
    void EnqueueInput(int input)
    {
        inputQueue.RemoveAt(0); // 맨 앞의 입력 제거 (가장 오래된 입력 삭제)
        inputQueue.Add(input);  // 새로운 입력 추가
        Debug.Log($"현재 입력 상태: {string.Join(", ", inputQueue)}");
    }

    //  Alt → F4 입력이 포함되었는지 확인하는 함수 (가독성 개선)
    bool CheckGameOver()
    {
        bool case1 = (inputQueue[0] == 1 && inputQueue[1] == 2); // [Alt, F4, ?]
        bool case2 = (inputQueue[1] == 1 && inputQueue[2] == 2); // [?, Alt, F4]

        return case1 || case2; // 둘 중 하나라도 성립하면 게임오버
    }

    //  F4 입력 시 플레이어 이동 처리
    void MovePlayer()
    {
        Vector3 moveDirection = Vector3.zero;

        // 방향값에 따라 이동 벡터 결정
        switch (direction)
        {
            case 0: moveDirection = Vector3.right; break;  // 오른쪽 이동
            case 1: moveDirection = Vector3.down; break;   // 아래쪽 이동
            case 2: moveDirection = Vector3.left; break;   // 왼쪽 이동
            case 3: moveDirection = Vector3.up; break;     // 위쪽 이동
        }

        transform.position += moveDirection; // 실제 이동 적용
    }

    //  현재 방향을 문자열로 변환 (디버깅용)
    string DirectionToString()
    {
        switch (direction)
        {
            case 0: return "오른쪽";
            case 1: return "아래쪽";
            case 2: return "왼쪽";
            case 3: return "위쪽";
            default: return "알 수 없음";
        }
    }
}
