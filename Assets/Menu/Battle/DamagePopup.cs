using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private float moveY = 60f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease ease = Ease.OutQuad;

    private RectTransform damageTextRect;
    private Vector2 initialAnchoredPosition;
    private Sequence sequence;

    private void Awake()
    {
        if (damageText == null)
        {
            Debug.LogWarning($"[DamagePopup] damageText が設定されていません: {name}");
            return;
        }

        damageTextRect = damageText.rectTransform;
        initialAnchoredPosition = damageTextRect.anchoredPosition;
        damageText.alpha = 0f;
        damageText.gameObject.SetActive(false);
    }

    public void ShowDamage(int amount)
    {
        if (damageText == null)
            return;

        if (sequence != null && sequence.IsActive())
        {
            sequence.Kill();
        }

        damageText.text = amount.ToString();
        damageText.alpha = 1f;
        damageText.gameObject.SetActive(true);

        if (damageTextRect != null)
        {
            damageTextRect.anchoredPosition = initialAnchoredPosition;
        }

        sequence = DOTween.Sequence();
        if (damageTextRect != null)
        {
            sequence.Append(damageTextRect.DOAnchorPos(initialAnchoredPosition + Vector2.up * moveY, duration).SetEase(ease));
        }
        sequence.Join(damageText.DOFade(0f, duration));
        sequence.OnComplete(() =>
        {
            if (damageText != null)
            {
                damageText.gameObject.SetActive(false);
            }
        });
    }

    private void OnDisable()
    {
        if (sequence != null && sequence.IsActive())
        {
            sequence.Kill();
        }
    }
}
