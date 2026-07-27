using App.BaseSystem.DataStores.ScriptableObjects.Status;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 武器 UI の挙動を制御するクラス。
/// 武器は敵をロックオンし、一定間隔で攻撃動作を行います。
/// </summary>
public class BattleUI : MonoBehaviour
{
    // 使用する装備データ。
    public D_Eq_StatusData WeaponUsed;

    [Header("ロックオン設定")]
    [SerializeField] private float lockOnRange = 300f; // ロックオン可能な最大距離
    [SerializeField] private float returnDuration = 0.2f; // 攻撃後に元の位置に戻る時間
    [SerializeField] private float attackMoveSpeed = 500f; // 攻撃時に敵に向かって飛ぶ速度
    [SerializeField] private float idleAmplitude = 0.5f; // 待機時の上下ゆらぎの幅
    [SerializeField] private float idleDuration = 1.5f; // 待機時の往復時間
    [SerializeField] private Vector3 idleRotation = new Vector3(0, 0, -45f); // 待機時の角度

    private RectTransform rectTransform;
    private Vector3 startWorldPosition;
    private Vector2 startAnchoredPosition;
    private float attackTimer;
    private EnemyBehavior currentTarget;
    private bool isAttacking;
    private bool isReturning;
    private bool useUIOverlap;
    private Tween idleTween;
    private Tween moveTween;
    private BattleManager battleManager;

    private void Awake()
    {
        // RectTransform は UI 表示時に必要になる
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            startAnchoredPosition = rectTransform.anchoredPosition;
        }

        if (transform.parent != null)
        {
            startWorldPosition = transform.parent.position;
        }
        else
        {
            startWorldPosition = transform.position;
        }

        transform.localRotation = Quaternion.Euler(idleRotation);

