using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// 한 라운드에 소환할 유형별 몬스터 수.
public class RoundSpawnData
{
    public int Stage { get; }
    public int Round { get; }
    // EnemyType 순서의 유형별 개수
    public IReadOnlyList<int> Counts { get; }
    public int Total { get; }

    public RoundSpawnData(int stage, int round, int[] counts)
    {
        Stage = stage;
        Round = round;
        Counts = counts;

        int total = 0;
        foreach (int count in counts)
        {
            total += count;
        }

        Total = total;
    }

    public int GetCount(EnemyType type) => Counts[(int)type];
}

// 스테이지 CSV(스테이지, 라운드, 원형, 삼각형, 사각형, 오각형)를 읽은 표. 🏰 스테이지 문서 2. 스테이지 구성을 따른다.
// 첫 줄은 머리글로 건너뛰고, 빈 줄과 #으로 시작하는 줄은 무시한다.
public class StageTable
{
    public const int RoundsPerStage = 10;
    // 👿 몬스터 문서 2. 공통: 한 스테이지의 오각형 몬스터는 최대 5개
    public const int MaxPentagonsPerStage = 5;

    private const int ColumnCount = 2 + EnemyTypes.Count;

    // 스테이지 -> 라운드 -> 데이터. 스테이지와 라운드는 오름차순으로 정렬해 둔다.
    private readonly SortedDictionary<int, SortedDictionary<int, RoundSpawnData>> stages =
        new SortedDictionary<int, SortedDictionary<int, RoundSpawnData>>();

    private readonly List<string> errors = new List<string>();
    private readonly List<string> warnings = new List<string>();

    public bool IsEmpty => stages.Count == 0;
    // 읽지 못한 줄 등 데이터 오류. Parse(TextAsset)은 이를 콘솔에 남긴다.
    public IReadOnlyList<string> Errors => errors;
    // 기획 규칙 위반 등 진행은 가능한 문제. Parse(TextAsset)은 이를 콘솔에 남긴다.
    public IReadOnlyList<string> Warnings => warnings;
    public int FirstStage { get; private set; }
    public int LastStage { get; private set; }

    public static StageTable Parse(TextAsset csv)
    {
        if (csv == null)
        {
            Debug.LogError("스테이지 CSV가 연결되지 않았습니다.");
            return new StageTable();
        }

        StageTable table = Parse(csv.text, csv.name);
        foreach (string error in table.errors)
        {
            Debug.LogError(error, csv);
        }

        foreach (string warning in table.warnings)
        {
            Debug.LogWarning(warning, csv);
        }

        return table;
    }

    // 콘솔에 로그를 남기지 않고 Errors, Warnings에 모은다.
    public static StageTable Parse(string text, string sourceName = "StageTable")
    {
        var table = new StageTable();
        string[] lines = (text ?? string.Empty).Split('\n');

        // 0번 줄은 머리글
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith("#"))
            {
                continue;
            }

            if (!TryParseLine(line, out RoundSpawnData data, out string error))
            {
                table.errors.Add($"{sourceName} {i + 1}번째 줄: {error} ({line})");
                continue;
            }

