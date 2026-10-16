using System;

public class Backpack : BlockGrid
{
    public static Backpack Instance { get; private set; }

    // 가방에 블록이 들어오거나 빠질 때 호출된다.
    // 인벤토리 패널이 꺼져 있으면 Instance가 아직 없을 수 있어, 구독 시점과 무관하도록 정적 이벤트로 둔다.
    public static event Action<Backpack> BlocksChanged;

    protected override void Awake()
    {
        Instance = this;
        base.Awake();

        Changed += () => BlocksChanged?.Invoke(this);
    }
}