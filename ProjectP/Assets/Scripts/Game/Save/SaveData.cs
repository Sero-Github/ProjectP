using System;
using System.Collections.Generic;

// 라운드가 끝날 때마다 자동 저장되는 진행 상황과 블록. JsonUtility로 직렬화하므로 필드는 public이다.
[Serializable]
public class SaveData
{
    // 다음에 시작할 스테이지, 라운드. 0이면 저장된 진행이 없는 것으로 보고 첫 라운드부터 시작한다.
    public int stage;
    public int round;
    // 보스(마지막 라운드)를 한 번이라도 처치한 스테이지. 최초 처치 보상은 한 번만 준다.
    public List<int> bossClearedStages = new List<int>();
    // 가지고 있는 블록(창고, 가방). 병합 슬롯에 올려 둔 블록은 창고에 있는 것으로 저장한다.
    public List<BlockSaveData> blocks = new List<BlockSaveData>();

    public bool HasProgress => stage > 0 && round > 0;

    public bool IsBossCleared(int stageNumber) => bossClearedStages.Contains(stageNumber);
}