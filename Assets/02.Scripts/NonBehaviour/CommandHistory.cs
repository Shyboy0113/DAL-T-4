using System.Collections.Generic;

public class CommandHistory
{
    private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();

    public bool HasUndo => HasPlayerCommand(_undoStack);

    public void Push(ICommand command)
    {
        _undoStack.Push(command);
    }

    // 플레이어 커맨드 위에 쌓인 비플레이어 커맨드들을 Undo/처리
    public void PopNonPlayerCommands(bool undo)
    {
        while (_undoStack.Count > 0 && !IsPlayerCommand(_undoStack.Peek()))
        {
            ICommand cmd = _undoStack.Pop();
            if (undo) cmd.Undo();
        }
    }

    // undo 스택 맨 위 플레이어 커맨드를 꺼내 반환 (Undo 전용)
    public ICommand PopUndoPlayerCommand()
    {
        if (_undoStack.Count == 0 || !IsPlayerCommand(_undoStack.Peek())) return null;
        return _undoStack.Pop();
    }

    public int UndoPlayerCommandCount() => CountPlayerCommands(_undoStack);

    public void Clear() => _undoStack.Clear();

    private bool HasPlayerCommand(Stack<ICommand> stack)
    {
        foreach (var cmd in stack)
            if (IsPlayerCommand(cmd)) return true;
        return false;
    }

    private int CountPlayerCommands(Stack<ICommand> stack)
    {
        int count = 0;
        foreach (var cmd in stack)
            if (IsPlayerCommand(cmd)) count++;
        return count;
    }

    public static bool IsPlayerCommand(ICommand command)
    {
        return command is MoveCommand
            || command is ClockwiseRotateCommand
            || command is CounterClockwiseRotateCommand;
    }

    public static bool IsEnemyCommand(ICommand command)
    {
        return command is EnemyMoveCommand || command is EnemyDeathCommand;
    }
}
