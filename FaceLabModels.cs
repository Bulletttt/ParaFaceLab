// FaceLabModels.cs —— 数据模型与面部/身材骨骼中文目录（基于 Paralives 真实骨架）
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParaFaceLab
{
    [Serializable]
    public class Vec3Data
    {
        public float x;
        public float y;
        public float z;

        public Vec3Data() { }
        public Vec3Data(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public Vector3 ToVector3() { return new Vector3(x, y, z); }
        public bool IsZero()
        {
            return Mathf.Abs(x) < 0.00001f && Mathf.Abs(y) < 0.00001f && Mathf.Abs(z) < 0.00001f;
        }
        public Vec3Data Clone() { return new Vec3Data(x, y, z); }
    }

    [Serializable]
    public class BoneOffset
    {
        public Vec3Data pos = new Vec3Data();
        public Vec3Data rot = new Vec3Data();
        public Vec3Data scale = new Vec3Data();
        public float uniformScale = 0f;

        public bool IsZero()
        {
            return (pos == null || pos.IsZero())
                && (rot == null || rot.IsZero())
                && (scale == null || scale.IsZero())
                && Mathf.Abs(uniformScale) < 0.0001f;
        }

        public void Reset()
        {
            pos = new Vec3Data();
            rot = new Vec3Data();
            scale = new Vec3Data();
            uniformScale = 0f;
        }

        public BoneOffset Clone()
        {
            return new BoneOffset
            {
                pos = pos != null ? pos.Clone() : new Vec3Data(),
                rot = rot != null ? rot.Clone() : new Vec3Data(),
                scale = scale != null ? scale.Clone() : new Vec3Data(),
                uniformScale = uniformScale
            };
        }
    }

    [Serializable]
    public class FaceProfile
    {
        public int version = 1;
        public ulong characterGuid;
        public string characterName = "";
        public string updatedAt = "";
        public bool enabled = true;
        public bool unlockVanillaLimits = false;
        public float sourceAge = -1f;

        public Dictionary<string, BoneOffset> bones = new Dictionary<string, BoneOffset>();
        public Dictionary<string, float> vanillaSliders = new Dictionary<string, float>();
        public Dictionary<string, float> effectiveVanillaSliders = new Dictionary<string, float>();

        public BoneOffset GetOrCreateBone(string boneName)
        {
            if (bones == null) bones = new Dictionary<string, BoneOffset>();
            BoneOffset b;
            if (!bones.TryGetValue(boneName, out b) || b == null)
            {
                b = new BoneOffset();
                bones[boneName] = b;
            }
            return b;
        }

        public BoneOffset TryGetBone(string boneName)
        {
            if (bones == null) return null;
            BoneOffset b;
            return bones.TryGetValue(boneName, out b) ? b : null;
        }

        public void PruneZeros()
        {
            if (bones == null) return;
            List<string> remove = null;
            foreach (var kv in bones)
            {
                if (kv.Value == null || kv.Value.IsZero())
                {
                    if (remove == null) remove = new List<string>();
                    remove.Add(kv.Key);
                }
            }
            if (remove != null)
            {
                for (int i = 0; i < remove.Count; i++) bones.Remove(remove[i]);
            }
        }

        public int ActiveBoneCount()
        {
            if (bones == null) return 0;
            int n = 0;
            foreach (var kv in bones)
            {
                if (kv.Value != null && !kv.Value.IsZero()) n++;
            }
            return n;
        }

        public FaceProfile Clone()
        {
            var copy = new FaceProfile
            {
                version = version,
                characterGuid = characterGuid,
                characterName = characterName,
                updatedAt = updatedAt,
                enabled = enabled,
                unlockVanillaLimits = unlockVanillaLimits,
                sourceAge = sourceAge,
                bones = new Dictionary<string, BoneOffset>(),
                vanillaSliders = new Dictionary<string, float>(),
                effectiveVanillaSliders = new Dictionary<string, float>()
            };
            if (bones != null)
            {
                foreach (var kv in bones)
                {
                    if (kv.Value != null && !kv.Value.IsZero())
                        copy.bones[kv.Key] = kv.Value.Clone();
                }
            }
            if (vanillaSliders != null)
            {
                foreach (var kv in vanillaSliders)
                    copy.vanillaSliders[kv.Key] = kv.Value;
            }
            if (effectiveVanillaSliders != null)
            {
                foreach (var kv in effectiveVanillaSliders)
                    copy.effectiveVanillaSliders[kv.Key] = kv.Value;
            }
            return copy;
        }
    }

    public sealed class BoneEntryDef
    {
        public string Category;     // "眼" / "眉" / "鼻" / "唇" / "颊颌" / "头耳" / "身材"
        public string Label;
        public string LeftBone;
        public string RightBone;
        public float PosRangeMm;
        public float RotRangeDeg;
        public float ScaleRange;

        public bool IsPaired { get { return !string.IsNullOrEmpty(RightBone); } }
    }

    public static class BoneCatalog
    {
        public static readonly string[] Categories = new string[]
        {
            "眼", "眉", "鼻", "唇", "颊颌", "头耳", "身材", "全部"
        };

        public static readonly BoneEntryDef[] Entries = new BoneEntryDef[]
        {
            // ---- 眼部 ----
            new BoneEntryDef { Category = "眼", Label = "眼部整体", LeftBone = "EyeMaster.L", RightBone = "EyeMaster.R", PosRangeMm = 18f, RotRangeDeg = 30f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "眼", Label = "眼球位置/大小", LeftBone = "Eye.L", RightBone = "Eye.R", PosRangeMm = 10f, RotRangeDeg = 20f, ScaleRange = 0.40f },
            new BoneEntryDef { Category = "眼", Label = "上眼皮", LeftBone = "UpperLid.L", RightBone = "UpperLid.R", PosRangeMm = 10f, RotRangeDeg = 30f, ScaleRange = 0.40f },
            new BoneEntryDef { Category = "眼", Label = "下眼皮/卧蚕", LeftBone = "LowerLid.L", RightBone = "LowerLid.R", PosRangeMm = 10f, RotRangeDeg = 30f, ScaleRange = 0.40f },

            // ---- 眉部 ----
            new BoneEntryDef { Category = "眉", Label = "眉毛整体", LeftBone = "EyebrowMaster.L", RightBone = "EyebrowMaster.R", PosRangeMm = 18f, RotRangeDeg = 35f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "眉", Label = "眉头（内段）", LeftBone = "Eyebrow1.L", RightBone = "Eyebrow1.R", PosRangeMm = 12f, RotRangeDeg = 35f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "眉", Label = "眉峰（中段）", LeftBone = "Eyebrow2.L", RightBone = "Eyebrow2.R", PosRangeMm = 12f, RotRangeDeg = 35f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "眉", Label = "眉尾（外段）", LeftBone = "Eyebrow3.L", RightBone = "Eyebrow3.R", PosRangeMm = 12f, RotRangeDeg = 35f, ScaleRange = 0.45f },

            // ---- 鼻部 ----
            new BoneEntryDef { Category = "鼻", Label = "鼻子整体", LeftBone = "NoseMaster", RightBone = null, PosRangeMm = 20f, RotRangeDeg = 30f, ScaleRange = 0.50f },
            new BoneEntryDef { Category = "鼻", Label = "山根 / 鼻梁", LeftBone = "NoseBridge", RightBone = null, PosRangeMm = 16f, RotRangeDeg = 25f, ScaleRange = 0.50f },
            new BoneEntryDef { Category = "鼻", Label = "鼻尖", LeftBone = "NoseTip", RightBone = null, PosRangeMm = 16f, RotRangeDeg = 35f, ScaleRange = 0.50f },
            new BoneEntryDef { Category = "鼻", Label = "鼻翼（左右）", LeftBone = "Nosetril.L", RightBone = "Nosetril.R", PosRangeMm = 12f, RotRangeDeg = 30f, ScaleRange = 0.50f },

            // ---- 唇部 ----
            new BoneEntryDef { Category = "唇", Label = "上唇整体", LeftBone = "LipsMasterTop", RightBone = null, PosRangeMm = 16f, RotRangeDeg = 25f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "唇", Label = "上唇中 / 唇珠", LeftBone = "LipsTop", RightBone = null, PosRangeMm = 12f, RotRangeDeg = 30f, ScaleRange = 0.50f },
            new BoneEntryDef { Category = "唇", Label = "上唇两侧", LeftBone = "LipsTopMiddle.L", RightBone = "LipsTopMiddle.R", PosRangeMm = 10f, RotRangeDeg = 30f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "唇", Label = "嘴角（微笑弧度）", LeftBone = "LipsSmile.L", RightBone = "LipsSmile.R", PosRangeMm = 14f, RotRangeDeg = 35f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "唇", Label = "下唇整体", LeftBone = "LipsMasterBottom", RightBone = null, PosRangeMm = 16f, RotRangeDeg = 25f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "唇", Label = "下唇中", LeftBone = "LipsBottom", RightBone = null, PosRangeMm = 12f, RotRangeDeg = 30f, ScaleRange = 0.50f },
            new BoneEntryDef { Category = "唇", Label = "下唇两侧", LeftBone = "LipsBottomMiddle.L", RightBone = "LipsBottomMiddle.R", PosRangeMm = 10f, RotRangeDeg = 30f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "唇", Label = "上牙", LeftBone = "UpperTeeth", RightBone = null, PosRangeMm = 10f, RotRangeDeg = 20f, ScaleRange = 0.35f },
            new BoneEntryDef { Category = "唇", Label = "下牙", LeftBone = "LowerTeeth", RightBone = null, PosRangeMm = 10f, RotRangeDeg = 20f, ScaleRange = 0.35f },
            new BoneEntryDef { Category = "唇", Label = "舌头", LeftBone = "Tongue1", RightBone = null, PosRangeMm = 12f, RotRangeDeg = 25f, ScaleRange = 0.35f },

            // ---- 颊颌 ----
            new BoneEntryDef { Category = "颊颌", Label = "苹果肌 / 颧颊", LeftBone = "CheekSmile.L", RightBone = "CheekSmile.R", PosRangeMm = 20f, RotRangeDeg = 30f, ScaleRange = 0.50f },
            new BoneEntryDef { Category = "颊颌", Label = "面颊 / 颊脂垫", LeftBone = "CheekInflate.L", RightBone = "CheekInflate.R", PosRangeMm = 20f, RotRangeDeg = 30f, ScaleRange = 0.50f },
            new BoneEntryDef { Category = "颊颌", Label = "下颌骨", LeftBone = "Jaw", RightBone = null, PosRangeMm = 20f, RotRangeDeg = 25f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "颊颌", Label = "下巴骨相", LeftBone = "JawDeformation", RightBone = null, PosRangeMm = 22f, RotRangeDeg = 30f, ScaleRange = 0.50f },

            // ---- 头耳 ----
            new BoneEntryDef { Category = "头耳", Label = "五官整体中枢", LeftBone = "FaceMaster", RightBone = null, PosRangeMm = 25f, RotRangeDeg = 20f, ScaleRange = 0.35f },
            new BoneEntryDef { Category = "头耳", Label = "头部整体（头身比）", LeftBone = "Head", RightBone = null, PosRangeMm = 30f, RotRangeDeg = 20f, ScaleRange = 0.35f },
            new BoneEntryDef { Category = "头耳", Label = "脖子（长短/粗细）", LeftBone = "Neck", RightBone = null, PosRangeMm = 35f, RotRangeDeg = 20f, ScaleRange = 0.40f },
            new BoneEntryDef { Category = "头耳", Label = "耳朵（招风/精灵耳）", LeftBone = "Ear.L", RightBone = "Ear.R", PosRangeMm = 18f, RotRangeDeg = 45f, ScaleRange = 0.60f },
            new BoneEntryDef { Category = "头耳", Label = "发型挂点微调", LeftBone = "Hair", RightBone = null, PosRangeMm = 30f, RotRangeDeg = 25f, ScaleRange = 0.35f },

            // ---- 身材（全身/胸/肩/腰胯/手臂/大腿/小腿/脚掌全覆盖） ----
            new BoneEntryDef { Category = "身材", Label = "整体身高 / 全身缩放", LeftBone = "HumanRig", RightBone = null, PosRangeMm = 40f, RotRangeDeg = 15f, ScaleRange = 0.35f },
            new BoneEntryDef { Category = "身材", Label = "胸部骨骼（压平/提拉）", LeftBone = "Breast.L", RightBone = "Breast.R", PosRangeMm = 45f, RotRangeDeg = 35f, ScaleRange = 0.60f },
            new BoneEntryDef { Category = "身材", Label = "胸腔 / 上身廓形", LeftBone = "Chest", RightBone = null, PosRangeMm = 40f, RotRangeDeg = 20f, ScaleRange = 0.40f },
            new BoneEntryDef { Category = "身材", Label = "肩膀（宽窄/平肩）", LeftBone = "Shoulder.L", RightBone = "Shoulder.R", PosRangeMm = 45f, RotRangeDeg = 25f, ScaleRange = 0.40f },
            new BoneEntryDef { Category = "身材", Label = "腰腹 / 脊柱", LeftBone = "Spine", RightBone = null, PosRangeMm = 40f, RotRangeDeg = 20f, ScaleRange = 0.40f },
            new BoneEntryDef { Category = "身材", Label = "胯部 / 骨盆", LeftBone = "Hips", RightBone = null, PosRangeMm = 40f, RotRangeDeg = 20f, ScaleRange = 0.40f },
            new BoneEntryDef { Category = "身材", Label = "臀瓣（翘度/丰满）", LeftBone = "Bottom_L", RightBone = "Bottom_R", PosRangeMm = 35f, RotRangeDeg = 25f, ScaleRange = 0.50f },
            new BoneEntryDef { Category = "身材", Label = "大臂（长短/粗细）", LeftBone = "UpperArm.L", RightBone = "UpperArm.R", PosRangeMm = 35f, RotRangeDeg = 20f, ScaleRange = 0.40f },
            new BoneEntryDef { Category = "身材", Label = "大臂中段（线条/肌肉）", LeftBone = "UpperArm_Twist2.L", RightBone = "UpperArm_Twist2.R", PosRangeMm = 20f, RotRangeDeg = 20f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "身材", Label = "小臂（长短/粗细）", LeftBone = "LowerArm.L", RightBone = "LowerArm.R", PosRangeMm = 35f, RotRangeDeg = 20f, ScaleRange = 0.40f },
            new BoneEntryDef { Category = "身材", Label = "小臂中段（前臂线条）", LeftBone = "LowerArmTwist2.L", RightBone = "LowerArmTwist2.R", PosRangeMm = 20f, RotRangeDeg = 20f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "身材", Label = "手掌", LeftBone = "Hand.L", RightBone = "Hand.R", PosRangeMm = 25f, RotRangeDeg = 20f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "身材", Label = "大腿（腿长Y / 粗细XZ）", LeftBone = "UpperLeg.L", RightBone = "UpperLeg.R", PosRangeMm = 40f, RotRangeDeg = 20f, ScaleRange = 0.40f },
            new BoneEntryDef { Category = "身材", Label = "大腿中段（肉感/线条）", LeftBone = "UpperLeg_Twist2.L", RightBone = "UpperLeg_Twist2.R", PosRangeMm = 25f, RotRangeDeg = 20f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "身材", Label = "小腿（腿长Y / 粗细XZ）", LeftBone = "LowerLeg.L", RightBone = "LowerLeg.R", PosRangeMm = 40f, RotRangeDeg = 20f, ScaleRange = 0.40f },
            new BoneEntryDef { Category = "身材", Label = "小腿肚（肌肉/线条）", LeftBone = "LowerLeg_Twist2.L", RightBone = "LowerLeg_Twist2.R", PosRangeMm = 25f, RotRangeDeg = 20f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "身材", Label = "脚掌（大小/角度）", LeftBone = "Foot.L", RightBone = "Foot.R", PosRangeMm = 30f, RotRangeDeg = 25f, ScaleRange = 0.45f },
            new BoneEntryDef { Category = "身材", Label = "脚尖 / 脚趾", LeftBone = "Toes.L", RightBone = "Toes.R", PosRangeMm = 20f, RotRangeDeg = 25f, ScaleRange = 0.45f }
        };

        public static int GetBoneSide(string boneName)
        {
            if (string.IsNullOrEmpty(boneName)) return 0;
            if (boneName.EndsWith(".L", StringComparison.OrdinalIgnoreCase) || boneName.EndsWith("_L", StringComparison.OrdinalIgnoreCase))
                return -1;
            if (boneName.EndsWith(".R", StringComparison.OrdinalIgnoreCase) || boneName.EndsWith("_R", StringComparison.OrdinalIgnoreCase))
                return 1;
            return 0;
        }
    }
}
