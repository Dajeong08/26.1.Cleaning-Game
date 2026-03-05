using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>
/// 잠수함 전용 데칼 세척 시스템.
/// 물총 히트 시 데칼을 생성해서 표면에 붙임.
/// SubmarinePart, SubmarineManager 대신 이걸 사용.
/// </summary>
public class SubmarineDecalCleaner : MonoBehaviour
{
    public static SubmarineDecalCleaner Instance;

    [Header("데칼 설정")]
    public Material decalMaterial;         // SubmarineCleanDecal 매터리얼
    public float decalDepth = 0.5f;        // 데칼 투영 깊이

    [Header("잠수함 표면적 설정")]
    [Tooltip("잠수함 전체 표면을 덮으려면 데칼이 몇 개 필요한지 (테스트로 조절)")]
    public float totalSurfaceScore = 300f; // 이 숫자 조절해서 진행도 맞춤

    [Header("완료 기준 (%)")]
    [Range(0f, 100f)]
    public float clearThreshold = 95f;

    // 내부 상태
    private List<GameObject> _spawnedDecals = new List<GameObject>();
    private float _currentScore = 0f;
    private bool _isCleared = false;

    // 진행도 (SubmarineManager 대신 여기서 관리)
    public float Progress => Mathf.Clamp(_currentScore / totalSurfaceScore * 100f, 0f, 100f);

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// PlayerMovement.HandleCleaning()에서 호출.
    /// worldPos: 히트 포인트, normal: 표면 법선, radius: 데칼 크기
    /// </summary>
    public void SpawnCleanDecal(Vector3 worldPos, Vector3 normal, float radius, float speed)
    {
        if (decalMaterial == null) return;

        // 데칼 오브젝트 생성
        GameObject decalObj = new GameObject("CleanDecal");
        decalObj.transform.position = worldPos;

        // 표면 법선 방향으로 데칼 회전 (표면에 딱 붙게)
        decalObj.transform.rotation = Quaternion.LookRotation(-normal, Vector3.up);

        // DecalProjector 컴포넌트 추가 및 설정
        DecalProjector projector = decalObj.AddComponent<DecalProjector>();
        projector.material = decalMaterial;
        projector.size = new Vector3(radius * 2f, radius * 2f, decalDepth);
        projector.fadeFactor = 1f;

        // 잠수함 오브젝트의 자식으로 넣기 (잠수함 움직여도 같이 따라가게)
        decalObj.transform.SetParent(this.transform);

        _spawnedDecals.Add(decalObj);

        // 진행도 계산 (데칼 크기 기반)
        float decalArea = radius * radius; // 근사 면적
        _currentScore += decalArea * speed;

        UpdateProgress();
    }

    private void UpdateProgress()
    {
        float progress = Progress;

        // MissionManager 연동
        if (MissionManager.Instance != null)
            MissionManager.Instance.UpdateProgress(progress);

        // 완료 판정
        if (!_isCleared && progress >= clearThreshold)
        {
            _isCleared = true;
            Debug.Log("🎉 잠수함 세척 완료!");
        }
    }

    /// <summary>
    /// 새 미션 시작 시 초기화
    /// </summary>
    public void ResetCleaning()
    {
        foreach (var d in _spawnedDecals)
            if (d != null) Destroy(d);

        _spawnedDecals.Clear();
        _currentScore = 0f;
        _isCleared = false;
    }
}