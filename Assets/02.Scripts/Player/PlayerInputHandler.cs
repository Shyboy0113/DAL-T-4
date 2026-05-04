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

    private void OnEnable()
    {
        GameEvents.ChatCommandRotateCW  += OnChatRotateCW;
        GameEvents.ChatCommandRotateCCW += OnChatRotateCCW;
        GameEvents.ChatCommandMove      += OnChatMove;
        GameEvents.ChatCommandSuicide   += OnChatSuicide;
    }

    private void OnDisable()
    {
        GameEvents.ChatCommandRotateCW  -= OnChatRotateCW;
        GameEvents.ChatCommandRotateCCW -= OnChatRotateCCW;
        GameEvents.ChatCommandMove      -= OnChatMove;
        GameEvents.ChatCommandSuicide   -= OnChatSuicide;
    }

    private void Update()
    {
        if (playerBehaviour.CheckSkip()) return;
        if (GameManager.Instance is null
            || GameManager.Instance.isChatting
            || GameManager.Instance.isOption
            || GameManager.Instance.isPaused ) return; // 채팅 중 게임 입력 차단

        // Undo (Redo 제거)
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Z)) behaviourManager.UndoTurn();
            return;
        }
        
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;

        // 플레이어 액션
        if (Input.GetKeyDown(KeyCode.LeftAlt) && (GameManager.Instance.currentStageData?.canUseLeftALT ?? true))
            EnqueueCommand(new ClockwiseRotateCommand(playerBehaviour), KeyType.Alt, rotateSound);

        if (Input.GetKeyDown(KeyCode.Tab) && (GameManager.Instance.currentStageData?.canUseTAB ?? false))
            EnqueueCommand(new CounterClockwiseRotateCommand(playerBehaviour), KeyType.Tab, rotateSound);

        if (Input.GetKeyDown(KeyCode.F4) && (GameManager.Instance.currentStageData?.canUseF4 ?? true))
            EnqueueCommand(new MoveCommand(playerBehaviour), KeyType.F4, moveSound);
    }

    // ── 채팅 커맨드 핸들러 (키보드 입력과 동일한 조건/효과) ─────────────────────
    private void OnChatRotateCW()
    {
        if (playerBehaviour.CheckSkip()) return;
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;
        if (!(GameManager.Instance.currentStageData?.canUseLeftALT ?? true)) return;
        EnqueueCommand(new ClockwiseRotateCommand(playerBehaviour), KeyType.Alt, rotateSound);
    }

    private void OnChatRotateCCW()
    {
        if (playerBehaviour.CheckSkip()) return;
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;
        if (!(GameManager.Instance.currentStageData?.canUseTAB ?? false)) return;
        EnqueueCommand(new CounterClockwiseRotateCommand(playerBehaviour), KeyType.Tab, rotateSound);
    }

    private void OnChatMove()
    {
        if (playerBehaviour.CheckSkip()) return;
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;
        if (!(GameManager.Instance.currentStageData?.canUseF4 ?? true)) return;
        EnqueueCommand(new MoveCommand(playerBehaviour), KeyType.F4, moveSound);
    }
    
    private void OnChatSuicide()
    {
        if (playerBehaviour.CheckSkip()) return;
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;

        // 자살 커맨드 생성 후 스택에 추가 (자살은 특정 키 입력 횟수(KeyType)를 올리지 않으려면 KeyType.None 전달)
        EnqueueCommand(new SuicideCommand(playerBehaviour), KeyType.None, null);
    }

    private void EnqueueCommand(ICommand command, KeyType keyType, AudioClip sound)
    {
        // 키 사용 횟수 증가 (미션 달성 판정 + 누적 도전과제 추적용)
        switch (keyType)
        {
            case KeyType.Alt: GameManager.Instance.pushedNumberALT++; break;
            case KeyType.F4:  GameManager.Instance.pushedNumberF4++;  break;
            case KeyType.Tab: GameManager.Instance.pushedNumberTAB++; break;
        }
        GameEvents.RaiseKeyUsed(keyType);

        bool isMapChange = playerBehaviour.HandleInput(keyType);

        if (isMapChange)
        {
            behaviourManager.ExecuteCommand(new TileMapChangeCommand(mapManager, playerBehaviour));
            
            Debug.Log("PlayerInputHandler : 맵 변환 커맨드를 실행시킵니다.");
        }
        
        // 게임오버(Alt+F4 등)가 발생해도 커맨드는 실행합니다.
        // actionCount는 플레이어 행동이 있을 때 무조건 올라가야 하며,
        // OnPlayerActionFinished()의 isGameOver 체크가 적 턴 등 부수 효과를 차단합니다.
        behaviourManager.ExecuteCommand(command);

        if (GameManager.Instance.isGameOver) return;
        soundEffectPlayer.PlaySoundEffect(sound);
    }
}
