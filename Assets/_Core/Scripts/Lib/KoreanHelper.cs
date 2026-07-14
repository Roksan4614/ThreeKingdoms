using UnityEngine;

public static class KoreanHelper
{
    public enum JosaType
    {
        IgA,        // 이/가
        EulLeul,    // 을/를
        EuroroRo // 으로/로
    }

    public static string AppendJosa(string _word, JosaType _josaType, string _format = null)
    {
        char lastChar = _word[_word.Length - 1];

        // 한글 여부 판단
        {
            bool isKorean = (lastChar >= 0xAC00 && lastChar <= 0xD7A3);

            if (!isKorean)
            {
                return _word;
            }
        }

        // 받침 여부 판별 (종성 인덱스가 0이면 받침 없음, 0보다 크면 받침 있음)
        int batchimIndex = (lastChar - 0xAC00) % 28;
        bool hasBatchim = batchimIndex > 0;

        if (_format.IsActive() == true)
            _word = string.Format(_format, _word);

        // 이넘 타입에 따른 최종 결과물 리턴
        switch (_josaType)
        {
            case JosaType.IgA:
                return hasBatchim ? _word + "이" : _word + "가";

            case JosaType.EulLeul:
                return hasBatchim ? _word + "을" : _word + "를";

            case JosaType.EuroroRo:
                return hasBatchim ? _word + "으로" : _word + "로";

            default:
                return _word;
        }
    }
}
