// FaceLabUi.cs —— ParaFaceLab v1.0.0 五国语言正式版面板（简中/繁中/EN/日本語/한국어 + 4套圆角皮肤 + 3档字号 + 50步撤回）
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;

namespace ParaFaceLab
{
    public partial class FaceLabPlugin
    {
        private static readonly float[] SCALE_TIERS = new float[] { 1.00f, 1.16f, 1.32f };
        private static readonly string[] SCALE_NAMES = new string[] { "100%", "115%", "130%" };

        private bool _uiReady = false;
        private int _themeIdx = 0;   // 0=奶油, 1=淡雾, 2=暗房, 3=樱雾
        private int _scaleIdx = 1;   // 默认 115% 舒适大字
        private int _builtThemeIdx = -1;
        private int _builtScaleIdx = -1;

        private float _posX = -1f;
        private float _posY = 64f;
        private float _panelW = 486f;
        private float _panelH = 740f;

        private bool _draggingWin = false;
        private Vector2 _dragWinOffset;

        private int _mainTab = 0;
        private int _boneCatIdx = 0;
        private bool _symmetry = true;
        private int _editSide = 0;
        private int _boneRangeMulIdx = 0;
        private static readonly float[] BONE_RANGE_MULS = new float[] { 1f, 2f, 3f };

        private readonly HashSet<string> _expandedCards = new HashSet<string>
        {
            "EyeMaster.L", "EyebrowMaster.L", "NoseMaster", "LipsMasterTop", "CheekSmile.L", "Head", "Breast.L"
        };

        private string _boneSearch = "";
        private readonly List<string> _allRigBones = new List<string>();
        private Vector2 _boneScroll;

        private int _vanillaCatIdx = 5;
        private int _vanillaRangeIdx = 1;
        private string _vanillaSearch = "";
        private Vector2 _vanillaScroll;
        private readonly List<Setting.DeformationSlider> _vanillaSliders = new List<Setting.DeformationSlider>();
        private ulong _vanillaCachedModelGuid = 0;

        private string _presetNameInput = "";
        private int _presetPartMode = 0;     // 0=全身(脸+身材), 1=仅脸部, 2=仅身材
        private int _presetApplyMode = 0;    // 0=骨骼+原版, 1=仅骨骼, 2=仅原版
        private bool _crossAgeAdapt = true;  // 跨年龄智能等比换算（不动目标年龄）
        private readonly List<string> _presetNames = new List<string>();
        private Vector2 _presetScroll;
        private string _statusMsg = "";
        private float _statusMsgUntil = 0f;
        private bool _confirmRevert = false;
        private float _confirmRevertUntil = 0f;
        private string _confirmOverwriteName = null;
        private float _confirmOverwriteUntil = 0f;
        private bool _hadTextFocus = false;

        private string _activeSliderId = null;
        private string _activeSliderLabel = "";
        private FaceLabSnapshot _dragBeforeSnap = null;
        private bool _dragModified = false;

        private Font _font;
        private FaceLabPalette _pal;
        private Texture2D _tPanel, _tHeader, _tCard, _tSoftBox, _tTabWrap, _tTabOn, _tBtn, _tBtnHover, _tAccent, _tAccentSoft, _tDanger, _tInput, _tTrack, _tTrackFill, _tThumb, _tBorderLine;
        private GUIStyle _stPanel, _stHeader, _stCard, _stSoftBox, _stTabWrap, _stAccentBox, _stAccentSoftBox, _stTrack, _stTrackFill;
        private GUIStyle _sTitle, _sSub, _sLabel, _sSmall, _sVal, _sBtn, _sBtnAccent, _sBtnSoft, _sBtnDanger, _sTab, _sTabOn, _sInput, _sCardTitle, _sBadge;

        private float Sc { get { return SCALE_TIERS[Mathf.Clamp(_scaleIdx, 0, SCALE_TIERS.Length - 1)]; } }
        private static string T(string k) { return FaceLabI18n.T(k); }

        private void ShowToast(string msg)
        {
            _statusMsg = msg;
            _statusMsgUntil = Time.unscaledTime + 3.0f;
        }

        private void PerformUndo()
        {
            if (_targetChar == null) return;
            string lbl = FaceLabHistory.Undo(_targetChar, _vanillaSliders);
            if (lbl != null) ShowToast(T("undo_ok") + (string.IsNullOrEmpty(lbl) ? T("prev_step") : lbl));
            else ShowToast(T("undo_empty"));
        }

        private void PerformRedo()
        {
            if (_targetChar == null) return;
            string lbl = FaceLabHistory.Redo(_targetChar, _vanillaSliders);
            if (lbl != null) ShowToast(T("redo_ok") + (string.IsNullOrEmpty(lbl) ? T("next_step") : lbl));
            else ShowToast(T("redo_empty"));
        }

