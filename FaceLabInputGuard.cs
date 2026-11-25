// FaceLabInputGuard.cs —— 面板防点击穿透（含 Unity UGUI 按钮防穿透）& 输入框屏蔽游戏键盘
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;

namespace ParaFaceLab
{
    internal static class FaceLabInputGuard
    {
        internal static bool uiDrag = false;
        private static Vector2 lastGuiPos = new Vector2(-9999f, -9999f);
        private static bool hasGuiPos = false;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT p);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static readonly bool[] rawHeld = new bool[256];

        private static bool kbTried = false;
        private static bool kbDisabled = false;
        private static object kbDevice = null;
        private static MethodInfo mDisable = null, mEnable = null;
        private static int mDisableArgs = 1, mEnableArgs = 1;

        private static bool extraPatched = false;
        private static PropertyInfo pDevice = null;

        internal static bool RawKeyDown(int vk)
        {
            int i = vk & 0xFF;
            bool down;
            try { down = (GetAsyncKeyState(vk) & 0x8000) != 0; }
            catch { return false; }
            bool was = rawHeld[i];
            rawHeld[i] = down;
            return down && !was;
        }

        internal static void NoteGui(Vector2 p)
        {
            lastGuiPos = p;
            hasGuiPos = true;
        }

        private static Vector2 Physical()
        {
            try
            {
                POINT p;
                if (GetCursorPos(out p)) return new Vector2(p.X, p.Y);
            }
            catch { }
            return new Vector2(-9999f, -9999f);
        }

        internal static Vector2 Pointer()
        {
            if (hasGuiPos) return lastGuiPos;
            try
            {
                Vector3 m = Input.mousePosition;
                return new Vector2(m.x, Screen.height - m.y);
            }
            catch { return new Vector2(-9999f, -9999f); }
        }

        internal static bool PointerOnPanel()
        {
            if (!FaceLabPlugin.PanelOpen) return false;
            Rect r = FaceLabPlugin.ScreenRect;
            if (r.width <= 1f || r.height <= 1f) return false;
            const float P = 14f;
            Rect pad = new Rect(r.x - P, r.y - P, r.width + P * 2f, r.height + P * 2f);
            Vector2 g = Pointer();
            if (pad.Contains(g)) return true;
            Vector2 p = Physical();
            float dx = p.x - g.x, dy = p.y - g.y;
            if (dx * dx + dy * dy < 80f * 80f && pad.Contains(p)) return true;
            return false;
        }

        internal static bool ShouldBlockMouse()
        {
            return FaceLabPlugin.PanelOpen && (uiDrag || PointerOnPanel());
        }

