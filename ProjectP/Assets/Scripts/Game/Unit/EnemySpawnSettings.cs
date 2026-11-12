using System;
using System.Collections.Generic;
using UnityEngine;

// EnemySpawner가 몬스터를 소환할 때 쓰는 설정. 씬을 고치지 않고 이 에셋만 수정해 소환할 몬스터를 바꾼다.
[CreateAssetMenu(fileName = "EnemySpawnSettings", menuName = "ProjectP/Battle/Enemy Spawn Settings")]
public class EnemySpawnSettings : ScriptableObject
{
    [Serializable]
    public class EnemyEntry
    {
        [SerializeField] private EnemyType type;
        [SerializeField] private Enemy prefab;

        public EnemyType Type => type;
        public Enemy Prefab => prefab;
    }

    [Tooltip("몬스터 유형별 프리팹. 유형마다 하나씩 연결합니다.")]
    [SerializeField] private List<EnemyEntry> enemies = new List<EnemyEntry>();

    [Tooltip("카메라 화면 가장자리에서 이 거리(유닛)만큼 바깥에서 몬스터를 소환합니다.")]
    [Min(0f)]
    [SerializeField] private float spawnMargin = 1f;

    public IReadOnlyList<EnemyEntry> Enemies => enemies;
    public float SpawnMargin => spawnMargin;
}
