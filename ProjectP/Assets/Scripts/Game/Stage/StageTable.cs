using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// 한 라운드에 소환할 유형별 몬스터 수.
public class RoundSpawnData
{
    public int Stage { get; }
    public int Round { get; }
    // 몬스터 목록(EnemySpawnSettings) 순서의 유형별 개수
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

    public int GetCount(int enemyIndex) => enemyIndex >= 0 && enemyIndex < Counts.Count ? Counts[enemyIndex] : 0;
}

// 스테이지 CSV를 읽은 표. 🏰 스테이지 문서 2. 스테이지 구성을 따른다.
// 첫 줄은 머리글이다. 앞의 두 열은 스테이지, 라운드이고, 그 뒤의 열은 머리글 이름으로 몬스터 목록의 id와 연결한다.
// (예: Stage,Round,Circle,Triangle,Square,Pentagon) 열 순서는 몬스터 목록 순서와 달라도 된다.
// 빈 줄과 #으로 시작하는 줄은 무시한다.
public class StageTable
{
    public const int RoundsPerStage = 10;

    private const int StageColumn = 0;
    private const int RoundColumn = 1;
    private const int FirstEnemyColumn = 2;

    // 스테이지 -> 라운드 -> 데이터. 스테이지와 라운드는 오름차순으로 정렬해 둔다.
    private readonly SortedDictionary<int, SortedDictionary<int, RoundSpawnData>> stages =
        new SortedDictionary<int, SortedDictionary<int, RoundSpawnData>>();

    private readonly List<string> errors = new List<string>();
    private readonly List<string> warnings = new List<string>();

    private IReadOnlyList<EnemyDefinition> enemies = Array.Empty<EnemyDefinition>();
    // 몬스터 인덱스 -> CSV 열 인덱스. 머리글에 열이 없으면 -1이다.
    private int[] enemyColumns = Array.Empty<int>();
    private int columnCount;

    public bool IsEmpty => stages.Count == 0;
    // 읽지 못한 줄 등 데이터 오류. Parse(TextAsset)은 이를 콘솔에 남긴다.
    public IReadOnlyList<string> Errors => errors;
    // 기획 규칙 위반 등 진행은 가능한 문제. Parse(TextAsset)은 이를 콘솔에 남긴다.
    public IReadOnlyList<string> Warnings => warnings;
    public int FirstStage { get; private set; }
    public int LastStage { get; private set; }

    // enemies는 EnemySpawnSettings의 몬스터 목록. RoundSpawnData.Counts는 이 순서를 따른다.
    public static StageTable Parse(TextAsset csv, IReadOnlyList<EnemyDefinition> enemies)
    {
        if (csv == null)
        {
            Debug.LogError("스테이지 CSV가 연결되지 않았습니다.");
            return new StageTable();
        }

        StageTable table = Parse(csv.text, enemies, csv.name);
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
    public static StageTable Parse(string text, IReadOnlyList<EnemyDefinition> enemies, string sourceName = "StageTable")
    {
        var table = new StageTable();
        string[] lines = (text ?? string.Empty).Split('\n');

        // 0번 줄은 머리글
        if (!table.ReadHeader(lines[0].Trim(), enemies, sourceName))
        {
            return table;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith("#"))
            {
                continue;
            }

            if (!table.TryParseLine(line, out RoundSpawnData data, out string error))
            {
                table.errors.Add($"{sourceName} {i + 1}번째 줄: {error} ({line})");
                continue;
            }

            table.Add(data, sourceName, i + 1);
        }

        table.Validate(sourceName);
        return table;
    }