        private void EnsureUiResources()
        {
            if (_uiReady && _builtThemeIdx == _themeIdx && _builtScaleIdx == _scaleIdx && _tPanel != null)
                return;

            _uiReady = true;
            _builtThemeIdx = _themeIdx;
            _builtScaleIdx = _scaleIdx;
            _pal = FaceLabThemes.All[Mathf.Clamp(_themeIdx, 0, FaceLabThemes.All.Length - 1)];
            float sc = Sc;

            int baseFontPx = Mathf.RoundToInt(14 * sc);
            try
            {
                // 完整中/繁/日/韩/英系统字体回退链，确保五种语言零方块乱码
                _font = Font.CreateDynamicFontFromOSFont(
                    new string[]
                    {
                        "Microsoft YaHei UI",
                        "Microsoft JhengHei UI",
                        "Yu Gothic UI",
                        "Meiryo UI",
                        "Malgun Gothic",
                        "Segoe UI",
                        "Arial"
                    }, baseFontPx);
            }
            catch { _font = GUI.skin != null ? GUI.skin.font : null; }

            _tPanel = FaceLabThemes.MakeRoundedTex(48, 14, _pal.Bg, _pal.Border, 1.4f);
            _tHeader = FaceLabThemes.MakeRoundedTex(48, 14, _pal.HeaderBg, Color.clear, 0f);
            _tCard = FaceLabThemes.MakeRoundedTex(40, 10, _pal.Card, _pal.Border, 1.2f);
            _tSoftBox = FaceLabThemes.MakeRoundedTex(40, 10, _pal.SoftBox, _pal.Border, 1.1f);
            _tTabWrap = FaceLabThemes.MakeRoundedTex(36, 9, _pal.TabWrap, Color.clear, 0f);
            _tTabOn = FaceLabThemes.MakeRoundedTex(32, 8, _pal.Card, _pal.Border, 1.0f);
            _tBtn = FaceLabThemes.MakeRoundedTex(32, 8, _pal.Btn, Color.clear, 0f);
            _tBtnHover = FaceLabThemes.MakeRoundedTex(32, 8, _pal.BtnHover, Color.clear, 0f);
            _tAccent = FaceLabThemes.MakeRoundedTex(32, 8, _pal.Accent, Color.clear, 0f);
            _tAccentSoft = FaceLabThemes.MakeRoundedTex(32, 8, _pal.AccentSoft, Color.clear, 0f);
            _tDanger = FaceLabThemes.MakeRoundedTex(32, 8, _pal.Danger, Color.clear, 0f);
            _tInput = FaceLabThemes.MakeRoundedTex(32, 8, _pal.InputBg, _pal.Border, 1.2f);
            _tTrack = FaceLabThemes.MakeRoundedTex(16, 3, _pal.Track, Color.clear, 0f);
            _tTrackFill = FaceLabThemes.MakeRoundedTex(16, 3, _pal.Accent, Color.clear, 0f);
            _tThumb = FaceLabThemes.MakeThumbTex(Mathf.RoundToInt(28 * sc), _pal.Accent, _pal.ThumbRing);
            _tBorderLine = MakeSolid(_pal.Border);

            _stPanel = MakeSliceStyle(_tPanel, 14);
            _stHeader = MakeSliceStyle(_tHeader, 14);
            _stCard = MakeSliceStyle(_tCard, 10);
            _stSoftBox = MakeSliceStyle(_tSoftBox, 10);
            _stTabWrap = MakeSliceStyle(_tTabWrap, 9);
            _stAccentBox = MakeSliceStyle(_tAccent, 8);
            _stAccentSoftBox = MakeSliceStyle(_tAccentSoft, 8);
            _stTrack = MakeSliceStyle(_tTrack, 3);
            _stTrackFill = MakeSliceStyle(_tTrackFill, 3);

            _sTitle = MakeStyle(Mathf.RoundToInt(16 * sc), FontStyle.Bold, _pal.Text, TextAnchor.MiddleLeft);
            _sCardTitle = MakeStyle(Mathf.RoundToInt(14 * sc), FontStyle.Bold, _pal.Text, TextAnchor.MiddleLeft);
            _sSub = MakeStyle(Mathf.RoundToInt(11.5f * sc), FontStyle.Normal, _pal.Sub, TextAnchor.MiddleLeft);
            _sLabel = MakeStyle(Mathf.RoundToInt(13.5f * sc), FontStyle.Normal, _pal.Text, TextAnchor.MiddleLeft);
            _sSmall = MakeStyle(Mathf.RoundToInt(13 * sc), FontStyle.Normal, _pal.Text, TextAnchor.MiddleLeft);
            _sVal = MakeStyle(Mathf.RoundToInt(12.5f * sc), FontStyle.Normal, _pal.Sub, TextAnchor.MiddleRight);

            _sBadge = MakeStyle(Mathf.RoundToInt(11f * sc), FontStyle.Bold, _pal.AccentSoftText, TextAnchor.MiddleCenter);
            _sBadge.normal.background = _tAccentSoft;
            _sBadge.border = new RectOffset(8, 8, 8, 8);
            _sBadge.padding = new RectOffset(8, 8, 2, 2);

            _sBtn = MakeStyle(Mathf.RoundToInt(12 * sc), FontStyle.Normal, _pal.BtnText, TextAnchor.MiddleCenter);
            _sBtn.normal.background = _tBtn;
            _sBtn.hover.background = _tBtnHover;
            _sBtn.active.background = _tAccentSoft;
            _sBtn.border = new RectOffset(8, 8, 8, 8);
            _sBtn.padding = new RectOffset(5, 5, 3, 3);

            _sBtnSoft = MakeStyle(Mathf.RoundToInt(12 * sc), FontStyle.Bold, _pal.AccentSoftText, TextAnchor.MiddleCenter);
            _sBtnSoft.normal.background = _tAccentSoft;
            _sBtnSoft.hover.background = _tAccentSoft;
            _sBtnSoft.border = new RectOffset(8, 8, 8, 8);
            _sBtnSoft.padding = new RectOffset(5, 5, 3, 3);

            _sBtnAccent = MakeStyle(Mathf.RoundToInt(12 * sc), FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _sBtnAccent.normal.background = _tAccent;
            _sBtnAccent.hover.background = _tAccent;
            _sBtnAccent.active.background = _tAccent;
            _sBtnAccent.border = new RectOffset(8, 8, 8, 8);
            _sBtnAccent.padding = new RectOffset(5, 5, 3, 3);

            _sBtnDanger = MakeStyle(Mathf.RoundToInt(12 * sc), FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _sBtnDanger.normal.background = _tDanger;
            _sBtnDanger.hover.background = _tDanger;
            _sBtnDanger.border = new RectOffset(8, 8, 8, 8);
            _sBtnDanger.padding = new RectOffset(5, 5, 3, 3);

            _sTab = MakeStyle(Mathf.RoundToInt(12.5f * sc), FontStyle.Normal, _pal.Sub, TextAnchor.MiddleCenter);
            _sTab.normal.background = null;
            _sTab.hover.background = _tBtnHover;
            _sTab.border = new RectOffset(8, 8, 8, 8);

            _sTabOn = MakeStyle(Mathf.RoundToInt(12.5f * sc), FontStyle.Bold, _pal.Text, TextAnchor.MiddleCenter);
            _sTabOn.normal.background = _tTabOn;
            _sTabOn.border = new RectOffset(8, 8, 8, 8);

            _sInput = MakeStyle(Mathf.RoundToInt(13 * sc), FontStyle.Normal, _pal.Text, TextAnchor.MiddleLeft);
            _sInput.normal.background = _tInput;
            _sInput.focused.background = _tInput;
            _sInput.border = new RectOffset(8, 8, 8, 8);
            _sInput.padding = new RectOffset(10, 10, 4, 4);
        }

        private GUIStyle MakeStyle(int size, FontStyle style, Color color, TextAnchor align)
        {
            var s = new GUIStyle();
            if (_font != null) s.font = _font;
            s.fontSize = size;
            s.fontStyle = style;
            s.alignment = align;
            s.wordWrap = false;
            s.clipping = TextClipping.Clip;
            s.normal.textColor = color;
            s.hover.textColor = color;
            s.active.textColor = color;
            s.focused.textColor = color;
            return s;
        }

        private static Texture2D MakeSolid(Color c)
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            t.hideFlags = HideFlags.HideAndDontSave;
            t.SetPixels(new Color[] { c, c, c, c });
            t.Apply();
            return t;
        }

        private static GUIStyle MakeSliceStyle(Texture2D tex, int b)
        {
            var st = new GUIStyle();
            st.normal.background = tex;
            st.border = new RectOffset(b, b, b, b);
            st.margin = new RectOffset(0, 0, 0, 0);
            st.padding = new RectOffset(0, 0, 0, 0);
            return st;
        }

        private void DrawSliced(Rect r, GUIStyle st)
        {
            if (st == null || r.width <= 0.5f || r.height <= 0.5f) return;
            GUI.Box(r, GUIContent.none, st);
        }

        private void OnGUI()
        {
            Event e = Event.current;
            if (e != null) FaceLabInputGuard.NoteGui(e.mousePosition);

            if (!_open || !OfficialModEnabled) return;

            EnsureUiResources();
            float sc = Sc;

            _panelW = Mathf.Min(Mathf.Round(438f * sc), Screen.width - 24f);
            _panelH = Mathf.Clamp(Screen.height - 96f, 540f, Mathf.Round(860f * sc));
            if (_posX < 0f) _posX = Mathf.Max(16f, Screen.width - _panelW - 22f);
            _posX = Mathf.Clamp(_posX, 8f, Mathf.Max(8f, Screen.width - _panelW - 8f));
            _posY = Mathf.Clamp(_posY, 8f, Mathf.Max(8f, Screen.height - _panelH - 8f));

            Rect winRect = new Rect(_posX, _posY, _panelW, _panelH);
            ScreenRect = winRect;

            if (e != null)
            {
                if (e.type == EventType.MouseDown && winRect.Contains(e.mousePosition))
                    FaceLabInputGuard.uiDrag = true;
                else if (e.type == EventType.MouseUp)
                {
                    FaceLabInputGuard.uiDrag = false;
                    FinishActiveSliderDrag();
                    FaceLabRuntime.EndCompareOriginal();
                }
            }

            GUI.depth = -120;

            GUI.color = new Color(0f, 0f, 0f, 0.20f);
            DrawSliced(new Rect(winRect.x - 3f, winRect.y + 4f, winRect.width + 6f, winRect.height + 6f), _stPanel);
            GUI.color = Color.white;
            DrawSliced(winRect, _stPanel);

            float hdrH = Mathf.Round(44f * sc);
            HandleWindowDrag(new Rect(winRect.x, winRect.y, winRect.width - Mathf.Round(205f * sc), hdrH));

            Color prevCursor = GUI.skin.settings.cursorColor;
            Color prevSel = GUI.skin.settings.selectionColor;
            GUI.skin.settings.cursorColor = _pal.Text;
            GUI.skin.settings.selectionColor = new Color(_pal.Accent.r, _pal.Accent.g, _pal.Accent.b, 0.32f);

            GUILayout.BeginArea(winRect);
            DrawHeader(hdrH, sc);
            DrawCharacterAndUndoBar(sc);
            DrawQuickActions(sc);
            DrawMainTabs(sc);

            if (_targetChar == null)
            {
                GUILayout.Space(28f);
                GUILayout.Label("   " + T("prompt_pick"), _sSub);
                GUILayout.Space(10f);
                GUILayout.BeginHorizontal();
                GUILayout.Space(14f);
                if (GUILayout.Button(T("rescan"), _sBtnAccent, GUILayout.Height(34f * sc)))
                {
                    _manualPick = false;
                    RefreshTargetCharacter();
                }
                GUILayout.Space(14f);
                GUILayout.EndHorizontal();
            }
            else
            {
                if (_mainTab == 0) DrawBonesTab(sc);
                else if (_mainTab == 1) DrawVanillaTab(sc);
                else DrawPresetsTab(sc);
            }

            DrawFooterToast(sc);
            GUILayout.EndArea();

            GUI.skin.settings.cursorColor = prevCursor;
            GUI.skin.settings.selectionColor = prevSel;

            string focused = GUI.GetNameOfFocusedControl();
            bool typing = !string.IsNullOrEmpty(focused) && focused.StartsWith("FL_INPUT_");
            if (typing != _hadTextFocus)
            {
                _hadTextFocus = typing;
                FaceLabInputGuard.SetGameKeyboard(!typing);
            }

            if (e != null && winRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
                e.Use();
        }

        private void FinishActiveSliderDrag()
        {
            if (_activeSliderId != null)
            {
                if (_dragModified && _dragBeforeSnap != null && _targetChar != null)
                    FaceLabHistory.PushUndo(_targetChar.GUID, _dragBeforeSnap, _activeSliderLabel);
                _activeSliderId = null;
                _activeSliderLabel = "";
                _dragBeforeSnap = null;
                _dragModified = false;
                GUIUtility.hotControl = 0;
                FaceLabRuntime.TickPendingMeshRebuild(true);
            }
        }

        private void HandleWindowDrag(Rect header)
        {
            Event e = Event.current;
            if (e == null) return;
            if (e.type == EventType.MouseDown && e.button == 0 && header.Contains(e.mousePosition))
            {
                _draggingWin = true;
                _dragWinOffset = e.mousePosition - new Vector2(_posX, _posY);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _draggingWin)
            {
                Vector2 p = e.mousePosition - _dragWinOffset;
                _posX = p.x;
                _posY = p.y;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _draggingWin)
            {
                _draggingWin = false;
            }
        }

        private void DrawHeader(float hdrH, float sc)
        {
            DrawSliced(new Rect(1f, 1f, _panelW - 2f, hdrH), _stHeader);
            GUI.DrawTexture(new Rect(1f, hdrH, _panelW - 2f, 1f), _tBorderLine);

            float btnH = Mathf.Round(26f * sc);
            float btnY = (hdrH - btnH) * 0.5f;
            float closeW = Mathf.Round(26f * sc);
            float scaleW = Mathf.Round(36f * sc);
            float langW = Mathf.Round(36f * sc);
            float themeW = Mathf.Round(56f * sc);
            float rightEdge = _panelW - 10f;

            float xClose = rightEdge - closeW;
            float xScale = xClose - 5f - scaleW;
            float xLang = xScale - 5f - langW;
            float xTheme = xLang - 5f - themeW;

            float dot = Mathf.Round(10f * sc);
            GUI.DrawTexture(new Rect(14f, (hdrH - dot) * 0.5f, dot, dot), _tThumb);
            float titleX = 14f + dot + 8f;
            GUI.Label(new Rect(titleX, 0f, Mathf.Max(80f, xTheme - titleX - 6f), hdrH), "ParaFaceLab  v1.3.0", _sTitle);

            // 皮肤切换
            string themeShort = T("th_" + _themeIdx + "_short");
            if (GUI.Button(new Rect(xTheme, btnY, themeW, btnH), themeShort, _sBtnSoft))
            {
                _themeIdx = (_themeIdx + 1) % FaceLabThemes.All.Length;
                if (_themeCfg != null) _themeCfg.Value = _themeIdx;
                _uiReady = false;
                ShowToast(T("toast_theme") + T("th_" + _themeIdx + "_name"));
            }

            // 五国语言一键切换（简 / 繁 / EN / 日 / 한）
            int lIdx = (int)FaceLabI18n.Current;
            if (GUI.Button(new Rect(xLang, btnY, langW, btnH), FaceLabI18n.LangShort[lIdx], _sBtn))
            {
                int next = (lIdx + 1) % FaceLabI18n.LangCodes.Length;
                FaceLabI18n.Current = (FaceLabLang)next;
                if (_langCfg != null) _langCfg.Value = FaceLabI18n.LangCodes[next];
                ShowToast(T("toast_lang") + FaceLabI18n.LangNames[next]);
            }

            // 缩放档位
            if (GUI.Button(new Rect(xScale, btnY, scaleW, btnH), "Aa", _sBtn))
            {
                _scaleIdx = (_scaleIdx + 1) % SCALE_TIERS.Length;
                if (_scaleCfg != null) _scaleCfg.Value = _scaleIdx;
                _uiReady = false;
                ShowToast(T("toast_scale") + SCALE_NAMES[_scaleIdx]);
            }

            if (GUI.Button(new Rect(xClose, btnY, closeW, btnH), "×", _sBtn))
                TogglePanel();

            GUILayout.Space(hdrH + 8f);
        }

        private void DrawCharacterAndUndoBar(float sc)
        {
            float rowH = Mathf.Round(30f * sc);
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);

            string name = GetCharacterDisplayName(_targetChar);
            FaceProfile prof = _targetChar != null ? FaceLabStorage.TryGetProfile(_targetChar.GUID) : null;
            int activeBones = prof != null ? prof.ActiveBoneCount() : 0;

            if (!_inParamaker && _candidates.Count > 1)
            {
                if (GUILayout.Button("‹", _sBtn, GUILayout.Width(28f * sc), GUILayout.Height(rowH)))
                    CycleCandidate(-1);
                GUILayout.Space(4f);
            }

            GUILayout.Label(name + (activeBones > 0 ? " · " + activeBones + T("mods_cnt") : ""), _sCardTitle, GUILayout.Height(rowH));
            GUILayout.Space(6f);

            float badgeH = Mathf.Round(22f * sc);
            float badgePadTop = Mathf.Max(0f, (rowH - badgeH) * 0.5f);
            GUILayout.BeginVertical(GUILayout.Height(rowH));
            GUILayout.Space(badgePadTop);
            GUILayout.Label(_inParamaker ? T("badge_pam") : T("badge_live"), _sBadge, GUILayout.Height(badgeH));
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (!_inParamaker && _candidates.Count > 1)
            {
                if (GUILayout.Button("›", _sBtn, GUILayout.Width(28f * sc), GUILayout.Height(rowH)))
                    CycleCandidate(1);
                GUILayout.Space(4f);
            }

            int uCnt = _targetChar != null ? FaceLabHistory.UndoCount(_targetChar.GUID) : 0;
            int rCnt = _targetChar != null ? FaceLabHistory.RedoCount(_targetChar.GUID) : 0;

            GUI.enabled = uCnt > 0;
            if (GUILayout.Button(T("undo") + (uCnt > 0 ? "(" + uCnt + ")" : ""), uCnt > 0 ? _sBtnSoft : _sBtn, GUILayout.Width(92f * sc), GUILayout.Height(rowH)))
                PerformUndo();
            GUILayout.Space(4f);
            GUI.enabled = rCnt > 0;
            if (GUILayout.Button("↷", rCnt > 0 ? _sBtnSoft : _sBtn, GUILayout.Width(32f * sc), GUILayout.Height(rowH)))
                PerformRedo();
            GUI.enabled = true;

            GUILayout.Space(14f);
            GUILayout.EndHorizontal();
            GUILayout.Space(7f);
        }

        private void CycleCandidate(int dir)
        {
            if (_inParamaker || _candidates.Count == 0) return;
            int idx = _targetChar != null ? _candidates.IndexOf(_targetChar) : 0;
            if (idx < 0) idx = 0;
            idx = (idx + dir + _candidates.Count) % _candidates.Count;
            _targetChar = _candidates[idx];
            _manualPick = true;
            CurrentTargetGuid = _targetChar.GUID;
            RefreshVanillaSlidersCache(true);
            RefreshAllBonesList();
            FaceLabHistory.EnsureInitialSnapshot(_targetChar, _vanillaSliders);
        }

        private void DrawQuickActions(float sc)
        {
            if (_targetChar == null) return;
            FaceProfile prof = FaceLabStorage.GetOrCreateProfile(_targetChar.GUID, GetCharacterDisplayName(_targetChar));
            float h = Mathf.Round(30f * sc);

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);

            if (GUILayout.Button(prof.enabled ? T("sculpt_on") : T("sculpt_off"), prof.enabled ? _sBtnAccent : _sBtn, GUILayout.Width(94f * sc), GUILayout.Height(h)))
            {
                FaceLabSnapshot before = FaceLabHistory.Capture(_targetChar, _vanillaSliders);
                prof.enabled = !prof.enabled;
                FaceLabHistory.PushUndo(_targetChar.GUID, before, prof.enabled ? T("sculpt_on") : T("sculpt_off"));
                FaceLabStorage.MarkDirty(_targetChar.GUID);
            }

            GUILayout.Space(6f);

            Rect cmpRect = GUILayoutUtility.GetRect(120f, h, GUILayout.ExpandWidth(true));
            Event e = Event.current;
            if (e != null && cmpRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                FaceLabRuntime.BeginCompareOriginal(_targetChar, _vanillaSliders);
            }

            bool holding = FaceLabRuntime.CompareOriginalHeld && FaceLabRuntime.CompareTargetGuid == _targetChar.GUID;
            GUI.Box(cmpRect, holding ? T("comparing") : T("hold_cmp"), holding ? _sBtnAccent : _sBtn);

            GUILayout.Space(6f);

            if (_confirmRevert && Time.unscaledTime > _confirmRevertUntil) _confirmRevert = false;
            if (GUILayout.Button(_confirmRevert ? T("confirm_click") : T("revert_init"), _confirmRevert ? _sBtnDanger : _sBtn, GUILayout.Width(92f * sc), GUILayout.Height(h)))
            {
                if (_confirmRevert)
                {
                    if (FaceLabHistory.RevertToInitial(_targetChar, _vanillaSliders))
                        ShowToast(T("reverted"));
                    _confirmRevert = false;
                }
                else
                {
                    _confirmRevert = true;
                    _confirmRevertUntil = Time.unscaledTime + 3.0f;
                    ShowToast(T("revert_tip"));
                }
            }

            GUILayout.Space(14f);
            GUILayout.EndHorizontal();
            GUILayout.Space(9f);
        }

