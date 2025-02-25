using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StackManager_3D : MonoBehaviour
{
    [SerializeField] private Transform _arrowTransform;

    // 입력을 저장할 큐 (크기 3 유지, 0=Null, 1=Alt, 2=F4)
    private List<int> inputQueue = new List<int> { 0, 0, 0 };

    [SerializeField] private TMP_Text[] tmp_Text = new TMP_Text[3];

    // 플레이어 이동 방향 (0: 오른쪽, 1: 아래, 2: 왼쪽, 3: 위쪽)
    private int direction = 0;

    // 현재 입력을 저장하는 인덱스
    private int stack = 0;

    //게임 오버 불
    private bool _isGameOver = false;

    //3D 새로 추가함
    Vector3 targetPosition;
    Vector3 velocity = Vector3.zero;
    float smoothTime = 0.3f;
    bool isMoving = false;

    private void Start()
    {
        //3D 필드 기준에서는, XY 평면에 평행한 방향으로 중력이 작용한다. 그래서 바꿔줘야 함
        Physics.gravity = new Vector3(0, 0, 9.81f);
    }

    void Update()
    {
        if (_isGameOver is false)
        {
            // 3D 기준 움직이는 로직 구현
            if (Input.GetKeyDown(KeyCode.Space))
            {
                targetPosition = transform.position + Vector3.right * 1.0f;
                isMoving = true;
            }

            if (isMoving)
            {
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)  // 목표 위치에 거의 도달하면 멈추기
                {
                    isMoving = false;
                    velocity = Vector3.zero;
                }
            }


            // Alt 키 입력 처리 (회전)
            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {

                // 리스트 초기화 (3개 입력 후)
                if (stack >= 3)
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

                // 리스트 초기화 (3개 입력 후)
                if (stack >= 3)
                {
                    ResetQueue();
                }

                HandleInput(2); // F4 입력 (2)
                MovePlayer(); // 현재 방향으로 한 칸 이동
                Debug.Log($"F4 입력됨 → {DirectionToString()} 방향으로 이동");

            }
        }
    }

    //화살표 스프라이트 회전
    void RotateArrow()
    {
        float angle = 0f;

        switch (direction)
        {
            case 0: angle = 0f; break;    // ➡ 오른쪽 (0도)
            case 1: angle = 270f; break;  // ⬇ 아래 (270도, 시계방향)
            case 2: angle = 180f; break;  // ⬅ 왼쪽 (180도)
            case 3: angle = 90f; break;   // ⬆ 위 (90도)
        }

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
            Debug.Log("게임 오버! 오브 파괴됨!");
            _isGameOver = true;
            //Destroy(gameObject); // 플레이어 삭제
        }
    }

    // 📌 숫자(int) → 문자열(string) 변환 함수
    string ConvertIntToString(int input)
    {
        switch (input)
        {
            case 1: return "ALT";
            case 2: return "F4";
            default: return ""; // 0일 경우 빈 문자열
        }
    }

    // 📌 Alt → F4 입력이 감지되었는지 확인
    bool CheckGameOver()
    {
        return (inputQueue[0] == 1 && inputQueue[1] == 2) || (inputQueue[1] == 1 && inputQueue[2] == 2);
    }

    // 📌 리스트 초기화 (새로운 입력을 받을 준비)
    void ResetQueue()
    {
        inputQueue = new List<int> { 0, 0, 0 }; // 입력 리스트 초기화
        stack = 0;

        // UI 텍스트도 초기화
        for (int i = 0; i < 3; i++)
        {
            tmp_Text[i].text = "";
        }

        Debug.Log("Succeed to Reset inputQueue.");
    }

    // 📌 플레이어 이동 처리
    void MovePlayer()
    {
        Vector3 moveDirection = Vector3.zero;

        // 방향값에 따라 이동 벡터 결정
        switch (direction)
        {
            case 0: moveDirection = Vector3.right; break;
            case 1: moveDirection = Vector3.down; break;
            case 2: moveDirection = Vector3.left; break;
            case 3: moveDirection = Vector3.up; break;
        }

        transform.position += moveDirection; // 실제 이동 적용
    }

    // 📌 현재 방향을 문자열로 변환 (디버깅용)
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


