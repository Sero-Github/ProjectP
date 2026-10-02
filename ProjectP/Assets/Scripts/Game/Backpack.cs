public class Backpack : BlockGrid
{
    public static Backpack Instance { get; private set; }

    protected override void Awake()
    {
        Instance = this;
        base.Awake();
    }
}