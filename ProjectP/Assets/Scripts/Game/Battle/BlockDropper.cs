using UnityEngine;

// 적을 처치하면 일정 확률로 무작위 블록을 만들어 창고에 넣는다.
public class BlockDropper : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float dropChance = 0.3f;
    // 인벤토리 패널이 꺼져 있으면 Storage.Instance가 아직 없으므로 인스펙터에서 연결해 둔다.
    [SerializeField] private Storage storage;

    private void OnEnable()
    {
        Enemy.Killed += OnEnemyKilled;
    }

    private void OnDisable()
    {
        Enemy.Killed -= OnEnemyKilled;
    }

    private void OnEnemyKilled(Enemy enemy)
    {
        if (Random.value >= dropChance)
        {
            return;
        }

        Storage target = storage != null ? storage : Storage.Instance;
        if (target == null || BlockManager.Instance == null)
        {
            return;
        }

        Block block = BlockManager.Instance.CreateBlock();
        if (block != null)
        {
            target.AddBlock(block);
        }
    }
}