        useUIOverlap = (rectTransform != null);
        battleManager = FindAnyObjectByType<BattleManager>();
        ResetAttackTimer();
        StartIdleMotion();
        //transform.localRotation = Quaternion.Euler(idleRotation);
    }

    private void OnDisable()
    {
        // 非表示になる際に tweens を安全に破棄する
        KillTweens();
    }

    private void Update()
    {
        if (WeaponUsed == null)
            return;

        if (moveTween != null && moveTween.IsActive())
            return;

        // 攻撃中は移動処理を続行
        if (isAttacking)
        {
            UpdateAttackMovement();
            return;
        }

        // ターゲットが無効なら再検索
        if (currentTarget == null || currentTarget.EnemyData == null || currentTarget.EnemyData.Hp <= 0)
        {
            currentTarget = FindNearestEnemy();
        }

        if (currentTarget != null)
        {
            // 標的に向けて角度を調整
            if (rectTransform != null)
            {
                Vector3 direction = currentTarget.transform.position - transform.position;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.localRotation = Quaternion.Euler(0f, 0f, angle - 45f);
            }

            // 射程内なら攻撃タイマーを進める
            if (Vector3.Distance(transform.position, currentTarget.transform.position) <= lockOnRange)
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    //後にもう少し遅くする
                    attackTimer = WeaponUsed.Speed > 0 ? 1f / WeaponUsed.Speed : 1f;
                    StartAttack(currentTarget);
                }
            }
        }
        else
        {
            // 標的がいない場合は待機アニメーションを維持
            if (idleTween == null || !idleTween.IsActive())
            {
                transform.localRotation = Quaternion.Euler(idleRotation);
                StartIdleMotion();
            }
        }
    }

    /// <summary>
    /// 最も近い敵を検索する。
    /// </summary>
    private EnemyBehavior FindNearestEnemy()
    {
        var enemies = FindObjectsOfType<EnemyBehavior>();
        float bestDistance = float.MaxValue;
        EnemyBehavior bestTarget = null;

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.EnemyData == null || enemy.EnemyData.Hp <= 0)
                continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < bestDistance && distance <= lockOnRange)
            {
                bestDistance = distance;
                bestTarget = enemy;
            }
        }

        return bestTarget;
    }

    /// <summary>
    /// 指定した敵に向かって攻撃を開始する。
    /// </summary>
    private void StartAttack(EnemyBehavior target)
    {
        if (target == null || WeaponUsed == null)
            return;

        KillIdleMotion();
        isAttacking = true;
        currentTarget = target;
    }

    /// <summary>
    /// 攻撃中の移動を更新し、敵に近づいたらダメージ判定を行う。
    /// </summary>
    private void UpdateAttackMovement()
    {
        if (currentTarget == null || currentTarget.EnemyData == null || currentTarget.EnemyData.Hp <= 0)
        {
            isAttacking = false;
            ReturnToOrigin();
            return;
        }

        float step = attackMoveSpeed * Time.deltaTime;
        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = currentTarget.transform.position;
        transform.position = Vector3.MoveTowards(currentPosition, targetPosition, step);

        Vector3 angleDir = targetPosition - transform.position;
        float angle = Mathf.Atan2(angleDir.y, angleDir.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle - 45f);

        if (IsTouchingCurrentTarget())
        {
            HitEnemy();
            return;
        }
    }

    private void HitEnemy()
    {
        if (!isAttacking || currentTarget == null)
            return;

        isAttacking = false;

        if (battleManager != null)
        {
            battleManager.ApplyEnemyDamage(WeaponUsed.Attack, currentTarget.gameObject);
        }

        currentTarget = null;
        ReturnToOrigin();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isAttacking || currentTarget == null)
            return;

        if (other.gameObject.CompareTag("Enemy"))
        {
            var collided = other.gameObject;
            if (collided == currentTarget.gameObject || collided.transform.IsChildOf(currentTarget.transform) || currentTarget.transform.IsChildOf(collided.transform))
            {
                HitEnemy();
            }
        }
    }

    private bool IsTouchingCurrentTarget()
    {
        if (!isAttacking || currentTarget == null)
            return false;

        if (useUIOverlap)
        {
            var enemyRect = currentTarget.GetComponent<RectTransform>();
            if (enemyRect != null && rectTransform != null)
            {
                return RectTransformOverlaps(rectTransform, enemyRect);
            }
        }

        return false;
    }

    private bool RectTransformOverlaps(RectTransform a, RectTransform b)
    {
        if (a == null || b == null)
            return false;

        Vector3[] aCorners = new Vector3[4];
        Vector3[] bCorners = new Vector3[4];
        a.GetWorldCorners(aCorners);
        b.GetWorldCorners(bCorners);

        var aMin = aCorners[0];
        var aMax = aCorners[2];
        var bMin = bCorners[0];
        var bMax = bCorners[2];

        Rect rectA = new Rect(aMin.x, aMin.y, aMax.x - aMin.x, aMax.y - aMin.y);
        Rect rectB = new Rect(bMin.x, bMin.y, bMax.x - bMin.x, bMax.y - bMin.y);

        return rectA.Overlaps(rectB);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!isReturning)
            return;

        // 返却中は接触判定の余計な処理をしない
    }

    /// <summary>
    /// 元の位置に戻るアニメーションを実行する。
    /// </summary>
    private void ReturnToOrigin()
    {
        isReturning = true;
        moveTween = transform.DOMove(startWorldPosition, returnDuration).SetEase(Ease.OutQuad);

        moveTween.OnComplete(() =>
        {
            isReturning = false;
            currentTarget = null;
            ResetAttackTimer();
            StartIdleMotion();
            transform.localRotation = Quaternion.Euler(idleRotation);
        });
    }

    /// <summary>
    /// 待機時のゆらぎアニメーションを開始する。
    /// </summary>
    private void StartIdleMotion()
    {
        if (rectTransform == null)
            return;

        KillIdleMotion();
        idleTween = rectTransform.DOAnchorPosY(startAnchoredPosition.y + idleAmplitude, idleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// 待機アニメーションを停止する。
    /// </summary>
    private void KillIdleMotion()
    {
        if (idleTween != null && idleTween.IsActive())
        {
            idleTween.Kill();
            idleTween = null;
        }
    }

    /// <summary>
    /// 稼働中の Tween をすべて停止する。
    /// </summary>
    private void KillTweens()
    {
        if (idleTween != null && idleTween.IsActive())
            idleTween.Kill();
        if (moveTween != null && moveTween.IsActive())
            moveTween.Kill();
    }

    /// <summary>
    /// 攻撃タイマーを初期化する。
    /// </summary>
    private void ResetAttackTimer()
    {
        attackTimer = WeaponUsed != null && WeaponUsed.Speed > 0 ? 1f / WeaponUsed.Speed : 1f;
    }
}