        private void DrawMainTabs(float sc)
        {
            float wrapH = Mathf.Round(36f * sc);
            Rect r = GUILayoutUtility.GetRect(_panelW - 28f, wrapH);
            r.x = 14f;
            r.width = _panelW - 28f;
            DrawSliced(r, _stTabWrap);

            string[] tabs = new string[] { T("tab_bones"), T("tab_vanilla"), T("tab_presets") };
            float pad = 4f;
            float btnW = (r.width - pad * 4f) / 3f;
            float btnH = r.height - pad * 2f;

            for (int i = 0; i < tabs.Length; i++)
            {
                Rect br = new Rect(r.x + pad + i * (btnW + pad), r.y + pad, btnW, btnH);
                if (GUI.Button(br, tabs[i], _mainTab == i ? _sTabOn : _sTab))
                {
                    _mainTab = i;
                    GUI.FocusControl(null);
                    if (i == 1) RefreshVanillaSlidersCache(false);
                    if (i == 2) RefreshPresetList();
                }
            }
            GUILayout.Space(8f);
        }

        // ==================== TAB 0：骨骼精修 ====================
        private void DrawBonesTab(float sc)
        {
            FaceProfile prof = FaceLabStorage.GetOrCreateProfile(_targetChar.GUID, GetCharacterDisplayName(_targetChar));
            float pillH = Mathf.Round(27f * sc);

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            for (int i = 0; i < 4; i++)
            {
                if (GUILayout.Button(T("bcat_" + i), _boneCatIdx == i ? _sBtnAccent : _sBtn, GUILayout.Height(pillH)))
                    _boneCatIdx = i;
                if (i < 3) GUILayout.Space(4f);
            }
            GUILayout.Space(14f);
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            for (int i = 4; i < BoneCatalog.Categories.Length; i++)
            {
                if (GUILayout.Button(T("bcat_" + i), _boneCatIdx == i ? _sBtnAccent : _sBtn, GUILayout.Height(pillH)))
                {
                    _boneCatIdx = i;
                    if (i == BoneCatalog.Categories.Length - 1) RefreshAllBonesList();
                }
                if (i < BoneCatalog.Categories.Length - 1) GUILayout.Space(4f);
            }
            GUILayout.Space(14f);
            GUILayout.EndHorizontal();

            GUILayout.Space(7f);

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            if (GUILayout.Button(_symmetry ? T("sym_on") : T("sym_off"), _symmetry ? _sBtnSoft : _sBtn, GUILayout.Height(pillH)))
                _symmetry = !_symmetry;
            GUILayout.Space(6f);
            string[] rLbls = new string[] { T("range_1x"), T("range_2x"), T("range_3x") };
            if (GUILayout.Button(rLbls[_boneRangeMulIdx], _boneRangeMulIdx > 0 ? _sBtnAccent : _sBtn, GUILayout.Width(94f * sc), GUILayout.Height(pillH)))
                _boneRangeMulIdx = (_boneRangeMulIdx + 1) % BONE_RANGE_MULS.Length;
            GUILayout.Space(14f);
            GUILayout.EndHorizontal();

            if (!_symmetry && _boneCatIdx < BoneCatalog.Categories.Length - 1)
            {
                GUILayout.Space(5f);
                GUILayout.BeginHorizontal();
                GUILayout.Space(14f);
                GUILayout.Label(T("cur_side"), _sSub, GUILayout.Width(86f * sc), GUILayout.Height(24f * sc));
                if (GUILayout.Button(T("side_l"), _editSide == 0 ? _sBtnAccent : _sBtn, GUILayout.Height(24f * sc))) _editSide = 0;
                GUILayout.Space(5f);
                if (GUILayout.Button(T("side_r"), _editSide == 1 ? _sBtnAccent : _sBtn, GUILayout.Height(24f * sc))) _editSide = 1;
                GUILayout.Space(14f);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8f);
            float mul = BONE_RANGE_MULS[_boneRangeMulIdx];

            if (_boneCatIdx == BoneCatalog.Categories.Length - 1)
            {
                DrawAllRigBonesView(prof, mul, sc);
                return;
            }

            string curCat = BoneCatalog.Categories[_boneCatIdx];
            CharacterVisual curVis = FaceLabRuntime.FindVisualFor(_targetChar);
            Dictionary<string, BoneTransform> rigMap = curVis != null ? curVis.BoneTransformByName : null;
            _boneScroll = GUILayout.BeginScrollView(_boneScroll, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandHeight(true));

            for (int i = 0; i < BoneCatalog.Entries.Length; i++)
            {
                BoneEntryDef def = BoneCatalog.Entries[i];
                if (def.Category != curCat) continue;
                if (rigMap != null && rigMap.Count > 0 && !rigMap.ContainsKey(def.LeftBone)) continue;
                DrawBoneEntryCard(prof, def, mul, sc);
            }

            GUILayout.Space(22f);
            GUILayout.EndScrollView();
        }

