using App.BaseSystem.DataStores.ScriptableObjects.Status;
using UnityEngine;

public class MagicUI : MonoBehaviour
{
    public D_Sk_StatusData ActivationSpell;

    //ActivationSpell.Speedの数値の速さで右に前進し敵と接触したら
    //BattleManagerのstatusData.Magic×ActivationSpell.Efficacy1のダメージを与えてから
    //MagicUIがアタッチされているこのオブジェクトを削除する
    //ダメージの処理は敵にアタッチされているEnemyBehaviorスクリプトで行う

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
