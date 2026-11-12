using App.BaseSystem.DataStores.ScriptableObjects.Status;
using UnityEngine;
using UnityEngine.UI;

public class HealthGauge : MonoBehaviour
{
    [SerializeField] private Image healthGauge;

    [Header("プレイヤーデータ")]
    [SerializeField] public D_Ch_StatusData statusData;

    private void Awake()
    {
        GaugeManagement();
    }

    /// <summary>HPの割合を計算し、体力ゲージのImage.fillAmountへ反映する。</summary>
    public void GaugeManagement()
    {
        if (healthGauge == null || statusData == null)
            return;

        if (statusData.MaxHp <= 0)
        {
            healthGauge.fillAmount = 0f;
            return;
        }

        healthGauge.fillAmount = Mathf.Clamp01((float)statusData.Hp / statusData.MaxHp);
    }
}
