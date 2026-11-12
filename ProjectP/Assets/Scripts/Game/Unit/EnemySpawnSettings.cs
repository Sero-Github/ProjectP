using System.Collections.Generic;
using UnityEngine;

// EnemySpawner가 몬스터를 소환할 때 쓰는 설정. 씬을 고치지 않고 이 에셋만 수정해 소환할 몬스터를 바꾼다.
// 몬스터 종류를 추가하려면 프리팹과 EnemyData를 만들고, 여기에 항목을 추가한 뒤 스테이지 CSV에 같은 이름의 열을 추가한다.
[CreateAssetMenu(fileName = "EnemySpawnSettings", menuName = "ProjectP/Battle/Enemy Spawn Settings")]
public class EnemySpawnSettings : ScriptableObject
{
    [Tooltip("소환할 몬스터 종류. 순서가 가장 많은 유형이 동률일 때의 소환 우선순위입니다.")]
    [SerializeField] private List<EnemyDefinition> enemies = new List<EnemyDefinition>();

    [Tooltip("카메라 화면 가장자리에서 이 거리(유닛)만큼 바깥에서 몬스터를 소환합니다.")]
    [Min(0f)]
    [SerializeField] private float spawnMargin = 1f;

    public IReadOnlyList<EnemyDefinition> Enemies => enemies;
    public float SpawnMargin => spawnMargin;
}
