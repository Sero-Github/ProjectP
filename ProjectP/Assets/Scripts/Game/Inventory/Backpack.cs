using System;

public class Backpack : BlockGrid
{
    public static Backpack Instance { get; private set; }

    // 가방에 블록이 들어오거나 빠질 때 호출된다.
    // 인벤토리 패널이 꺼져 있으면 Instance가 아직 없을 수 있어, 구독 시점과 무관하도록 정적 이벤트로 둔다.
    public static event Action<Backpack> BlocksChanged;

    // 패널이 꺼진 채 저장된 블록을 불러오면 Awake보다 먼저 초기화되므로 Awake 대신 여기서 등록한다.
    protected override void OnInitialized()
    {
        Instance = this;
        Changed += () => BlocksChanged?.Invoke(this);
    }
}
