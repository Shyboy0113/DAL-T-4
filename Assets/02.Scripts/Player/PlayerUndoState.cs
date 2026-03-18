public class PlayerUndoState
{
    public bool IsUndo { get; private set; }

    public void BeginUndo() => IsUndo = true;
    public void EndUndo()   => IsUndo = false;

    public void Reset() => IsUndo = false;
}