        internal static void InstallInputSystemPatches(Harmony h)
        {
            if (extraPatched) return;
            extraPatched = true;

            // 1. 拦 Unity 新 InputSystem 的鼠标与 Action（防镜头旋转/场景选中穿透）
            try
            {
                Type bc = Type.GetType("UnityEngine.InputSystem.Controls.ButtonControl, Unity.InputSystem");
                if (bc != null)
                {
                    pDevice = bc.GetProperty("device", BindingFlags.Public | BindingFlags.Instance);
                    PatchProp(h, bc, "wasPressedThisFrame");
                    PatchProp(h, bc, "isPressed");
                }
                Type ia = Type.GetType("UnityEngine.InputSystem.InputAction, Unity.InputSystem");
                if (ia != null)
                {
                    PatchMethod(h, ia, "WasPressedThisFrame");
                    PatchMethod(h, ia, "IsPressed");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ParaFaceLab] InputSystem patch skipped: " + e.Message);
            }

            // 2. 拦 Unity UGUI GraphicRaycaster（彻底防止点面板时穿透点到背后的游戏退出/菜单按钮！）
            try
            {
                Type gr = Type.GetType("UnityEngine.UI.GraphicRaycaster, UnityEngine.UI");
                if (gr != null)
                {
                    MethodInfo[] ms = gr.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                    MethodInfo prefix = typeof(FaceLabInputGuard).GetMethod("PrefixGraphicRaycast", BindingFlags.NonPublic | BindingFlags.Static);
                    for (int i = 0; i < ms.Length; i++)
                    {
                        if (ms[i].Name == "Raycast" && ms[i].GetParameters().Length == 2)
                        {
                            h.Patch(ms[i], new HarmonyMethod(prefix));
                            Debug.Log("[ParaFaceLab] GraphicRaycaster.Raycast blocked when pointer over panel.");
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ParaFaceLab] GraphicRaycaster patch skipped: " + e.Message);
            }
        }

        private static bool PrefixGraphicRaycast()
        {
            try
            {
                if (ShouldBlockMouse()) return false; // 指针在 FaceLab 面板上时，背后所有游戏 UGUI 按钮完全收不到点击
                return true;
            }
            catch { return true; }
        }

        private static void PatchProp(Harmony h, Type t, string name)
        {
            try
            {
                PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                MethodInfo get = p != null ? p.GetGetMethod() : null;
                if (get == null) return;
                MethodInfo prefix = typeof(FaceLabInputGuard).GetMethod("PrefixButton", BindingFlags.NonPublic | BindingFlags.Static);
                h.Patch(get, new HarmonyMethod(prefix));
            }
            catch { }
        }

        private static void PatchMethod(Harmony h, Type t, string name)
        {
            try
            {
                MethodInfo m = t.GetMethod(name, Type.EmptyTypes);
                if (m == null) return;
                MethodInfo prefix = typeof(FaceLabInputGuard).GetMethod("PrefixAction", BindingFlags.NonPublic | BindingFlags.Static);
                h.Patch(m, new HarmonyMethod(prefix));
            }
            catch { }
        }

        private static bool PrefixAction(ref bool __result)
        {
            try
            {
                if (!ShouldBlockMouse()) return true;
                __result = false;
                return false;
            }
            catch { return true; }
        }

        private static bool PrefixButton(object __instance, ref bool __result)
        {
            try
            {
                if (!IsMouseControl(__instance)) return true;
                if (!ShouldBlockMouse()) return true;
                __result = false;
                return false;
            }
            catch { return true; }
        }

        private static bool IsMouseControl(object ctrl)
        {
            if (ctrl == null || pDevice == null) return false;
            object dev = pDevice.GetValue(ctrl, null);
            if (dev == null) return false;
            string tn = dev.GetType().Name;
            if (tn != "Mouse" && tn != "Pointer") return false;
            try
            {
                PropertyInfo np = ctrl.GetType().GetProperty("name", BindingFlags.Public | BindingFlags.Instance);
                string nm = np == null ? "" : (np.GetValue(ctrl, null) as string);
                return nm == "leftButton" || nm == "rightButton" || nm == "middleButton" || nm == "press";
            }
            catch { return false; }
        }

        private static void InitKb()
        {
            if (kbTried) return;
            kbTried = true;
            try
            {
                Type isT = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
                Type kbT = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
                if (isT == null || kbT == null) return;
                PropertyInfo cur = kbT.GetProperty("current", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (cur != null) kbDevice = cur.GetValue(null, null);
                MethodInfo[] ms = isT.GetMethods(BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < ms.Length; i++)
                {
                    MethodInfo m = ms[i];
                    if (m.IsGenericMethodDefinition) continue;
                    ParameterInfo[] ps = m.GetParameters();
                    if (ps.Length < 1 || ps.Length > 2) continue;
                    if (ps.Length == 2 && ps[1].ParameterType != typeof(bool)) continue;
                    if (m.Name == "DisableDevice" && (mDisable == null || ps.Length < mDisableArgs))
                    { mDisable = m; mDisableArgs = ps.Length; }
                    else if (m.Name == "EnableDevice" && (mEnable == null || ps.Length < mEnableArgs))
                    { mEnable = m; mEnableArgs = ps.Length; }
                }
            }
            catch { }
        }

        internal static void SetGameKeyboard(bool enable)
        {
            InitKb();
            if (kbDevice == null || mDisable == null || mEnable == null) return;
            try
            {
                if (!enable && !kbDisabled)
                {
                    object[] a = mDisableArgs == 2 ? new object[] { kbDevice, false } : new object[] { kbDevice };
                    mDisable.Invoke(null, a);
                    kbDisabled = true;
                }
                else if (enable && kbDisabled)
                {
                    object[] a = mEnableArgs == 2 ? new object[] { kbDevice, false } : new object[] { kbDevice };
                    mEnable.Invoke(null, a);
                    kbDisabled = false;
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(InputManager), "IsPointerOverUI")]
    internal static class FaceLabPointerOverUiPatch
    {
        private static void Postfix(ref bool __result)
        {
            try
            {
                if (FaceLabInputGuard.ShouldBlockMouse()) __result = true;
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(Input), "GetMouseButtonDown", new Type[] { typeof(int) })]
    internal static class FaceLabMouseDownPatch
    {
        private static bool Prefix(ref bool __result)
        {
            try
            {
                if (FaceLabInputGuard.PointerOnPanel()) { __result = false; return false; }
                return true;
            }
            catch { return true; }
        }
    }

    [HarmonyPatch(typeof(Input), "GetMouseButton", new Type[] { typeof(int) })]
    internal static class FaceLabMouseHoldPatch
    {
        private static bool Prefix(ref bool __result)
        {
            try
            {
                if (FaceLabInputGuard.uiDrag) { __result = false; return false; }
                return true;
            }
            catch { return true; }
        }
    }

    [HarmonyPatch(typeof(Input), "GetMouseButtonUp", new Type[] { typeof(int) })]
    internal static class FaceLabMouseUpPatch
    {
        private static bool Prefix()
        {
            FaceLabInputGuard.uiDrag = false;
            return true;
        }
    }
}
