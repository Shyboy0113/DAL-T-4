
using TMPro;

public interface ICommand
{
    void Execute();
}

public class ClockwiseRotateCommand : ICommand
{
    private readonly StackManager _sm;
    public ClockwiseRotateCommand(StackManager sm)
    {
        _sm = sm;
    }

    public void Execute()
    {
        
        _sm.HandleInput(KeyType.Alt); // ALT 입력
        _sm.UpdateDirection(1); // 시계 방향이므로 1을 전달하여 방향 업데이트
        _sm.RotateArrow(); // 정방향 회전
    }
}
    

public class CounterClockwiseRotateCommand : ICommand
{
    private readonly StackManager _sm;
    public CounterClockwiseRotateCommand(StackManager sm)
    {
        _sm = sm;
    }

    public void Execute()
    {
        _sm.HandleInput(KeyType.Tab); // Tab 입력
        _sm.UpdateDirection(-1);// 반시계 방향이므로 -1을 전달하여 방향 업데이트
        _sm.RotateArrow(); // 역방향 회전
    }
}

public class MoveCommand : ICommand
{
    private readonly StackManager _sm;
    public MoveCommand(StackManager sm)
    {
        _sm = sm;
    }

    public void Execute()
    {
        _sm.HandleInput(KeyType.F4); // F4 입력
        _sm.MovePlayer();
        _sm.ExecuteMoveEvent(); // 플레이어가 움직였다는 이벤트 발동 (MainCameraMovement에서 수신)
    }
}