        private void DrawBoneEntryCard(FaceProfile prof, BoneEntryDef def, float rangeMul, float sc)
        {
            string key = def.LeftBone;
            string activeBone = (!def.IsPaired || _symmetry || _editSide == 0) ? def.LeftBone : def.RightBone;
            BoneOffset leftOff = prof.TryGetBone(def.LeftBone);
            BoneOffset rightOff = def.IsPaired ? prof.TryGetBone(def.RightBone) : null;
            bool modified = (leftOff != null && !leftOff.IsZero()) || (rightOff != null && !rightOff.IsZero());
            bool expanded = _expandedCards.Contains(key);
            string displayLabel = FaceLabI18n.BoneLabel(def.LeftBone, def.Label);

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.BeginVertical();

            float hdrH = Mathf.Round(34f * sc);
            Rect hdr = GUILayoutUtility.GetRect(_panelW - 44f, hdrH);
            DrawSliced(hdr, modified ? _stAccentSoftBox : _stCard);

            string sideTag = def.IsPaired ? (_symmetry ? T("tag_sym") : (_editSide == 0 ? T("tag_l") : T("tag_r"))) : "";
            string title = (expanded ? "▾  " : "▸  ") + displayLabel + sideTag + (modified ? "  ●" : "");
            if (GUI.Button(new Rect(hdr.x + 10f, hdr.y, hdr.width - 70f * sc, hdr.height), title, _sCardTitle))
            {
                if (expanded) _expandedCards.Remove(key);
                else _expandedCards.Add(key);
            }

            if (modified)
            {
                float rW = Mathf.Round(54f * sc);
                if (GUI.Button(new Rect(hdr.xMax - rW - 6f, hdr.y + 4f, rW, hdrH - 8f), T("reset"), _sBtn))
                {
                    FaceLabSnapshot before = FaceLabHistory.Capture(_targetChar, _vanillaSliders);
                    if (prof.bones.ContainsKey(def.LeftBone)) prof.bones.Remove(def.LeftBone);
                    if (def.IsPaired && prof.bones.ContainsKey(def.RightBone)) prof.bones.Remove(def.RightBone);
                    FaceLabHistory.PushUndo(_targetChar.GUID, before, T("reset") + " " + displayLabel);
                    FaceLabStorage.MarkDirty(_targetChar.GUID);
                }
            }

            if (expanded)
            {
                GUILayout.Space(4f);
                BoneOffset cur = prof.TryGetBone(activeBone);
                float px = cur != null && cur.pos != null ? cur.pos.x : 0f;
                float py = cur != null && cur.pos != null ? cur.pos.y : 0f;
                float pz = cur != null && cur.pos != null ? cur.pos.z : 0f;
                float rx = cur != null && cur.rot != null ? cur.rot.x : 0f;
                float ry = cur != null && cur.rot != null ? cur.rot.y : 0f;
                float rz = cur != null && cur.rot != null ? cur.rot.z : 0f;
                float su = cur != null ? cur.uniformScale : 0f;
                float sx = cur != null && cur.scale != null ? cur.scale.x : 0f;
                float sy = cur != null && cur.scale != null ? cur.scale.y : 0f;
                float sz = cur != null && cur.scale != null ? cur.scale.z : 0f;

                float pMax = def.PosRangeMm * rangeMul;
                float rMax = def.RotRangeDeg * rangeMul;
                float sMax = Mathf.Clamp(def.ScaleRange * rangeMul, 0.2f, 1.5f);

                bool changed = false;
                changed |= DrawComfortSlider("B_" + activeBone + "_px", def.IsPaired ? T("ax_spread") : T("ax_lr"), ref px, -pMax, pMax, 0f, "mm", false, sc);
                changed |= DrawComfortSlider("B_" + activeBone + "_py", T("ax_ud"), ref py, -pMax, pMax, 0f, "mm", false, sc);
                changed |= DrawComfortSlider("B_" + activeBone + "_pz", T("ax_fb"), ref pz, -pMax, pMax, 0f, "mm", false, sc);
                changed |= DrawComfortSlider("B_" + activeBone + "_rz", T("ax_roll"), ref rz, -rMax, rMax, 0f, "°", false, sc);
                changed |= DrawComfortSlider("B_" + activeBone + "_rx", T("ax_pitch"), ref rx, -rMax, rMax, 0f, "°", false, sc);
                changed |= DrawComfortSlider("B_" + activeBone + "_ry", T("ax_yaw"), ref ry, -rMax, rMax, 0f, "°", false, sc);
                changed |= DrawComfortSlider("B_" + activeBone + "_su", T("ax_scale_u"), ref su, -sMax, sMax, 0f, "x", true, sc);
                changed |= DrawComfortSlider("B_" + activeBone + "_sx", T("ax_scale_x"), ref sx, -sMax, sMax, 0f, "x", true, sc);
                changed |= DrawComfortSlider("B_" + activeBone + "_sy", T("ax_scale_y"), ref sy, -sMax, sMax, 0f, "x", true, sc);
                changed |= DrawComfortSlider("B_" + activeBone + "_sz", T("ax_scale_z"), ref sz, -sMax, sMax, 0f, "x", true, sc);

                if (changed)
                {
                    WriteBoneValues(prof, activeBone, px, py, pz, rx, ry, rz, su, sx, sy, sz);
                    if (def.IsPaired && _symmetry)
                    {
                        string other = activeBone == def.LeftBone ? def.RightBone : def.LeftBone;
                        WriteBoneValues(prof, other, px, py, pz, rx, ry, rz, su, sx, sy, sz);
                    }
                    FaceLabStorage.MarkDirty(_targetChar.GUID);
                }
            }

            GUILayout.EndVertical();
            GUILayout.Space(14f);
            GUILayout.EndHorizontal();
            GUILayout.Space(7f);
        }

