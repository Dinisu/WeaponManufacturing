using System.Collections;
using App.BaseSystem.DataStores.ScriptableObjects.Status;
using UnityEngine;
using UnityEngine.UI;

/// <summary>クールダウン付きの強力な魔法を発動するボタン。</summary>
public class MagicButton : MonoBehaviour
{
    [Header("魔法データ")]
    // 発動する強力な魔法データをInspectorから設定する。
    [SerializeField] private D_Sk_StatusData activationSpell;

    [Header("クールダウン表示")]
    // ボタンと子オブジェクトの再使用時間ゲージ。ゲージは名前から自動取得する。
    [SerializeField] private Button magicButton;
    [SerializeField] private GameObject recastTimeGaugeObject;
    [SerializeField] private Image recastTimeGauge;

    // 発動先とクールダウンの状態。
    private BattleManager battleManager;
    private Coroutine recastCoroutine;
    private bool isCoolingDown;

    /// <summary>ボタンとゲージの参照を初期化する。</summary>
    private void Awake()
    {
        if (magicButton == null)
            magicButton = GetComponent<Button>();

        if (recastTimeGaugeObject == null)
        {
            Transform gaugeTransform = transform.Find("Recast Time Gauge");
            if (gaugeTransform != null)
                recastTimeGaugeObject = gaugeTransform.gameObject;
        }

        if (recastTimeGauge == null && recastTimeGaugeObject != null)
            recastTimeGauge = recastTimeGaugeObject.GetComponent<Image>();

        battleManager = FindAnyObjectByType<BattleManager>();
        ResetGauge();
    }

    /// <summary>ButtonのOnClickから呼び出し、強力な魔法を発動する。</summary>
    public void UseMagic()
    {
        if (isCoolingDown || activationSpell == null || battleManager == null)
            return;

        // 魔法を生成できた場合だけクールダウンを開始する。
        if (!battleManager.TryCastMagic(activationSpell))
            return;

        int duration = activationSpell.Duration;
        if (duration <= 0)
        {
            ResetGauge();
            return;
        }

        isCoolingDown = true;
        if (magicButton != null)
            magicButton.interactable = false;

        if (recastTimeGaugeObject != null)
            recastTimeGaugeObject.SetActive(true);
        if (recastTimeGauge != null)
            recastTimeGauge.fillAmount = 1f;

        recastCoroutine = StartCoroutine(UpdateRecastGauge(duration));
    }

    /// <summary>Duration秒をかけてゲージを減らし、完了時にボタンを再使用可能にする。</summary>
    private IEnumerator UpdateRecastGauge(float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            if (recastTimeGauge != null)
                recastTimeGauge.fillAmount = Mathf.Clamp01(1f - elapsedTime / duration);

            yield return null;
        }

        if (recastTimeGauge != null)
            recastTimeGauge.fillAmount = 0f;

        ResetGauge();
        recastCoroutine = null;
    }

    /// <summary>ゲージを隠して初期値へ戻し、ボタンを使用可能にする。</summary>
    private void ResetGauge()
    {
        if (recastTimeGaugeObject != null)
            recastTimeGaugeObject.SetActive(false);
        if (recastTimeGauge != null)
            recastTimeGauge.fillAmount = 1f;
        if (magicButton != null)
            magicButton.interactable = true;

        isCoolingDown = false;
    }
}
