using DG.Tweening;
using UnityEngine; // Vector3 및 transform.position 사용 목적

public interface ICommand
{
    void Execute();
    void Undo();
    void Redo();
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
        _playerBehaviour.UpdateSequenceCanvas(-1);
    }

    public void Redo() => Execute();
}

public class CounterClockwiseRotateCommand : ICommand
{
    private readonly PlayerBehaviour _playerBehaviour;
    public CounterClockwiseRotateCommand(PlayerBehaviour playerBehaviour) => _playerBehaviour = playerBehaviour;

    public void Execute()
    {
        _playerBehaviour.CalculateRotationCount(1);
        _playerBehaviour.UpdateDirection(-1);
        _playerBehaviour.RotateArrow();
    }

    public void Undo()
    {
        _playerBehaviour.UpdateDirection(1);
        _playerBehaviour.RotateArrow(true);
        _playerBehaviour.CalculateRotationCount(-1);
        _playerBehaviour.UpdateSequenceCanvas(-1);
    }

    public void Redo() => Execute();
}

public class MoveCommand : ICommand
{
    private readonly PlayerBehaviour _playerBehaviour;
    private Vector3 _previousPosition; // 이동 전 위치 (Undo용)
    private Vector3 _nextPosition;     // 이동 후 위치 (Redo 텔레포트용)
    private bool _nextPositionRecorded = false;
    private Vector2 _moveDirection;    // 이동 방향 (Redo 텔레포트 후 Ice 슬라이드 방향 복원용)

    private bool _wasOnIce;      // 이동 전 얼음 상태 (Undo용)
    private bool _wasOnIceAfter; // 이동 후 얼음 상태 (Redo용)

    public MoveCommand(PlayerBehaviour playerBehaviour) => _playerBehaviour = playerBehaviour;

    public void Execute()
    {
        // 최초 실행: 이동 전 위치와 상태 기록
        _previousPosition = _playerBehaviour.transform.position;
        _wasOnIce = _playerBehaviour.IsOnIce();
        _nextPositionRecorded = false;

        _playerBehaviour.CalculateMoveCount(1);
        _playerBehaviour.MovePlayer();
    }

    // 이동이 완전히 끝난 뒤(Stop 타일 또는 AddForce 안착 후) 호출됩니다.
    // PlayerBehaviour.StopIceAndFinish() 또는 RaiseActionFinished() 시점에 기록합니다.
    public void RecordNextPosition(Vector3 pos, bool isOnIce)
    {
        _nextPosition         = pos;
        _wasOnIceAfter        = isOnIce;
        _moveDirection        = _playerBehaviour.GetLastMoveDirection(); // 이동 완료 후 방향 기록
        _nextPositionRecorded = true;
    }

    public void Undo()
    {
        _playerBehaviour.transform.position = _previousPosition;
        _playerBehaviour.CalculateMoveCount(-1);
        _playerBehaviour.UpdateSequenceCanvas(-1);
        _playerBehaviour.EnableIceMode(_wasOnIce);
        _playerBehaviour.StopVelocity();
    }

    public void Redo()
    {
        // RecordNextPosition()이 호출되지 않은 상태면 Redo 불가
        if (!_nextPositionRecorded) return;

        // 물리 이동 없이 기록된 위치로 즉시 텔레포트합니다.
        // AddForce/Slide를 쓰면 타일들을 물리적으로 지나쳐 OnTriggerEnter가 발생합니다.
        _playerBehaviour.SetLastMoveDirection(_moveDirection);
        _playerBehaviour.TeleportTo(_nextPosition);
        _playerBehaviour.EnableIceMode(_wasOnIceAfter);
        _playerBehaviour.CalculateMoveCount(1);
    }
}

public class DeathCommand : ICommand
{
    public void Execute() { }
    public void Undo()    { }
    public void Redo()    { }
}

#endregion

#region EnemyCommand

public class EnemyMoveCommand : ICommand
{
    private EnemyBehaviour _enemy;
    private Vector3 _targetPosition;       // 이동 목표 월드 좌표
    private Vector3 _previousLocalPosition; // 이동 전 로컬 좌표 (맵 회전에 독립적)

    public EnemyMoveCommand(EnemyBehaviour enemy, Vector3 targetPosition)
    {
        _enemy = enemy;
        _targetPosition = targetPosition;
    }
    
    public void Execute()
    {
        // 로컬 좌표로 저장합니다.
        // 월드 좌표로 저장하면 맵 회전 후 Undo 시 엉뚱한 위치로 복원됩니다.
        _previousLocalPosition = _enemy.transform.localPosition;
        
        _enemy.MoveEnemy(_targetPosition);
    }

    public void Undo()
    {
        _enemy.transform.DOKill();
        _enemy.EnableIceMode(false);
        // 로컬 좌표로 복원합니다.
        _enemy.transform.localPosition = _previousLocalPosition;
    }

    public void Redo() => Execute();
}

public class EnemyDeathCommand : ICommand
{
    private readonly EnemyBehaviour _enemy;
    public EnemyDeathCommand(EnemyBehaviour enemy) => _enemy = enemy;

    public void Execute() => _enemy.PlayExplosion();

    public void Undo() => _enemy.SetDeadState(false);

    // Redo 시 PlayExplosion을 다시 호출하면 이펙트·이벤트 체인이 중복 발생합니다.
    // 상태만 직접 전환합니다.
    public void Redo() => _enemy.SetDeadState(true);
}
    
#endregion

#region TileCommand

public class TileCommand : ICommand
{
    private TileBehaviour _tile;
    private TileStateSnapshot _beforeState;

    private PlayerBehaviour _pb;
    private EnemyBehaviour _eb;

    public TileCommand(TileBehaviour tile, PlayerBehaviour pb = null, EnemyBehaviour eb = null)
    {
        _tile = tile;
        _pb = pb;
        _eb = eb;
        _beforeState = _tile.GetSnapShot();
    }
    public void Execute() => _tile.ApplyTileCommand(_pb, _eb);
    public void Undo()    => _tile.RestoreSnapshot(_beforeState);
    public void Redo()    => _tile.ApplyTileCommand(_pb, _eb);
}

#endregion