#if UNITY_EDITOR
using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#endif

namespace ThreeKingdoms.Client.Server
{
    // Capture is read-only. Explicit Editor fixtures exercise the existing UI input path without API or asset writes.
    public static class ServerInputProof
    {
        private static object Field(object target, string name)
            => target?.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(target);

        public static JObject Capture()
        {
            var controller = ControllerManager.instance;
            var main = TeamManager.instance?.mainHero;
            var controlled = Field(controller, "m_mainHero") as CharacterComponent;
            var guide = TutorialManager.instance.ServerIssue;
            var stage = StageManager.instance;
            return new JObject
            {
                ["captured_at"] = DateTime.UtcNow.ToString("O"),
                ["frame"] = Time.frameCount,
                ["time_scale"] = Time.timeScale,
                ["scene"] = SceneManager.GetActiveScene().name,
                ["controller"] = controller == null ? null : new JObject
                {
                    ["is_switch"] = controller.isSwitch,
                    ["is_doing"] = controller.isDoing,
                    ["is_keyboard_mode"] = controller.isKeyboardMode,
                    ["has_main_hero"] = controlled != null,
                    ["main_matches_team"] = controlled != null && controlled == main,
                    ["is_pointer_down"] = Field(controller, "m_isPointerDown") as bool?,
                    ["is_keyboard_moving"] = Field(controller, "m_isKeyboardMoving") as bool?,
                    ["active"] = controller.isActiveAndEnabled,
                },
                ["main_hero"] = main == null ? null : new JObject
                {
                    ["key"] = main.info?.key,
                    ["is_live"] = main.isLive,
                    ["position"] = new JObject { ["x"] = main.position.x, ["y"] = main.position.y, ["z"] = main.position.z },
                    ["is_attack_push"] = main.attack?.isAttackPush,
                    ["control_attack_count"] = main.attack?.controlAttackCount,
                    ["skill_use_count"] = main.attack?.skillUseCount,
                    ["is_use_skill"] = main.attack?.isUseSkill,
                    ["is_dash"] = main.move?.isDash,
                },
                ["guide"] = guide == null ? null : new JObject
                {
                    ["issue_id"] = guide.IssueId,
                    ["kind"] = guide.Kind.ToString(),
                    ["quest_id"] = guide.QuestId,
                    ["quest_key"] = guide.QuestKey,
                    ["target_value"] = guide.TargetValue,
                    ["progress"] = TutorialManager.data?.countTagetValue,
                    ["content_ready"] = TutorialManager.instance.ServerContentReady,
                    ["claiming"] = TutorialManager.instance.ServerClaiming,
                    ["complete"] = TutorialManager.instance.ServerProgressComplete,
                },
                ["stage"] = stage == null ? null : new JObject
                {
                    ["stop_stage_start"] = Field(stage, "m_stopStageStart") as bool?,
                    ["reached_chapter"] = stage.recordData?.chapterNumber,
                    ["reached_stage"] = stage.recordData?.stageNumber,
                },
                ["legacy_keys_held"] = new JObject
                {
                    ["w"] = Input.GetKey(KeyCode.W), ["a"] = Input.GetKey(KeyCode.A),
                    ["s"] = Input.GetKey(KeyCode.S), ["d"] = Input.GetKey(KeyCode.D),
                    ["backspace"] = Input.GetKey(KeyCode.Backspace),
                },
            };
        }

#if UNITY_EDITOR
        public static async UniTask<JObject> ButtonTouchAsync(string control, CancellationToken token)
        {
            PrepareMobileControls();
            var controller = ControllerManager.instance;
            var main = TeamManager.instance?.mainHero;
            if (main == null || !main.isLive || !controller.isSwitch)
                throw new InvalidOperationException("LIVE_ENABLED_HERO_REQUIRED");
            var elements = Field(controller, "m_element");
            var button = control == "skill"
                ? (Field(elements, "skill") as Controller_Skill)?.GetComponent<Button>()
                : Field(elements, control == "attack" ? "btnAttack" : "btnDash") as Button;
            if (button == null || !button.isActiveAndEnabled || !button.IsInteractable())
                throw new InvalidOperationException("CONTROL_BUTTON_NOT_AVAILABLE:" + control);
            var events = EventSystem.current;
            var canvas = button.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var rect = (RectTransform)button.transform;
            var point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(events) { pointerId = 9029, button = PointerEventData.InputButton.Left, position = point, pressPosition = point };
            var hits = new List<RaycastResult>();
            events.RaycastAll(pointer, hits);
            if (hits.Count == 0 || !(hits[0].module is GraphicRaycaster)
                || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) != button.gameObject)
                throw new InvalidOperationException("CONTROL_BUTTON_OBSTRUCTED:" + control);
            pointer.pointerPressRaycast = hits[0]; pointer.pointerCurrentRaycast = hits[0];
            var before = Capture();
            var attackCount = main.attack.controlAttackCount;
            var skillCount = main.attack.skillUseCount;
            var pressed = ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);
            try { await UniTask.NextFrame(token); }
            finally { if (pressed != null) ExecuteEvents.Execute(pressed, pointer, ExecuteEvents.pointerUpHandler); }
            token.ThrowIfCancellationRequested();
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            var observedAction = false;
            for (var frame = 0; frame < 45; frame++)
            {
                observedAction |= control == "attack" ? main.attack.isAttackPush : control == "skill" ? main.attack.isUseSkill : main.move.isDash;
                await UniTask.NextFrame(token);
            }
            if (control == "attack") observedAction = main.attack.controlAttackCount > attackCount;
            if (control == "skill") observedAction = main.attack.skillUseCount > skillCount;
            return new JObject
            {
                ["input_source"] = "Unity EventSystem test button gesture", ["control"] = control,
                ["button"] = button.name, ["before"] = before, ["after"] = Capture(),
                ["checks"] = new JArray(new JObject { ["name"] = "actual_control_action_observed", ["passed"] = observedAction })
            };
        }

        public static async UniTask<JObject> MoveTouchAsync(CancellationToken token)
        {
            var before = Capture();
            var mode = PrepareMobileControls();
            var controller = ControllerManager.instance;
            var hero = TeamManager.instance?.mainHero;
            var eventSystem = EventSystem.current;
            if (eventSystem == null || !eventSystem.isActiveAndEnabled)
                throw new InvalidOperationException("ACTIVE_EVENT_SYSTEM_REQUIRED");
            if (hero == null || !hero.isLive || !ReferenceEquals(Field(controller, "m_mainHero"), hero))
                throw new InvalidOperationException("LIVE_ENABLED_CONTROLLER_HERO_REQUIRED");
            if (Time.timeScale <= 0) throw new InvalidOperationException("GAME_TIME_IS_PAUSED");
            var rect = controller.transform as RectTransform;
            if (rect == null) throw new InvalidOperationException("CONTROLLER_RECT_REQUIRED");
            var hit = FindControllerHit(controller, eventSystem, rect, out var start);
            var camera = hit.module.eventCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, start, camera, out var localStart))
                throw new InvalidOperationException("CONTROLLER_SCREEN_POINT_INVALID");
            // Deflect the real joystick beyond its 150-unit clamp; do not move the hero or pad directly.
            var end = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(new Vector3(localStart.x + 180, localStart.y, 0)));
            if (end.x < 2 || end.x >= Screen.width - 2)
                end = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(new Vector3(localStart.x - 180, localStart.y, 0)));
            if (end.x < 2 || end.x >= Screen.width - 2 || end.y < 2 || end.y >= Screen.height - 2)
                throw new InvalidOperationException("JOYSTICK_DRAG_WOULD_LEAVE_GAME_VIEW");
            var pointer = new PointerEventData(eventSystem)
            {
                pointerId = 9029, button = PointerEventData.InputButton.Left,
                position = start, pressPosition = start, delta = Vector2.zero,
                pointerPressRaycast = hit, pointerCurrentRaycast = hit,
                pointerEnter = hit.gameObject, clickCount = 1, clickTime = Time.unscaledTime,
                eligibleForClick = false, useDragThreshold = false,
            };
            var origin = hero.position;
            var firstFrame = Time.frameCount;
            var started = Time.realtimeSinceStartup;
            var frames = 0;
            var samples = new JArray();
            GameObject pressed = null;
            GameObject dragged = null;
            JObject held = null;
            try
            {
                ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
                pressed = ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                if (pressed != controller.gameObject) throw new InvalidOperationException("CONTROLLER_DID_NOT_RECEIVE_POINTER_DOWN");
                pointer.pointerPress = pressed;
                pointer.rawPointerPress = hit.gameObject;
                dragged = ExecuteEvents.GetEventHandler<IDragHandler>(hit.gameObject);
                pointer.pointerDrag = dragged;
                if (dragged != controller.gameObject || !controller.isDoing)
                    throw new InvalidOperationException("CONTROLLER_REJECTED_JOYSTICK_POINTER_DOWN");
                ExecuteEvents.Execute(dragged, pointer, ExecuteEvents.initializePotentialDrag);
                ExecuteEvents.Execute(dragged, pointer, ExecuteEvents.beginDragHandler);
                pointer.dragging = true;
                while (Time.realtimeSinceStartup - started < 1f)
                {
                    token.ThrowIfCancellationRequested();
                    if (controller == null || hero == null || TeamManager.instance?.mainHero != hero)
                        throw new InvalidOperationException("CONTROLLER_OR_HERO_CHANGED_DURING_GESTURE");
                    var position = Vector2.Lerp(start, end, Mathf.Clamp01((Time.realtimeSinceStartup - started) / .12f));
                    pointer.delta = position - pointer.position;
                    pointer.position = position;
                    ExecuteEvents.Execute(dragged, pointer, ExecuteEvents.dragHandler);
                    if (frames % 10 == 0) samples.Add(new JObject
                    {
                        ["frame"] = Time.frameCount, ["elapsed_seconds"] = Time.realtimeSinceStartup - started,
                        ["position"] = Position(hero.position), ["controller_is_doing"] = controller.isDoing,
                        ["guide_progress"] = TutorialManager.data?.countTagetValue,
                    });
                    frames++;
                    await UniTask.NextFrame(token);
                }
                // Keep the pointer down through a final game frame so normal movement and guide observers run.
                await UniTask.NextFrame(token);
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
                held = Capture();
            }
            finally
            {
                if (pressed != null) ExecuteEvents.Execute(pressed, pointer, ExecuteEvents.pointerUpHandler);
                if (dragged != null) ExecuteEvents.Execute(dragged, pointer, ExecuteEvents.endDragHandler);
                if (hit.gameObject != null) ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerExitHandler);
                pointer.dragging = false;
            }
            await UniTask.NextFrame(token);
            var after = Capture();
            var distance = hero == null ? 0 : Vector3.Distance(origin, hero.position);
            var expectedGuide = (string)before["guide"]?["quest_key"] == "move"
                && (long?)before["guide"]?["progress"] < (long?)before["guide"]?["target_value"];
            var sameIssue = (string)before["guide"]?["issue_id"] == (string)after["guide"]?["issue_id"];
            var progressed = (long?)after["guide"]?["progress"] > (long?)before["guide"]?["progress"];
            return new JObject
            {
                ["input_source"] = "Unity EventSystem test gesture",
                ["fixture"] = "move_touch",
                ["mode_preparation"] = mode,
                ["raycast_target"] = hit.gameObject == null ? null : hit.gameObject.name,
                ["raycaster"] = hit.module.GetType().Name,
                ["pointer_start"] = new JObject { ["x"] = start.x, ["y"] = start.y },
                ["pointer_end"] = new JObject { ["x"] = end.x, ["y"] = end.y },
                ["before"] = before, ["during_hold"] = held, ["after"] = after,
                ["samples"] = samples, ["drag_frames"] = frames,
                ["game_frames_elapsed"] = Time.frameCount - firstFrame,
                ["elapsed_seconds"] = Time.realtimeSinceStartup - started,
                ["displacement_meters"] = distance,
                ["checks"] = new JArray
                {
                    new JObject { ["name"] = "gesture_spans_game_frames", ["passed"] = Time.frameCount - firstFrame >= 2 },
                    new JObject { ["name"] = "actual_displacement_above_two_meters", ["actual"] = distance, ["passed"] = distance > 2f },
                    new JObject { ["name"] = "movement_guide_observed_real_input", ["applicable"] = expectedGuide,
                        ["passed"] = !expectedGuide || (sameIssue && progressed) },
                    new JObject { ["name"] = "pointer_released", ["passed"] = !(Field(controller, "m_isPointerDown") is bool down && down) },
                },
            };
        }

        private static JObject Position(Vector3 value) => new JObject { ["x"] = value.x, ["y"] = value.y, ["z"] = value.z };

        private static RaycastResult FindControllerHit(ControllerManager controller, EventSystem events, RectTransform rect, out Vector2 point)
        {
            var canvas = controller.GetComponentInParent<Canvas>();
            if (canvas == null) throw new InvalidOperationException("CONTROLLER_CANVAS_REQUIRED");
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var hits = new List<RaycastResult>();
            foreach (var y in new[] { .5f, .35f, .65f, .2f, .8f })
                foreach (var x in new[] { .25f, .15f, .4f, .55f, .7f, .85f })
                {
                    var world = Vector3.Lerp(Vector3.Lerp(corners[0], corners[3], x), Vector3.Lerp(corners[1], corners[2], x), y);
                    point = RectTransformUtility.WorldToScreenPoint(camera, world);
                    if (point.x < 2 || point.x >= Screen.width - 2 || point.y < 2 || point.y >= Screen.height - 2) continue;
                    var pointer = new PointerEventData(events) { position = point };
                    hits.Clear(); events.RaycastAll(pointer, hits);
                    if (hits.Count > 0 && hits[0].module is GraphicRaycaster
                        && ExecuteEvents.GetEventHandler<IPointerDownHandler>(hits[0].gameObject) == controller.gameObject)
                        return hits[0];
                }
            throw new InvalidOperationException("NO_UNOBSTRUCTED_GRAPHIC_RAYCAST_TO_CONTROLLER");
        }

        // Explicit test fixture for real pointer drags. It changes one field on the live instance only.
        public static JObject PrepareMobileControls()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("PLAYMODE_REQUIRED");
            if (!GameServer.IsLoggedIn || GameServer.Uid != 29 || GameServer.Settings.TestUid != 29)
                throw new InvalidOperationException("DEDICATED_TEST_UID_29_REQUIRED");
            var controller = ControllerManager.instance;
            if (controller == null || !controller.gameObject.scene.IsValid() || !controller.gameObject.scene.isLoaded
                || EditorUtility.IsPersistent(controller) || PrefabUtility.IsPartOfPrefabAsset(controller))
                throw new InvalidOperationException("LIVE_CONTROLLER_REQUIRED");
            if (Field(controller, "m_isPointerDown") is bool down && down)
                throw new InvalidOperationException("RELEASE_POINTER_BEFORE_SWITCHING_CONTROLS");
            var before = Capture();
            var serialized = new SerializedObject(controller);
            var keyboard = serialized.FindProperty("m_isKeyboardMode");
            if (keyboard == null || keyboard.propertyType != SerializedPropertyType.Boolean)
                throw new InvalidOperationException("CONTROLLER_KEYBOARD_FIELD_MISSING");
            keyboard.boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var after = Capture();
            Debug.Log($"[INPUT_PROOF] runtime_only keyboard_mode={(bool?)before["controller"]?["is_keyboard_mode"]}->false");
            return new JObject
            {
                ["fixture"] = "prepare_mobile_controls",
                ["scope"] = "Current PlayMode controller instance only; no Prefab, Scene or ProjectSettings save.",
                ["before"] = before,
                ["after"] = after,
                ["checks"] = new JArray(new JObject
                {
                    ["name"] = "live_controller_mobile_mode", ["expected"] = false,
                    ["actual"] = controller.isKeyboardMode, ["passed"] = !controller.isKeyboardMode,
                }),
            };
        }
#endif
    }
}
#endif
