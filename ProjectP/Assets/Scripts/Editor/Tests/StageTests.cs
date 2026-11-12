using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

// 테스트용 몬스터 목록. Data/EnemySpawnSettings.asset과 같은 구성이다.
public static class TestEnemies
{
    public const int Circle = 0;
    public const int Triangle = 1;
    public const int Square = 2;
    public const int Pentagon = 3;

    public static readonly EnemyDefinition[] Default =
    {
        new EnemyDefinition("Circle"),
        new EnemyDefinition("Triangle"),
        new EnemyDefinition("Square"),
        new EnemyDefinition("Pentagon", spawnLastOne: true, maxPerStage: 5),
    };
}

public class SpawnSequenceTests
{
    private static List<int> Build(params int[] counts) => SpawnSequence.Build(counts, TestEnemies.Default);

    private static int Count(List<int> sequence, int type)
    {
        int count = 0;
        foreach (int t in sequence)
        {
            if (t == type)
            {
                count++;
            }
        }

        return count;
    }

    [Test]
    public void 원형100_삼각형20이면_원형5개마다_삼각형1개()
    {
        List<int> sequence = Build(100, 20, 0, 0);

        Assert.AreEqual(120, sequence.Count);
        for (int block = 0; block < 20; block++)
        {
            int start = block * 6;
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(TestEnemies.Circle, sequence[start + i], $"index {start + i}");
            }

            Assert.AreEqual(TestEnemies.Triangle, sequence[start + 5], $"index {start + 5}");
        }
    }

    [Test]
    public void 간격이_나누어떨어지지_않아도_모두_소환()
    {
        List<int> sequence = Build(100, 30, 0, 0);

        Assert.AreEqual(130, sequence.Count);
        Assert.AreEqual(100, Count(sequence, TestEnemies.Circle));
        Assert.AreEqual(30, Count(sequence, TestEnemies.Triangle));
        // floor(100 / 30) = 3 이므로 원형 3개 뒤에 첫 삼각형
        Assert.AreEqual(TestEnemies.Triangle, sequence[3]);
    }

    [Test]
    public void 최다_유형이_동률이면_목록_순서가_앞선_유형부터()
    {
        List<int> sequence = Build(10, 10, 0, 0);

        Assert.AreEqual(20, sequence.Count);
        for (int i = 0; i < sequence.Count; i++)
        {
            Assert.AreEqual(i % 2 == 0 ? TestEnemies.Circle : TestEnemies.Triangle, sequence[i]);
        }
    }

    [Test]
    public void 최다_유형이_원형이_아닐_수도_있다()
    {
        List<int> sequence = Build(5, 0, 20, 0);

        Assert.AreEqual(TestEnemies.Square, sequence[0]);
        Assert.AreEqual(TestEnemies.Circle, sequence[4]);
        Assert.AreEqual(5, Count(sequence, TestEnemies.Circle));
        Assert.AreEqual(20, Count(sequence, TestEnemies.Square));
    }

    [Test]
    public void 마지막_오각형은_맨_마지막에_소환()
    {
        List<int> sequence = Build(40, 8, 4, 3);

        Assert.AreEqual(55, sequence.Count);
        Assert.AreEqual(TestEnemies.Pentagon, sequence[sequence.Count - 1]);
        Assert.AreEqual(3, Count(sequence, TestEnemies.Pentagon));
        Assert.AreEqual(4, Count(sequence, TestEnemies.Square));
        Assert.AreEqual(8, Count(sequence, TestEnemies.Triangle));
    }

    [Test]
    public void 오각형_하나는_마지막에_한번만()
    {
        List<int> sequence = Build(60, 12, 6, 1);

        Assert.AreEqual(79, sequence.Count);
        Assert.AreEqual(1, Count(sequence, TestEnemies.Pentagon));
        Assert.AreEqual(TestEnemies.Pentagon, sequence[sequence.Count - 1]);
    }

    [Test]
    public void 오각형만_있는_경우()
    {
        CollectionAssert.AreEqual(new[] { TestEnemies.Pentagon }, Build(0, 0, 0, 1));
        CollectionAssert.AreEqual(new[] { TestEnemies.Pentagon, TestEnemies.Pentagon }, Build(0, 0, 0, 2));
    }

    [Test]
    public void 몬스터가_없으면_빈_순서()
    {
        Assert.AreEqual(0, Build(0, 0, 0, 0).Count);
    }

    [Test]
    public void 맨_끝_소환_유형이_여럿이면_목록_순서대로_끝에_붙인다()
    {
        var enemies = new[]
        {
            new EnemyDefinition("Circle"),
            new EnemyDefinition("Square", spawnLastOne: true),
            new EnemyDefinition("Pentagon", spawnLastOne: true),
        };

        List<int> sequence = SpawnSequence.Build(new[] { 10, 2, 1 }, enemies);

        Assert.AreEqual(13, sequence.Count);
        Assert.AreEqual(1, sequence[sequence.Count - 2]);
        Assert.AreEqual(2, sequence[sequence.Count - 1]);
    }

    [Test]
    public void 맨_끝_소환을_끄면_모두_비율대로_섞인다()
    {
        var normal = new[] { new EnemyDefinition("Circle"), new EnemyDefinition("Pentagon") };
        var spawnLast = new[] { new EnemyDefinition("Circle"), new EnemyDefinition("Pentagon", spawnLastOne: true) };

        // 끄면 원형 2개마다 오각형 1개
        CollectionAssert.AreEqual(new[] { 0, 0, 1, 0, 0, 1 }, SpawnSequence.Build(new[] { 4, 2 }, normal));
        // 켜면 1개를 빼 두고 남은 1개를 원형 4개마다 배치한 뒤 맨 끝에 붙인다
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 1, 1 }, SpawnSequence.Build(new[] { 4, 2 }, spawnLast));
    }
}

