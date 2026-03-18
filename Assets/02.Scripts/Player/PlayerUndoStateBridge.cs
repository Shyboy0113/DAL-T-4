using UnityEngine;

/// <summary>
/// PlayerUndoRedoState(순수 C#)를 MonoBehaviour로 감싸서
/// 인스펙터 참조 및 컴포넌트 접근이 가능하게 합니다.
/// </summary>
public class PlayerUndoStateBridge : MonoBehaviour
{
    private readonly PlayerUndoState _state = new PlayerUndoState();

    public bool IsUndo => _state.IsUndo;

    public void BeginUndo() => _state.BeginUndo();
    public void EndUndo()   => _state.EndUndo();
    public void Reset()     => _state.Reset();
}