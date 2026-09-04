using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;

// AI CODE
public static class DeepCopyExtensions
{
    // private/public 포함 모든 인스턴스 필드를 가져오는 플래그
    private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>
    /// 객체의 필드를 재귀적으로 탐색하여 완전히 독립된 깊은 복사본(Deep Copy)을 생성합니다.
    /// </summary>
    public static T DeepClone<T>(this T source) where T : class
    {
        if (source == null) return null;
        return (T)DeepCloneInternal(source);
    }

    private static object DeepCloneInternal(object source)
    {
        if (source == null) return null;

        Type type = source.GetType();

        // 1. 값 타입(int, float 등) 및 string, Enum은 불변(Immutable) 상태이므로 그대로 반환
        if (type.IsValueType || type == typeof(string))
        {
            return source;
        }

        // 2. 배열(Array) 처리
        if (type.IsArray)
        {
            Type elementType = type.GetElementType();
            Array sourceArray = (Array)source;
            Array newArray = Array.CreateInstance(elementType, sourceArray.Length);

            for (int i = 0; i < sourceArray.Length; i++)
            {
                object item = sourceArray.GetValue(i);
                newArray.SetValue(DeepCloneInternal(item), i);
            }
            return newArray;
        }

        // 3. Dictionary 처리
        if (source is IDictionary dict)
        {
            IDictionary newDict = (IDictionary)CreateInstance(type);
            foreach (DictionaryEntry entry in dict)
            {
                object key = DeepCloneInternal(entry.Key);
                object value = DeepCloneInternal(entry.Value);
                newDict.Add(key, value);
            }
            return newDict;
        }

        // 4. IList (List<T> 등) 처리
        if (source is IList list)
        {
            IList newList = (IList)CreateInstance(type);
            foreach (var item in list)
            {
                newList.Add(DeepCloneInternal(item));
            }
            return newList;
        }

        // 5. 일반 클래스 객체 처리 (필드 단위 재귀 복사)
        object newInstance = CreateInstance(type);
        FieldInfo[] fields = type.GetFields(FieldFlags);

        foreach (var field in fields)
        {
            object fieldValue = field.GetValue(source);
            field.SetValue(newInstance, DeepCloneInternal(fieldValue));
        }

        return newInstance;
    }

    /// <summary>
    /// 기본 생성자가 없는 클래스도 안전하게 인스턴스를 생성
    /// </summary>
    private static object CreateInstance(Type type)
    {
        try
        {
            return Activator.CreateInstance(type);
        }
        catch
        {
            // 기본 생성자가 없거나 private 생성자인 경우 처리
            return FormatterServices.GetUninitializedObject(type);
        }
    }
}