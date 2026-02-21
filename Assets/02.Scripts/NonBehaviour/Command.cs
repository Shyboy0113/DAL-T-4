public interface ICommand
{
    void Execute();
}

public class ClockwiseRotateCommand : ICommand
{
    private readonly StackManager _sm;
    public ClockwiseRotateCommand(StackManager sm) => _sm = sm;

    public void Execute()
    {
        _sm.HandleInput(KeyType.Alt);
        _sm.UpdateDirection(1); 
        _sm.RotateArrow(); 
    }
}

public class CounterClockwiseRotateCommand : ICommand
{
    private readonly StackManager _sm;
    public CounterClockwiseRotateCommand(StackManager sm) => _sm = sm;

    public void Execute()
    {
        _sm.HandleInput(KeyType.Tab);
        _sm.UpdateDirection(-1);
        _sm.RotateArrow();
    }
}

public class MoveCommand : ICommand
{
    private readonly StackManager _sm;
    public MoveCommand(StackManager sm) => _sm = sm;

    public void Execute()
    {
        _sm.HandleInput(KeyType.F4);
        _sm.MovePlayer();
    }
}