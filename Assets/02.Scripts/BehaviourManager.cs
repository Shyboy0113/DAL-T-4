using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TurnState {Player, Tile, Enemy}

public class BehaviourManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerBehaviour playerBehaviour;
    [SerializeField] private EnemyManager enemyManager;
    
    [SerializeField] private MapManager mapManager;
    
    [Header("Turn State")]
    public TurnState currentTurn = TurnState.Player;

    [Header("Command History")]
    private Stack<ICommand> _undoStack = new Stack<ICommand>();
    private Stack<ICommand> _redoStack = new Stack<ICommand>();

    // 플레이어가 실제로 완료한 행동 횟수 (3의 배수마다 적 턴)
    private int _actionCount = 0;

    private void Awake()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _actionCount = 0;
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerTurnStarted += StartPlayerTurn;
        GameEvents.OnEnemyTurnStarted += StartEnemyTurn;
        GameEvents.PlayerDied += StopAllEnemiesTurn;
        GameEvents.PlayerActionFinished += OnPlayerActionFinished;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerTurnStarted -= StartPlayerTurn;
        GameEvents.OnEnemyTurnStarted -= StartEnemyTurn;
        GameEvents.PlayerDied -= StopAllEnemiesTurn;
        GameEvents.PlayerActionFinished -= OnPlayerActionFinished;
    }

    private void Update()
    {
        if (playerBehaviour.CheckSkip()) return;
        
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Z)) UndoTurn();
            if (Input.GetKeyDown(KeyCode.Y)) RedoTurn();
        }
    }

    public void ExecuteCommand(ICommand command)
    {
        if (IsPlayerCommand(command))
        {
            GameEvents.RaiseSaveStateBeforeAction();
        }
        
        command.Execute();
        _undoStack.Push(command);

        // Undo/Redo 중에는 redoStack을 비우지 않습니다.
        // Redo 중 TileCommand.Execute() → HandleToggle() → ExecuteCommand() 체인이
        // 발생할 수 있으며, 이때 Clear()하면 남은 redoStack이 통째로 날아가는 버그가 있습니다.
        if (!playerBehaviour.isUndoRedo)
        {
            _redoStack.Clear();
        }

        // TileCommand는 씬 초기화 시에도 발생하므로 UI 카운트에서 제외
        if (IsPlayerCommand(command) || IsEnemyCommand(command))
        {
            // GameEvents.RaiseUndoRedoCountChanged 호출
            UpdateUndoRedoUI();
        }
    }

    // 플레이어 행동 + 타일 반응이 모두 끝난 시점에 수신
    private void OnPlayerActionFinished()
    {
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;

        // 이동이 완전히 끝난 위치를 MoveCommand에 기록합니다.
        // Redo 시 AddForce/Slide 대신 이 위치로 텔레포트하여
        // 중간 타일들을 물리적으로 지나치는 OnTriggerEnter를 방지합니다.
        RecordMoveDestination();

        _actionCount++;

        if (_actionCount % 3 == 0)
        {
            StartCoroutine(TurnSequence());
        }
    }

    // undoStack 맨 위의 MoveCommand에 현재 위치를 기록합니다.
    private void RecordMoveDestination()
    {
        foreach (var cmd in _undoStack)
        {
            if (cmd is MoveCommand moveCmd)
            {
                moveCmd.RecordNextPosition(
                    playerBehaviour.transform.position,
                    playerBehaviour.IsOnIce()
                );
                return;
            }
            // TileCommand는 건너뜀, 다른 플레이어 커맨드(Rotate)면 MoveCommand 없음
            if (IsPlayerCommand(cmd)) return;
        }
    }

    private IEnumerator TurnSequence()
    {
        if (mapManager != null)
        {
            yield return new WaitUntil(() => !mapManager.IsRotating);
        }
        
        GameEvents.RaiseEnemyTurnStarted(playerBehaviour.transform.position);
        enemyManager.StartAllEnemiesTurn(playerBehaviour.transform.position);
        yield return new WaitUntil(() => !enemyManager.IsAnyEnemyActing);
        GameEvents.RaisePlayerTurnStarted();
    }

    public void UndoTurn()
    {
        if (currentTurn != TurnState.Player) return;
        if (playerBehaviour.CheckSkip()) return; // 회전/이동/적 턴 중엔 금지

        // 플레이어 커맨드가 스택에 없으면 Undo 불가
        if (!HasPlayerCommand(_undoStack)) return;

        playerBehaviour.isUndoRedo = true;

        // 1. 맨 위의 TileCommand들을 Undo (플레이어 행동 직후 쌓인 것)
        PopNonPlayerCommands(_undoStack, _redoStack, undo: true);

        // 2. 플레이어 커맨드 Undo
        if (_undoStack.Count > 0 && IsPlayerCommand(_undoStack.Peek()))
        {
            ICommand playerCommand = _undoStack.Pop();
            _redoStack.Push(playerCommand);
            playerCommand.Undo();

            playerBehaviour.UndoState();
            GameEvents.RaiseUndoTriggered();
            _actionCount = Mathf.Max(0, _actionCount - 1);
        }

        // 참고: 적 커맨드는 플레이어 3번째 행동 이후 스택 맨 위에 쌓입니다.
        // 따라서 3번째 행동을 Undo할 때 step 1의 PopNonPlayerCommands가
        // 적/타일 커맨드를 먼저 처리하고, step 2에서 플레이어 커맨드를 Undo합니다.
        // 1~2번째 행동을 Undo할 때는 step 1에서 처리할 비플레이어 커맨드가 없습니다.

        UpdateUndoRedoUI();
        StartCoroutine(IUndoRedo(false));
    }

    public void RedoTurn()
    {
        if (currentTurn != TurnState.Player) return;
        if (playerBehaviour.CheckSkip()) return; // 회전/이동/적 턴 중엔 금지
        if (!HasPlayerCommand(_redoStack)) return;

        playerBehaviour.isUndoRedo = true;

        // 1. Redo 스택 맨 위 TileCommand들 재실행 (있다면)
        PopNonPlayerCommands(_redoStack, _undoStack, undo: false);

        // 2. 플레이어 커맨드 재실행
        if (_redoStack.Count > 0 && IsPlayerCommand(_redoStack.Peek()))
        {
            ICommand playerCommand = _redoStack.Pop();
            playerCommand.Execute();
            _undoStack.Push(playerCommand);

            KeyType type = ReturnKeyType(playerCommand);
            playerBehaviour.RedoState(type);

            // RaiseActionFinished는 isUndoRedo=true라 차단됩니다.
            // _actionCount는 여기서 직접 증가시킵니다.
            _actionCount++;

            GameEvents.RaiseRedoTriggered();
        }

        // 3. 이 행동에 딸린 비플레이어 커맨드(적/타일) 처리
        //    Redo 스택 맨 위가 비플레이어 커맨드면 이 행동에 연결된 것으로 판단합니다.
        //    - 적 커맨드가 있으면: 스택에서 꺼내 재실행 (TurnSequence는 호출하지 않음)
        //    - 없으면: 3번째 행동이어도 적 턴 없이 넘어감 (정상)
        if (_redoStack.Count > 0 && !IsPlayerCommand(_redoStack.Peek()))
        {
            PopNonPlayerCommands(_redoStack, _undoStack, undo: false);
            // PopNonPlayerCommands 내부에서 EnemyMoveCommand.Execute()가 호출되므로
            // TurnSequence()를 별도로 호출하면 적이 2번 움직입니다. 호출하지 않습니다.
        }

        UpdateUndoRedoUI();
        StartCoroutine(IUndoRedo(false));
    }

    // 스택 맨 위의 비플레이어 커맨드(적/타일)를 대상 스택으로 이동하며 실행/취소
    private void PopNonPlayerCommands(Stack<ICommand> from, Stack<ICommand> to, bool undo)
    {
        while (from.Count > 0 && !IsPlayerCommand(from.Peek()))
        {
            ICommand cmd = from.Pop();
            to.Push(cmd);
            if (undo) cmd.Undo();
            else cmd.Execute();
        }
    }

    // 스택 안에 플레이어 커맨드가 하나라도 있는지 확인
    private bool HasPlayerCommand(Stack<ICommand> stack)
    {
        foreach (var cmd in stack)
        {
            if (IsPlayerCommand(cmd)) return true;
        }
        return false;
    }

    // 플레이어 커맨드 수만 카운트 (TileCommand 제외하여 UI 오활성화 방지)
    private int PlayerCommandCount(Stack<ICommand> stack)
    {
        int count = 0;
        foreach (var cmd in stack)
        {
            if (IsPlayerCommand(cmd)) count++;
        }
        return count;
    }

    #region UndoRedoUI

    private void UpdateUndoRedoUI()
    {
        // 키 시퀀스 UI에서의 Undo/Redo Button의 SetActive를 결정하는 이벤트
        GameEvents.RaiseUndoRedoCountChanged(
            PlayerCommandCount(_undoStack),
            PlayerCommandCount(_redoStack)
        );
    }

    #endregion
    

    public bool IsPlayerCommand(ICommand command)
    {
        return command is MoveCommand ||
               command is ClockwiseRotateCommand ||
               command is CounterClockwiseRotateCommand;
    }

    private bool IsEnemyCommand(ICommand command)
    {
        return command is EnemyMoveCommand || command is EnemyDeathCommand;
    }

    public KeyType ReturnKeyType(ICommand command)
    {
        return command switch
        {
            MoveCommand => KeyType.F4,
            ClockwiseRotateCommand => KeyType.Alt,
            CounterClockwiseRotateCommand => KeyType.Tab,
            _ => KeyType.None
        };
    }

    private void StartPlayerTurn()
    {
        currentTurn = TurnState.Player;
        GameEvents.RaiseInputLockChanged(false);
    }

    private void StartEnemyTurn(Vector3 playerPos)
    {
        currentTurn = TurnState.Enemy;
        GameEvents.RaiseInputLockChanged(true);
    }

    public void Init()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _actionCount = 0;
        currentTurn = TurnState.Player;
        playerBehaviour.InitPlayer();
        enemyManager.InitEnemies();
        GameEvents.RaiseUndoRedoCountChanged(0, 0);
    }

    private void StopAllEnemiesTurn()
    {
        StopAllCoroutines();
        currentTurn = TurnState.Player;
    }

    private IEnumerator IUndoRedo(bool toggle)
    {
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.05f);
        playerBehaviour.isUndoRedo = toggle;
    }
}