        private static void WriteBoneValues(FaceProfile prof, string boneName,
            float px, float py, float pz, float rx, float ry, float rz, float su, float sx, float sy, float sz)
        {
            BoneOffset b = prof.GetOrCreateBone(boneName);
            b.pos.x = px; b.pos.y = py; b.pos.z = pz;
            b.rot.x = rx; b.rot.y = ry; b.rot.z = rz;
            b.uniformScale = su;
            b.scale.x = sx; b.scale.y = sy; b.scale.z = sz;
        }

        private void RefreshAllBonesList()
        {
            _allRigBones.Clear();
            CharacterVisual vis = FaceLabRuntime.FindVisualFor(_targetChar);
            if (vis == null || vis.BoneTransformByName == null) return;
            foreach (string bn in vis.BoneTransformByName.Keys)
            {
                if (!string.IsNullOrEmpty(bn)) _allRigBones.Add(bn);
            }
            _allRigBones.Sort(StringComparer.OrdinalIgnoreCase);
        }

        private string DrawInputField(string controlName, string text, string placeholder, float height)
        {
            GUI.SetNextControlName(controlName);
            Rect r = GUILayoutUtility.GetRect(100f, height, GUILayout.ExpandWidth(true));
            bool focused = (GUI.GetNameOfFocusedControl() == controlName);
            if (focused)
            {
                DrawSliced(new Rect(r.x - 1.5f, r.y - 1.5f, r.width + 3f, r.height + 3f), _stAccentBox);
            }
            string next = GUI.TextField(r, text ?? "", _sInput);
            if (string.IsNullOrEmpty(next) && string.IsNullOrEmpty(Input.compositionString))
            {
                GUI.Label(new Rect(r.x + 10f, r.y, r.width - 16f, r.height), placeholder, _sSub);
            }
            return next;
        }

        private void DrawAllRigBonesView(FaceProfile prof, float rangeMul, float sc)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            _boneSearch = DrawInputField("FL_INPUT_BONE_SEARCH", _boneSearch, T("ph_bone_search"), 30f * sc);
            GUILayout.Space(5f);
            if (GUILayout.Button(T("clear"), _sBtn, GUILayout.Width(54f * sc), GUILayout.Height(30f * sc)))
            {
                _boneSearch = "";
                GUI.FocusControl(null);
            }
            GUILayout.Space(14f);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            if (_allRigBones.Count == 0) RefreshAllBonesList();

            _boneScroll = GUILayout.BeginScrollView(_boneScroll, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandHeight(true));
            string kw = (_boneSearch ?? "").Trim();
            int shown = 0;

