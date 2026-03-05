using System.Collections.Generic;

using UnityEngine;

using TMPro;



/// <summary>

/// 잠수함 전체 세척 진행도 관리.

/// Update() 폴링 대신 이벤트 기반으로 동작 (성능 개선).

/// </summary>

public class SubmarineManager : MonoBehaviour

{

    [Header("파트 리스트 (Inspector에서 껍데기 오브젝트들 드래그)")]

    public List<SubmarinePart> allParts = new List<SubmarinePart>();



    [Header("UI")]

    public TextMeshProUGUI progressText;



    [Header("완료 판정 기준 (%)")]

    [Range(0f, 100f)]

    public float clearThreshold = 95f;



    private bool _isCleared = false;



    void Start()

    {

        // 각 파트의 진행도 변경 이벤트 구독

        foreach (var part in allParts)

            part.onProgressChanged += OnAnyPartChanged;

    }



    private void OnAnyPartChanged()

    {

        if (allParts.Count == 0) return;



        float sum = 0f;

        foreach (var part in allParts)

            sum += part.partProgress;



        float overall = sum / allParts.Count;



        // UI 업데이트

        if (progressText != null)

            progressText.text = $"잠수함 세척도: {overall:F1}%";



        // MissionManager 연동 (DirtPainter와 동일한 방식)

        if (MissionManager.Instance != null)

            MissionManager.Instance.UpdateProgress(overall);



        // 완료 판정

        if (!_isCleared && overall >= clearThreshold)

        {

            _isCleared = true;

            Debug.Log("🎉 잠수함 세척 완료!");

        }

    }



    /// <summary>런타임 중 파트 동적 추가</summary>

    public void RegisterPart(SubmarinePart part)

    {

        if (!allParts.Contains(part))

        {

            allParts.Add(part);

            part.onProgressChanged += OnAnyPartChanged;

        }

    }



    void OnDestroy()

    {

        foreach (var part in allParts)

            if (part != null)

                part.onProgressChanged -= OnAnyPartChanged;

    }

}