public class StageTableTests
{
    private const string Header = "Stage,Round,Circle,Triangle,Square,Pentagon\n";

    private static StageTable Parse(string text) => StageTable.Parse(text, TestEnemies.Default);

    private static string FullStage(int stage, int pentagonPerBoss = 1)
    {
        string text = string.Empty;
        for (int round = 1; round <= 10; round++)
        {
            text += $"{stage},{round},10,2,1,{(round == 10 ? pentagonPerBoss : 0)}\n";
        }

        return text;
    }

    [Test]
    public void 정상_파싱과_라운드_이동()
    {
        StageTable table = Parse(Header + FullStage(1) + FullStage(2));

        Assert.AreEqual(1, table.FirstStage);
        Assert.AreEqual(2, table.LastStage);
        Assert.AreEqual(10, table.GetLastRound(1));
        Assert.IsTrue(table.TryGet(1, 10, out RoundSpawnData data));
        Assert.AreEqual(14, data.Total);
        Assert.AreEqual(1, data.GetCount(TestEnemies.Pentagon));

        Assert.AreEqual(2, table.GetNextRound(1, 1));
        Assert.AreEqual(0, table.GetNextRound(1, 10));
        Assert.AreEqual(2, table.GetNextStage(1));
        Assert.AreEqual(0, table.GetNextStage(2));
        Assert.AreEqual(0, table.GetPreviousRound(2, 1));
        Assert.AreEqual(1, table.GetPreviousStage(2));
        Assert.AreEqual(0, table.GetPreviousStage(1));
    }

    [Test]
    public void CRLF와_빈_줄을_무시()
    {
        StageTable table = Parse(Header.Replace("\n", "\r\n") + FullStage(1).Replace("\n", "\r\n") + "\r\n\r\n");

        Assert.AreEqual(10, table.GetLastRound(1));
    }

    [Test]
    public void 정상_데이터는_경고가_없다()
    {
        StageTable table = Parse(Header + FullStage(1));

        CollectionAssert.IsEmpty(table.Errors);
        CollectionAssert.IsEmpty(table.Warnings);
    }

    [Test]
    public void 스테이지당_최대_수를_넘으면_경고()
    {
        StageTable table = Parse(Header + FullStage(1, 6));

        Assert.IsTrue(table.Warnings.Any(w => w.Contains("Pentagon") && w.Contains("최대 5개")));
    }

    [Test]
    public void 마지막_라운드가_10이_아니면_경고()
    {
        StageTable table = Parse(Header + "1,1,10,0,0,0\n1,2,10,0,0,0\n");

        Assert.IsTrue(table.Warnings.Any(w => w.Contains("마지막 라운드")));
    }

    [Test]
    public void 잘못된_줄은_에러를_남기고_건너뜀()
    {
        StageTable table = Parse(Header + FullStage(1) + "2,1,abc,0,0,0\n");

        Assert.IsTrue(table.Errors.Any(e => e.Contains("정수가 아닙니다")));
        Assert.IsFalse(table.HasStage(2));
    }

    [Test]
    public void 머리글_이름으로_열을_연결하므로_순서가_달라도_된다()
    {
        StageTable table = Parse("Stage,Round,pentagon,Square,Circle,Triangle\n1,1,1,3,10,2\n");

        CollectionAssert.IsEmpty(table.Errors);
        Assert.IsTrue(table.TryGet(1, 1, out RoundSpawnData data));
        Assert.AreEqual(10, data.GetCount(TestEnemies.Circle));
        Assert.AreEqual(2, data.GetCount(TestEnemies.Triangle));
        Assert.AreEqual(3, data.GetCount(TestEnemies.Square));
        Assert.AreEqual(1, data.GetCount(TestEnemies.Pentagon));
    }

    [Test]
    public void 목록에_없는_열은_에러를_남기고_무시()
    {
        StageTable table = Parse("Stage,Round,Circle,Triangle,Square,Pentagon,Hexagon\n1,1,10,0,0,0,7\n");

        Assert.IsTrue(table.Errors.Any(e => e.Contains("Hexagon")));
        Assert.IsTrue(table.TryGet(1, 1, out RoundSpawnData data));
        Assert.AreEqual(10, data.Total);
    }

    [Test]
    public void CSV에_없는_몬스터는_0마리와_경고()
    {
        StageTable table = Parse("Stage,Round,Circle,Triangle\n1,1,10,2\n");

        Assert.IsTrue(table.Warnings.Any(w => w.Contains("Square") && w.Contains("0마리")));
        Assert.IsTrue(table.Warnings.Any(w => w.Contains("Pentagon") && w.Contains("0마리")));
        Assert.IsTrue(table.TryGet(1, 1, out RoundSpawnData data));
        Assert.AreEqual(0, data.GetCount(TestEnemies.Square));
        Assert.AreEqual(12, data.Total);
    }

    [Test]
    public void 몬스터_목록이_비어_있으면_빈_표와_에러()
    {
        StageTable table = StageTable.Parse(Header + FullStage(1), new EnemyDefinition[0]);

        Assert.IsTrue(table.IsEmpty);
        Assert.IsTrue(table.Errors.Any(e => e.Contains("몬스터 목록")));
    }
}
