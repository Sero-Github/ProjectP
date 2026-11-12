using System.Collections.Generic;

// 한 라운드의 몬스터 소환 순서. 🏰 스테이지 문서 3. 몬스터 생성 규칙을 따른다.
// 몬스터 유형은 EnemySpawnSettings 목록의 인덱스로 다룬다.
// - 가장 많은 유형(동률이면 목록 순서가 앞선 유형)을 기준으로, 다른 유형은 비율에 맞춰 사이사이에 소환한다.
//   유형 k는 기준 유형이 floor(기준 개수 / k 개수)개 나올 때마다 1개씩 나온다. (원형 100, 삼각형 20이면 원형 5개마다 삼각형 1개)
// - SpawnLastOne인 유형(오각형)의 마지막 1개는 반드시 맨 마지막에 소환한다. 여러 유형이면 목록 순서대로 끝에 붙인다.
public static class SpawnSequence
{
    // counts는 enemies 순서의 유형별 개수. 같은 입력에는 항상 같은 순서를 돌려준다.
    public static List<int> Build(IReadOnlyList<int> counts, IReadOnlyList<EnemyDefinition> enemies)
    {
        int typeCount = enemies != null ? enemies.Count : 0;
        var remaining = new int[typeCount];
        int total = 0;
        for (int i = 0; i < typeCount; i++)
        {
            remaining[i] = counts != null && i < counts.Count ? System.Math.Max(0, counts[i]) : 0;
            total += remaining[i];
        }

        var sequence = new List<int>(total);
        if (total == 0)
        {
            return sequence;
        }

        // 맨 끝에 소환할 마지막 1개는 따로 빼 두었다가 마지막에 붙인다.
        var reservedLast = new List<int>();
        for (int i = 0; i < typeCount; i++)
        {
            if (enemies[i] != null && enemies[i].SpawnLastOne && remaining[i] > 0)
            {
                remaining[i]--;
                reservedLast.Add(i);
            }
        }

        int major = FindMajor(remaining);
        if (major >= 0)
        {
            var intervals = new int[typeCount];
            for (int i = 0; i < typeCount; i++)
            {
                if (i != major && remaining[i] > 0)
                {
                    // 소수점은 버린다. 버린 만큼 간격이 짧아지므로 기준 유형을 다 내보내기 전에 모두 소진된다.
                    intervals[i] = System.Math.Max(1, remaining[major] / remaining[i]);
                }
            }

            int majorCount = remaining[major];
            for (int step = 1; step <= majorCount; step++)
            {
                sequence.Add(major);
                remaining[major]--;

                for (int i = 0; i < typeCount; i++)
                {
                    if (i != major && remaining[i] > 0 && step % intervals[i] == 0)
                    {
                        sequence.Add(i);
                        remaining[i]--;
                    }
                }
            }

            // 위 루프에서 모두 소진되지만, 혹시 남은 것이 있으면 유형 순서대로 덧붙인다.
            for (int i = 0; i < typeCount; i++)
            {
                for (; remaining[i] > 0; remaining[i]--)
                {
                    sequence.Add(i);
                }
            }
        }

        sequence.AddRange(reservedLast);
        return sequence;
    }

    // 개수가 가장 많은 유형. 동률이면 앞선 유형이며, 모두 0이면 -1이다.
    private static int FindMajor(int[] counts)
    {
        int major = -1;
        for (int i = 0; i < counts.Length; i++)
        {
            if (counts[i] > 0 && (major < 0 || counts[i] > counts[major]))
            {
                major = i;
            }
        }

        return major;
    }
}
