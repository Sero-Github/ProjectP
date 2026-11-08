using System;
using System.Collections;
using UnityEngine;

// 스테이지, 라운드 진행. 🏰 스테이지 문서 2, 4, 5를 따른다.
// - 라운드마다 CSV의 몬스터를 roundDuration초 동안 모두 소환하고, 다 소환한 뒤 모든 몬스터를 처치하면 승리한다.
// - 승리하면 다음 라운드(없으면 다음 스테이지 첫 라운드)로, 사망하면 이전 라운드(없으면 이전 스테이지 마지막 라운드)로 간다.
// - 라운드가 끝날 때마다(승리, 패배 모두) 다음에 시작할 라운드와 가지고 있는 블록(창고, 가방 배치)을 자동 저장한다.
// - 라운드를 시작할 때마다 플레이어 체력을 가득 채운다. 사망해 사라진 플레이어는 defeatDelay초 뒤 이때 다시 나타난다.
public class RoundManager : MonoBehaviour
{
    private enum State
    {
        Idle,
        Playing,
        Transition,
    }

    public static RoundManager Instance { get; private set; }

    // (스테이지, 라운드)
    public static event Action<int, int> RoundStarted;
    // (스테이지, 라운드, 승리 여부)
    public static event Action<int, int, bool> RoundEnded;
    // 스테이지 보스(마지막 라운드)를 처음 처치했을 때. 보상 내용은 기획 미정이다.
    public static event Action<int> BossFirstCleared;

    [Tooltip("스테이지, 라운드, 원형, 삼각형, 사각형, 오각형 열을 가진 CSV")]
    [SerializeField] private TextAsset stageCsv;
    [SerializeField] private EnemySpawner spawner;

    [Tooltip("한 라운드에서 모든 몬스터를 소환하는 시간(초)")]
    [Min(1f)]
    [SerializeField] private float roundDuration = 100f;

    [Tooltip("라운드에서 승리하고 다음 라운드를 시작하기까지 기다리는 시간(초)")]
    [Min(0f)]
    [SerializeField] private float transitionDelay = 2f;

    [Tooltip("플레이어가 사망하고 다시 나타나 이전 라운드를 시작하기까지 기다리는 시간(초)")]
    [Min(0f)]
    [SerializeField] private float defeatDelay = 3f;

    private StageTable table;
    private SaveData save;
    private State state = State.Idle;
    private Coroutine transition;

    public int CurrentStage { get; private set; }
    public int CurrentRound { get; private set; }
    public bool IsPlaying => state == State.Playing;
    public bool IsBossRound => table != null && CurrentRound == table.GetLastRound(CurrentStage);

    // 몬스터 스탯 레벨. 1스테이지 10라운드는 10, 2스테이지 1라운드는 11이다. (👿 몬스터 3-1)
    public int Level => (CurrentStage - 1) * StageTable.RoundsPerStage + CurrentRound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        Player.Died += OnPlayerDied;
    }

    private void OnDisable()
    {
        Player.Died -= OnPlayerDied;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (spawner == null)
        {
            Debug.LogError($"{name}: EnemySpawner가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        table = StageTable.Parse(stageCsv);
        if (table.IsEmpty)
        {
            enabled = false;
            return;
        }

        save = SaveSystem.Load();
        if (save.HasProgress && table.Contains(save.stage, save.round))
        {
            CurrentStage = save.stage;
            CurrentRound = save.round;
        }
        else
        {
            if (save.HasProgress)
            {
                Debug.LogWarning($"저장된 {save.stage} - {save.round}가 스테이지 CSV에 없어 처음부터 시작합니다.");
            }

            CurrentStage = table.FirstStage;
            CurrentRound = table.GetFirstRound(CurrentStage);
        }

        // 라운드를 시작하며 체력을 채우기 전에 가방을 채워야 블록 스탯(체력 등)이 반영된다.
        InventorySaver.Restore(save);

        BeginRound();
    }

    private void Update()
    {
        if (state != State.Playing || spawner.IsSpawning || Enemy.Actives.Count > 0)
        {
            return;
        }

        Player player = Player.Instance;
        if (player != null && !player.IsDead)
        {
            EndRound(true);
        }
    }

    private void BeginRound()
    {
        transition = null;

        if (!table.TryGet(CurrentStage, CurrentRound, out RoundSpawnData data))
        {
            Debug.LogError($"{CurrentStage} - {CurrentRound} 라운드 데이터가 없습니다.", this);
            state = State.Idle;
            return;
        }

        if (Player.Instance != null)
        {
            Player.Instance.Revive();
        }

        spawner.StartRound(SpawnSequence.Build(data.Counts), roundDuration, Level);
        state = State.Playing;
        RoundStarted?.Invoke(CurrentStage, CurrentRound);
    }

    private void OnPlayerDied()
    {
        // 전환 중에 남은 탄환 등에 맞아 죽은 것은 무시한다. 다음 라운드 시작 때 되살아난다.
        if (state != State.Playing)
        {
            return;
        }

        spawner.StopSpawning();
        // 몬스터가 모두 죽는 파티클 (🏰 스테이지 5)
        spawner.DespawnAll(true);
        EnemyBullet.DespawnAll();
        EndRound(false);
    }

    private void EndRound(bool victory)
    {
        state = State.Transition;
        int endedStage = CurrentStage;
        int endedRound = CurrentRound;

        if (victory)
        {
            EnemyBullet.DespawnAll();
            CheckBossFirstClear(endedStage, endedRound);
            MoveToNextRound();
        }
        else
        {
            MoveToPreviousRound();
        }

        save.stage = CurrentStage;
        save.round = CurrentRound;
        InventorySaver.Capture(save);
        SaveSystem.Save(save);

        RoundEnded?.Invoke(endedStage, endedRound, victory);

        if (transition != null)
        {
            StopCoroutine(transition);
        }

        transition = StartCoroutine(BeginRoundAfterDelay(victory ? transitionDelay : defeatDelay));
    }

    private IEnumerator BeginRoundAfterDelay(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        BeginRound();
    }

    private void CheckBossFirstClear(int stage, int round)
    {
        if (round != table.GetLastRound(stage) || save.IsBossCleared(stage))
        {
            return;
        }

        save.bossClearedStages.Add(stage);
        Debug.Log($"{stage}스테이지 보스 최초 처치");
        BossFirstCleared?.Invoke(stage);
    }

    // 다음 라운드 -> 다음 스테이지 첫 라운드 -> (마지막 스테이지 마지막 라운드면) 같은 라운드 반복
    private void MoveToNextRound()
    {
        int nextRound = table.GetNextRound(CurrentStage, CurrentRound);
        if (nextRound > 0)
        {
            CurrentRound = nextRound;
            return;
        }

        int nextStage = table.GetNextStage(CurrentStage);
        if (nextStage > 0)
        {
            CurrentStage = nextStage;
            CurrentRound = table.GetFirstRound(nextStage);
        }
    }

    // 이전 라운드 -> 이전 스테이지 마지막 라운드 -> (첫 스테이지 첫 라운드면) 같은 라운드 반복
    private void MoveToPreviousRound()
    {
        int previousRound = table.GetPreviousRound(CurrentStage, CurrentRound);
        if (previousRound > 0)
        {
            CurrentRound = previousRound;
            return;
        }

        int previousStage = table.GetPreviousStage(CurrentStage);
        if (previousStage > 0)
        {
            CurrentStage = previousStage;
            CurrentRound = table.GetLastRound(previousStage);
        }
    }
}