using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager instance;

    public int currentCoins = 0;
    public TextMeshProUGUI coinDisplay; // 화면에 돈 표시해줄 텍스트

    void Awake()
    {
        // 싱글톤 설정: 어디서든 접근 가능하게
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateUI();
    }

    // 돈 추가 기능 (쓰레기 주울 때 호출됨)
    public void AddCoins(int amount)
    {
        currentCoins += amount;
        UpdateUI();
    }
    public void SubtractCoins(int amount)
    {
        currentCoins -= amount;
        if (currentCoins < 0) currentCoins = 0; // 마이너스 방지
        UpdateUI();
    }

    // UI 업데이트
    void UpdateUI()
    {
        if (coinDisplay != null)
            coinDisplay.text = ": " + currentCoins.ToString();
    }
}