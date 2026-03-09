using DG.Tweening;
using UnityEditor;
using UnityEngine; // Vector3 및 transform.position 사용 목적

public interface ICommand
{
    void Execute();
    void Undo();
}

#region PlayerCommand

public class ClockwiseRotateCommand : ICommand
{
    private readonly PlayerBehaviour _playerBehaviour;
    public ClockwiseRotateCommand(PlayerBehaviour playerBehaviour) => _playerBehaviour = playerBehaviour;

    public void Execute()
    {
        _playerBehaviour.CalculateRotationCount(1);
        _playerBehaviour.UpdateDirection(1);
        _playerBehaviour.RotateArrow();
    }

    public void Undo()
    {
        _playerBehaviour.UpdateDirection(-1);
        _playerBehaviour.RotateArrow(true);
        
        _playerBehaviour.CalculateRotationCount(-1);
        _playerBehaviour.UpdateSequenceCanvas(-1); // UI 입력 칸 되돌리기
    }
}

public class CounterClockwiseRotateCommand : ICommand
{
    private readonly PlayerBehaviour _playerBehaviour;
    public CounterClockwiseRotateCommand(PlayerBehaviour PlayerBehaviour) => _playerBehaviour = PlayerBehaviour;

    public void Execute()
    {
        _playerBehaviour.CalculateRotationCount(1);
        _playerBehaviour.UpdateDirection(-1);
        _playerBehaviour.RotateArrow();
    }

    public void Undo()
    {
        //_playerBehaviour.UpdateDirection(1);
        _playerBehaviour.RotateArrow(true);
        _playerBehaviour.CalculateRotationCount(-1);
        _playerBehaviour.UpdateSequenceCanvas(-1); // UI 입력 칸 되돌리기
    }
}

public class MoveCommand : ICommand
{
    private readonly PlayerBehaviour _playerBehaviour;
    private Vector3 _previousPosition; // 이동 전 위치 기억 (Undo용)

    private bool _wasOnIce; // 이동 전 얼음 상태 저장 (Undo용)
    
    public MoveCommand(PlayerBehaviour PlayerBehaviour) => _playerBehaviour = PlayerBehaviour;

    public void Execute()
    {
        // 이동 전 위치와 상태 기록
        _previousPosition = _playerBehaviour.transform.position;
        _wasOnIce = _playerBehaviour.IsOnIce();
        
        _playerBehaviour.CalculateMoveCount(1);
        _playerBehaviour.MovePlayer();
    }

    public void Undo()
    {
        _playerBehaviour.transform.position = _previousPosition;
        
        _playerBehaviour.CalculateMoveCount(-1);
        
        _playerBehaviour.UpdateSequenceCanvas(-1); // 키 시퀀스 UI 업데이트
        
        // IceMode 처리
        _playerBehaviour.EnableIceMode(_wasOnIce);
        _playerBehaviour.StopVelocity();
    }
}

#endregion

#region EnemyCommand

public class EnemyMoveCommand : ICommand
{
    private EnemyBehaviour _enemy;
    private Vector3 _targetPosition;
    private Vector3 _previousPosition;

    public EnemyMoveCommand(EnemyBehaviour enemy, Vector3 targetPosition)
    {
        _enemy = enemy;
        _targetPosition = targetPosition;
    }
    
    public void Execute()
    {
        _previousPosition = _enemy.transform.position;
        
        _enemy.transform.DOMove(_targetPosition, 0.25f).SetEase(Ease.OutBounce);
    }

    public void Undo()
    {
        _enemy.transform.DOKill();
        _enemy.transform.position = _previousPosition;
    }
}

public class EnemyDeathCommand : ICommand
{
    private EnemyBehaviour _enemy;

    public EnemyDeathCommand(EnemyBehaviour enemy) => _enemy = enemy;

    public void Execute()
    {
        _enemy.SetDeadState(true);
    }

    public void Undo()
    {
        _enemy.SetDeadState(false);
    }
}
    
#endregion