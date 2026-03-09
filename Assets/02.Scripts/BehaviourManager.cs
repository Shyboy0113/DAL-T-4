using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TurnState {Player, Enemy, Processing}

public class BehaviourManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerBehaviour playerBehaviour; // 직접 참조
    [SerializeField] private EnemyManager enemyManager;   // 적들을 관리하는 매니저
    
    [Header("Turn State")]
    public TurnState currentTurn = TurnState.Player;

    [Header("Command History")]
    private Stack<ICommand> _undoStack = new Stack<ICommand>();
    private Stack<ICommand> _redoStack = new Stack<ICommand>();
    
    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    private int _actionCount = 0; // 플레이어 행동 횟수 카운트

    private void OnEnable()
    {
        GameEvents.OnPlayerTurnStarted += StartPlayerTurn;
        GameEvents.OnEnemyTurnStarted += StartEnemyTurn;
        GameEvents.PlayerDied += StopAllEnemiesTurn;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerTurnStarted -= StartPlayerTurn;
        GameEvents.OnEnemyTurnStarted -= StartEnemyTurn;
        GameEvents.PlayerDied -= StopAllEnemiesTurn;
    }

    private void Update()
    {
        if (playerBehaviour.CheckSkip()) return;
        
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if(Input.GetKeyDown(KeyCode.Z))
            {
                UndoTurn();
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
                RedoTurn();
            }
        }
    }

    public void ExecuteCommand(ICommand command)
    {
        // 실행 전 상태 저장 이벤트 발생
        if (IsPlayerTurn(command))
        {
            GameEvents.RaiseSaveStateBeforeAction();
        }
        
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear(); // 새로운 행동 시 Redo 초기화
        
        // UI 업데이트 등 필요한 이벤트 호출
        GameEvents.RaiseUndoRedoCountChanged(_undoStack.Count, _redoStack.Count);

        // 플레이어 행동이 끝났다면 적 턴으로 전환
        if (IsPlayerTurn(command))
        {
            _actionCount++;

            if (_actionCount % 3 == 0 && !GameManager.Instance.isGameOver)
            {
                StartCoroutine(TurnSequence());
            }
        }
        
    }

    private IEnumerator TurnSequence()
    {
        // 현재 턴 상태를 Enemy로 전환
        GameEvents.RaiseEnemyTurnStarted(playerBehaviour.transform.position);
        
        // 적들에게 플레이어 위치를 주고 행동 개시
        enemyManager.StartAllEnemiesTurn(playerBehaviour.transform.position);
        
        // 적들의 모든 행동(애니메이션 포함)이 끝날 때까지 대기하는 로직
        yield return new WaitUntil(() => !enemyManager.IsAnyEnemyActing);

        GameEvents.RaisePlayerTurnStarted();

    }
    
    public void UndoTurn()
    {
        if (_undoStack.Count <= 0 || currentTurn != TurnState.Player) return;

        playerBehaviour.isUndoRedo = true;
        
        // 1. 적의 행동 취소
        while (_undoStack.Count > 0 && !IsPlayerTurn(_undoStack.Peek()))
        {
            ICommand enemyCommand = _undoStack.Pop();
            enemyCommand.Undo();
            _redoStack.Push(enemyCommand);
        }

        // 2. 플레이어의 행동 취소
        if (_undoStack.Count > 0 && IsPlayerTurn(_undoStack.Peek()))
        {
            ICommand playerCommand = _undoStack.Pop();
            playerCommand.Undo();
            _redoStack.Push(playerCommand);
            
            playerBehaviour.UndoState();
            GameEvents.RaiseUndoTriggered();
            
            _actionCount = Mathf.Max(0, _actionCount - 1);
            
        }
        
        GameEvents.RaiseUndoRedoCountChanged(_undoStack.Count, _redoStack.Count);
        StartCoroutine(IUndoRedo(false));
    }

    public void RedoTurn()
    {
        if (_redoStack.Count <= 0 || currentTurn != TurnState.Player) return;

        playerBehaviour.isUndoRedo = true;
        
        // 1. 플레이어 행동 재실행
        ICommand playerCommand = _redoStack.Pop();
        playerCommand.Execute();
        _undoStack.Push(playerCommand);

        KeyType type = ReturnKeyType(playerCommand);
        playerBehaviour.RedoState(type);
        _actionCount++;
        
        GameEvents.RaiseRedoTriggered();

        if (_actionCount % 3 == 0)
        {
            while (_redoStack.Count > 0 && !IsPlayerTurn(_redoStack.Peek()))
            {
                ICommand enemyCommand = _redoStack.Pop();
                enemyCommand.Execute();
                _undoStack.Push(enemyCommand);
            }
        }

        GameEvents.RaiseUndoRedoCountChanged(_undoStack.Count, _redoStack.Count);
        playerBehaviour.isUndoRedo = false;
        Physics2D.SyncTransforms();
    }
    
    public bool IsPlayerTurn(ICommand command)
    {
        return (command is MoveCommand ||
                command is ClockwiseRotateCommand ||
                command is CounterClockwiseRotateCommand);
    }

    public KeyType ReturnKeyType(ICommand command)
    {
        KeyType type = command switch
        {
            MoveCommand            => KeyType.F4,
            ClockwiseRotateCommand => KeyType.Alt,
            CounterClockwiseRotateCommand => KeyType.Tab,
            _ => KeyType.None
        };
        return type;
    }

    private void StartPlayerTurn()
    {
        currentTurn = TurnState.Player;
        GameEvents.RaiseInputLockChanged(false); // 플레이어 조작 해제
    }

    private void StartEnemyTurn(Vector3 playerPos)
    {
        currentTurn = TurnState.Enemy;
        GameEvents.RaiseInputLockChanged(true); // 플레이어 조작 잠금
    }

    public void Init()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        playerBehaviour.InitPlayer();
        enemyManager.InitEnemies();
    }
    
    // 플레이어가 죽었을 때 호출
    private void StopAllEnemiesTurn()
    {
        StopAllCoroutines();
        currentTurn = TurnState.Player; // 상태 초기화
    }

    private IEnumerator IUndoRedo(bool toggle)
    {
        Physics2D.SyncTransforms();
        
        yield return new WaitForSeconds(0.05f);
        
        playerBehaviour.isUndoRedo = toggle;
    }
    
}
