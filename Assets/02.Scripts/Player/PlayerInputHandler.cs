using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] private BehaviourManager  behaviourManager;
    [SerializeField] private PlayerBehaviour   playerBehaviour;
    [SerializeField] private SoundEffectPlayer soundEffectPlayer;
    [SerializeField] private MapManager        mapManager;

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
            || GameManager.Instance.isPaused) return;

        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Z)) behaviourManager.UndoTurn();
            return;
        }

        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;

        if (Input.GetKeyDown(KeyCode.LeftAlt) && GameManager.Instance.CanUseKey(KeyType.Alt))
            EnqueueCommand(new ClockwiseRotateCommand(playerBehaviour), KeyType.Alt, rotateSound);

        if (Input.GetKeyDown(KeyCode.Tab) && GameManager.Instance.CanUseKey(KeyType.Tab))
            EnqueueCommand(new CounterClockwiseRotateCommand(playerBehaviour), KeyType.Tab, rotateSound);

        if (Input.GetKeyDown(KeyCode.F4) && GameManager.Instance.CanUseKey(KeyType.F4))
            EnqueueCommand(new MoveCommand(playerBehaviour), KeyType.F4, moveSound);
    }

    private void OnChatRotateCW()
    {
        if (playerBehaviour.CheckSkip()) return;
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;
        if (!GameManager.Instance.CanUseKey(KeyType.Alt)) return;
        EnqueueCommand(new ClockwiseRotateCommand(playerBehaviour), KeyType.Alt, rotateSound);
    }

    private void OnChatRotateCCW()
    {
        if (playerBehaviour.CheckSkip()) return;
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;
        if (!GameManager.Instance.CanUseKey(KeyType.Tab)) return;
        EnqueueCommand(new CounterClockwiseRotateCommand(playerBehaviour), KeyType.Tab, rotateSound);
    }

    private void OnChatMove()
    {
        if (playerBehaviour.CheckSkip()) return;
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;
        if (!GameManager.Instance.CanUseKey(KeyType.F4)) return;
        EnqueueCommand(new MoveCommand(playerBehaviour), KeyType.F4, moveSound);
    }

    private void OnChatSuicide()
    {
        if (playerBehaviour.CheckSkip()) return;
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;
        EnqueueCommand(new SuicideCommand(playerBehaviour), KeyType.None, null);
    }

    private void EnqueueCommand(ICommand command, KeyType keyType, AudioClip sound)
    {
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

        behaviourManager.ExecuteCommand(command);

        if (GameManager.Instance.isGameOver) return;
        soundEffectPlayer.PlaySoundEffect(sound);
    }
}