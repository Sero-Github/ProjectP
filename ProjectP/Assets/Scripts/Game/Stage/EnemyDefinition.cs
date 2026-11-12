using System;
using UnityEngine;

// 몬스터 한 종류. EnemySpawnSettings의 목록 순서가 가장 많은 유형이 동률일 때의 소환 우선순위이다.
// id는 스테이지 CSV 머리글의 열 이름과 같아야 그 열의 개수만큼 소환된다.
[Serializable]
public class EnemyDefinition
{
    [Tooltip("스테이지 CSV 머리글의 열 이름 (예: Circle)")]
    [SerializeField] private string id;

    [SerializeField] private Enemy prefab;

    [Tooltip("이 유형의 마지막 1마리를 라운드 맨 끝에 소환합니다. 여러 유형이 켜져 있으면 목록 순서대로 끝에 붙입니다.")]
    [SerializeField] private bool spawnLastOne;

    [Tooltip("한 스테이지에서 이 유형의 최대 수. 넘으면 스테이지 CSV를 읽을 때 경고합니다. 0이면 제한하지 않습니다.")]
    [Min(0)]
    [SerializeField] private int maxPerStage;

    public string Id => id;
    public Enemy Prefab => prefab;
    public bool SpawnLastOne => spawnLastOne;
    public int MaxPerStage => maxPerStage;

    public EnemyDefinition()
    {
    }

    public EnemyDefinition(string id, bool spawnLastOne = false, int maxPerStage = 0, Enemy prefab = null)
    {
        this.id = id;
        this.spawnLastOne = spawnLastOne;
        this.maxPerStage = maxPerStage;
        this.prefab = prefab;
    }
}
