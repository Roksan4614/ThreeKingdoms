using UnityEngine;

public class AuthWorker
{
    static AuthWorker m_instance;
    public static AuthWorker instance => m_instance ??= new();

    public static void Release() => m_instance = null;

    AuthData m_data = new();
    public static AuthData data => instance.m_data;

    public void SetAuthData(AuthType _type, string _token)
    {
        m_data.type = _type;
        m_data.token = _token;
    }

    public enum AuthType
    {
        NONE =-1,
        Google,
        Guest,
        Max
    }

    public class AuthData
    {
        public AuthType type;
        public string token;

        public bool isActive => token.IsActive();
    }
}
