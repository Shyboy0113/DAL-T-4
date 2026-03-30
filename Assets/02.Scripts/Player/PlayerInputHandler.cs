using UnityEngine;

/// <summary>
/// 키 입력을 감지해 커맨드를 생성하고 BehaviourManager에 전달합니다.
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] private BehaviourManager  behaviourManager;
    [SerializeField] private PlayerBehaviour   playerBehaviour;
    [SerializeField] private SoundEffectPlayer soundEffectPlayer;
    [SerializeField] private MapManager mapManager;
    
    [SerializeField] private AudioClip rotateSound;
    [SerializeField] private AudioClip moveSound;

    private void Update()
    {
        if (playerBehaviour.CheckSkip()) return;
        if (GameManager.Instance.isChatting) return; // 채팅 중 게임 입력 차단

        // Undo (Redo 제거)
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Z)) behaviourManager.UndoTurn();
            return;
        }
        
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;

        // 플레이어 액션
        if (Input.GetKeyDown(KeyCode.LeftAlt) && GameManager.Instance.canUseLeftALT)
            EnqueueCommand(new ClockwiseRotateCommand(playerBehaviour), KeyType.Alt, rotateSound);

        if (Input.GetKeyDown(KeyCode.Tab) && GameManager.Instance.canUseTAB)
            EnqueueCommand(new CounterClockwiseRotateCommand(playerBehaviour), KeyType.Tab, rotateSound);

        if (Input.GetKeyDown(KeyCode.F4) && GameManager.Instance.canUseF4)
            EnqueueCommand(new MoveCommand(playerBehaviour), KeyType.F4, moveSound);
    }

    private void EnqueueCommand(ICommand command, KeyType keyType, AudioClip sound)
    {
        bool isMapChange = playerBehaviour.HandleInput(keyType);

        if (isMapChange)
        {
            behaviourManager.ExecuteCommand(new TileMapChangeCommand(mapManager, playerBehaviour));
            //return;
        }
        
        // 게임오버(Alt+F4 등)가 발생해도 커맨드는 실행합니다.
        // actionCount는 플레이어 행동이 있을 때 무조건 올라가야 하며,
        // OnPlayerActionFinished()의 isGameOver 체크가 적 턴 등 부수 효과를 차단합니다.
        behaviourManager.ExecuteCommand(command);

        if (GameManager.Instance.isGameOver) return;
        soundEffectPlayer.PlaySoundEffect(sound);
    }
}
