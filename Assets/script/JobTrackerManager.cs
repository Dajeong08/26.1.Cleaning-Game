using UnityEngine;
using System.Collections.Generic;

public class JobTrackerManager : MonoBehaviour
{
    public GameObject trackerItemPrefab;
    public Transform contentArea;

    // 인수 형식을 MissionData로 바꿔서 에러 해결!
    public void AddJob(MissionData data)
    {
        GameObject newItem = Instantiate(trackerItemPrefab, contentArea);
        TrackerItem script = newItem.GetComponent<TrackerItem>();

        script.Setup(data);
    }
}