            for (int i = 0; i < _allRigBones.Count; i++)
            {
                string bn = _allRigBones[i];
                if (!string.IsNullOrEmpty(kw) && bn.IndexOf(kw, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                BoneEntryDef adHoc = new BoneEntryDef
                {
                    Category = "全部",
                    Label = bn,
                    LeftBone = bn,
                    RightBone = null,
                    PosRangeMm = 30f,
                    RotRangeDeg = 35f,
                    ScaleRange = 0.50f
                };
                DrawBoneEntryCard(prof, adHoc, rangeMul, sc);
                shown++;
                if (shown >= 80) break;
            }

            if (shown == 0)
            {
                GUILayout.Space(16f);
                GUILayout.Label("   " + T("no_bones") + " (" + _allRigBones.Count + ")", _sSub);
            }

            GUILayout.EndScrollView();
        }

        // ==================== TAB 1：原版超限 ====================
        private void RefreshVanillaSlidersCache(bool force)
        {
            if (_targetChar == null) { _vanillaSliders.Clear(); return; }
            ulong modelGuid = 0;
            try
            {
                if (_targetChar.Data != null) modelGuid = _targetChar.Data.CurrentCharacterModelGUID;
            }
            catch { }

            if (!force && modelGuid == _vanillaCachedModelGuid && _vanillaSliders.Count > 0) return;
            _vanillaCachedModelGuid = modelGuid;
            _vanillaSliders.Clear();

            try
            {
                CharacterDeformationSliderManager mgr = CharacterDeformationSliderManager.Instance;
                if (mgr == null || mgr.DeformationSliders == null || mgr.DeformationSliders.Sliders == null) return;
                Setting.DeformationSlider[] arr = mgr.DeformationSliders.Sliders;
                for (int i = 0; i < arr.Length; i++)
                {
                    Setting.DeformationSlider s = arr[i];
                    if (s == null) continue;
                    if (modelGuid != 0 && s.CharacterModelGUID != 0 && s.CharacterModelGUID != modelGuid) continue;
                    _vanillaSliders.Add(s);
                }
                _vanillaSliders.Sort((a, b) =>
                {
                    SliderMeta ma = FaceLabTranslator.GetMeta(a.DisplayName);
                    SliderMeta mb = FaceLabTranslator.GetMeta(b.DisplayName);
                    int c = ((int)ma.Cat).CompareTo((int)mb.Cat);
                    if (c != 0) return c;
                    return string.Compare(ma.Zh, mb.Zh, StringComparison.Ordinal);
                });
            }
            catch { }
        }

        private void DrawVanillaTab(float sc)
        {
            FaceProfile prof = FaceLabStorage.GetOrCreateProfile(_targetChar.GUID, GetCharacterDisplayName(_targetChar));
            CharacterDeformationSliderManager mgr = CharacterDeformationSliderManager.Instance;
            float btnH = Mathf.Round(28f * sc);

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            bool unlocked = prof.unlockVanillaLimits || GlobalUnlockLimits;
            if (GUILayout.Button(unlocked ? T("unlock_on") : T("unlock_off"), unlocked ? _sBtnAccent : _sBtn, GUILayout.Height(btnH)))
            {
                FaceLabSnapshot before = FaceLabHistory.Capture(_targetChar, _vanillaSliders);
                bool next = !unlocked;
                if (next)
                {
                    // 无痕烘焙：先把当前角色各拉杆的视觉生效值同步进去，再解除限制，确保人物 0 变形！
                    FaceLabRuntime.BakeCurrentEffectiveSlidersForUnlock(_targetChar, _vanillaSliders);
                    prof.unlockVanillaLimits = true;
                }
                else
                {
                    // 无痕反烘焙：把解锁态视觉值精确反算回原版区间，关开关也不变高不变形！
                    FaceLabRuntime.UnbakeCurrentEffectiveSlidersForLock(_targetChar, _vanillaSliders);
                    prof.unlockVanillaLimits = false;
                    GlobalUnlockLimits = false;
                    if (_globalUnlockCfg != null) _globalUnlockCfg.Value = false;
                }
                FaceLabHistory.PushUndo(_targetChar.GUID, before, next ? T("unlock_on") : T("unlock_off"));
                FaceLabStorage.MarkDirty(_targetChar.GUID);
                FaceLabRuntime.MarkVisualDirty(_targetChar);
                ShowToast(next ? T("toast_unlock_on") : T("toast_unlock_off"));
            }
            GUILayout.Space(6f);
            string[] rNames = new string[] { T("vrange_0"), T("vrange_1"), T("vrange_2") };
            if (GUILayout.Button(rNames[_vanillaRangeIdx], _vanillaRangeIdx > 0 ? _sBtnSoft : _sBtn, GUILayout.Width(134f * sc), GUILayout.Height(btnH)))
            {
                _vanillaRangeIdx = (_vanillaRangeIdx + 1) % 3;
            }
            GUILayout.Space(14f);
            GUILayout.EndHorizontal();

            GUILayout.Space(7f);

            float pillH = Mathf.Round(26f * sc);
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            for (int i = 0; i < 4; i++)
            {
                if (GUILayout.Button(T("vcat_" + i), _vanillaCatIdx == i ? _sBtnAccent : _sBtn, GUILayout.Height(pillH)))
                    _vanillaCatIdx = i;
                if (i < 3) GUILayout.Space(4f);
            }
            GUILayout.Space(14f);
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            for (int i = 4; i < 8; i++)
            {
                if (GUILayout.Button(T("vcat_" + i), _vanillaCatIdx == i ? _sBtnAccent : _sBtn, GUILayout.Height(pillH)))
                    _vanillaCatIdx = i;
                if (i < 7) GUILayout.Space(4f);
            }
            GUILayout.Space(14f);
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            _vanillaSearch = DrawInputField("FL_INPUT_VANILLA_SEARCH", _vanillaSearch, T("ph_vanilla_search"), 29f * sc);
            GUILayout.Space(5f);
            if (GUILayout.Button(T("clear"), _sBtn, GUILayout.Width(54f * sc), GUILayout.Height(29f * sc)))
            {
                _vanillaSearch = "";
                GUI.FocusControl(null);
            }
            GUILayout.Space(14f);
            GUILayout.EndHorizontal();

            GUILayout.Space(7f);

            if (mgr == null || _vanillaSliders.Count == 0)
            {
                GUILayout.Label("   " + T("no_vanilla"), _sSub);
                return;
            }

            float minV = _vanillaRangeIdx == 0 ? 0f : (_vanillaRangeIdx == 1 ? -0.5f : -1.0f);
            float maxV = _vanillaRangeIdx == 0 ? 1f : (_vanillaRangeIdx == 1 ? 1.5f : 2.0f);
            string kw = (_vanillaSearch ?? "").Trim();
            bool hasKw = !string.IsNullOrEmpty(kw);

            _vanillaScroll = GUILayout.BeginScrollView(_vanillaScroll, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandHeight(true));

            if (_vanillaCatIdx == (int)VanillaCat.ChestBack || _vanillaCatIdx == (int)VanillaCat.All)
                DrawSafeChestModeCard(sc);
            if (_vanillaCatIdx == (int)VanillaCat.BodyLimb || _vanillaCatIdx == (int)VanillaCat.All)
                DrawSafeBottomModeCard(sc);

            int shown = 0;
            for (int i = 0; i < _vanillaSliders.Count; i++)
            {
                Setting.DeformationSlider s = _vanillaSliders[i];
                string raw = s.DisplayName ?? ("Slider " + s.GUID);
                SliderMeta meta = FaceLabTranslator.GetMeta(raw);

                if (meta.HiddenByDefault && !hasKw) continue;

                float curVal = s.DefaultValue;
                try { curVal = mgr.GetCharacterSliderValue(_targetChar, s.GUID); } catch { }

                if (_vanillaCatIdx == (int)VanillaCat.Modified)
                {
                    if (Mathf.Abs(curVal - s.DefaultValue) < 0.005f) continue;
                }
                else if (_vanillaCatIdx != (int)VanillaCat.All)
                {
                    if ((int)meta.Cat != _vanillaCatIdx) continue;
                }

                string locName = FaceLabTranslator.GetLocalizedName(raw);
                if (hasKw)
                {
                    bool hitLoc = locName.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0;
                    bool hitZh = meta.Zh.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0;
                    bool hitEn = raw.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!hitLoc && !hitZh && !hitEn) continue;
                }

                float val = curVal;
                GUILayout.BeginHorizontal();
                GUILayout.Space(14f);
                bool changed = DrawComfortSlider("V_" + s.GUID, locName, ref val, minV, maxV, s.DefaultValue, "", false, sc);
                GUILayout.Space(14f);
                GUILayout.EndHorizontal();

                if (changed)
                {
                    bool immediate = (_activeSliderId == null);
                    FaceLabRuntime.SetVanillaSliderSmooth(_targetChar, s.GUID, val, immediate);
                }

                shown++;
                if (shown >= 180) break;
            }

            if (shown == 0)
            {
                GUILayout.Space(16f);
                GUILayout.Label("   " + T("no_vanilla"), _sSub);
            }

            GUILayout.Space(22f);
            GUILayout.EndScrollView();
        }

        private void DrawSafeChestModeCard(float sc)
        {
            bool isMale = FaceLabRuntime.IsMalePecMode(_targetChar);
            float cardH = Mathf.Round(66f * sc);

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            Rect box = GUILayoutUtility.GetRect(_panelW - 44f, cardH);
            DrawSliced(box, _stSoftBox);

            GUI.Label(new Rect(box.x + 10f, box.y + 5f, box.width - 20f, 22f * sc), T("chest_title"), _sSub);

            float btnY = box.y + Mathf.Round(29f * sc);
            float btnH = Mathf.Round(28f * sc);
            float halfW = (box.width - 26f) * 0.5f;

            if (GUI.Button(new Rect(box.x + 10f, btnY, halfW, btnH), T("chest_male"), isMale ? _sBtnAccent : _sBtn))
            {
                FaceLabSnapshot before = FaceLabHistory.Capture(_targetChar, _vanillaSliders);
                FaceLabRuntime.SetChestBaseMode(_targetChar, true);
                FaceLabHistory.PushUndo(_targetChar.GUID, before, T("chest_male"));
                ShowToast(T("toast_chest_m"));
            }
            if (GUI.Button(new Rect(box.x + 16f + halfW, btnY, halfW, btnH), T("chest_female"), !isMale ? _sBtnAccent : _sBtn))
            {
                FaceLabSnapshot before = FaceLabHistory.Capture(_targetChar, _vanillaSliders);
                FaceLabRuntime.SetChestBaseMode(_targetChar, false);
                FaceLabHistory.PushUndo(_targetChar.GUID, before, T("chest_female"));
                ShowToast(T("toast_chest_f"));
            }

            GUILayout.Space(14f);
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);
        }

