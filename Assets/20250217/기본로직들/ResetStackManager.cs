using System.Collections.Generic;
using UnityEngine;

public class ResetStackManager : MonoBehaviour
{
    private int direction = 0;
    private int stack = 0;
    private bool _isGameOver = false;
    private List<int> inputQueue = new List<int> { 0, 0, 0 };

    private Rigidbody2D _rigidbody2D;
    [SerializeField]
    private float forceAmount = 1f;

    [SerializeField]
    private Animator _animatior;
    public GameObject arrow;

    private void Awake()
    {
        _animatior = GetComponent<Animator>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    public bool IsGameOver()
    {
        return _isGameOver;
    }

    public void ProcessAltInput()
    {
        if (stack >= 3) ResetQueue();
        HandleInput(1); // ALT 입력
        direction = (direction + 1) % 4;
        RotateArrow();
    }

    public void ProcessF4Input()
    {
        if (stack >= 3) ResetQueue();
        HandleInput(2); // F4 입력
        MovePlayer();
    }

    public void ProcessTabInput()
    {
        if (stack >= 3) ResetQueue();
        HandleInput(3); // Tab 입력
    }

    void HandleInput(int keyCode)
    {
        inputQueue[stack] = keyCode;
        stack++;

        if (CheckGameOver())
        {
            Debug.Log("게임 오버!");
            _isGameOver = true;
            _animatior.Play("Explosion");
            arrow.SetActive(false);
        }
    }

    bool CheckGameOver()
    {
        return (inputQueue[0] == 1 && inputQueue[1] == 2) || (inputQueue[1] == 1 && inputQueue[2] == 2);
    }

    void ResetQueue()
    {
        inputQueue = new List<int> { 0, 0, 0 };
        stack = 0;
    }

    void MovePlayer()
    {
        Vector2 moveDirection = direction switch
        {
            0 => Vector2.right,
            1 => Vector2.down,
            2 => Vector2.left,
            3 => Vector2.up,
            _ => Vector2.zero
        };
        Debug.Log(moveDirection + " 이동");
        _rigidbody2D.AddForce(moveDirection * forceAmount, ForceMode2D.Impulse);
    }

    void RotateArrow()
    {
        float angle = direction switch
        {
            0 => 0f,
            1 => 270f,
            2 => 180f,
            3 => 90f,
            _ => 0f
        };

        arrow.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public int CheckInputQueue(int slot)
    {
        return inputQueue[slot];
    }
}
