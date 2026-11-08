// 몬스터 유형. 스테이지 CSV의 열 순서이자, 가장 많은 유형이 동률일 때 소환 우선순위이다.
public enum EnemyType
{
    Circle,
    Triangle,
    Square,
    Pentagon,
}

public static class EnemyTypes
{
    public const int Count = 4;
}