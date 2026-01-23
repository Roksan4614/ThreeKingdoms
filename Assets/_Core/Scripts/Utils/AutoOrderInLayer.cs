using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public enum OrderLayerType
{
    none,

    Screen,

    Lobby_Screen_Panel,
    Lobby_Bottom,

    Popup,

    MAX
}

public class AutoOrderInLayer : MonoBehaviour
{
    [SerializeField] private OrderLayerType m_orderLayer = OrderLayerType.none;

    [Header("Apply > Copy > Paste > SAVE!!")]
    [SerializeField]
    private string m_layerName;

    private void Start()
    {
        if (m_orderLayer != OrderLayerType.none)
        {
            SetSortingOrder();
        }

#if !UNITY_EDITOR
        Destroy(this);
#endif
    }

    public void OnButton_Reset()
    {
        m_layerName = m_orderLayer.ToString();
        SetSortingOrder();
    }

    public void OnButton_Repeat()
    {
        for (OrderLayerType layer = OrderLayerType.none; layer <= OrderLayerType.MAX; layer++)
        {
            if (m_layerName == layer.ToString())
            {
                m_orderLayer = layer;
                break;
            }
        }

        SetSortingOrder();
    }

    private void SetSortingOrder()
    {
        int sortingOrder = -1;

        if (string.IsNullOrEmpty(m_layerName) == true)
            return;

        if (m_layerName == m_orderLayer.ToString())
            sortingOrder = (int)m_orderLayer;
        else
        {
            for (OrderLayerType layer = OrderLayerType.none; layer <= OrderLayerType.MAX; layer++)
            {
                if (m_layerName == layer.ToString())
                {
                    sortingOrder = (int)layer;
                    m_orderLayer = layer;

                    IngameLog.Add(0x248700, $"AutoOrderInLayer: {layer}: {transform.GetHierarchyPath()}");
                    break;
                }
            }

            if (sortingOrder == -1)
            {
                IngameLog.AddError($"AutoSortingOrder: Failed: cant find enum data: {gameObject.name} / {m_layerName}");
                return;
            }
        }

        Utils.SetOrderInLayer(transform, m_orderLayer);
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(AutoOrderInLayer))]
public class AutoOrderInLayerButton : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        AutoOrderInLayer generator = target as AutoOrderInLayer;

        if (GUILayout.Button("Save"))
            generator.OnButton_Reset();
        if (GUILayout.Button("Recover"))
            generator.OnButton_Repeat();

        EditorUtility.SetDirty(generator);
    }
}
#endif