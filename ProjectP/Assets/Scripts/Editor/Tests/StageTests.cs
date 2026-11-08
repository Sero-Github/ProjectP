using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class SpawnSequenceTests
{
    private static int Count(List<EnemyType> sequence, EnemyType type)
    {
        int count = 0;
        foreach (EnemyType t in sequence)
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
        List<EnemyType> sequence = SpawnSequence.Build(new[] { 100, 20, 0, 0 });

        Assert.AreEqual(120, sequence.Count);
        for (int block = 0; block < 20; block++)
        {
            int start = block * 6;
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(EnemyType.Circle, sequence[start + i], $"index {start + i}");
            }

            Assert.AreEqual(EnemyType.Triangle, sequence[start + 5], $"index {start + 5}");
        }
    }

    [Test]
    public void 간격이_나누어떨어지지_않아도_모두_소환()
    {
        List<EnemyType> sequence = SpawnSequence.Build(new[] { 100, 30, 0, 0 });

        Assert.AreEqual(130, sequence.Count);
        Assert.AreEqual(100, Count(sequence, EnemyType.Circle));
        Assert.AreEqual(30, Count(sequence, EnemyType.Triangle));
        // floor(100 / 30) = 3 이므로 원형 3개 뒤에 첫 삼각형
        Assert.AreEqual(EnemyType.Triangle, sequence[3]);
    }

    [Test]
    public void 최다_유형이_동률이면_원형부터()
    {
        List<EnemyType> sequence = SpawnSequence.Build(new[] { 10, 10, 0, 0 });

        Assert.AreEqual(20, sequence.Count);
        for (int i = 0; i < sequence.Count; i++)
        {
            Assert.AreEqual(i % 2 == 0 ? EnemyType.Circle : EnemyType.Triangle, sequence[i]);
        }
    }

    [Test]
    public void 최다_유형이_원형이_아닐_수도_있다()
    {
        List<EnemyType> sequence = SpawnSequence.Build(new[] { 5, 0, 20, 0 });

        Assert.AreEqual(EnemyType.Square, sequence[0]);
        Assert.AreEqual(EnemyType.Circle, sequence[4]);
        Assert.AreEqual(5, Count(sequence, EnemyType.Circle));
        Assert.AreEqual(20, Count(sequence, EnemyType.Square));
    }

    [Test]
    public void 마지막_오각형은_맨_마지막에_소환()
    {
        List<EnemyType> sequence = SpawnSequence.Build(new[] { 40, 8, 4, 3 });

        Assert.AreEqual(55, sequence.Count);
        Assert.AreEqual(EnemyType.Pentagon, sequence[sequence.Count - 1]);
        Assert.AreEqual(3, Count(sequence, EnemyType.Pentagon));
        Assert.AreEqual(4, Count(sequence, EnemyType.Square));
        Assert.AreEqual(8, Count(sequence, EnemyType.Triangle));
    }

    [Test]
    public void 오각형_하나는_마지막에_한번만()
    {
        List<EnemyType> sequence = SpawnSequence.Build(new[] { 60, 12, 6, 1 });

        Assert.AreEqual(79, sequence.Count);
        Assert.AreEqual(1, Count(sequence, EnemyType.Pentagon));
        Assert.AreEqual(EnemyType.Pentagon, sequence[sequence.Count - 1]);
    }

    [Test]
    public void 오각형만_있는_경우()
    {
        CollectionAssert.AreEqual(new[] { EnemyType.Pentagon }, SpawnSequence.Build(new[] { 0, 0, 0, 1 }));
        CollectionAssert.AreEqual(new[] { EnemyType.Pentagon, EnemyType.Pentagon }, SpawnSequence.Build(new[] { 0, 0, 0, 2 }));
    }

    [Test]
    public void 몬스터가_없으면_빈_순서()
    {
        Assert.AreEqual(0, SpawnSequence.Build(new[] { 0, 0, 0, 0 }).Count);
    }
}

public class StageTableTests
{
    private const string Header = "Stage,Round,Circle,Triangle,Square,Pentagon\n";

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
        StageTable table = StageTable.Parse(Header + FullStage(1) + FullStage(2));

        Assert.AreEqual(1, table.FirstStage);
        Assert.AreEqual(2, table.LastStage);
        Assert.AreEqual(10, table.GetLastRound(1));
        Assert.IsTrue(table.TryGet(1, 10, out RoundSpawnData data));
        Assert.AreEqual(14, data.Total);
        Assert.AreEqual(1, data.GetCount(EnemyType.Pentagon));

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
        StageTable table = StageTable.Parse(Header.Replace("\n", "\r\n") + FullStage(1).Replace("\n", "\r\n") + "\r\n\r\n");

        Assert.AreEqual(10, table.GetLastRound(1));
    }

    [Test]
    public void 정상_데이터는_경고가_없다()
    {
        StageTable table = StageTable.Parse(Header + FullStage(1));

        CollectionAssert.IsEmpty(table.Errors);
        CollectionAssert.IsEmpty(table.Warnings);
    }

    [Test]
    public void 오각형이_5개를_넘으면_경고()
    {
        StageTable table = StageTable.Parse(Header + FullStage(1, 6));

        Assert.IsTrue(table.Warnings.Any(w => w.Contains("오각형")));
    }

    [Test]
    public void 마지막_라운드가_10이_아니면_경고()
    {
        StageTable table = StageTable.Parse(Header + "1,1,10,0,0,0\n1,2,10,0,0,0\n");

        Assert.IsTrue(table.Warnings.Any(w => w.Contains("마지막 라운드")));
    }

    [Test]
    public void 잘못된_줄은_에러를_남기고_건너뜀()
    {
        StageTable table = StageTable.Parse(Header + FullStage(1) + "2,1,abc,0,0,0\n");

        Assert.IsTrue(table.Errors.Any(e => e.Contains("정수가 아닙니다")));
        Assert.IsFalse(table.HasStage(2));
    }
}