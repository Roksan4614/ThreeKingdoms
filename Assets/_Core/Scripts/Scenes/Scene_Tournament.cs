using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

namespace Rev9.Tournament
{
    public class Scene_Tournament : SceneBase
    {
        Transform m_centerPos;
        Camera m_camera;

        TextMeshProUGUI m_txtTimer;

        private void Start()
        {
            Signal.instance.TournamentStatus.connect = SlotTournamentStatus;

            m_txtTimer = transform.GetComponent<TextMeshProUGUI>("Canvas/SafeArea/txt_timer");
            m_txtTimer.text = "";
            m_camera = CameraManager.instance.main;
            m_centerPos = transform.Find("Map/Heroes/CenterPos");

            StartAsync().Forget();
        }

        List<CharacterComponent> m_heroes = new();
        List<CharacterComponent> m_heroesMe = new();
        List<CharacterComponent> m_heroesOther = new();

        public CharacterComponent GetCharacter(string _key, bool _isMe)
            => _isMe ? m_heroesMe.Find(x => x.info.key == _key) : m_heroesOther.Find(x => x.info.key == _key);

        async UniTask StartAsync()
        {
            List<UniTask> tasks = new();

            tasks.Add(BatchHeroesAsync(TournamentWorker.data.teamAttack, transform.Find("Map/Heroes/Team"), true));
            tasks.Add(BatchHeroesAsync(TournamentWorker.instance.enterUserData.batchData, transform.Find("Map/Heroes/Enemy"), false));

            await UniTask.WhenAll(tasks);
            await UniTask.NextFrame();

            m_heroes.AddRange(m_heroesMe);
            m_heroes.AddRange(m_heroesOther);

            StageManager.instance.AddEnemyList(m_heroesOther.ToArray());
            TeamManager.instance.SetHeroes(m_heroesMe.ToArray());

            TournamentHeroInfoManager.instance.Initialize();

#if UNITY_EDITOR
            Update();
#else
            FixedUpdate();
#endif
            CameraManager.instance.SetCameraPosTarget(m_centerPos, true);

            await PopupManager.instance.ShowDimmAsync(false);

            await UniTask.WaitForSeconds(0.5f);

            TimerAsync().Forget();
        }

        async UniTask BatchHeroesAsync(TournamentBatchData _batchData, Transform _parent, bool _isMe)
        {
            foreach (var h in _batchData.heroes)
            {
                var obj = await AddressableManager.instance.GetHeroCharacterAsync(h.skin);

                if (obj != null)
                {
                    var objHero = Instantiate(obj, _parent);

                    var hero = objHero.GetComponent<CharacterComponent>();
                    if (_isMe)
                    {
                        m_heroesMe.Add(hero);
                        hero.SetHeroData(h.key);
                        hero.SetFaction(FactionType.Alliance);
                        hero.move.SetFlip(true);
                    }
                    if (_isMe == false)
                    {
                        m_heroesOther.Add(hero);
                        hero.SetHeroData_TournamentOther(h);
                    }

                    hero.position = TournamentPositionManager.instance.GetPosition(_isMe, h.sortIdx);
                }
            }
        }

#if UNITY_EDITOR
        private void Update()
#else
        private void FixedUpdate()
#endif
        {
            if (m_heroes.Count == 0)
                return;

            // 카메라 중앙 맞추기
            var result = m_heroes.SortBy(x => x.position.x);
            var right = result[result.Count - 1].position;
            var left = result[0].position;

            Vector2 cameraPos = Vector2.zero;
            cameraPos.x = (right.x + left.x) * .5f;

            result = m_heroes.SortBy(x => x.position.y);
            var bottom = result[result.Count - 1].position;
            var top = result[0].position;

            cameraPos.y = (bottom.y + top.y) * .5f;

            m_centerPos.position = cameraPos;

            // 카메라 확대 작업
            var value = Mathf.Clamp((left - right).sqrMagnitude, 180, 400);
            m_camera.fieldOfView = Mathf.Lerp(110, 140, Mathf.InverseLerp(180, 400, value));

            var sqrX = (left - right).sqrMagnitude;
            ScreenLogWorker.Add("sqr X", sqrX);
        }

        void SlotTournamentStatus(TournamentStatusType _status)
        {
            switch (_status)
            {
                case TournamentStatusType.Finished:
                    {
                        m_cts = m_cts.ReleaseCTS();

                        foreach (var hero in m_heroes)
                            if (hero.isLive)
                                hero.SetState(CharacterStateType.None);

                        TournamentHeroInfoManager.instance.SetResult();
                    }
                    break;
            }
        }

        CancellationTokenSource m_cts;
        async UniTask TimerAsync()
        {
            TournamentHeroInfoManager.instance.StartBattle();

            //StageManager.instance.SetState(CharacterStateType.Battle);
            TeamManager.instance.SetState(CharacterStateType.Battle);

            m_cts = m_cts.ReleaseCTS(true);
            var token = m_cts.Token;

            var dtEnd = DateTime.Now.AddSeconds(60);

            TimeSpan ts;
            while (dtEnd >= DateTime.Now)
            {
                ts = dtEnd - DateTime.Now;

                m_txtTimer.text = $"_남은시간_\n<color=#000000><size=160%>{ts.ToRemainTime(55)}";

                await UniTask.NextFrame(token);
            }

            m_txtTimer.text = "";
            TournamentWorker.instance.Finished();
        }
    }
}