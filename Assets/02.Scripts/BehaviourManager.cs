using System.Collections;
using UnityEngine;

public enum TurnState { Player, Tile, Enemy }

public class BehaviourManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerBehaviour           playerBehaviour;
    [SerializeField] private PlayerUndoStateBridge undoState;
    [SerializeField] private EnemyManager              enemyManager;
    [SerializeField] private MapManager                mapManager;

    [Header("Turn State")]
    public TurnState currentTurn = TurnState.Player;
    private readonly CommandHistory _history = new CommandHistory();

    private void Awake()
    {
        _history.Clear();
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerTurnStarted  += StartPlayerTurn;
        GameEvents.OnEnemyTurnStarted   += StartEnemyTurn;
        GameEvents.PlayerDied           += StopAllEnemiesTurn;
        GameEvents.PlayerActionFinished += OnPlayerActionFinished;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerTurnStarted  -= StartPlayerTurn;
        GameEvents.OnEnemyTurnStarted   -= StartEnemyTurn;
        GameEvents.PlayerDied           -= StopAllEnemiesTurn;
        GameEvents.PlayerActionFinished -= OnPlayerActionFinished;
    }

    public void ExecuteCommand(ICommand command)
    {
        if (CommandHistory.IsPlayerCommand(command))
            GameEvents.RaiseSaveStateBeforeAction(playerBehaviour);

        _history.Push(command);
        command.Execute();

        if (CommandHistory.IsPlayerCommand(command) || CommandHistory.IsEnemyCommand(command))
            UpdateUndoUI();
    }

    private void OnPlayerActionFinished()
    {
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;
        
        GameEvents.RaisePlayerActed(playerBehaviour.TotalActionCount);

        if (playerBehaviour.TotalActionCount % 3 == 0)
            StartCoroutine(TurnSequence());
    }

    private IEnumerator TurnSequence()
    {
        if (mapManager != null)
            yield return new WaitUntil(() => !mapManager.IsRotating);


        yield return new WaitForSeconds(0.1f);
        if (GameManager.Instance.isGameOver) yield break;

        GameEvents.RaiseEnemyTurnStarted(playerBehaviour.transform.position);
        enemyManager.StartAllEnemiesTurn(playerBehaviour.transform.position);
        yield return new WaitUntil(() => !enemyManager.IsAnyEnemyActing);
        GameEvents.RaisePlayerTurnStarted();
    }

    public void UndoTurn()
    {
        if (currentTurn != TurnState.Player) return;
        if (playerBehaviour.CheckSkip())     return;
        if (undoState.IsUndo)            return;
        if (!_history.HasUndo)               return;

        undoState.BeginUndo();

        // 1. 플레이어 커맨드 위에 쌓인 비플레이어 커맨드(타일/적) 먼저 Undo
        _history.PopNonPlayerCommands(undo: true);

        // 2. 플레이어 커맨드 Undo
        ICommand playerCommand = _history.PopUndoPlayerCommand();
        if (playerCommand != null)
        {
            GameEvents.RaiseUndoTriggered();
            
            playerCommand.Undo();
            playerBehaviour.UndoState();
            
            GameEvents.RaisePlayerActed(playerBehaviour.TotalActionCount);
            
            if (playerCommand is MoveCommand)
                GameEvents.RaisePlayerMoved(playerBehaviour.moveCount);
            else if (playerCommand is ClockwiseRotateCommand || playerCommand is CounterClockwiseRotateCommand)
                GameEvents.RaisePlayerRotated(playerBehaviour.rotationCount);
        }

        UpdateUndoUI();
        StartCoroutine(EndUndoAfterSync());
    }

    private void UpdateUndoUI()
    {
        GameEvents.RaiseUndoCountChanged(
            _history.UndoPlayerCommandCount(), 0);
    }

    public KeyType ReturnKeyType(ICommand command)
    {
        return command switch
        {
            MoveCommand                   => KeyType.F4,
            ClockwiseRotateCommand        => KeyType.Alt,
            CounterClockwiseRotateCommand => KeyType.Tab,
            _                             => KeyType.None
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
        _history.Clear();
        currentTurn = TurnState.Player;

        undoState.Reset();
        playerBehaviour.InitPlayer();
        enemyManager.InitEnemies();

        if (mapManager != null) mapManager.Init();

        GameEvents.RaiseUndoCountChanged(0, 0);
    }

    private void StopAllEnemiesTurn()
    {
        StopAllCoroutines();
        undoState.Reset();
        currentTurn = TurnState.Player;
    }

    private IEnumerator EndUndoAfterSync()
    {
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.05f);
        undoState.EndUndo();
    }
}
