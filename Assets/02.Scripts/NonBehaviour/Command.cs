using UnityEngine; // Vector3 및 transform.position 사용 목적

public interface ICommand
{
    void Execute();
    void Undo();
}

#region PlayerCommand

public class ClockwiseRotateCommand : ICommand
{
    private readonly StackManager _sm;
    public ClockwiseRotateCommand(StackManager stackManager) => _sm = stackManager;

    public void Execute()
    {
        _sm.CalculateRotationCount(1);
        _sm.UpdateDirection(1);
        _sm.RotateArrow(); 
    }

    public void Undo()
    {
        _sm.UpdateDirection(-1);
        _sm.RotateArrow(true);
        
        _sm.CalculateRotationCount(-1);
        _sm.UpdateSequenceCanvas(-1); // UI 입력 칸 되돌리기
    }
}

public class CounterClockwiseRotateCommand : ICommand
{
    private readonly StackManager _sm;
    public CounterClockwiseRotateCommand(StackManager stackManager) => _sm = stackManager;

    public void Execute()
    {
        _sm.CalculateRotationCount(1);
        _sm.UpdateDirection(-1);
        _sm.RotateArrow();
    }

    public void Undo()
    {
        //_sm.UpdateDirection(1);
        _sm.RotateArrow(true);
        _sm.CalculateRotationCount(-1);
        _sm.UpdateSequenceCanvas(-1); // UI 입력 칸 되돌리기
    }
}

public class MoveCommand : ICommand
{
    private readonly StackManager _sm;
    private Vector3 _previousPosition; // 이동 전 위치 기억 (Undo용)

    private bool _wasOnIce; // 이동 전 얼음 상태 저장 (Undo용)
    
    public MoveCommand(StackManager stackManager) => _sm = stackManager;

    public void Execute()
    {
        // 이동 전 위치와 상태 기록
        _previousPosition = _sm.transform.position;
        _wasOnIce = _sm.IsOnIce();
        
        _sm.CalculateMoveCount(1);
        _sm.MovePlayer();
    }

    public void Undo()
    {
        _sm.transform.position = _previousPosition;
        
        _sm.CalculateMoveCount(-1);
        
        _sm.UpdateSequenceCanvas(-1); // 키 시퀀스 UI 업데이트
        
        // IceMode 처리
        _sm.EnableIceMode(_wasOnIce);
        _sm.StopVelocity();
    }
}

#endregion

#region EnemyCommand

public class EnemyMoveCommand : ICommand
{
    private EnemyBehaviour _enemy;
    public EnemyMoveCommand(EnemyBehaviour enemy) => _enemy = enemy;
    
    private Vector3 _previousPosition;
    
    public void Execute()
    {
        _previousPosition = _enemy.transform.position;
        _enemy.TakeTurn();
    }

    public void Undo()
    {
        _enemy.transform.position = _previousPosition;
    }
}


#endregion