        private void DrawSafeBottomModeCard(float sc)
        {
            bool isMale = FaceLabRuntime.IsMaleBottomMode(_targetChar);
            float cardH = Mathf.Round(66f * sc);

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            Rect box = GUILayoutUtility.GetRect(_panelW - 44f, cardH);
            DrawSliced(box, _stSoftBox);

            GUI.Label(new Rect(box.x + 10f, box.y + 5f, box.width - 20f, 22f * sc), T("bottom_title"), _sSub);

            float btnY = box.y + Mathf.Round(29f * sc);
            float btnH = Mathf.Round(28f * sc);
            float halfW = (box.width - 26f) * 0.5f;

            if (GUI.Button(new Rect(box.x + 10f, btnY, halfW, btnH), T("bottom_male"), isMale ? _sBtnAccent : _sBtn))
            {
                FaceLabSnapshot before = FaceLabHistory.Capture(_targetChar, _vanillaSliders);
                FaceLabRuntime.SetBottomBaseMode(_targetChar, true);
                FaceLabHistory.PushUndo(_targetChar.GUID, before, T("bottom_male"));
                ShowToast(T("toast_bot_m"));
            }
            if (GUI.Button(new Rect(box.x + 16f + halfW, btnY, halfW, btnH), T("bottom_female"), !isMale ? _sBtnAccent : _sBtn))
            {
                FaceLabSnapshot before = FaceLabHistory.Capture(_targetChar, _vanillaSliders);
                FaceLabRuntime.SetBottomBaseMode(_targetChar, false);
                FaceLabHistory.PushUndo(_targetChar.GUID, before, T("bottom_female"));
                ShowToast(T("toast_bot_f"));
            }

            GUILayout.Space(14f);
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);
        }

        // ==================== TAB 2：预设与存盘 ====================
        private void RefreshPresetList()
        {
            _presetNames.Clear();
            _presetNames.AddRange(FaceLabStorage.ListPresets());
        }

        private void DrawPresetsTab(float sc)
        {
            FaceProfile prof = FaceLabStorage.GetOrCreateProfile(_targetChar.GUID, GetCharacterDisplayName(_targetChar));
            float h = Mathf.Round(30f * sc);

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.BeginVertical();

            GUILayout.Label(T("preset_save_title"), _sCardTitle);
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();
            _presetNameInput = DrawInputField("FL_INPUT_PRESET_NAME", _presetNameInput, T("ph_preset_name"), h);
            GUILayout.Space(6f);
            if (GUILayout.Button(T("preset_save_btn"), _sBtnAccent, GUILayout.Width(84f * sc), GUILayout.Height(h)))
            {
                string nm = (_presetNameInput ?? "").Trim();
                if (string.IsNullOrEmpty(nm)) nm = GetCharacterDisplayName(_targetChar) + "_" + DateTime.Now.ToString("MMdd_HHmm");
                bool existed = false;
                for (int k = 0; k < _presetNames.Count; k++)
                {
                    if (string.Equals(_presetNames[k], nm, StringComparison.OrdinalIgnoreCase)) { existed = true; break; }
                }
                FaceProfile snap = BuildFullSnapshot(_targetChar, prof);
                string err;
                if (FaceLabStorage.SavePreset(nm, snap, out err))
                {
                    _presetNameInput = "";
                    _confirmOverwriteName = null;
                    GUI.FocusControl(null);
                    RefreshPresetList();
                    ShowToast((existed ? T("toast_overwritten") : T("toast_saved")) + nm);
                }
                else
                {
                    ShowToast(err);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label(T("preset_part_scope"), _sSub);
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            string[] parts = new string[] { T("part_all"), T("part_face"), T("part_body") };
            for (int i = 0; i < parts.Length; i++)
            {
                if (GUILayout.Button(parts[i], _presetPartMode == i ? _sBtnAccent : _sBtn, GUILayout.Height(26f * sc)))
                    _presetPartMode = i;
                if (i < parts.Length - 1) GUILayout.Space(5f);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(7f);
            GUILayout.Label(T("preset_scope"), _sSub);
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            string[] modes = new string[] { T("scope_all"), T("scope_bones"), T("scope_vanilla") };
            for (int i = 0; i < modes.Length; i++)
            {
                if (GUILayout.Button(modes[i], _presetApplyMode == i ? _sBtnAccent : _sBtn, GUILayout.Height(26f * sc)))
                    _presetApplyMode = i;
                if (i < modes.Length - 1) GUILayout.Space(5f);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            if (GUILayout.Button(_crossAgeAdapt ? T("cross_age_on") : T("cross_age_off"), _crossAgeAdapt ? _sBtnSoft : _sBtn, GUILayout.Height(25f * sc)))
            {
                _crossAgeAdapt = !_crossAgeAdapt;
            }

            GUILayout.Space(10f);
            GUILayout.Label(T("preset_list_title") + " (" + _presetNames.Count + ")", _sCardTitle);
            GUILayout.Space(5f);

            _presetScroll = GUILayout.BeginScrollView(_presetScroll, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandHeight(true));
            if (_presetNames.Count == 0)
            {
                GUILayout.Label(T("preset_empty"), _sSub);
            }
            else
            {
                if (_confirmOverwriteName != null && Time.unscaledTime > _confirmOverwriteUntil)
                    _confirmOverwriteName = null;

                for (int i = 0; i < _presetNames.Count; i++)
                {
                    string pn = _presetNames[i];
                    bool waitingConfirm = string.Equals(_confirmOverwriteName, pn, StringComparison.Ordinal);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(pn, _sLabel, GUILayout.Height(h), GUILayout.ExpandWidth(true));
                    if (GUILayout.Button(T("apply"), _sBtnAccent, GUILayout.Width(54f * sc), GUILayout.Height(26f * sc)))
                    {
                        _confirmOverwriteName = null;
                        ApplyPresetToCurrent(pn);
                    }
                    GUILayout.Space(4f);
                    if (GUILayout.Button(waitingConfirm ? T("confirm_short") : T("overwrite"), waitingConfirm ? _sBtnDanger : _sBtnSoft, GUILayout.Width(54f * sc), GUILayout.Height(26f * sc)))
                    {
                        if (!waitingConfirm)
                        {
                            _confirmOverwriteName = pn;
                            _confirmOverwriteUntil = Time.unscaledTime + 3.5f;
                            ShowToast(T("overwrite_tip") + pn);
                        }
                        else
                        {
                            _confirmOverwriteName = null;
                            FaceProfile snap = BuildFullSnapshot(_targetChar, prof);
                            string err;
                            if (FaceLabStorage.SavePreset(pn, snap, out err))
                            {
                                RefreshPresetList();
                                ShowToast(T("toast_overwritten") + pn);
                            }
                            else
                            {
                                ShowToast(err);
                            }
                        }
                    }
                    GUILayout.Space(4f);
                    if (GUILayout.Button(T("del"), _sBtn, GUILayout.Width(38f * sc), GUILayout.Height(26f * sc)))
                    {
                        if (waitingConfirm) _confirmOverwriteName = null;
                        FaceLabStorage.DeletePreset(pn);
                        RefreshPresetList();
                        ShowToast(T("toast_deleted") + pn);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(4f);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(T("refresh_thumb"), _sBtnSoft, GUILayout.Height(32f * sc)))
            {
                if (FaceLabRuntime.RequestThumbnailRefresh(_targetChar))
                    ShowToast(T("toast_thumb_ok"));
                else
                    ShowToast(T("toast_thumb_fail"));
            }
            GUILayout.Space(6f);
            if (GUILayout.Button(T("open_dir"), _sBtn, GUILayout.Height(32f * sc)))
            {
                try { Process.Start("explorer.exe", FaceLabStorage.RootDir); } catch { }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
            GUILayout.Label(T("shift_tip"), _sSub);
            GUILayout.Label(T("safety_note"), _sSub);

            GUILayout.EndVertical();
            GUILayout.Space(14f);
            GUILayout.EndHorizontal();
        }

        private FaceProfile BuildFullSnapshot(AssetCharacter ch, FaceProfile prof)
        {
            FaceProfile snap = prof.Clone();
            snap.vanillaSliders.Clear();
            try
            {
                CharacterDeformationSliderManager mgr = CharacterDeformationSliderManager.Instance;
                if (mgr != null && _vanillaSliders.Count > 0)
                {
                    for (int i = 0; i < _vanillaSliders.Count; i++)
                    {
                        Setting.DeformationSlider s = _vanillaSliders[i];
                        if (s == null || FaceLabTranslator.IsSystemSlider(s.DisplayName, s.GUID)) continue;
                        float v = mgr.GetCharacterSliderValue(ch, s.GUID);
                        if (Mathf.Abs(v - s.DefaultValue) > 0.001f)
                            snap.vanillaSliders[s.GUID.ToString(CultureInfo.InvariantCulture)] = v;
                    }
                    FaceLabRuntime.CaptureEffectiveSliders(ch, _vanillaSliders, snap);
                }
            }
            catch { }
            return snap;
        }

        private void ApplyPresetToCurrent(string presetName)
        {
            if (_targetChar == null) return;
            string err;
            FaceProfile preset = FaceLabStorage.LoadPreset(presetName, out err);
            if (preset == null) { ShowToast(err); return; }

            FaceLabSnapshot before = FaceLabHistory.Capture(_targetChar, _vanillaSliders);
            FaceProfile dst = FaceLabStorage.GetOrCreateProfile(_targetChar.GUID, GetCharacterDisplayName(_targetChar));

            // 1) 骨骼精修按部位筛选套用（仅脸部不覆盖身材骨骼；仅身材不覆盖脸部骨骼）
            if (_presetApplyMode == 0 || _presetApplyMode == 1)
            {
                if (_presetPartMode == 0)
                {
                    dst.bones.Clear();
                }
                else
                {
                    var toDel = new List<string>();
                    foreach (var kv in dst.bones)
                    {
                        bool isBody = FaceLabRuntime.IsBodyBone(kv.Key);
                        if ((_presetPartMode == 1 && !isBody) || (_presetPartMode == 2 && isBody))
                            toDel.Add(kv.Key);
                    }
                    for (int i = 0; i < toDel.Count; i++) dst.bones.Remove(toDel[i]);
                }

                foreach (var kv in preset.bones)
                {
                    if (kv.Value == null || kv.Value.IsZero()) continue;
                    bool isBody = FaceLabRuntime.IsBodyBone(kv.Key);
                    if (_presetPartMode == 1 && isBody) continue;
                    if (_presetPartMode == 2 && !isBody) continue;
                    dst.bones[kv.Key] = kv.Value.Clone();
                }
                dst.enabled = true;
                FaceLabStorage.MarkDirty(_targetChar.GUID);
            }

            // 2) 原版拉杆：不动目标年龄，支持全身/仅脸/仅身材 + 跨年龄自动等比换算
            bool didCrossAge = false;
            if ((_presetApplyMode == 0 || _presetApplyMode == 2) && preset.vanillaSliders != null && preset.vanillaSliders.Count > 0)
            {
                if (preset.unlockVanillaLimits) dst.unlockVanillaLimits = true;
                didCrossAge = FaceLabRuntime.ApplyPresetVanillaCrossAge(_targetChar, preset, _vanillaSliders, _presetPartMode, _crossAgeAdapt);
            }

            FaceLabHistory.PushUndo(_targetChar.GUID, before, presetName);
            ShowToast((didCrossAge ? T("toast_applied_age") : T("toast_applied")) + presetName);
        }

        // ==================== 1:1 丝滑直拖圆角滑块 ====================
        private bool DrawComfortSlider(string sliderId, string label, ref float value, float min, float max, float defaultVal, string unit, bool showAsMultiplier, float sc)
        {
            float rowH = Mathf.Round(42f * sc);
            Rect box = GUILayoutUtility.GetRect(_panelW - 46f, rowH);

            float topH = Mathf.Round(19f * sc);
            float rstW = Mathf.Round(24f * sc);
            float valW = Mathf.Round(60f * sc);

            GUI.Label(new Rect(box.x, box.y, box.width - valW - rstW - 10f, topH), label, _sSmall);

            string valText;
            if (showAsMultiplier)
                valText = (1f + value).ToString("0.00", CultureInfo.InvariantCulture) + "x";
            else if (unit == "mm" || unit == "°")
                valText = value.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + unit;
            else
                valText = value.ToString("0.00", CultureInfo.InvariantCulture);

            GUI.Label(new Rect(box.xMax - rstW - valW - 6f, box.y, valW, topH), valText, _sVal);

            bool changed = false;
            bool isDefault = Mathf.Abs(value - defaultVal) < 0.0005f;
            if (!isDefault)
            {
                if (GUI.Button(new Rect(box.xMax - rstW, box.y, rstW, topH), "↺", _sBtn))
                {
                    FaceLabSnapshot before = FaceLabHistory.Capture(_targetChar, _vanillaSliders);
                    value = defaultVal;
                    FaceLabHistory.PushUndo(_targetChar.GUID, before, label);
                    return true;
                }
            }

            float trackH = Mathf.Round(7f * sc);
            float trackY = box.y + topH + Mathf.Round(8f * sc);
            Rect trackRect = new Rect(box.x + 2f, trackY, box.width - 4f, trackH);
            DrawSliced(trackRect, _stTrack);

            float span = Mathf.Max(0.0001f, max - min);
            float defT = Mathf.Clamp01((defaultVal - min) / span);
            float defX = trackRect.x + defT * trackRect.width;
            float curT = Mathf.Clamp01((value - min) / span);
            float curX = trackRect.x + curT * trackRect.width;

            float fillLeft = Mathf.Min(defX, curX);
            float fillW = Mathf.Abs(curX - defX);
            if (fillW > 2f)
                DrawSliced(new Rect(fillLeft, trackRect.y, fillW, trackRect.height), _stTrackFill);

            GUI.DrawTexture(new Rect(defX - 0.75f, trackRect.y - 3f, 1.5f, trackRect.height + 6f), _tBorderLine);

            Rect hitRect = new Rect(trackRect.x - 8f, trackRect.y - 10f * sc, trackRect.width + 16f, trackH + 20f * sc);
            int cid = GUIUtility.GetControlID(FocusType.Passive);
            Event e = Event.current;
            bool isDraggingThis = (_activeSliderId == sliderId);
            bool hovered = e != null && hitRect.Contains(e.mousePosition);

            if (e != null)
            {
                if (e.type == EventType.MouseDown && e.button == 0 && hitRect.Contains(e.mousePosition))
                {
                    if (e.clickCount >= 2)
                    {
                        FaceLabSnapshot before = FaceLabHistory.Capture(_targetChar, _vanillaSliders);
                        value = defaultVal;
                        FaceLabHistory.PushUndo(_targetChar.GUID, before, label);
                        _activeSliderId = null;
                        GUIUtility.hotControl = 0;
                        e.Use();
                        return true;
                    }

                    GUIUtility.hotControl = cid;
                    _activeSliderId = sliderId;
                    _activeSliderLabel = label;
                    _dragBeforeSnap = FaceLabHistory.Capture(_targetChar, _vanillaSliders);
                    _dragModified = false;
                    isDraggingThis = true;

                    float t = Mathf.Clamp01((e.mousePosition.x - trackRect.x) / Mathf.Max(1f, trackRect.width));
                    float nv = Mathf.Lerp(min, max, t);
                    if (Mathf.Abs(nv - value) > 0.0001f)
                    {
                        value = nv;
                        changed = true;
                        _dragModified = true;
                    }
                    e.Use();
                }
                else if (isDraggingThis && e.type == EventType.MouseDrag && e.button == 0)
                {
                    float nv;
                    if (e.shift)
                    {
                        float step = (e.delta.x / Mathf.Max(1f, trackRect.width)) * (max - min) * 0.30f;
                        nv = Mathf.Clamp(value + step, min, max);
                    }
                    else
                    {
                        float t = Mathf.Clamp01((e.mousePosition.x - trackRect.x) / Mathf.Max(1f, trackRect.width));
                        nv = Mathf.Lerp(min, max, t);
                    }
                    if (Mathf.Abs(nv - value) > 0.00005f)
                    {
                        value = nv;
                        changed = true;
                        _dragModified = true;
                    }
                    e.Use();
                }
                else if (isDraggingThis && e.type == EventType.MouseUp)
                {
                    if (GUIUtility.hotControl == cid) GUIUtility.hotControl = 0;
                    FinishActiveSliderDrag();
                    e.Use();
                }
            }

            curT = Mathf.Clamp01((value - min) / span);
            curX = trackRect.x + curT * trackRect.width;

            float thumbSz = Mathf.Round((isDraggingThis ? 21f : (hovered ? 19.5f : 18f)) * sc);
            GUI.DrawTexture(new Rect(curX - thumbSz * 0.5f, trackRect.y + trackH * 0.5f - thumbSz * 0.5f, thumbSz, thumbSz), _tThumb);

            return changed;
        }

        private void DrawFooterToast(float sc)
        {
            if (string.IsNullOrEmpty(_statusMsg) || Time.unscaledTime > _statusMsgUntil) return;
            float h = Mathf.Round(30f * sc);
            Rect r = new Rect(16f, _panelH - h - 10f, _panelW - 32f, h);
            DrawSliced(r, _stAccentBox);
            GUI.Label(r, _statusMsg, _sBtnAccent);
        }
    }
}
