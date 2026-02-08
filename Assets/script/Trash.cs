using UnityEngine;

public class Trash : MonoBehaviour
{
    public enum TrashType { Small, Large }
    public TrashType type;
    public int scoreValue;

    private void Awake()
    {
        // 게임 시작 시 한 번만 설정
        if (scoreValue == 0) // 인스펙터에서 설정 안 했을 때만 기본값 부여
        {
            scoreValue = (type == TrashType.Large) ? 50 : 20;
        }
    }
}