using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Castle_Claim : MonoBehaviour, IValidatable
{
    CastleObjectType m_objectType = CastleObjectType.NONE;

    float duration = .2f;
    float strength = 5f;

    Transform panel => m_element.imgProcess.transform.parent;

    private void Start()
    {
        Signal.instance.UpdateFarmMarketData.connect = SlotUpdateFarmMarketData;

        transform.GetComponent<Button>().onClick.AddListener(OnButton_Claim);
    }

    private void OnEnable()
    {
        transform.rotation = Quaternion.Euler(0, 0, m_element.rotZ);

        Utils.AfterSecond(() =>
        {
            if (gameObject.activeInHierarchy == true)
                ShakeRotation();
            return;
        }, Random.Range(.5f, 2.5f));

        SlotUpdateFarmMarketData(DataManager.castle.GetCaslteData(m_objectType));
    }

    void ShakeRotation()
    {
        var dir = Random.value < .5f ? 1 : -1;

        transform.DOKill();
        transform.DORotate(new Vector3(0, 0, m_element.rotZ - strength * dir), duration).SetEase(Ease.InOutCubic).OnComplete(() =>
        {
            transform.DORotate(new Vector3(0, 0, m_element.rotZ + strength * dir), duration * 2).SetEase(Ease.InOutCubic).OnComplete(() =>
            {
                transform.DORotate(new Vector3(0, 0, m_element.rotZ - strength * dir), duration * 2).SetEase(Ease.InOutCubic).OnComplete(() =>
                {
                    transform.DORotate(new Vector3(0, 0, m_element.rotZ), duration).SetEase(Ease.InOutCubic).OnComplete(() =>
                    {
                        Utils.AfterSecond(() =>
                        {
                            if (gameObject.activeInHierarchy == true)
                                ShakeRotation();
                            return;
                        }, duration + Random.Range(1f, 3.5f));
                    });
                });
            });
        });
    }

    private void OnDisable()
    {
        transform.DOKill();
    }


    public void Initialize(CastleObjectType _objectType)
    {
        m_objectType = _objectType;

        for (int i = 0; i < m_element.txtProcess.Length; i++)
            m_element.txtProcess[i].text = _objectType == CastleObjectType.Market ? "금화" : "군량";

        SlotUpdateFarmMarketData(DataManager.castle.GetCaslteData(_objectType));
    }

    void SlotUpdateFarmMarketData(Data_Castle.CastleData _castleData)
    {
        if (gameObject.activeInHierarchy == false || _castleData.type != m_objectType)
            return;

        var maxAmount = DataManager.castle.GetMaxAmount(_castleData);
        var process = _castleData.totalAmount / (float)maxAmount;

        if (process < 0.1f)
        {
            panel.gameObject.SetActive(false);
            return;
        }

        panel.gameObject.SetActive(true);

        m_element.imgProcess.fillAmount = process;

        //for (int i = 0; i < m_element.txtProcess.Length; i++)
        //    m_element.txtProcess[i].text = process == 1 ? "100%" : $"{process * 100: 0.0}%";
    }

    void OnButton_Claim()
    {
        DataManager.castle.ClaimAsync(m_objectType, _result =>
        {
            if (_result == StatusType.Success)
                SlotUpdateFarmMarketData(DataManager.castle.GetCaslteData(m_objectType));
        }).Forget();
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Image imgProcess;
        public TextMeshProUGUI[] txtProcess;

        public float rotZ;

        public void Initialize(Transform _transform)
        {
            imgProcess = _transform.GetComponent<Image>("Panel/Image");
            txtProcess = _transform.GetComponentsInChildren<TextMeshProUGUI>();

            rotZ = _transform.rotation.eulerAngles.z;
        }
    }
    #endregion VALIDATE

}
