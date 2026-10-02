using System.Collections.Generic;
using UnityEngine.EventSystems;

// 블록을 드래그해서 내려놓을 수 있는 대상(가방, 창고, 병합 슬롯 등).
public interface IBlockDropTarget
{
    // 드롭 위치에 블록을 받을 수 있으면 배치하고 true를 반환한다.
    bool TryAccept(Block block, PointerEventData eventData);

    // 블록이 드래그로 이 대상에서 빠져나갈 때 호출된다.
    void Detach(Block block);

    // 드롭에 실패했을 때 Detach 직전 상태로 되돌린다.
    void Restore(Block block);

    // Detach된 블록이 다른 대상에 놓여 이 대상을 완전히 떠났을 때 호출된다.
    void Release(Block block);
}

// 활성화된 드롭 대상 목록. 비활성 패널의 대상은 OnDisable에서 빠지므로 판정에서 자동으로 제외된다.
public static class BlockDropTargets
{
    private static readonly List<IBlockDropTarget> targets = new List<IBlockDropTarget>();

    public static IReadOnlyList<IBlockDropTarget> All => targets;

    public static void Register(IBlockDropTarget target)
    {
        if (!targets.Contains(target))
        {
            targets.Add(target);
        }
    }

    public static void Unregister(IBlockDropTarget target)
    {
        targets.Remove(target);
    }
}