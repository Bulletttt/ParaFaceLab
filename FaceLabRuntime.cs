// FaceLabRuntime.cs —— v0.5 防累乘自恢复骨骼引擎 + 真正突破 0~1 上下限外推 + 零抖动写入 + 安全胸型切换
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ParaFaceLab
{
    public static class FaceLabRuntime
    {
        public const ulong GUID_SLIDER_PEC = 2067835496678116228UL;
        public const ulong GUID_SLIDER_BREAST = 8681488120197377277UL;
        public const ulong GUID_SLIDER_BOTTOM_MASC = 6022626022936383398UL;
        public const ulong GUID_SLIDER_BOTTOM_FEM = 7607763405866787395UL;
        public const ulong GUID_SLIDER_AGE = 7771415417556004105UL;

        public static bool SuppressRangeLimitBypass = false;
        public static bool CompareOriginalHeld = false;
        public static ulong CompareTargetGuid = 0;

        private static FaceLabSnapshot _compareSavedCurrent = null;
        private static AssetCharacter _compareChar = null;

        private static readonly HashSet<int> AppliedVisualIds = new HashSet<int>();
        private static float _nextMeshRebuildAt = 0f;
        private static AssetCharacter _pendingRebuildChar = null;

        // 每个 Transform 的上一帧基准态与写入态记录：
        // 彻底根治「Animator 不刷新某根骨骼的 localScale/Position/Rotation 时，每帧叠加导致无限膨胀成肉墙或缩成 0 消失」的致命 Bug！
        private sealed class BoneStateRec
        {
            public Transform Tr;
            public bool PosActive;
            public Vector3 BasePos;
            public Vector3 FinalPos;

            public bool RotActive;
            public Quaternion BaseRot;
            public Quaternion FinalRot;

            public bool ScaleActive;
            public Vector3 BaseScale;
            public Vector3 FinalScale;
        }

        // key = CharacterVisual.GetInstanceID() -> (boneName -> BoneStateRec)
        private static readonly Dictionary<int, Dictionary<string, BoneStateRec>> VisualBoneStates
            = new Dictionary<int, Dictionary<string, BoneStateRec>>();

        public static bool IsLimitUnlockedFor(AssetCharacter ch)
        {
            if (SuppressRangeLimitBypass) return false;
            if (!FaceLabPlugin.OfficialModEnabled) return false;
            if (FaceLabPlugin.GlobalUnlockLimits) return true;
            if (ch == null) return false;
            FaceProfile p = FaceLabStorage.TryGetProfile(ch.GUID);
            return p != null && p.unlockVanillaLimits;
        }

        public static void BeginCompareOriginal(AssetCharacter ch, List<Setting.DeformationSlider> allSliders)
        {
            if (ch == null || CompareOriginalHeld) return;
            CompareOriginalHeld = true;
            CompareTargetGuid = ch.GUID;
            _compareChar = ch;

            FaceLabSnapshot initSnap = FaceLabHistory.GetInitialSnapshot(ch.GUID);
            if (initSnap != null && initSnap.Vanilla != null && initSnap.Vanilla.Count > 0)
            {
                _compareSavedCurrent = FaceLabHistory.Capture(ch, allSliders, "compare_tmp");
                ApplyVanillaBatch(ch, initSnap.Vanilla);
            }
            else
            {
                _compareSavedCurrent = null;
            }
        }

        public static void EndCompareOriginal()
        {
            if (!CompareOriginalHeld) return;
            CompareOriginalHeld = false;
            CompareTargetGuid = 0;

            if (_compareChar != null && _compareSavedCurrent != null && _compareSavedCurrent.Vanilla != null)
            {
                ApplyVanillaBatch(_compareChar, _compareSavedCurrent.Vanilla);
            }
            _compareSavedCurrent = null;
            _compareChar = null;
        }

        public static void TickPendingMeshRebuild(bool forceNow)
        {
            if (_pendingRebuildChar == null) return;
            if (!forceNow && Time.unscaledTime < _nextMeshRebuildAt) return;
            AssetCharacter ch = _pendingRebuildChar;
            _pendingRebuildChar = null;
            MarkVisualDirty(ch);
        }

        public static void MarkVisualDirty(AssetCharacter ch)
        {
            if (ch == null) return;
            try
            {
                AssetCharacterVisual vis = ch.Visual;
                if (vis != null && vis.Data != null)
                {
                    vis.Data.IsMeshCompositionDirty = true;
                    vis.Data.AreBoneDeformationsDirty = true;
                }
            }
            catch { }
        }

        public static void SetVanillaSliderSmooth(AssetCharacter ch, ulong sliderGuid, float value, bool immediateRebuild)
        {
            if (ch == null || sliderGuid == 0) return;
            CharacterDeformationSliderManager mgr = CharacterDeformationSliderManager.Instance;
            if (mgr == null) return;

            try
            {
                SaveDataCharacterSlider s = mgr.GetCharacterSlider(ch, sliderGuid, 0UL);
                if (s == null)
                {
                    mgr.SetCharacterSlider(ch, sliderGuid, Mathf.Clamp01(value), false, false, 0UL);
                    s = mgr.GetCharacterSlider(ch, sliderGuid, 0UL);
                }

                if (s != null) s.SliderValue = value;
                else mgr.SetCharacterSlider(ch, sliderGuid, value, false, false, 0UL);

                if (immediateRebuild)
                {
                    _pendingRebuildChar = null;
                    MarkVisualDirty(ch);
                }
                else
                {
                    _pendingRebuildChar = ch;
                    if (Time.unscaledTime >= _nextMeshRebuildAt)
                    {
                        _nextMeshRebuildAt = Time.unscaledTime + 0.055f;
                        _pendingRebuildChar = null;
                        MarkVisualDirty(ch);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ParaFaceLab] SetVanillaSliderSmooth failed: " + e.Message);
            }
        }

        public static void ApplyVanillaBatch(AssetCharacter ch, Dictionary<ulong, float> values)
        {
            if (ch == null || values == null || values.Count == 0) return;
            CharacterDeformationSliderManager mgr = CharacterDeformationSliderManager.Instance;
            if (mgr == null) return;

            bool anyChanged = false;
            try
            {
                foreach (var kv in values)
                {
                    float cur = mgr.GetCharacterSliderValue(ch, kv.Key);
                    if (Mathf.Abs(cur - kv.Value) < 0.0004f) continue;

                    SaveDataCharacterSlider s = mgr.GetCharacterSlider(ch, kv.Key, 0UL);
                    if (s == null)
                    {
                        mgr.SetCharacterSlider(ch, kv.Key, Mathf.Clamp01(kv.Value), false, false, 0UL);
                        s = mgr.GetCharacterSlider(ch, kv.Key, 0UL);
                    }
                    if (s != null) s.SliderValue = kv.Value;
                    anyChanged = true;
                }
                if (anyChanged) MarkVisualDirty(ch);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ParaFaceLab] ApplyVanillaBatch failed: " + e.Message);
            }
        }

        private static void WriteSliderDirect(CharacterDeformationSliderManager mgr, AssetCharacter ch, ulong guid, float val)
        {
            if (mgr == null || ch == null || guid == 0) return;
            SaveDataCharacterSlider s = mgr.GetCharacterSlider(ch, guid, 0UL);
            if (s == null)
            {
                mgr.SetCharacterSlider(ch, guid, Mathf.Clamp01(val), false, false, 0UL);
                s = mgr.GetCharacterSlider(ch, guid, 0UL);
            }
            if (s != null) s.SliderValue = val;
        }

        public static bool IsBodyVanillaSlider(Setting.DeformationSlider s)
        {
            if (s == null) return false;
            string dn = s.DisplayName ?? "";
            if (dn.Equals("HeadSize", StringComparison.OrdinalIgnoreCase)
                || dn.Equals("NeckSize", StringComparison.OrdinalIgnoreCase)
                || dn.Equals("AdamsApple", StringComparison.OrdinalIgnoreCase))
                return true;
            SliderMeta m = FaceLabTranslator.GetMeta(dn);
            return m != null && (m.Cat == VanillaCat.ChestBack || m.Cat == VanillaCat.BodyLimb);
        }

        private static bool IsMasterBodySlider(string dn)
        {
            if (string.IsNullOrEmpty(dn)) return false;
            return dn.Equals("Height", StringComparison.OrdinalIgnoreCase)
                || dn.Equals("FatMasterSlider", StringComparison.OrdinalIgnoreCase)
                || dn.Equals("MuscleMasterSlider", StringComparison.OrdinalIgnoreCase)
                || dn.Equals("OverallBodyWeight", StringComparison.OrdinalIgnoreCase)
                || dn.Equals("OverallBodyMuscle", StringComparison.OrdinalIgnoreCase)
                || dn.Equals("OverallFat", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBaseSwitchGuid(ulong g)
        {
            return g == GUID_SLIDER_PEC || g == GUID_SLIDER_BREAST
                || g == GUID_SLIDER_BOTTOM_MASC || g == GUID_SLIDER_BOTTOM_FEM;
        }

        // 保存预设时，记录源角色年龄与每条拉杆经游戏 RangeLimiter 映射后的“真实视觉生效值”
        public static void CaptureEffectiveSliders(AssetCharacter ch, List<Setting.DeformationSlider> allSliders, FaceProfile snap)
        {
            if (ch == null || snap == null || allSliders == null) return;
            if (snap.effectiveVanillaSliders == null) snap.effectiveVanillaSliders = new Dictionary<string, float>();
            snap.effectiveVanillaSliders.Clear();

            CharacterDeformationSliderManager mgr = CharacterDeformationSliderManager.Instance;
            if (mgr == null) return;

            bool prevSuppress = SuppressRangeLimitBypass;
            try
            {
                SuppressRangeLimitBypass = true;
                snap.sourceAge = mgr.GetCharacterSliderValue(ch, GUID_SLIDER_AGE);
                for (int i = 0; i < allSliders.Count; i++)
                {
                    Setting.DeformationSlider s = allSliders[i];
                    if (s == null || FaceLabTranslator.IsSystemSlider(s.DisplayName, s.GUID)) continue;
                    float raw = mgr.GetCharacterSliderValue(ch, s.GUID);
                    float eff = mgr.RemapDeformationSliderValuesWithRangeLimiting(s, raw, ch);
                    if (Mathf.Abs(raw - s.DefaultValue) > 0.001f || Mathf.Abs(eff - s.DefaultValue) > 0.001f)
                    {
                        snap.effectiveVanillaSliders[s.GUID.ToString(System.Globalization.CultureInfo.InvariantCulture)] = eff;
                    }
                }
            }
            catch { }
            finally
            {
                SuppressRangeLimitBypass = prevSuppress;
            }
        }

        private static float InvertEffectiveForTarget(
            CharacterDeformationSliderManager mgr,
            AssetCharacter targetChar,
            Setting.DeformationSlider s,
            float rawVal,
            Dictionary<ulong, float> effMap,
            bool useCrossAge)
        {
            if (!useCrossAge || effMap == null || s == null) return rawVal;
            float targetEff;
            if (!effMap.TryGetValue(s.GUID, out targetEff))
                targetEff = s.DefaultValue;

            // 用 0 和 1 探测目标角色当前年龄下的真实线性范围 [dstMin, dstMax]
            float dstMin = mgr.RemapDeformationSliderValuesWithRangeLimiting(s, 0f, targetChar);
            float dstMax = mgr.RemapDeformationSliderValuesWithRangeLimiting(s, 1f, targetChar);
            float span = dstMax - dstMin;

            // 若该拉杆没有范围限制（0->0, 1->1），直接使用原始值（保留超限设定）
            if (Mathf.Abs(dstMin) < 0.0005f && Mathf.Abs(dstMax - 1f) < 0.0005f)
            {
                // 如果原预设被源年龄限制了，targetEff 与 rawVal 会不同，用 targetEff 还原视觉比例！
                return targetEff;
            }

            if (Mathf.Abs(span) < 0.0005f) return rawVal;

            float inv = (targetEff - dstMin) / span;
            float lo = Mathf.Min(dstMin, dstMax);
            float hi = Mathf.Max(dstMin, dstMax);
            if (rawVal >= -0.001f && rawVal <= 1.001f && targetEff >= lo - 0.002f && targetEff <= hi + 0.002f)
                return Mathf.Clamp01(inv);

            return Mathf.Clamp(inv, -1.0f, 2.0f);
        }

        // 无痕烘焙：开启「解除原版限制」前，先把每条普通拉杆的真实生效值写入 SliderValue，
        // 让人物在解锁瞬间 0.000% 变形（原地纹丝不动），之后可立刻自由拉动原本被锁死的背肌/背脂/柔肩等拉杆！
        public static void BakeCurrentEffectiveSlidersForUnlock(AssetCharacter ch, List<Setting.DeformationSlider> allSliders)
        {
            if (ch == null || allSliders == null || allSliders.Count == 0) return;
            CharacterDeformationSliderManager mgr = CharacterDeformationSliderManager.Instance;
            if (mgr == null) return;

            var baked = new Dictionary<ulong, float>();
            bool prevSuppress = SuppressRangeLimitBypass;
            try
            {
                SuppressRangeLimitBypass = true;
                for (int i = 0; i < allSliders.Count; i++)
                {
                    Setting.DeformationSlider s = allSliders[i];
                    if (s == null) continue;
                    if (FaceLabTranslator.IsSystemSlider(s.DisplayName, s.GUID)) continue;
                    if (IsBaseSwitchGuid(s.GUID)) continue;

                    float raw = mgr.GetCharacterSliderValue(ch, s.GUID);
                    if (Mathf.Approximately(raw, -1f)) continue;
                    float eff = mgr.RemapDeformationSliderValuesWithRangeLimiting(s, raw, ch);
                    if (Mathf.Abs(eff - raw) > 0.0005f)
                        baked[s.GUID] = eff;
                }

                foreach (var kv in baked)
                    WriteSliderDirect(mgr, ch, kv.Key, kv.Value);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ParaFaceLab] BakeCurrentEffectiveSlidersForUnlock failed: " + e.Message);
            }
            finally
            {
                SuppressRangeLimitBypass = prevSuppress;
            }
        }

        // 无痕反烘焙：关闭「解除原版限制」时，按当前角色各拉杆的真实区间把视觉值精确反算回原版原始值，
        // 彻底杜绝「开→关→开→关」时 Height 等带区间映射的拉杆像打气筒一样越按越高的 Bug！
        public static void UnbakeCurrentEffectiveSlidersForLock(AssetCharacter ch, List<Setting.DeformationSlider> allSliders)
        {
            if (ch == null || allSliders == null || allSliders.Count == 0) return;
            CharacterDeformationSliderManager mgr = CharacterDeformationSliderManager.Instance;
            if (mgr == null) return;

            var curEff = new Dictionary<ulong, float>();
            bool prevSuppress = SuppressRangeLimitBypass;
            try
            {
                SuppressRangeLimitBypass = true;

                // 先记录解锁态下的全部当前视觉值
                for (int i = 0; i < allSliders.Count; i++)
                {
                    Setting.DeformationSlider s = allSliders[i];
                    if (s == null || FaceLabTranslator.IsSystemSlider(s.DisplayName, s.GUID) || IsBaseSwitchGuid(s.GUID))
                        continue;
                    float v = mgr.GetCharacterSliderValue(ch, s.GUID);
                    if (!Mathf.Approximately(v, -1f)) curEff[s.GUID] = v;
                }

                // 第一轮：先反算主控拉杆（Height / Fat / Muscle）
                var pass1Done = new HashSet<ulong>();
                for (int i = 0; i < allSliders.Count; i++)
                {
                    Setting.DeformationSlider s = allSliders[i];
                    if (s == null || !IsMasterBodySlider(s.DisplayName)) continue;
                    float eff;
                    if (!curEff.TryGetValue(s.GUID, out eff)) continue;
                    float dMin = mgr.RemapDeformationSliderValuesWithRangeLimiting(s, 0f, ch);
                    float dMax = mgr.RemapDeformationSliderValuesWithRangeLimiting(s, 1f, ch);
                    float span = dMax - dMin;
                    if (Mathf.Abs(span) > 0.0005f && (Mathf.Abs(dMin) > 0.0005f || Mathf.Abs(dMax - 1f) > 0.0005f))
                    {
                        float inv = Mathf.Clamp((eff - dMin) / span, -1.0f, 2.0f);
                        WriteSliderDirect(mgr, ch, s.GUID, inv);
                    }
                    pass1Done.Add(s.GUID);
                }

                // 第二轮：反算其余普通拉杆
                for (int i = 0; i < allSliders.Count; i++)
                {
                    Setting.DeformationSlider s = allSliders[i];
                    if (s == null || pass1Done.Contains(s.GUID)) continue;
                    float eff;
                    if (!curEff.TryGetValue(s.GUID, out eff)) continue;
                    float dMin = mgr.RemapDeformationSliderValuesWithRangeLimiting(s, 0f, ch);
                    float dMax = mgr.RemapDeformationSliderValuesWithRangeLimiting(s, 1f, ch);
                    float span = dMax - dMin;
                    if (Mathf.Abs(span) > 0.0005f && (Mathf.Abs(dMin) > 0.0005f || Mathf.Abs(dMax - 1f) > 0.0005f))
                    {
                        float inv = Mathf.Clamp((eff - dMin) / span, -1.0f, 2.0f);
                        WriteSliderDirect(mgr, ch, s.GUID, inv);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ParaFaceLab] UnbakeCurrentEffectiveSlidersForLock failed: " + e.Message);
            }
            finally
            {
                SuppressRangeLimitBypass = prevSuppress;
            }
        }

        // 跨年龄智能换算套用原版拉杆（不动目标角色年龄，支持全身/仅脸/仅身材）
        // 返回是否触发了跨年龄等比换算
        public static bool ApplyPresetVanillaCrossAge(
            AssetCharacter targetChar,
            FaceProfile preset,
            List<Setting.DeformationSlider> allSliders,
            int partFilter,
            bool crossAgeAdapt)
        {
            if (targetChar == null || preset == null || allSliders == null || allSliders.Count == 0) return false;
            if (preset.vanillaSliders == null || preset.vanillaSliders.Count == 0) return false;

            CharacterDeformationSliderManager mgr = CharacterDeformationSliderManager.Instance;
            if (mgr == null) return false;

            var byGuid = new Dictionary<ulong, Setting.DeformationSlider>();
            for (int i = 0; i < allSliders.Count; i++)
            {
                Setting.DeformationSlider s = allSliders[i];
                if (s != null) byGuid[s.GUID] = s;
            }

            // 1) 解析预设原始值（自动忽略系统拉杆，但提取旧预设里可能保存的源 Age 用于跨年龄换算）
            float legacySrcAge = -1f;
            var presetRaw = new Dictionary<ulong, float>();
            foreach (var kv in preset.vanillaSliders)
            {
                ulong sGuid;
                if (!ulong.TryParse(kv.Key, out sGuid)) continue;
                if (sGuid == GUID_SLIDER_AGE)
                {
                    legacySrcAge = kv.Value;
                    continue;
                }
                Setting.DeformationSlider sDef;
                if (byGuid.TryGetValue(sGuid, out sDef) && FaceLabTranslator.IsSystemSlider(sDef.DisplayName, sGuid))
                    continue;
                presetRaw[sGuid] = kv.Value;
            }

            // 保证胸/下身 0/1 底座成对互斥
            float baseV;
            if (presetRaw.TryGetValue(GUID_SLIDER_PEC, out baseV) && !presetRaw.ContainsKey(GUID_SLIDER_BREAST))
                presetRaw[GUID_SLIDER_BREAST] = baseV >= 0.5f ? 0f : 1f;
            else if (presetRaw.TryGetValue(GUID_SLIDER_BREAST, out baseV) && !presetRaw.ContainsKey(GUID_SLIDER_PEC))
                presetRaw[GUID_SLIDER_PEC] = baseV >= 0.5f ? 0f : 1f;

            if (presetRaw.TryGetValue(GUID_SLIDER_BOTTOM_MASC, out baseV) && !presetRaw.ContainsKey(GUID_SLIDER_BOTTOM_FEM))
                presetRaw[GUID_SLIDER_BOTTOM_FEM] = baseV >= 0.5f ? 0f : 1f;
            else if (presetRaw.TryGetValue(GUID_SLIDER_BOTTOM_FEM, out baseV) && !presetRaw.ContainsKey(GUID_SLIDER_BOTTOM_MASC))
                presetRaw[GUID_SLIDER_BOTTOM_MASC] = baseV >= 0.5f ? 0f : 1f;

            float srcAge = preset.sourceAge >= 0f ? preset.sourceAge : legacySrcAge;
            float targetAge = mgr.GetCharacterSliderValue(targetChar, GUID_SLIDER_AGE);
            bool ageDiffers = crossAgeAdapt && srcAge >= 0f && targetAge >= 0f && Mathf.Abs(srcAge - targetAge) > 0.003f;

            // 2) 构建源视觉生效值表（新预设直接读 effectiveVanillaSliders；旧预设用源 Age 在内存中瞬时探测，绝不改目标年龄）
            Dictionary<ulong, float> effMap = null;
            bool prevSuppress = SuppressRangeLimitBypass;
            try
            {
                SuppressRangeLimitBypass = true;

                if (ageDiffers)
                {
                    effMap = new Dictionary<ulong, float>();
                    if (preset.effectiveVanillaSliders != null && preset.effectiveVanillaSliders.Count > 0)
                    {
                        foreach (var kv in preset.effectiveVanillaSliders)
                        {
                            ulong g;
                            if (ulong.TryParse(kv.Key, out g)) effMap[g] = kv.Value;
                        }
                    }
                    else
                    {
                        // 兼容 v1.0 旧预设（例如已保存的王勉立.json）：临时在内存中以源年龄探测各拉杆的真实生效值，随后立刻完整复原
                        var origSnap = new Dictionary<ulong, float>(allSliders.Count + 1);
                        origSnap[GUID_SLIDER_AGE] = targetAge;
                        for (int i = 0; i < allSliders.Count; i++)
                        {
                            Setting.DeformationSlider s = allSliders[i];
                            if (s != null) origSnap[s.GUID] = mgr.GetCharacterSliderValue(targetChar, s.GUID);
                        }

                        try
                        {
                            WriteSliderDirect(mgr, targetChar, GUID_SLIDER_AGE, srcAge);
                            for (int i = 0; i < allSliders.Count; i++)
                            {
                                Setting.DeformationSlider s = allSliders[i];
                                if (s == null || FaceLabTranslator.IsSystemSlider(s.DisplayName, s.GUID)) continue;
                                float rv;
                                if (!presetRaw.TryGetValue(s.GUID, out rv))
                                {
                                    if (IsBaseSwitchGuid(s.GUID)) continue;
                                    rv = s.DefaultValue;
                                }
                                WriteSliderDirect(mgr, targetChar, s.GUID, rv);
                            }

                            for (int i = 0; i < allSliders.Count; i++)
                            {
                                Setting.DeformationSlider s = allSliders[i];
                                if (s == null || FaceLabTranslator.IsSystemSlider(s.DisplayName, s.GUID)) continue;
                                float rv = mgr.GetCharacterSliderValue(targetChar, s.GUID);
                                effMap[s.GUID] = mgr.RemapDeformationSliderValuesWithRangeLimiting(s, rv, targetChar);
                            }
                        }
                        finally
                        {
                            // 完整复原目标角色的原始 Age 和所有拉杆
                            foreach (var kv in origSnap)
                                WriteSliderDirect(mgr, targetChar, kv.Key, kv.Value);
                        }
                    }
                }

                // 3) 按部位筛选并两轮写入目标角色（目标角色的 Age 全程纹丝不动！）
                // 第一轮：底座开关 + 主控拉杆（Height / Fat / Muscle），让第二轮 RangeLimiter 探测基于最终体型主轴
                var pass1Done = new HashSet<ulong>();
                if (partFilter != 1) // 全身 或 仅身材
                {
                    ulong[] switches = new ulong[] { GUID_SLIDER_PEC, GUID_SLIDER_BREAST, GUID_SLIDER_BOTTOM_MASC, GUID_SLIDER_BOTTOM_FEM };
                    for (int i = 0; i < switches.Length; i++)
                    {
                        float sv;
                        if (presetRaw.TryGetValue(switches[i], out sv))
                        {
                            WriteSliderDirect(mgr, targetChar, switches[i], sv);
                            pass1Done.Add(switches[i]);
                        }
                    }

                    for (int i = 0; i < allSliders.Count; i++)
                    {
                        Setting.DeformationSlider s = allSliders[i];
                        if (s == null || !IsMasterBodySlider(s.DisplayName)) continue;
                        float rv;
                        if (!presetRaw.TryGetValue(s.GUID, out rv)) rv = s.DefaultValue;
                        float fv = InvertEffectiveForTarget(mgr, targetChar, s, rv, effMap, ageDiffers);
                        WriteSliderDirect(mgr, targetChar, s.GUID, fv);
                        pass1Done.Add(s.GUID);
                    }
                }

                // 第二轮：其余所有匹配部位的普通拉杆
                for (int i = 0; i < allSliders.Count; i++)
                {
                    Setting.DeformationSlider s = allSliders[i];
                    if (s == null || pass1Done.Contains(s.GUID)) continue;
                    if (FaceLabTranslator.IsSystemSlider(s.DisplayName, s.GUID)) continue;
                    if (IsBaseSwitchGuid(s.GUID)) continue;

                    bool isBody = IsBodyVanillaSlider(s);
                    if (partFilter == 1 && isBody) continue;   // 仅脸部：跳过身材
                    if (partFilter == 2 && !isBody) continue;  // 仅身材：跳过脸部

                    float rv;
                    if (!presetRaw.TryGetValue(s.GUID, out rv)) rv = s.DefaultValue;
                    float fv = InvertEffectiveForTarget(mgr, targetChar, s, rv, effMap, ageDiffers);
                    WriteSliderDirect(mgr, targetChar, s.GUID, fv);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ParaFaceLab] ApplyPresetVanillaCrossAge failed: " + e.Message);
            }
            finally
            {
                SuppressRangeLimitBypass = prevSuppress;
            }

            MarkVisualDirty(targetChar);
            return ageDiffers;
        }

        public static void SetChestBaseMode(AssetCharacter ch, bool malePecMode)
        {
            if (ch == null) return;
            var batch = new Dictionary<ulong, float>
            {
                { GUID_SLIDER_PEC, malePecMode ? 1f : 0f },
                { GUID_SLIDER_BREAST, malePecMode ? 0f : 1f }
            };
            ApplyVanillaBatch(ch, batch);
        }

        public static bool IsMalePecMode(AssetCharacter ch)
        {
            if (ch == null) return true;
            try
            {
                CharacterDeformationSliderManager mgr = CharacterDeformationSliderManager.Instance;
                if (mgr == null) return true;
                float pec = mgr.GetCharacterSliderValue(ch, GUID_SLIDER_PEC);
                float breast = mgr.GetCharacterSliderValue(ch, GUID_SLIDER_BREAST);
                return pec >= breast;
            }
            catch { return true; }
        }

        public static void SetBottomBaseMode(AssetCharacter ch, bool maleBottomMode)
        {
            if (ch == null) return;
            var batch = new Dictionary<ulong, float>
            {
                { GUID_SLIDER_BOTTOM_MASC, maleBottomMode ? 1f : 0f },
                { GUID_SLIDER_BOTTOM_FEM, maleBottomMode ? 0f : 1f }
            };
            ApplyVanillaBatch(ch, batch);
        }

        public static bool IsMaleBottomMode(AssetCharacter ch)
        {
            if (ch == null) return true;
            try
            {
                CharacterDeformationSliderManager mgr = CharacterDeformationSliderManager.Instance;
                if (mgr == null) return true;
                float m = mgr.GetCharacterSliderValue(ch, GUID_SLIDER_BOTTOM_MASC);
                float f = mgr.GetCharacterSliderValue(ch, GUID_SLIDER_BOTTOM_FEM);
                return m >= f;
            }
            catch { return true; }
        }

        // 在游戏 UpdateCharacterBoneDeformations.LateUpdate 之前调用：
        // 若某根骨骼未被 Animator 刷新（仍等于上一帧我们写入的 Final*），先无痕归位到 Base*，
        // 彻底避免游戏自身的 Additive 骨骼变形在我们偏移上逐帧叠加或关闭精修后残留拉伸！
        public static void BeginFrameRestoreAllVisuals()
        {
            if (VisualBoneStates.Count == 0) return;
            foreach (var vkv in VisualBoneStates)
            {
                Dictionary<string, BoneStateRec> recs = vkv.Value;
                if (recs == null || recs.Count == 0) continue;
                foreach (var bkv in recs)
                {
                    BoneStateRec rec = bkv.Value;
                    if (rec == null || rec.Tr == null) continue;
                    Transform t = rec.Tr;
                    if (rec.PosActive && (t.localPosition - rec.FinalPos).sqrMagnitude < 1e-6f)
                        t.localPosition = rec.BasePos;
                    if (rec.RotActive && Quaternion.Angle(t.localRotation, rec.FinalRot) < 0.05f)
                        t.localRotation = rec.BaseRot;
                    if (rec.ScaleActive && (t.localScale - rec.FinalScale).sqrMagnitude < 1e-5f)
                        t.localScale = rec.BaseScale;
                }
            }
        }

        public static bool IsBodyBone(string boneName)
        {
            if (string.IsNullOrEmpty(boneName)) return false;
            return boneName.StartsWith("HumanRig", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("FullBody", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("Chest", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("Breast", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("Shoulder", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("Spine", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("Hips", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("Neck", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("UpperArm", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("LowerArm", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("Hand", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("UpperLeg", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("LowerLeg", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("Foot", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("Toes", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("Bottom", StringComparison.OrdinalIgnoreCase);
        }

        // 恢复某视觉实例所有曾被我们修改过的骨骼（当精修关闭/归零/对比原版/退出时立刻无痕复原）
        private static void RestoreAllTrackedBones(int visId)
        {
            Dictionary<string, BoneStateRec> recs;
            if (!VisualBoneStates.TryGetValue(visId, out recs) || recs == null || recs.Count == 0) return;

            foreach (var kv in recs)
            {
                BoneStateRec rec = kv.Value;
                if (rec == null || rec.Tr == null) continue;
                RestoreRec(rec);
            }
            recs.Clear();
        }

        private static void RestoreRec(BoneStateRec rec)
        {
            Transform t = rec.Tr;
            if (t == null) return;
            if (rec.PosActive)
            {
                if ((t.localPosition - rec.FinalPos).sqrMagnitude < 1e-7f)
                    t.localPosition = rec.BasePos;
                rec.PosActive = false;
            }
            if (rec.RotActive)
            {
                if (Quaternion.Angle(t.localRotation, rec.FinalRot) < 0.01f)
                    t.localRotation = rec.BaseRot;
                rec.RotActive = false;
            }
            if (rec.ScaleActive)
            {
                if ((t.localScale - rec.FinalScale).sqrMagnitude < 1e-6f)
                    t.localScale = rec.BaseScale;
                rec.ScaleActive = false;
            }
        }

        public static void RestoreAllVisualsImmediately()
        {
            List<int> keys = new List<int>(VisualBoneStates.Keys);
            for (int i = 0; i < keys.Count; i++)
                RestoreAllTrackedBones(keys[i]);
            VisualBoneStates.Clear();
        }

        public static void ApplyAllLoadedVisuals()
        {
            CharacterManager cm = CharacterManager.Instance;
            if (cm == null) return;

            AppliedVisualIds.Clear();

            if (cm.LoadedCharacterVisuals != null)
            {
                foreach (var kv in cm.LoadedCharacterVisuals)
                {
                    CharacterVisual vis = kv.Value;
                    if (vis == null) continue;
                    int id = vis.GetInstanceID();
                    if (!AppliedVisualIds.Add(id)) continue;
                    ApplyOrRestoreVisual(kv.Key, vis, id);
                }
            }

            try
            {
                List<AssetCharacter> chars = cm.Characters;
                if (chars != null)
                {
                    for (int i = 0; i < chars.Count; i++)
                    {
                        AssetCharacter c = chars[i];
                        if (c == null) continue;
                        if (!cm.IsCharacterInEdition(c.GUID)) continue;
                        CharacterVisual cv = cm.GetCharacterCreatorVisual(c.GUID);
                        if (cv == null) continue;
                        int id = cv.GetInstanceID();
                        if (!AppliedVisualIds.Add(id)) continue;
                        ApplyOrRestoreVisual(c.GUID, cv, id);
                    }
                }
            }
            catch { }

            try
            {
                ulong curGuid = FaceLabPlugin.CurrentTargetGuid;
                if (curGuid != 0)
                {
                    CharacterVisual cv = cm.GetCharacterCreatorVisual(curGuid);
                    if (cv != null)
                    {
                        int id = cv.GetInstanceID();
                        if (AppliedVisualIds.Add(id)) ApplyOrRestoreVisual(curGuid, cv, id);
                    }
                    CharacterVisual lv = cm.GetLoadedCharacterVisual(curGuid);
                    if (lv != null)
                    {
                        int id = lv.GetInstanceID();
                        if (AppliedVisualIds.Add(id)) ApplyOrRestoreVisual(curGuid, lv, id);
                    }
                }
            }
            catch { }

            // 清理已被销毁的旧 Visual 记录，避免内存增长
            if (VisualBoneStates.Count > 32)
            {
                List<int> dead = null;
                foreach (var kv in VisualBoneStates)
                {
                    if (!AppliedVisualIds.Contains(kv.Key))
                    {
                        if (dead == null) dead = new List<int>();
                        dead.Add(kv.Key);
                    }
                }
                if (dead != null)
                {
                    for (int i = 0; i < dead.Count; i++) VisualBoneStates.Remove(dead[i]);
                }
            }
        }

        private static void ApplyOrRestoreVisual(ulong guid, CharacterVisual visual, int visId)
        {
            if (visual == null) return;

            FaceProfile profile = FaceLabStorage.TryGetProfile(guid);
            bool shouldApply = FaceLabPlugin.OfficialModEnabled
                && !(CompareOriginalHeld && guid == CompareTargetGuid)
                && profile != null && profile.enabled && profile.bones != null && profile.bones.Count > 0;

            if (!shouldApply)
            {
                RestoreAllTrackedBones(visId);
                return;
            }

            Dictionary<string, BoneTransform> map = visual.BoneTransformByName;
            if (map == null || map.Count == 0) return;

            Dictionary<string, BoneStateRec> recs;
            if (!VisualBoneStates.TryGetValue(visId, out recs) || recs == null)
            {
                recs = new Dictionary<string, BoneStateRec>();
                VisualBoneStates[visId] = recs;
            }

            // 第一步：对于「之前改过、但现在已被归零/删除」的骨骼，立即恢复到原始基准态！
            List<string> toClean = null;
            foreach (var kv in recs)
            {
                BoneOffset curOff;
                if (!profile.bones.TryGetValue(kv.Key, out curOff) || curOff == null || curOff.IsZero())
                {
                    RestoreRec(kv.Value);
                    if (toClean == null) toClean = new List<string>();
                    toClean.Add(kv.Key);
                }
            }
            if (toClean != null)
            {
                for (int i = 0; i < toClean.Count; i++) recs.Remove(toClean[i]);
            }

            // 分别获取面部参考系（FaceMaster / Head）与躯干参考系（Chest / Spine）：
            // 两者在静止站姿下 XYZ 朝向完全一致（兼容已保存预设），
            // 但生活模式转头/低头时，胸/肩/胯不再跟着头部坐标系歪斜扭转！
            Transform faceRefFrame = null;
            BoneTransform fm;
            if (map.TryGetValue("FaceMaster", out fm) && fm != null && fm.Transform != null)
                faceRefFrame = fm.Transform;
            else if (map.TryGetValue("Head", out fm) && fm != null && fm.Transform != null)
                faceRefFrame = fm.Transform;

            Transform chestRefFrame = null;
            Transform spineRefFrame = null;
            if (map.TryGetValue("Chest", out fm) && fm != null && fm.Transform != null)
                chestRefFrame = fm.Transform;
            if (map.TryGetValue("Spine", out fm) && fm != null && fm.Transform != null)
                spineRefFrame = fm.Transform;

            // 第二步：防累乘安全叠加（先还原回本帧真实基准，再只应用 1 次偏移！）
            foreach (var kv in profile.bones)
            {
                BoneOffset off = kv.Value;
                if (off == null || off.IsZero()) continue;

                BoneTransform bt;
                if (!map.TryGetValue(kv.Key, out bt) || bt == null || bt.Transform == null)
                    continue;

                Transform t = bt.Transform;
                Transform refFrame;
                if (IsBodyBone(kv.Key))
                {
                    refFrame = (t == chestRefFrame) ? (spineRefFrame ?? faceRefFrame) : (chestRefFrame ?? spineRefFrame ?? faceRefFrame);
                }
                else
                {
                    refFrame = faceRefFrame;
                }
                BoneStateRec rec;
                if (!recs.TryGetValue(kv.Key, out rec) || rec == null || rec.Tr != t)
                {
                    rec = new BoneStateRec { Tr = t };
                    recs[kv.Key] = rec;
                }

                int side = BoneCatalog.GetBoneSide(kv.Key);

                // ---- 1) 位移（若上一帧值未被动画覆写，先回退到 BasePos，绝不逐帧飘移） ----
                bool wantPos = off.pos != null && !off.pos.IsZero();
                if (wantPos)
                {
                    Vector3 basePos = (rec.PosActive && (t.localPosition - rec.FinalPos).sqrMagnitude < 1e-7f)
                        ? rec.BasePos
                        : t.localPosition;

                    float mx = (side == -1) ? -off.pos.x : off.pos.x;
                    Vector3 faceDelta = new Vector3(mx * 0.001f, off.pos.y * 0.001f, off.pos.z * 0.001f);
                    Vector3 localDelta = (refFrame != null && t.parent != null && t != refFrame)
                        ? t.parent.InverseTransformVector(refFrame.TransformVector(faceDelta))
                        : faceDelta;

                    Vector3 finalPos = basePos + localDelta;
                    t.localPosition = finalPos;
                    rec.PosActive = true;
                    rec.BasePos = basePos;
                    rec.FinalPos = finalPos;
                }
                else if (rec.PosActive)
                {
                    if ((t.localPosition - rec.FinalPos).sqrMagnitude < 1e-7f)
                        t.localPosition = rec.BasePos;
                    rec.PosActive = false;
                }

                // ---- 2) 旋转（同样防暂停/静态骨骼逐帧自转） ----
                bool wantRot = off.rot != null && !off.rot.IsZero();
                if (wantRot)
                {
                    Quaternion baseRot = (rec.RotActive && Quaternion.Angle(t.localRotation, rec.FinalRot) < 0.01f)
                        ? rec.BaseRot
                        : t.localRotation;

                    // 先把旋转归回 baseRot，再算世界空间叠加
                    t.localRotation = baseRot;

                    float rx = off.rot.x;
                    float ry = (side == -1) ? -off.rot.y : off.rot.y;
                    float rz = (side == -1) ? -off.rot.z : off.rot.z;

                    if (refFrame != null && t != refFrame)
                    {
                        Quaternion q = Quaternion.AngleAxis(rz, refFrame.forward)
                                     * Quaternion.AngleAxis(ry, refFrame.up)
                                     * Quaternion.AngleAxis(rx, refFrame.right);
                        t.rotation = q * t.rotation;
                    }
                    else
                    {
                        t.localRotation = baseRot * Quaternion.Euler(rx, ry, rz);
                    }

                    rec.RotActive = true;
                    rec.BaseRot = baseRot;
                    rec.FinalRot = t.localRotation;
                }
                else if (rec.RotActive)
                {
                    if (Quaternion.Angle(t.localRotation, rec.FinalRot) < 0.01f)
                        t.localRotation = rec.BaseRot;
                    rec.RotActive = false;
                }

                // ---- 3) 缩放（彻底根治肉墙膨胀 & 断臂维纳斯！） ----
                bool wantScale = Mathf.Abs(off.uniformScale) > 0.0001f || (off.scale != null && !off.scale.IsZero());
                if (wantScale)
                {
                    Vector3 baseScale = (rec.ScaleActive && (t.localScale - rec.FinalScale).sqrMagnitude < 1e-6f)
                        ? rec.BaseScale
                        : t.localScale;

                    float u = 1f + off.uniformScale;
                    float sx = Mathf.Clamp(u * (1f + (off.scale != null ? off.scale.x : 0f)), 0.20f, 3.0f);
                    float sy = Mathf.Clamp(u * (1f + (off.scale != null ? off.scale.y : 0f)), 0.20f, 3.0f);
                    float sz = Mathf.Clamp(u * (1f + (off.scale != null ? off.scale.z : 0f)), 0.20f, 3.0f);

                    Vector3 finalScale = Vector3.Scale(baseScale, new Vector3(sx, sy, sz));
                    t.localScale = finalScale;
                    rec.ScaleActive = true;
                    rec.BaseScale = baseScale;
                    rec.FinalScale = finalScale;
                }
                else if (rec.ScaleActive)
                {
                    if ((t.localScale - rec.FinalScale).sqrMagnitude < 1e-6f)
                        t.localScale = rec.BaseScale;
                    rec.ScaleActive = false;
                }
            }
        }

        public static CharacterVisual FindVisualFor(AssetCharacter ch)
        {
            if (ch == null) return null;
            CharacterManager cm = CharacterManager.Instance;
            if (cm == null) return null;
            try
            {
                CharacterVisual cv = cm.GetCharacterCreatorVisual(ch.GUID);
                if (cv != null) return cv;
            }
            catch { }
            try
            {
                CharacterVisual lv = cm.GetLoadedCharacterVisual(ch.GUID);
                if (lv != null) return lv;
            }
            catch { }
            return null;
        }
    }

    [HarmonyPatch(typeof(UpdateCharacterBoneDeformations), "LateUpdate")]
    internal static class FaceLabBoneLateUpdatePatch
    {
        private static void Prefix()
        {
            try { FaceLabRuntime.BeginFrameRestoreAllVisuals(); }
            catch { }
        }

        private static void Postfix()
        {
            try { FaceLabRuntime.ApplyAllLoadedVisuals(); }
            catch { }
        }
    }

    [HarmonyPatch(typeof(CharacterDeformationSliderManager), "RemapDeformationSliderValuesWithRangeLimiting")]
    internal static class FaceLabRangeLimiterBypassPatch
    {
        private static bool Prefix(float originalDeformationSliderValue, AssetCharacter character, ref float __result)
        {
            try
            {
                if (FaceLabRuntime.IsLimitUnlockedFor(character))
                {
                    __result = originalDeformationSliderValue;
                    return false;
                }
                return true;
            }
            catch { return true; }
        }
    }

    [HarmonyPatch(typeof(CharacterDeformationSliderManager), "GetMeshDeformationSliderItemAndValue")]
    internal static class FaceLabOverLimitExtrapolatePatch
    {
        private static bool Prefix(
            Setting.DeformationSlider deformationSlider,
            float sliderValue,
            ref ValueTuple<Setting.MeshDeformationSliderItem, float> __result)
        {
            try
            {
                if (!FaceLabPlugin.OfficialModEnabled) return true;
                if (deformationSlider == null) return true;
                if (sliderValue >= 0f && sliderValue <= 1f) return true;
                // -1f 为游戏内部缺失拉杆哨兵值，绝对不可外推
                if (Mathf.Approximately(sliderValue, -1f)) return true;

                Setting.MeshDeformationSliderItem[] items = deformationSlider.MeshDeformationSliderItems;
                if (items == null || items.Length == 0) return true;

                Setting.MeshDeformationSliderItem chosen = null;
                if (sliderValue < 0f)
                {
                    float bestStart = float.MaxValue;
                    for (int i = 0; i < items.Length; i++)
                    {
                        Setting.MeshDeformationSliderItem it = items[i];
                        if (it == null) continue;
                        if (it.StartRatio < bestStart)
                        {
                            bestStart = it.StartRatio;
                            chosen = it;
                        }
                    }
                }
                else
                {
                    float bestEnd = float.MinValue;
                    for (int i = 0; i < items.Length; i++)
                    {
                        Setting.MeshDeformationSliderItem it = items[i];
                        if (it == null) continue;
                        if (it.EndRatio > bestEnd)
                        {
                            bestEnd = it.EndRatio;
                            chosen = it;
                        }
                    }
                }

                if (chosen == null) return true;

                float span = Mathf.Max(0.0001f, chosen.EndRatio - chosen.StartRatio);
                float t = (sliderValue - chosen.StartRatio) / span;
                if (chosen.Invert) t = 1f - t;

                t = Mathf.Clamp(t, -2.5f, 3.5f);
                __result = new ValueTuple<Setting.MeshDeformationSliderItem, float>(chosen, t);
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}
