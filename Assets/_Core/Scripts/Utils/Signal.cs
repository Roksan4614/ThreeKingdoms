using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;


public class SignalEntry<T>
{
    public UnityEngine.Object owner;
    public UnityAction<T> action;
    public bool isOwner;

    public SignalEntry(UnityEngine.Object _owner, UnityAction<T> _action)
    {
        owner = _owner;
        action = _action;
        isOwner = _owner != null;
    }

    public bool isInactive => isOwner == true && owner == null;
}

public class SignalEntry
{
    public UnityEngine.Object owner;
    public UnityAction action;
    public bool isOwner;

    public SignalEntry(UnityEngine.Object _owner, UnityAction _action)
    {
        owner = _owner;
        action = _action;
        isOwner = _owner != null;
    }

    public bool isInactive => isOwner == true && owner == null;
}

public class Signal
{
    #region INITIALIZE

    private static Signal m_instance;

    public static Signal instance
    {
        get
        {
            if (m_instance == null)
                m_instance = new Signal();
            return m_instance;
        }
    }

    public static void Release()
    {
        m_instance = null;
    }

    public class SignalObject<T0>
    {
        public delegate IEnumerator YieldSlot(T0 _param);

        private List<SignalEntry<T0>> m_func = new();
        //private YieldSlot m_doFunc;

        //private List<UnityAction<T0>> m_funcYieldFinished = new List<UnityAction<T0>>();

        //public IEnumerator DoEmit(T0 _param)
        //{
        //    Emit(_param);

        //    yield return m_doFunc(_param);

        //    for (int i = 0; i < m_funcYieldFinished.Count; i++)
        //    {
        //        if (m_funcYieldFinished[i].Target.Equals(null))
        //        {
        //            m_funcYieldFinished.RemoveAt(i);
        //            i--;
        //            continue;
        //        }

        //        m_funcYieldFinished[i].Invoke(_param);
        //    }
        //}

        public void Emit(T0 _param)
        {
            for (int i = 0; i < m_func.Count; i++)
            {
                try
                {
                    if (m_func[i].isInactive)
                    {
                        m_func.RemoveAt(i);
                        i--;
                        continue;
                    }

                    m_func[i].action(_param);
                }
                catch (Exception _e)
                {
                    string err = m_func[i].action.Target + ": " + (string.IsNullOrEmpty(_e.StackTrace) ? _e.ToString() : _e.StackTrace);

                    IngameLog.AddError($"SignalError: " + err);
                    //Api.SendExceptionError("SignalError: " + err);
                    //m_func.RemoveAt(i);
                    //i--;
                }
            }
        }

        public SignalEntry<T0> connectLambda
        {
            set
            {
                for (int i = 0; i < m_func.Count; i++)
                {
                    if (m_func[i].isInactive)
                    {
                        m_func.RemoveAt(i);
                        i--;
                    }
                }

                m_func.Add(value);
            }
        }

        public UnityAction<T0> connect
        {
            set
            {
                for (int i = 0; i < m_func.Count; i++)
                {
                    if (m_func[i].isInactive)
                    {
                        m_func.RemoveAt(i);
                        i--;
                    }
                }

                var entry = new SignalEntry<T0>(value.Target as UnityEngine.Object, value);

                m_func.Add(entry);
            }
        }

        //public YieldSlot connectYield
        //{
        //    set
        //    {
        //        if (m_doFunc != null)
        //            IngameLog.Add("ffff00", "Already combine ienumarator");
        //        m_doFunc = value;
        //    }
        //}
    }

    public class SignalObject
    {
        public delegate IEnumerator YieldSlot();

        private List<SignalEntry> m_func = new List<SignalEntry>();
        //private YieldSlot m_doFunc;

        //private List<UnityAction> m_funcYieldFinished = new List<UnityAction>();

        //public IEnumerator DoEmit()
        //{
        //    Emit();

        //    yield return m_doFunc();

        //    for (int i = 0; i < m_funcYieldFinished.Count; i++)
        //    {
        //        if (m_funcYieldFinished[i].Target.Equals(null))
        //        {
        //            m_funcYieldFinished.RemoveAt(i);
        //            i--;
        //            continue;
        //        }

        //        m_funcYieldFinished[i].Invoke();
        //    }
        //}

        public void Emit()
        {
            for (int i = 0; i < m_func.Count; i++)
            {
                try
                {
                    if (m_func[i].isInactive)
                    {
                        m_func.RemoveAt(i);
                        i--;
                        continue;
                    }

                    m_func[i].action();
                }
                catch (Exception _e)
                {
                    string err = m_func[i].action.Target + ": " + (string.IsNullOrEmpty(_e.StackTrace) ? _e.ToString() : _e.StackTrace);

                    IngameLog.AddError($"SignalError: " + err);

                    //m_func.RemoveAt(i);
                    //i--;
                }
            }
        }

        public SignalEntry connectLambda
        {
            set
            {
                for (int i = 0; i < m_func.Count; i++)
                {
                    if (m_func[i].isInactive)
                    {
                        m_func.RemoveAt(i);
                        i--;
                    }
                }

                m_func.Add(value);
            }
        }

        public UnityAction connect
        {
            set
            {
                for (int i = 0; i < m_func.Count; i++)
                {
                    if (m_func[i].isInactive)
                    {
                        m_func.RemoveAt(i);
                        i--;
                    }
                }

                var entry = new SignalEntry(value.Target as UnityEngine.Object, value);

                m_func.Add(entry);
            }
        }

        //public YieldSlot connectYield
        //{
        //    set
        //    {
        //        if (m_doFunc != null)
        //            IngameLog.Add("ffff00", "Already combine ienumarator");
        //        m_doFunc = value;
        //    }
        //}
    }

    #endregion INITIALIZE

    #region Signal

    public SignalObject ApplicationQuit = new();

    public SignalObject<LobbyScreenType> CloseLobbyScreen = new();

    public SignalObject<CharacterComponent> ConnectMainHero = new();
    public SignalObject<CharacterComponent> UpdateHP = new();
    public SignalObject<float> UpdageBossHP = new(); //percent

    public SignalObject<StageManager.LoadData_Stage> StartStage = new();
    public SignalObject<int> StartPhase = new(); // Phase index
    

    #endregion
}