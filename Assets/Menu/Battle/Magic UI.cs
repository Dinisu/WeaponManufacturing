using App.BaseSystem.DataStores.ScriptableObjects.Status;
using UnityEngine;

public class MagicUI : MonoBehaviour
{
    public D_Sk_StatusData ActivationSpell; // この魔法オブジェクトに設定された魔法データ。

    private BattleManager battleManager; // 魔法のダメージを敵へ適用するBattleManager。
    private RectTransform rectTransform; // UI同士の接触判定に使う魔法のRectTransform。
    private Canvas parentCanvas; // 魔法が属するCanvas。
    private bool useUIOverlap; // RectTransform同士の重なり判定を使うかどうか。
    private bool isResolved; // 命中または画面外処理が完了しているかどうか。
    private readonly Vector3[] worldCorners = new Vector3[4]; // UI画面外判定に使うRectTransformの四隅。

    /// <summary>接触判定とダメージ処理に使う参照を取得する。</summary>
    private void Awake()
    {
        battleManager = FindAnyObjectByType<BattleManager>();
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        useUIOverlap = rectTransform != null;
    }

    /// <summary>設定された魔法の速度で右方向へ移動する。</summary>
    private void Update()
    {
        if (ActivationSpell == null || isResolved)
            return;

        transform.position += Vector3.right * ActivationSpell.Speed * Time.deltaTime;

        if (useUIOverlap)
            CheckUIOverlap();

        if (!isResolved && IsOutsideScreen())
        {
            isResolved = true;
            Destroy(gameObject);
        }
    }

    /// <summary>UI敵との矩形の重なりを調べ、接触していれば命中処理を行う。</summary>
    private void CheckUIOverlap()
    {
        EnemyBehavior[] enemies = FindObjectsOfType<EnemyBehavior>();
        foreach (EnemyBehavior enemy in enemies)
        {
            if (enemy == null || enemy.EnemyData == null || enemy.EnemyData.Hp <= 0)
                continue;

            RectTransform enemyRectTransform = enemy.GetComponent<RectTransform>();
            if (enemyRectTransform != null &&
                RectTransformOverlaps(rectTransform, enemyRectTransform))
            {
                HitEnemy(enemy);
                return;
            }
        }
    }

    /// <summary>2つのRectTransformのワールド矩形が重なっているか判定する。</summary>
    private bool RectTransformOverlaps(RectTransform a, RectTransform b)
    {
        if (a == null || b == null)
            return false;

        Vector3[] aCorners = new Vector3[4];
        Vector3[] bCorners = new Vector3[4];
        a.GetWorldCorners(aCorners);
        b.GetWorldCorners(bCorners);

        Vector3 aMin = aCorners[0];
        Vector3 aMax = aCorners[2];
        Vector3 bMin = bCorners[0];
        Vector3 bMax = bCorners[2];

        Rect rectA = new Rect(aMin.x, aMin.y, aMax.x - aMin.x, aMax.y - aMin.y);
        Rect rectB = new Rect(bMin.x, bMin.y, bMax.x - bMin.x, bMax.y - bMin.y);
        return rectA.Overlaps(rectB);
    }

    /// <summary>魔法の表示領域が画面の外に出ているか判定する。</summary>
    private bool IsOutsideScreen()
    {
        if (rectTransform != null)
        {
            Camera uiCamera = null;
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main;

            rectTransform.GetWorldCorners(worldCorners);
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            foreach (Vector3 corner in worldCorners)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, corner);
                minX = Mathf.Min(minX, screenPoint.x);
                maxX = Mathf.Max(maxX, screenPoint.x);
                minY = Mathf.Min(minY, screenPoint.y);
                maxY = Mathf.Max(maxY, screenPoint.y);
            }

            return maxX < 0f || minX > Screen.width || maxY < 0f || minY > Screen.height;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return false;

        Vector3 viewportPosition = mainCamera.WorldToViewportPoint(transform.position);
        return viewportPosition.z < 0f || viewportPosition.x < 0f || viewportPosition.x > 1f ||
            viewportPosition.y < 0f || viewportPosition.y > 1f;
    }

    /// <summary>物理Colliderで敵に接触したとき命中処理を行う。</summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyBehavior enemy = other.GetComponentInParent<EnemyBehavior>();
        HitEnemy(enemy);
    }

    /// <summary>敵に魔法ダメージを与え、魔法オブジェクトを破棄する。</summary>
    private void HitEnemy(EnemyBehavior enemy)
    {
        if (isResolved || enemy == null || enemy.EnemyData == null)
            return;

        isResolved = true;

        if (battleManager != null && battleManager.statusData != null && ActivationSpell != null)
        {
            int damage = Mathf.RoundToInt(
                battleManager.statusData.Magic * ActivationSpell.Efficacy1);
            battleManager.ApplyEnemyDamage(damage, enemy.gameObject);
        }

        Destroy(gameObject);
    }
}
