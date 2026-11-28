// ParaFaceLab.cs —— Paralives 高自由度面部/身材捏脸拉杆工具 v0.3
using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ParaFaceLab
{
    [BepInPlugin("paralives.facelab", "ParaFaceLab", "1.3.0")]
    public partial class FaceLabPlugin : BaseUnityPlugin
    {
        public const ulong OFFICIAL_MOD_GUID = 9174628391048271645UL;

        public static FaceLabPlugin Instance;
        public static bool PanelOpen = false;
        public static Rect ScreenRect = new Rect(0, 0, 0, 0);
        public static ulong CurrentTargetGuid = 0;
        public static bool GlobalUnlockLimits = false;
        public static bool OfficialModEnabled = true;

        private float _nextModStatusCheck = 0f;

        private Harmony _harmony;
        private ConfigEntry<KeyCode> _hotkeyCfg;
        private ConfigEntry<int> _themeCfg;
        private ConfigEntry<int> _scaleCfg;
        private ConfigEntry<string> _langCfg;
        private ConfigEntry<bool> _globalUnlockCfg;

        private bool _open = false;
        private float _hotkeyLockUntil = 0f;
        private float _undoKeyLockUntil = 0f;

        private readonly List<AssetCharacter> _candidates = new List<AssetCharacter>();
        private AssetCharacter _targetChar = null;
        private bool _inParamaker = false;
        private bool _manualPick = false;
        private float _nextTargetRefresh = 0f;

        private static FieldInfo _fiCreatorCurrentChar;
        private static bool _fiCreatorChecked = false;

        private void Awake()
        {
            Instance = this;
            try
            {
                _hotkeyCfg = Config.Bind("General", "Hotkey", KeyCode.F9, "Hotkey to toggle ParaFaceLab panel / 打开或关闭捏脸面板快捷键");
                _themeCfg = Config.Bind("UI", "ThemeIndex", 0, "Theme: 0=Cream, 1=Mist, 2=Dark, 3=Sakura");
                _scaleCfg = Config.Bind("UI", "ScaleTier", 1, "UI Scale: 0=100%, 1=115%, 2=130%");
                _langCfg = Config.Bind("UI", "Language", "auto", "Language: auto / zh-CN / zh-TW / en / ja / ko");
                _globalUnlockCfg = Config.Bind("Sliders", "GlobalUnlockLimits", false, "Unlock vanilla deformation range limits globally (off by default so untouched saves/NPCs stay 100% vanilla)");

                _themeIdx = Mathf.Clamp(_themeCfg.Value, 0, FaceLabThemes.All.Length - 1);
                _scaleIdx = Mathf.Clamp(_scaleCfg.Value, 0, SCALE_TIERS.Length - 1);
                FaceLabI18n.Current = FaceLabI18n.ParseCode(_langCfg.Value);
                GlobalUnlockLimits = _globalUnlockCfg.Value;
            }
            catch { }

            try
            {
                FaceLabStorage.Init(Info.Location);
            }
            catch (Exception e)
            {
                Logger.LogError("[ParaFaceLab] Storage init failed: " + e);
            }

            try
            {
                _harmony = new Harmony("paralives.facelab");
                _harmony.PatchAll(typeof(FaceLabPlugin).Assembly);
                FaceLabInputGuard.InstallInputSystemPatches(_harmony);
                Logger.LogInfo("[ParaFaceLab] v1.3.0 loaded. Press " + (_hotkeyCfg != null ? _hotkeyCfg.Value.ToString() : "F9") + " to toggle.");
            }
            catch (Exception e)
            {
                Logger.LogError("[ParaFaceLab] Harmony patch failed: " + e);
            }
        }

        private void OnDestroy()
        {
            FaceLabInputGuard.SetGameKeyboard(true);
            FaceLabStorage.FlushAll();
            PanelOpen = false;
            ScreenRect = new Rect(0, 0, 0, 0);
            try { if (_harmony != null) _harmony.UnpatchSelf(); } catch { }
        }

        private void OnApplicationQuit()
        {
            FaceLabStorage.FlushAll();
        }

        private void RefreshOfficialModEnabled()
        {
            if (Time.unscaledTime < _nextModStatusCheck) return;
            _nextModStatusCheck = Time.unscaledTime + 0.5f;
            try
            {
                ModManager mm = ModManager.Instance;
                if (mm != null)
                {
                    AssetMod am = mm.GetAssetModByGUID(OFFICIAL_MOD_GUID);
                    if (am != null)
                    {
                        bool prev = OfficialModEnabled;
                        OfficialModEnabled = am.Enabled;
                        if (prev && !OfficialModEnabled)
                        {
                            if (_open) TogglePanel();
                            FaceLabRuntime.RestoreAllVisualsImmediately();
                        }
                        return;
                    }
                }
            }
            catch { }
            OfficialModEnabled = true;
        }

        private void Update()
        {
            RefreshOfficialModEnabled();
            if (!OfficialModEnabled)
            {
                PanelOpen = false;
                ScreenRect = new Rect(0, 0, 0, 0);
                FaceLabInputGuard.uiDrag = false;
                FaceLabRuntime.EndCompareOriginal();
                FaceLabInputGuard.SetGameKeyboard(true);
                return;
            }

            FaceLabStorage.TickAutoSave();
            FaceLabRuntime.TickPendingMeshRebuild(false);

            KeyCode hk = _hotkeyCfg != null ? _hotkeyCfg.Value : KeyCode.F9;
            int vk = KeyCodeToVk(hk);
            bool pressed = false;
            try
            {
                if (Time.unscaledTime >= _hotkeyLockUntil)
                {
                    if (vk != 0 && FaceLabInputGuard.RawKeyDown(vk)) pressed = true;
                    else if (Input.GetKeyDown(hk)) pressed = true;
                }
            }
            catch
            {
                if (vk != 0 && Time.unscaledTime >= _hotkeyLockUntil && FaceLabInputGuard.RawKeyDown(vk))
                    pressed = true;
            }

            if (pressed)
            {
                _hotkeyLockUntil = Time.unscaledTime + 0.25f;
                TogglePanel();
            }

            if (_open)
            {
                PanelOpen = true;
                if (Time.unscaledTime >= _nextTargetRefresh)
                {
                    _nextTargetRefresh = Time.unscaledTime + 0.25f;
                    RefreshTargetCharacter();
                }

                // 支持 Ctrl+Z 撤回 / Ctrl+Y 重做（仅在未输入文字且面板打开时）
                if (!_hadTextFocus && _targetChar != null && Time.unscaledTime >= _undoKeyLockUntil)
                {
                    try
                    {
                        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                        if (ctrl && Input.GetKeyDown(KeyCode.Z))
                        {
                            _undoKeyLockUntil = Time.unscaledTime + 0.18f;
                            PerformUndo();
                        }
                        else if (ctrl && Input.GetKeyDown(KeyCode.Y))
                        {
                            _undoKeyLockUntil = Time.unscaledTime + 0.18f;
                            PerformRedo();
                        }
                    }
                    catch { }
                }
            }
            else
            {
                PanelOpen = false;
                ScreenRect = new Rect(0, 0, 0, 0);
                FaceLabInputGuard.uiDrag = false;
                FaceLabRuntime.EndCompareOriginal();
                FaceLabInputGuard.SetGameKeyboard(true);
            }
        }

        private void TogglePanel()
        {
            _open = !_open;
            PanelOpen = _open;
            if (_open)
            {
                if (_langCfg != null && string.Equals(_langCfg.Value, "auto", StringComparison.OrdinalIgnoreCase))
                    FaceLabI18n.Current = FaceLabI18n.DetectGameLanguage();
                _manualPick = false;
                RefreshTargetCharacter();
                RefreshPresetList();
                RefreshVanillaSlidersCache(true);
                if (_targetChar != null)
                    FaceLabHistory.EnsureInitialSnapshot(_targetChar, _vanillaSliders);
            }
            else
            {
                FaceLabInputGuard.SetGameKeyboard(true);
                FaceLabRuntime.EndCompareOriginal();
                FaceLabRuntime.TickPendingMeshRebuild(true);
                FaceLabStorage.FlushAll();
                if (!_inParamaker && _targetChar != null && FaceLabRuntime.ConsumeThumbRefreshPending(_targetChar.GUID))
                    FaceLabRuntime.RequestThumbnailRefresh(_targetChar);
            }
        }

        private static AssetCharacter TryGetParamakerActiveCharacter()
        {
            try
            {
                if (!_fiCreatorChecked)
                {
                    _fiCreatorChecked = true;
                    _fiCreatorCurrentChar = typeof(UICharacterCreator).GetField("_currentCharacter",
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                }
                UICharacterCreator ui = UnityEngine.Object.FindObjectOfType<UICharacterCreator>();
                if (ui != null && ui.isActiveAndEnabled && _fiCreatorCurrentChar != null)
                {
                    return _fiCreatorCurrentChar.GetValue(ui) as AssetCharacter;
                }
            }
            catch { }
            return null;
        }

        private void RefreshTargetCharacter()
        {
            _candidates.Clear();
            CharacterManager cm = CharacterManager.Instance;
            if (cm == null)
            {
                _targetChar = null;
                CurrentTargetGuid = 0;
                _inParamaker = false;
                return;
            }

            Player player = null;
            ulong selGuid = 0;
            _inParamaker = false;
            try
            {
                PlayerManager pm = PlayerManager.Instance;
                if (pm != null)
                {
                    HybridPlayer hp = pm.GetHybridPlayer(0);
                    if (hp != null && hp.Player != null)
                    {
                        player = hp.Player;
                        _inParamaker = (player.State == GameStates.CharacterCreator);
                        selGuid = player.GetSelectedCharacterGUID();
                    }
                }
            }
            catch { }

            AssetCharacter creatorChar = _inParamaker ? TryGetParamakerActiveCharacter() : null;
            AssetCharacter selectedChar = null;
            if (selGuid != 0)
            {
                try { selectedChar = cm.GetCharacterByGUID(selGuid); } catch { }
            }

            HashSet<ulong> seen = new HashSet<ulong>();
            if (creatorChar != null && seen.Add(creatorChar.GUID)) _candidates.Add(creatorChar);
            if (selectedChar != null && seen.Add(selectedChar.GUID)) _candidates.Add(selectedChar);

            try
            {
                List<AssetCharacter> all = cm.Characters;
                if (all != null)
                {
                    for (int i = 0; i < all.Count; i++)
                    {
                        AssetCharacter c = all[i];
                        if (c == null || c.IsDummy) continue;
                        bool hasVis = (cm.LoadedCharacterVisuals != null && cm.LoadedCharacterVisuals.ContainsKey(c.GUID));
                        if (!hasVis && !c.IsInHousehold) continue;
                        if (seen.Add(c.GUID)) _candidates.Add(c);
                    }
                }
            }
            catch { }

            AssetCharacter prev = _targetChar;

            if (_inParamaker)
            {
                _manualPick = false;
                _targetChar = creatorChar ?? selectedChar ?? (_candidates.Count > 0 ? _candidates[0] : null);
            }
            else
            {
                if (selectedChar != null && !_manualPick)
                    _targetChar = selectedChar;
                else if (_targetChar == null || !seen.Contains(_targetChar.GUID))
                {
                    _targetChar = selectedChar ?? (_candidates.Count > 0 ? _candidates[0] : null);
                    _manualPick = false;
                }
            }

            CurrentTargetGuid = _targetChar != null ? _targetChar.GUID : 0;
            if (prev != _targetChar)
            {
                RefreshVanillaSlidersCache(true);
                RefreshAllBonesList();
                if (_targetChar != null)
                    FaceLabHistory.EnsureInitialSnapshot(_targetChar, _vanillaSliders);
            }
        }

        private static string GetCharacterDisplayName(AssetCharacter c)
        {
            if (c == null) return FaceLabI18n.T("no_char");
            try
            {
                AssetCharacterData d = c.Data;
                if (d != null)
                {
                    string fn = d.FirstName ?? "";
                    string ln = d.LastName ?? "";
                    string full = (fn + " " + ln).Trim();
                    if (!string.IsNullOrEmpty(full)) return full;
                }
            }
            catch { }
            return "Para #" + (c.GUID % 10000);
        }

        private static int KeyCodeToVk(KeyCode kc)
        {
            if (kc >= KeyCode.F1 && kc <= KeyCode.F12) return 0x70 + (kc - KeyCode.F1);
            if (kc >= KeyCode.A && kc <= KeyCode.Z) return 0x41 + (kc - KeyCode.A);
            if (kc >= KeyCode.Alpha0 && kc <= KeyCode.Alpha9) return 0x30 + (kc - KeyCode.Alpha0);
            if (kc == KeyCode.Escape) return 0x1B;
            if (kc == KeyCode.Insert) return 0x2D;
            if (kc == KeyCode.Home) return 0x24;
            if (kc == KeyCode.End) return 0x23;
            return 0;
        }
    }
}