    // 머리글의 몬스터 열을 몬스터 목록의 id와 연결한다. 읽을 수 없으면 false이다.
    private bool ReadHeader(string header, IReadOnlyList<EnemyDefinition> definitions, string sourceName)
    {
        if (definitions == null || definitions.Count == 0)
        {
            errors.Add($"{sourceName}: 몬스터 목록이 비어 있습니다. EnemySpawnSettings를 확인하세요.");
            return false;
        }

        string[] cells = header.Split(',');
        if (cells.Length <= FirstEnemyColumn)
        {
            errors.Add($"{sourceName}: 머리글에 스테이지, 라운드와 몬스터 열이 필요합니다. ({header})");
            return false;
        }

        enemies = definitions;
        columnCount = cells.Length;
        enemyColumns = new int[definitions.Count];
        var invalidIds = new bool[definitions.Count];
        for (int i = 0; i < definitions.Count; i++)
        {
            enemyColumns[i] = -1;
            string id = GetId(i);
            if (id.Length == 0)
            {
                errors.Add($"{sourceName}: 몬스터 목록 {i + 1}번째 항목의 id가 비어 있습니다.");
                invalidIds[i] = true;
            }
            else if (FindEnemy(id) != i)
            {
                errors.Add($"{sourceName}: 몬스터 목록에 {id}가 중복되어 처음 항목만 사용합니다.");
                invalidIds[i] = true;
            }
        }

        for (int c = FirstEnemyColumn; c < cells.Length; c++)
        {
            string name = cells[c].Trim();
            if (name.Length == 0)
            {
                continue;
            }

            int index = FindEnemy(name);
            if (index < 0)
            {
                errors.Add($"{sourceName}: 머리글의 {name} 열이 몬스터 목록에 없어 무시합니다.");
            }
            else if (enemyColumns[index] >= 0)
            {
                errors.Add($"{sourceName}: 머리글에 {name} 열이 중복되어 처음 열만 사용합니다.");
            }
            else
            {
                enemyColumns[index] = c;
            }
        }

        for (int i = 0; i < definitions.Count; i++)
        {
            if (!invalidIds[i] && enemyColumns[i] < 0)
            {
                warnings.Add($"{sourceName}: 머리글에 {GetId(i)} 열이 없어 0마리로 처리합니다.");
            }
        }

        return true;
    }

    private string GetId(int enemyIndex)
    {
        return enemies[enemyIndex]?.Id?.Trim() ?? string.Empty;
    }

    // id가 같은 첫 몬스터의 인덱스. 대소문자는 구분하지 않으며, 없으면 -1이다.
    private int FindEnemy(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return -1;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            if (string.Equals(GetId(i), id, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private bool TryParseLine(string line, out RoundSpawnData data, out string error)
    {
        data = null;
        string[] cells = line.Split(',');
        if (cells.Length < columnCount)
        {
            error = $"열이 {columnCount}개 필요합니다.";
            return false;
        }

        if (!TryParseCell(cells, StageColumn, out int stage, out error) || !TryParseCell(cells, RoundColumn, out int round, out error))
        {
            return false;
        }

        if (stage < 1 || round < 1)
        {
            error = "스테이지와 라운드는 1 이상이어야 합니다.";
            return false;
        }

        var counts = new int[enemies.Count];
        for (int i = 0; i < enemies.Count; i++)
        {
            int column = enemyColumns[i];
            if (column < 0)
            {
                continue;
            }

            if (!TryParseCell(cells, column, out counts[i], out error))
            {
                return false;
            }

            if (counts[i] < 0)
            {
                error = "몬스터 수는 음수일 수 없습니다.";
                return false;
            }
        }

        error = null;
        data = new RoundSpawnData(stage, round, counts);
        return true;
    }

    private static bool TryParseCell(string[] cells, int column, out int value, out string error)
    {
        if (int.TryParse(cells[column].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            error = null;
            return true;
        }

        error = $"{column + 1}번째 열이 정수가 아닙니다.";
        return false;
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

            var stageCounts = new int[enemies.Count];
            int expectedRound = 1;
            foreach (RoundSpawnData data in stage.Value.Values)
            {
                if (data.Round != expectedRound)
                {
                    warnings.Add($"{sourceName}: {stage.Key}스테이지 {expectedRound}라운드가 없습니다.");
                }

                expectedRound = data.Round + 1;
                for (int i = 0; i < enemies.Count; i++)
                {
                    stageCounts[i] += data.GetCount(i);
                }

                if (data.Total == 0)
                {
                    warnings.Add($"{sourceName}: {data.Stage} - {data.Round}에 몬스터가 없습니다.");
                }
            }

            // 👿 몬스터 문서 2. 공통: 한 스테이지의 오각형 몬스터는 최대 5개 (EnemyDefinition.MaxPerStage)
            for (int i = 0; i < enemies.Count; i++)
            {
                int max = enemies[i] != null ? enemies[i].MaxPerStage : 0;
                if (max > 0 && stageCounts[i] > max)
                {
                    warnings.Add($"{sourceName}: {stage.Key}스테이지의 {GetId(i)} 몬스터가 {stageCounts[i]}개로 최대 {max}개를 넘습니다.");
                }
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