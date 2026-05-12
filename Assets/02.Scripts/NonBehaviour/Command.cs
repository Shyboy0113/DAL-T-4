using DG.Tweening;
using UnityEngine;

public interface ICommand
{
    void Execute();
    void Undo();
}

#region PlayerCommand

public class ClockwiseRotateCommand : ICommand
{
    private readonly PlayerBehaviour _pb;
    private readonly bool            _isMap1; // 키를 누른 시점의 맵 캡처

    public ClockwiseRotateCommand(PlayerBehaviour pb)
    {
        _pb = pb;
        _isMap1          = pb.IsMap1Layer();
    }

    public void Execute()
    {
        _pb.CalculateRotationCount(1, _isMap1);
        _pb.UpdateDirection(1);
        _pb.RotateArrow(isMap1Override: _isMap1);
    }

    public void Undo()
    {
        _pb.UpdateDirection(-1);
        _pb.RotateArrow(immediate: true, isMap1Override: _isMap1);
        _pb.CalculateRotationCount(-1, _isMap1);
        _pb.UpdateSequenceCanvas(-1);
    }
}

public class CounterClockwiseRotateCommand : ICommand
{
    private readonly PlayerBehaviour _pb;
    private readonly bool            _isMap1; // 키를 누른 시점의 맵 캡처

    public CounterClockwiseRotateCommand(PlayerBehaviour pb)
    {
        _pb = pb;
        _isMap1          = pb.IsMap1Layer();
    }

    public void Execute()
    {
        _pb.CalculateRotationCount(1, _isMap1);
        _pb.UpdateDirection(-1);
        _pb.RotateArrow(isMap1Override: _isMap1);
    }

    public void Undo()
    {
        _pb.UpdateDirection(1);
        _pb.RotateArrow(immediate: true, isMap1Override: _isMap1);
        _pb.CalculateRotationCount(-1, _isMap1);
        _pb.UpdateSequenceCanvas(-1);
    }
}

public class MoveCommand : ICommand
{
    private readonly PlayerBehaviour _pb;
    private Vector3                  _previousPosition;
    private readonly bool            _isMap1; // 키를 누른 시점의 맵 캡처

    public MoveCommand(PlayerBehaviour pb)
    {
        _pb = pb;
        _isMap1          = pb.IsMap1Layer();
    }

    public void Execute()
    {
        Debug.Log("MoveCommand Execute");
        
        _previousPosition = _pb.transform.position;
        _pb.CalculateMoveCount(1, _isMap1);
        _pb.MovePlayer();
    }

    public void Undo()
    {
        Debug.Log("MoveCommand Undo");
        
        _pb.EnableIceMode(false);
        _pb.transform.position = _previousPosition;
        _pb.CalculateMoveCount(-1, _isMap1);
        _pb.UpdateSequenceCanvas(-1);
        _pb.StopVelocity();
    }
}

public class SuicideCommand : ICommand
{
    private readonly PlayerBehaviour _pb;

    public SuicideCommand(PlayerBehaviour pb)
    {
        _pb = pb;
    }

    public void Execute()
    {
        _pb.SetDeadState(true);
    }

    public void Undo()
    {
        _pb.SetDeadState(false);
    }
}

#endregion

#region EnemyCommand

public class EnemyMoveCommand : ICommand
{
    private readonly EnemyBehaviour _enemy;
    private readonly Vector3        _targetPosition;
    private Vector3                 _previousLocalPosition;

    public EnemyMoveCommand(EnemyBehaviour enemy, Vector3 targetPosition)
    {
        _enemy          = enemy;
        _targetPosition = targetPosition;
    }

    public void Execute()
    {
        _previousLocalPosition = _enemy.transform.localPosition;
        _enemy.MoveEnemy(_targetPosition);
    }

    public void Undo()
    {
        if (_enemy == null || !_enemy.gameObject.activeInHierarchy) return;
        _enemy.transform.DOKill();
        _enemy.EnableIceMode(false);
        _enemy.transform.localPosition = _previousLocalPosition;
    }
}

public class EnemyDeathCommand : ICommand
{
    private readonly EnemyBehaviour _enemy;

    public EnemyDeathCommand(EnemyBehaviour enemy) => _enemy = enemy;

    public void Execute() => _enemy.PlayExplosion();
    public void Undo()    => _enemy.SetDeadState(false);
}

#endregion

#region TileCommand

public class TileCommand : ICommand
{
    private readonly TileBehaviour     _tile;
    private readonly TileStateSnapshot _beforeState;
    private readonly PlayerBehaviour   _pb;
    private readonly EnemyBehaviour    _eb;

    public TileCommand(TileBehaviour tile, PlayerBehaviour pb = null, EnemyBehaviour eb = null)
    {
        _tile        = tile;
        _pb          = pb;
        _eb          = eb;
        _beforeState = _tile.GetSnapShot();
    }

    public void Execute() => _tile.ApplyTileCommand(_pb, _eb);
    public void Undo()    => _tile.RestoreSnapshot(_beforeState);
}

public class TileMapChangeCommand : ICommand
{
    private readonly MapManager _mapManager;
    private readonly PlayerBehaviour _player;

    public TileMapChangeCommand(MapManager mapManager, PlayerBehaviour player)
    {
        _mapManager = mapManager;
        _player = player;
    }

    public void Execute()
    {
        GameEvents.RaiseTileMapChanged();
        GameEvents.RaiseMapSwitched();
    }

    public void Undo()
    {
        GameEvents.RaiseTileMapChanged(); // 다시 전환하면 원래대로
    }
} 

// Command.cs에 추가
public class TileBreakCommand : ICommand
{
    private readonly TileBehaviour _tile;
    public TileBreakCommand(TileBehaviour tile) => _tile = tile;
    public void Execute() => _tile.ApplyBreak();
    public void Undo()    => _tile.RevertBreak();
}

#endregion