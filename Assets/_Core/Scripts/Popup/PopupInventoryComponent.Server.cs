using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ThreeKingdoms.Client.Server;
using ThreeKingdoms.Shared.Rest;
using ThreeKingdoms.Shared.Types;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Rev9.Inventory
{
    public partial class PopupInventoryComponent
    {
        private static readonly Dictionary<string, RestRequestOptions> PendingOpenRequests = new();
        private GameObject m_containerDialog;
        private TextMeshProUGUI m_containerTitle, m_containerOwned, m_containerQuantity, m_containerDescription;
        private Button m_containerMinus, m_containerPlus, m_containerMax, m_containerOpen, m_containerCancel;
        private long m_containerItemId;
        private int m_containerOpenCount = 1;
        private bool m_openingContainer;
        private bool m_refreshingInventory;

        private static JToken ItemMaster(long id)
            => GameServer.TableRows("s_item").FirstOrDefault(row => (long)row["idx"] == id);
        private static JToken ContainerMaster(long id)
        {
            var key = (string)ItemMaster(id)?["bundle_key"];
            return string.IsNullOrEmpty(key) ? null : GameServer.TableRows("s_bundle").FirstOrDefault(row => (string)row["key"] == key);
        }
        private static long OwnedCount(long id)
            => InventoryWorker.data?.FirstOrDefault(item => item.serverItemId == id)?.count ?? 0;
        private static bool CanOpenServerContainer(ItemData item)
            => GameServer.IsLoggedIn && item != null && item.serverItemId > 0 && OwnedCount(item.serverItemId) > 0 && ContainerMaster(item.serverItemId) != null;

        private async UniTask RefreshServerInventoryAsync()
        {
            if (m_refreshingInventory) return;
            m_refreshingInventory = true;
            try
            {
                await CharacterActions.RefreshAsync();
                if (gameObject.activeInHierarchy) SetItemList();
            }
            catch (Exception error) { PopupManager.instance.AlertShow(error.Message); }
            finally { m_refreshingInventory = false; }
        }
        private void OnServerInventoryChanged()
        {
            if (!GameServer.Enabled || !gameObject.activeInHierarchy) return;
            SetItemList();
            if (m_containerDialog != null && m_containerDialog.activeSelf) RefreshContainerSelection();
        }
        private void OnDestroy() => ServerState.Changed -= OnServerInventoryChanged;

        private void OpenServerContainer(ItemData item)
        {
            if (m_openingContainer || !CanOpenServerContainer(item)) return;
            BuildContainerDialog();
            m_containerItemId = item.serverItemId;
            m_containerOpenCount = 1;
            m_containerDialog.SetActive(true);
            m_containerDialog.transform.SetAsLastSibling();
            RefreshContainerSelection();
        }
        private static string ContainerName(long id)
        {
            var item = ItemMaster(id);
            var bundle = ContainerMaster(id);
            var key = (string)item["key"];
            var reward = (string)bundle?["reward_item_key"];
            string name;
            if ((int)bundle["bundle_item_type"] == 0) name = reward == "rice" ? "군량 주머니" : reward == "time_stone" ? "시간석 주머니" : reward == "free_gold" || reward == "paid_gold" ? "금화 주머니" : "아이템 주머니";
            else if (key.StartsWith("class_soul_stone_random_box")) name = "클래스 영혼석 상자";
            else if (key.StartsWith("dedicated_soul_stone_random_box")) name = "장수 영혼석 상자";
            else if (key.StartsWith("treasure_piece_random_box")) name = "보물 조각 상자";
            else name = key.Replace('_', ' ');
            var grade = (GradeType)(int)bundle["item_grade"];
            name = $"[{TableManager.stringTable.GetGradeType(grade)}] {name}";
            return name;
        }
        private void RefreshContainerSelection()
        {
            var owned = OwnedCount(m_containerItemId);
            var maximum = (int)Math.Min(100, owned);
            m_containerOpenCount = maximum > 0 ? Math.Max(1, Math.Min(maximum, m_containerOpenCount)) : 0;
            m_containerTitle.text = ContainerName(m_containerItemId);
            m_containerOwned.text = $"보유 수량: {owned:#,0}개";
            m_containerQuantity.text = m_containerOpenCount.ToString();
            m_containerDescription.text = "한 번에 최대 100개까지 개봉할 수 있습니다.";
            m_containerMinus.interactable = !m_openingContainer && m_containerOpenCount > 1;
            m_containerPlus.interactable = !m_openingContainer && m_containerOpenCount < maximum;
            m_containerMax.interactable = !m_openingContainer && maximum > 0;
            m_containerOpen.interactable = !m_openingContainer && maximum > 0;
            m_containerCancel.interactable = !m_openingContainer;
        }
        private void ChangeOpenCount(int delta)
        {
            if (m_openingContainer) return;
            m_containerOpenCount += delta;
            RefreshContainerSelection();
        }
        private void CloseContainerDialog()
        {
            if (!m_openingContainer && m_containerDialog != null) m_containerDialog.SetActive(false);
        }
        private async UniTask OpenSelectedContainerAsync()
        {
            if (m_openingContainer) return;
            m_openingContainer = true;
            RefreshContainerSelection();
            var request = new OpenItemContainerReq { ItemId = m_containerItemId, Quantity = m_containerOpenCount };
            var requestKey = $"{GameServer.Uid}:{GameServer.TableVersion}:{request.ItemId}:{request.Quantity}";
            try
            {
                if (request.Quantity < 1 || request.Quantity > 100 || request.Quantity > OwnedCount(request.ItemId) || ContainerMaster(request.ItemId) == null)
                    throw new InvalidOperationException("개봉 수량 또는 보유 아이템을 확인해 주세요.");
                if (!PendingOpenRequests.TryGetValue(requestKey, out var options)) PendingOpenRequests[requestKey] = options = GameServer.Options();
                var response = await GameServer.Item.OpenContainerAsync(request, options);
                var result = response.Data ?? throw new InvalidOperationException("Item container response is empty.");
                var rewards = result.Rewards.Select(reward => ServerState.ToItem(reward.ItemId, reward.Amount)).ToList();
                ServerState.ApplyAsset(result.Asset);
                ServerState.ApplyItems(result.ItemUpdates);
                if (result.CharacterSnapshot != null) await ServerState.ApplyCharactersAsync(result.CharacterSnapshot);
                PendingOpenRequests.Remove(requestKey);
                m_containerDialog.SetActive(false);
                SetItemList();
                // The response has already granted these items. This popup only displays/animates them.
                await PopupManager.instance.OpenPopupAndWait(PopupType.Reward, rewards, true);
            }
            catch (GameServerException error)
            {
                if (error.HttpStatus >= 200 && error.HttpStatus < 500
                    && error.Code != "SERVER_RESPONSE_INVALID" && error.Code != "SERVER_HTTP_ERROR") PendingOpenRequests.Remove(requestKey);
                PopupManager.instance.AlertShow(error.Message);
            }
            catch (Exception error) { PopupManager.instance.AlertShow(error.Message); }
            finally
            {
                m_openingContainer = false;
                if (m_containerDialog.activeSelf) RefreshContainerSelection();
            }
        }
        private void BuildContainerDialog()
        {
            if (m_containerDialog != null) return;
            m_containerDialog = new GameObject("ServerContainerDialog", typeof(RectTransform), typeof(Image));
            m_containerDialog.transform.SetParent(transform, false);
            var overlay = (RectTransform)m_containerDialog.transform;
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            m_containerDialog.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);
            var panel = NewPanel("Panel", overlay, new Vector2(Mathf.Min(760, PopupManager.instance.canvasSize.x * 0.9f), 520), Vector2.zero);
            panel.GetComponent<Image>().color = new Color(0.97f, 0.95f, 0.9f);
            m_containerTitle = NewText("Title", panel, new Vector2(640, 85), new Vector2(0, 178), 36);
            m_containerOwned = NewText("Owned", panel, new Vector2(620, 50), new Vector2(0, 108), 28);
            m_containerQuantity = NewText("Quantity", panel, new Vector2(210, 70), new Vector2(0, 20), 48);
            m_containerMinus = NewButton("Minus", "−", panel, new Vector2(100, 76), new Vector2(-205, 20), () => ChangeOpenCount(-1));
            m_containerPlus = NewButton("Plus", "+", panel, new Vector2(100, 76), new Vector2(205, 20), () => ChangeOpenCount(1));
            m_containerMax = NewButton("Maximum", "최대 수량", panel, new Vector2(250, 62), new Vector2(0, -67), () =>
            { m_containerOpenCount = (int)Math.Min(100, OwnedCount(m_containerItemId)); RefreshContainerSelection(); });
            m_containerDescription = NewText("Description", panel, new Vector2(640, 44), new Vector2(0, -126), 23);
            m_containerCancel = NewButton("Cancel", "취소", panel, new Vector2(240, 76), new Vector2(-154, -198), CloseContainerDialog);
            m_containerOpen = NewButton("Open", "개봉", panel, new Vector2(240, 76), new Vector2(154, -198), () => OpenSelectedContainerAsync().Forget());
        }
        private RectTransform NewPanel(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)obj.transform; rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f; rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }
        private TextMeshProUGUI NewText(string name, Transform parent, Vector2 size, Vector2 position, float fontSize)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)obj.transform; rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f; rect.sizeDelta = size; rect.anchoredPosition = position;
            var text = obj.GetComponent<TextMeshProUGUI>(); text.font = m_element.txtEmpty.font; text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center; text.color = new Color(0.14f, 0.16f, 0.19f); text.raycastTarget = false;
            text.enableAutoSizing = true; text.fontSizeMin = 18; text.fontSizeMax = fontSize;
            return text;
        }
        private Button NewButton(string name, string label, Transform parent, Vector2 size, Vector2 position, UnityAction action)
        {
            var rect = NewPanel(name, parent, size, position); var image = rect.GetComponent<Image>(); image.color = new Color(0.2f, 0.26f, 0.35f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
            NewText("Label", rect, size - new Vector2(12, 8), Vector2.zero, 30).color = Color.white;
            rect.GetComponentInChildren<TextMeshProUGUI>().text = label;
            return button;
        }
    }
}
