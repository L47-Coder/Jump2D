using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 统一处理游戏区域的跳跃输入。
// 触摸屏点击 UI 时交给 UI 处理，点击游戏区域时才触发跳跃；鼠标和键盘仍保留给电脑端调试。
public static class GameplayInput
{
    private static readonly List<RaycastResult> RaycastResults = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ConfigureTouchSimulation()
    {
        // 移动端直接读取 Touch，避免同一次触摸再被模拟成鼠标点击，导致 UI 重复触发。
        if (Application.isMobilePlatform)
            Input.simulateMouseWithTouches = false;
    }

    public static bool GetJumpDown()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
            return true;

        bool hasTouchBegan = false;
        bool hasTouchOverUi = false;
        bool hasGameplayTouch = false;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase != TouchPhase.Began)
                continue;

            hasTouchBegan = true;
            if (IsPointerOverUi(touch.position, touch.fingerId))
                hasTouchOverUi = true;
            else
                hasGameplayTouch = true;
        }

        if (hasTouchBegan)
        {
            // 只要本帧有触摸 UI，就不让同一组触摸误触发跳跃。
            // 这样点击暂停、继续、重开按钮时不会同时让角色跳跃。
            return !hasTouchOverUi && hasGameplayTouch;
        }

        // 编辑器/Standalone 继续支持鼠标左键模拟；移动端已关闭 touch->mouse 模拟。
        return Input.GetMouseButtonDown(0) && !IsPointerOverUi(Input.mousePosition, -1);
    }

    private static bool IsPointerOverUi(Vector2 position, int pointerId)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        if (pointerId >= 0 && eventSystem.IsPointerOverGameObject(pointerId))
            return true;
        if (pointerId < 0 && eventSystem.IsPointerOverGameObject())
            return true;

        // EventSystem 还没处理到本帧输入时，主动对 GraphicRaycaster 做一次射线检测，
        // 保证触摸按钮不会在 Player.Update 中被误判成游戏区域点击。
        var pointerEventData = new PointerEventData(eventSystem)
        {
            pointerId = pointerId,
            position = position
        };

        RaycastResults.Clear();
        eventSystem.RaycastAll(pointerEventData, RaycastResults);
        return RaycastResults.Count > 0;
    }
}