            table.Add(data, sourceName, i + 1);
        }

        table.Validate(sourceName);
        return table;
    }

    private static bool TryParseLine(string line, out RoundSpawnData data, out string error)
    {
        data = null;
        string[] cells = line.Split(',');
        if (cells.Length < ColumnCount)
        {
            error = $"열이 {ColumnCount}개 필요합니다.";
            return false;
        }

        var values = new int[ColumnCount];
        for (int c = 0; c < ColumnCount; c++)
        {
            if (!int.TryParse(cells[c].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out values[c]))
            {
                error = $"{c + 1}번째 열이 정수가 아닙니다.";
                return false;
            }
        }

        if (values[0] < 1 || values[1] < 1)
        {
            error = "스테이지와 라운드는 1 이상이어야 합니다.";
            return false;
        }

        var counts = new int[EnemyTypes.Count];
        for (int t = 0; t < EnemyTypes.Count; t++)
        {
            if (values[2 + t] < 0)
            {
                error = "몬스터 수는 음수일 수 없습니다.";
                return false;
            }

            counts[t] = values[2 + t];
        }

        error = null;
        data = new RoundSpawnData(values[0], values[1], counts);
        return true;
    }

    private void Add(RoundSpawnData data, string sourceName, int lineNumber)
    {
        if (!stages.TryGetValue(data.Stage, out SortedDictionary<int, RoundSpawnData> rounds))
        {
            rounds = new SortedDictionary<int, RoundSpawnData>();
            stages.Add(data.Stage, rounds);
        }

        if (rounds.ContainsKey(data.Round))
        {
            warnings.Add($"{sourceName} {lineNumber}번째 줄: {data.Stage} - {data.Round}가 중복되어 나중 값으로 덮어씁니다.");
        }

        rounds[data.Round] = data;
    }

    private void Validate(string sourceName)
    {
        if (stages.Count == 0)
        {
            errors.Add($"{sourceName}: 라운드 데이터가 없습니다.");
            return;
        }

        bool first = true;
        foreach (KeyValuePair<int, SortedDictionary<int, RoundSpawnData>> stage in stages)
        {
            if (first)
            {
                FirstStage = stage.Key;
                first = false;
            }

            LastStage = stage.Key;

            int pentagons = 0;
            int expectedRound = 1;
            foreach (RoundSpawnData data in stage.Value.Values)
            {
                if (data.Round != expectedRound)
                {
                    warnings.Add($"{sourceName}: {stage.Key}스테이지 {expectedRound}라운드가 없습니다.");
                }

                expectedRound = data.Round + 1;
                pentagons += data.GetCount(EnemyType.Pentagon);

                if (data.Total == 0)
                {
                    warnings.Add($"{sourceName}: {data.Stage} - {data.Round}에 몬스터가 없습니다.");
                }
            }

            if (pentagons > MaxPentagonsPerStage)
            {
                warnings.Add($"{sourceName}: {stage.Key}스테이지의 오각형 몬스터가 {pentagons}개로 최대 {MaxPentagonsPerStage}개를 넘습니다.");
            }

            int lastRound = GetLastRound(stage.Key);
            if (lastRound != RoundsPerStage)
            {
                warnings.Add($"{sourceName}: {stage.Key}스테이지의 마지막 라운드가 {lastRound}입니다. (기획: {RoundsPerStage})");
            }
        }
    }

    public bool HasStage(int stage) => stages.ContainsKey(stage);

    public bool TryGet(int stage, int round, out RoundSpawnData data)
    {
        data = null;
        return stages.TryGetValue(stage, out SortedDictionary<int, RoundSpawnData> rounds) && rounds.TryGetValue(round, out data);
    }

    public bool Contains(int stage, int round) => TryGet(stage, round, out _);

    // 스테이지에 있는 가장 큰 라운드 번호. 스테이지가 없으면 0이다.
    public int GetLastRound(int stage)
    {
        if (!stages.TryGetValue(stage, out SortedDictionary<int, RoundSpawnData> rounds))
        {
            return 0;
        }

        int last = 0;
        foreach (int round in rounds.Keys)
        {
            last = round;
        }

        return last;
    }

    public int GetFirstRound(int stage)
    {
        if (!stages.TryGetValue(stage, out SortedDictionary<int, RoundSpawnData> rounds))
        {
            return 0;
        }

        foreach (int round in rounds.Keys)
        {
            return round;
        }

        return 0;
    }

    // 다음 스테이지 번호. 없으면 0이다.
    public int GetNextStage(int stage)
    {
        foreach (int key in stages.Keys)
        {
            if (key > stage)
            {
                return key;
            }
        }

        return 0;
    }

    // 이전 스테이지 번호. 없으면 0이다.
    public int GetPreviousStage(int stage)
    {
        int previous = 0;
        foreach (int key in stages.Keys)
        {
            if (key >= stage)
            {
                break;
            }

            previous = key;
        }

        return previous;
    }

    // 다음 라운드 번호. 같은 스테이지에 없으면 0이다.
    public int GetNextRound(int stage, int round)
    {
        if (!stages.TryGetValue(stage, out SortedDictionary<int, RoundSpawnData> rounds))
        {
            return 0;
        }

        foreach (int key in rounds.Keys)
        {
            if (key > round)
            {
                return key;
            }
        }

        return 0;
    }

    // 이전 라운드 번호. 같은 스테이지에 없으면 0이다.
    public int GetPreviousRound(int stage, int round)
    {
        if (!stages.TryGetValue(stage, out SortedDictionary<int, RoundSpawnData> rounds))
        {
            return 0;
        }

        int previous = 0;
        foreach (int key in rounds.Keys)
        {
            if (key >= round)
            {
                break;
            }

            previous = key;
        }

        return previous;
    }
}