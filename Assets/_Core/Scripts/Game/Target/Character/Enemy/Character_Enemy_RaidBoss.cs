using DG.Tweening;
using UnityEngine;

public class Character_Enemy_RaidBoss : Character_Enemy
{
    float m_fTimeScale = 0.1f;
    float m_fMoveX = 2.5f;

    public override void SetBossData(string _key = null)
    {
        if (_key.IsActive())
            m_stat = TableManager.statHero.GetStatData(_key);

        if (m_stat.isActive == false)
        {
            m_stat = TableManager.statEnemy.GetStatData(_key);

            if (m_stat.isActive == false)
                m_stat = TableManager.statEnemy.GetStatData("Enemy");
        }

        SetBuffStat(((int)DataManager.bossRaid.data.nowGrade + 1) * 10, _isAttackPower: false);
        SetFaction(FactionType.Enemy);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            anim.Play(CharacterAnimType.Die_1);
            Time.timeScale = m_fTimeScale;

            transform.DOMoveX(transform.position.x + (m_fMoveX * (move.isFlip ? -1 : 1)), .3f).OnComplete(() => Time.timeScale = 1f);
        }
    }


    public override bool OnDamage(CharacterComponent _attacker, float _damage)
    {
        if (isLive == false)
            return true;

        var result = base.OnDamage(_attacker, _damage);
        Signal.instance.UpdageBossHP.Emit(isLive ? m_stat.health / (float)m_stat.healthMax : 0);

        // todo
        if (isLive == false)
        {
            StageManager.instance.SetState(CharacterStateType.None);
            TeamManager.instance.SetState(CharacterStateType.None);

            DataManager.bossRaid.Finish_FirstPhase();

            //ControllerManager.instance.SetSwitch(false);
            //Signal.instance.BossRaidFinished.Emit();
            Time.timeScale = m_fTimeScale;

            attack.ResetFX();

            CameraManager.instance.Shake();
            CameraManager.instance.SetCameraPosTarget(element.cameraPos, false);

            transform.DOMoveX(transform.position.x + (m_fMoveX * (move.isFlip ? -1 : 1)), .3f).OnComplete(() =>
            {
                Time.timeScale = 1f;
            });
        }

        return result;
    }
}
