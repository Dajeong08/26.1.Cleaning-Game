using UnityEngine;

[System.Serializable]
public class MissionData
{
    public enum MissionStatus { Locked, ReadyToMove, InProgress, Cleared, Rewarded }

    [Header("기본 정보")]
    public string missionName;    // 미션 이름
    public int unlockPrice;       // 의뢰 수령(언락) 비용
    public int rewardCoin;        // 완료 보상

    [Header("비주얼 설정")]
    public Sprite missionImage;   // ★ 회색 부분에 들어갈 미션 사진 (추가!)
    public GameObject missionObjectSet; // 미션 시작 시 맵에 나타날 쓰레기+오브젝트 세트

    [Header("실시간 상태")]
    public MissionStatus status = MissionStatus.Locked;
    public float currentProgress = 0f;
    public int remainingTrash;
    public int totalTrash;
}