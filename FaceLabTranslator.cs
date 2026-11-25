// FaceLabTranslator.cs —— Paralives 全部 200+ 原版拉杆精准中文翻译与分区归类
using System;
using System.Collections.Generic;

namespace ParaFaceLab
{
    public enum VanillaCat
    {
        All = 0,
        EyeBrow = 1,   // 眼眉
        Nose = 2,      // 鼻部
        LipJaw = 3,    // 唇颌
        FaceHead = 4,  // 脸头耳
        ChestBack = 5, // 胸肩背
        BodyLimb = 6,  // 腰胯四肢
        Modified = 7   // 已改
    }

    public sealed class SliderMeta
    {
        public string Zh;
        public VanillaCat Cat;
        public bool HiddenByDefault; // 幼儿专属/内部系统控制项默认隐藏（搜索时仍可显示）

        public SliderMeta(string zh, VanillaCat cat, bool hidden = false)
        {
            Zh = zh;
            Cat = cat;
            HiddenByDefault = hidden;
        }
    }

    public static class FaceLabTranslator
    {
        private static readonly Dictionary<string, SliderMeta> Map = new Dictionary<string, SliderMeta>(StringComparer.OrdinalIgnoreCase)
        {
            // ========== 胸肩背（男胸/女胸/胸腔/肩背重点区） ==========
            // Pec 与 Breast 为游戏内部 0/1 开关（VisibleInCatalog=False），默认隐藏避免误拖锁死，改用顶部的安全模式按钮切换
            { "Pec", new SliderMeta("男体胸肌总开关（内部0/1）", VanillaCat.ChestBack, true) },
            { "PecSize", new SliderMeta("男体胸肌大小", VanillaCat.ChestBack) },
            { "PecFat", new SliderMeta("男体胸肌饱满/脂肪", VanillaCat.ChestBack) },
            { "PecHeight", new SliderMeta("男体胸肌高低", VanillaCat.ChestBack) },
            { "PecWidth", new SliderMeta("男体胸肌宽窄", VanillaCat.ChestBack) },
            { "Breast", new SliderMeta("女体胸部总开关（内部0/1）", VanillaCat.ChestBack, true) },
            { "BreastSize", new SliderMeta("胸部大小", VanillaCat.ChestBack) },
            { "BreastHeight", new SliderMeta("胸部高低", VanillaCat.ChestBack) },
            { "BreastWidth", new SliderMeta("胸部间距/宽窄", VanillaCat.ChestBack) },
            { "ChestWidth", new SliderMeta("胸腔宽度", VanillaCat.ChestBack) },
            { "ChestHeight", new SliderMeta("胸腔高度", VanillaCat.ChestBack) },
            { "RibCageSize", new SliderMeta("肋骨/胸廓大小", VanillaCat.ChestBack) },
            { "ShoulderWidth", new SliderMeta("肩膀宽度", VanillaCat.ChestBack) },
            { "ShoulderHeight", new SliderMeta("肩膀高低/溜肩", VanillaCat.ChestBack) },
            { "ShoulderDepth", new SliderMeta("肩膀前后厚度", VanillaCat.ChestBack) },
            { "ShoulderRotationMasculin", new SliderMeta("男体平肩角度", VanillaCat.ChestBack) },
            { "ShoulderRotationFeminine", new SliderMeta("女体柔肩角度", VanillaCat.ChestBack) },
            { "Trapezius", new SliderMeta("斜方肌大小", VanillaCat.ChestBack) },
            { "BackMuscle", new SliderMeta("背部肌肉", VanillaCat.ChestBack) },
            { "BackFat", new SliderMeta("背部脂肪", VanillaCat.ChestBack) },
            { "BackRound", new SliderMeta("驼背/背部弧度", VanillaCat.ChestBack) },
            { "BackCorrection", new SliderMeta("背部姿态矫正", VanillaCat.ChestBack) },
            { "Posture", new SliderMeta("体态挺拔度", VanillaCat.ChestBack) },

            // ========== 腰胯四肢 / 整体身材 ==========
            { "Height", new SliderMeta("身高", VanillaCat.BodyLimb) },
            { "OverallBodyWeight", new SliderMeta("全身胖瘦总控", VanillaCat.BodyLimb) },
            { "OverallBodyMuscle", new SliderMeta("全身肌肉总控", VanillaCat.BodyLimb) },
            { "OverallFat", new SliderMeta("整体体脂", VanillaCat.BodyLimb) },
            { "FatMasterSlider", new SliderMeta("脂肪主拉杆", VanillaCat.BodyLimb) },
            { "MuscleMasterSlider", new SliderMeta("肌肉主拉杆", VanillaCat.BodyLimb) },
            { "WaistWidth", new SliderMeta("腰围宽窄", VanillaCat.BodyLimb) },
            { "WaistHeight", new SliderMeta("腰线高低", VanillaCat.BodyLimb) },
            { "StomachSize", new SliderMeta("腹部大小", VanillaCat.BodyLimb) },
            { "StomachFat", new SliderMeta("腹部脂肪", VanillaCat.BodyLimb) },
            { "StomachPregnant", new SliderMeta("孕肚隆起", VanillaCat.BodyLimb) },
            { "NavelFlat", new SliderMeta("肚脐平坦度", VanillaCat.BodyLimb) },
            { "BottomMasculine", new SliderMeta("男体下身总开关（内部0/1）", VanillaCat.BodyLimb, true) },
            { "BottomMasculineWidth", new SliderMeta("男体胯部宽度", VanillaCat.BodyLimb) },
            { "BottomMasculineFatMuscular", new SliderMeta("男体臀部肌肉/脂肪", VanillaCat.BodyLimb) },
            { "BottomFeminine", new SliderMeta("女体下身总开关（内部0/1）", VanillaCat.BodyLimb, true) },
            { "BottomFeminineWidth", new SliderMeta("女体胯部宽度", VanillaCat.BodyLimb) },
            { "BottomFeminineFatMuscular", new SliderMeta("女体臀部肌肉/脂肪", VanillaCat.BodyLimb) },
            { "BottomDepth", new SliderMeta("臀部翘度/前后", VanillaCat.BodyLimb) },
            { "BottomHeight", new SliderMeta("臀线高低", VanillaCat.BodyLimb) },
            { "BottomSplit", new SliderMeta("臀缝深度", VanillaCat.BodyLimb) },
            { "CrotchSize", new SliderMeta("裆部轮廓", VanillaCat.BodyLimb) },
            { "ArmSize", new SliderMeta("大臂粗细", VanillaCat.BodyLimb) },
            { "ArmMuscular", new SliderMeta("大臂肌肉", VanillaCat.BodyLimb) },
            { "ForearmSize", new SliderMeta("小臂粗细", VanillaCat.BodyLimb) },
            { "ForearmFatMuscular", new SliderMeta("小臂肌肉/脂肪", VanillaCat.BodyLimb) },
            { "HandSize", new SliderMeta("手掌大小", VanillaCat.BodyLimb) },
            { "ThighSize", new SliderMeta("大腿粗细", VanillaCat.BodyLimb) },
            { "ThighFatMuscular", new SliderMeta("大腿肌肉/脂肪", VanillaCat.BodyLimb) },
            { "CalvesSize", new SliderMeta("小腿粗细", VanillaCat.BodyLimb) },
            { "CalvesFatMuscular", new SliderMeta("小腿肌肉/脂肪", VanillaCat.BodyLimb) },
            { "FootSize", new SliderMeta("脚掌大小", VanillaCat.BodyLimb) },

            // ========== 眼眉 ==========
            { "EyeSize", new SliderMeta("眼睛大小", VanillaCat.EyeBrow) },
            { "EyePositionHeight", new SliderMeta("眼睛整体高低", VanillaCat.EyeBrow) },
            { "EyePositionWidth", new SliderMeta("眼距宽窄", VanillaCat.EyeBrow) },
            { "EyeDepth", new SliderMeta("眼窝深浅", VanillaCat.EyeBrow) },
            { "EyeRotation", new SliderMeta("眼睛整体斜度", VanillaCat.EyeBrow) },
            { "EyeInnerCornerHeight", new SliderMeta("内眼角高低", VanillaCat.EyeBrow) },
            { "EyeInnerCornerWidth", new SliderMeta("内眼角开合/宽度", VanillaCat.EyeBrow) },
            { "EyeOutterCornerHeight", new SliderMeta("外眼角高低（吊眼/下垂）", VanillaCat.EyeBrow) },
            { "EyeOutterCornerWidth", new SliderMeta("外眼角拉长/宽度", VanillaCat.EyeBrow) },
            { "EyeEpicanthalFoldShape", new SliderMeta("内眦赘皮（蒙古褶）", VanillaCat.EyeBrow) },
            { "EyeFoldHighShape", new SliderMeta("高眼褶/欧式大双", VanillaCat.EyeBrow) },
            { "EyeHoodedShape", new SliderMeta("内双/肿眼泡", VanillaCat.EyeBrow) },
            { "UpperEyelidHeight", new SliderMeta("上眼皮睁开高度", VanillaCat.EyeBrow) },
            { "UpperEyelidCurvature", new SliderMeta("上眼皮弧度", VanillaCat.EyeBrow) },
            { "UpperEyelidDepth", new SliderMeta("上眼皮深浅", VanillaCat.EyeBrow) },
            { "UpperEyelidPinch", new SliderMeta("上眼皮紧致度", VanillaCat.EyeBrow) },
            { "LowerEyelidHeight", new SliderMeta("下眼皮高低（下至）", VanillaCat.EyeBrow) },
            { "LowerEyelidCurvature", new SliderMeta("下眼皮弧度", VanillaCat.EyeBrow) },
            { "EyebrowHeight", new SliderMeta("眉毛整体高低", VanillaCat.EyeBrow) },
            { "EyebrowWidth", new SliderMeta("眉距/眉毛宽度", VanillaCat.EyeBrow) },
            { "EyebrowSize", new SliderMeta("眉毛大小/粗细", VanillaCat.EyeBrow) },
            { "EyebrowDepth", new SliderMeta("眉骨前后深浅", VanillaCat.EyeBrow) },
            { "EyebrowInnerCornerHeight", new SliderMeta("眉头高低", VanillaCat.EyeBrow) },
            { "EyebrowMiddleHeight", new SliderMeta("眉峰高低", VanillaCat.EyeBrow) },
            { "EyebrowOuterCornerHeight", new SliderMeta("眉尾高低", VanillaCat.EyeBrow) },

            // ========== 鼻部 ==========
            { "NoseSize", new SliderMeta("鼻子整体大小", VanillaCat.Nose) },
            { "NoseHeight", new SliderMeta("鼻子高低位置", VanillaCat.Nose) },
            { "NoseDepth", new SliderMeta("鼻子整体挺翘度", VanillaCat.Nose) },
            { "NoseRotation", new SliderMeta("鼻子上翘/下勾角度", VanillaCat.Nose) },
            { "NoseRootDepth", new SliderMeta("山根高低", VanillaCat.Nose) },
            { "NoseUpperBridgeWidth", new SliderMeta("山根/上鼻梁宽度", VanillaCat.Nose) },
            { "NoseBridgeWidth", new SliderMeta("鼻梁宽度", VanillaCat.Nose) },
            { "NoseBridgeDepth", new SliderMeta("鼻梁高度/挺度", VanillaCat.Nose) },
            { "NoseBridgeStraight", new SliderMeta("鼻梁笔直度", VanillaCat.Nose) },
            { "NoseBridgeBump", new SliderMeta("驼峰鼻节骨", VanillaCat.Nose) },
            { "NoseBumpBridgeShape", new SliderMeta("驼峰鼻型", VanillaCat.Nose) },
            { "NoseShapeBridgeBumpDeformation", new SliderMeta("鼻梁节骨起伏", VanillaCat.Nose) },
            { "NoseSideBumpShape", new SliderMeta("鼻侧骨感", VanillaCat.Nose) },
            { "NoseTipSize", new SliderMeta("鼻头大小", VanillaCat.Nose) },
            { "NoseTipWidth", new SliderMeta("鼻头宽窄", VanillaCat.Nose) },
            { "NoseTipHeight", new SliderMeta("鼻尖高低", VanillaCat.Nose) },
            { "NoseTipDepth", new SliderMeta("鼻尖前凸长度", VanillaCat.Nose) },
            { "NoseShapeTipPointy", new SliderMeta("尖翘鼻头", VanillaCat.Nose) },
            { "NoseShapeTipRound", new SliderMeta("圆润鼻头", VanillaCat.Nose) },
            { "NostrilWidth", new SliderMeta("鼻翼宽度", VanillaCat.Nose) },
            { "NostrilHeight", new SliderMeta("鼻翼高低", VanillaCat.Nose) },
            { "NostrilsHoleMax", new SliderMeta("鼻孔大小", VanillaCat.Nose) },
            { "NoseShape1", new SliderMeta("鼻型轮廓 1", VanillaCat.Nose) },
            { "NoseShape2", new SliderMeta("鼻型轮廓 2", VanillaCat.Nose) },
            { "NoseShape9", new SliderMeta("鼻型轮廓 9", VanillaCat.Nose) },

            // ========== 唇颌 ==========
            { "MouthWidth", new SliderMeta("嘴巴宽度", VanillaCat.LipJaw) },
            { "MouthHeight", new SliderMeta("嘴巴高低位置", VanillaCat.LipJaw) },
            { "MouthDepth", new SliderMeta("嘴巴前后凸度", VanillaCat.LipJaw) },
            { "MouthOuterCornerHeight", new SliderMeta("嘴角上扬/下垂", VanillaCat.LipJaw) },
            { "MouthInflatedShape", new SliderMeta("嘴周饱满度", VanillaCat.LipJaw) },
            { "UpperLipSize", new SliderMeta("上唇厚度", VanillaCat.LipJaw) },
            { "UpperLipWidth", new SliderMeta("上唇宽度", VanillaCat.LipJaw) },
            { "UpperLipDepth", new SliderMeta("上唇前凸", VanillaCat.LipJaw) },
            { "UpperLipMiddleHeight", new SliderMeta("上唇中/人中高度", VanillaCat.LipJaw) },
            { "LowerLipSize", new SliderMeta("下唇厚度", VanillaCat.LipJaw) },
            { "LowerLipWidth", new SliderMeta("下唇宽度", VanillaCat.LipJaw) },
            { "LowerLipDepth", new SliderMeta("下唇前凸", VanillaCat.LipJaw) },
            { "LowerLipMiddleHeight", new SliderMeta("下唇中部高低", VanillaCat.LipJaw) },
            { "LipsFullShape", new SliderMeta("丰满厚唇型", VanillaCat.LipJaw) },
            { "LipsPlumpShape", new SliderMeta("嘟嘟饱满唇型", VanillaCat.LipJaw) },
            { "LipsTripleLobeShape", new SliderMeta("M唇 / 唇珠轮廓", VanillaCat.LipJaw) },
            { "LipsThickOutterShape", new SliderMeta("唇外侧丰厚型", VanillaCat.LipJaw) },
            { "LipsThinOutterShape", new SliderMeta("唇外侧纤薄型", VanillaCat.LipJaw) },
            { "ShowTeeth", new SliderMeta("露齿程度", VanillaCat.LipJaw) },
            { "TeethDepthTemporary", new SliderMeta("牙齿前后深浅", VanillaCat.LipJaw) },
            { "ChinWidth", new SliderMeta("下巴宽度", VanillaCat.LipJaw) },
            { "ChinHeight", new SliderMeta("下巴长度/高低", VanillaCat.LipJaw) },
            { "ChinDepth", new SliderMeta("下巴前翘/后缩", VanillaCat.LipJaw) },
            { "ChinRoundShape", new SliderMeta("圆润下巴型", VanillaCat.LipJaw) },
            { "ChinCleftShape", new SliderMeta("下巴中沟（W下巴）", VanillaCat.LipJaw) },
            { "ChinDimple", new SliderMeta("下巴凹窝", VanillaCat.LipJaw) },
            { "DoubleChin", new SliderMeta("双下巴", VanillaCat.LipJaw) },
            { "JawWidth", new SliderMeta("下颌宽度", VanillaCat.LipJaw) },
            { "JawHeight", new SliderMeta("下颌高度", VanillaCat.LipJaw) },
            { "JawDepth", new SliderMeta("下颌前后深度", VanillaCat.LipJaw) },
            { "JawlineWidth", new SliderMeta("下颌线宽度", VanillaCat.LipJaw) },
            { "JawlineHeight", new SliderMeta("下颌折角高低", VanillaCat.LipJaw) },
            { "JawlineDepth", new SliderMeta("下颌线深浅", VanillaCat.LipJaw) },
            { "JawlineShape", new SliderMeta("下颌线棱角感", VanillaCat.LipJaw) },

            // ========== 脸头耳颈 ==========
            { "FaceHeight", new SliderMeta("脸型长度", VanillaCat.FaceHead) },
            { "FaceDepth", new SliderMeta("面部立体度/前后", VanillaCat.FaceHead) },
            { "HeadSize", new SliderMeta("头部整体大小", VanillaCat.FaceHead) },
            { "HeadWidth", new SliderMeta("头颅宽度", VanillaCat.FaceHead) },
            { "HeadLenght", new SliderMeta("头颅前后长度（颅顶/后脑）", VanillaCat.FaceHead) },
            { "HeadMove", new SliderMeta("头部前后位移", VanillaCat.FaceHead) },
            { "ForeheadHeight", new SliderMeta("额头高低/发际线", VanillaCat.FaceHead) },
            { "ForeheadDepth", new SliderMeta("额头饱满/前后", VanillaCat.FaceHead) },
            { "ForeheadProminentArchShape", new SliderMeta("眉弓突出度", VanillaCat.FaceHead) },
            { "CheekbonesSize", new SliderMeta("颧骨突出/大小", VanillaCat.FaceHead) },
            { "CheekbonesHeight", new SliderMeta("颧骨高低", VanillaCat.FaceHead) },
            { "CheekSize", new SliderMeta("脸颊胖瘦/肉感", VanillaCat.FaceHead) },
            { "CheekHeight", new SliderMeta("脸颊高低", VanillaCat.FaceHead) },
            { "EarSize", new SliderMeta("耳朵大小", VanillaCat.FaceHead) },
            { "EarPositionHeight", new SliderMeta("耳朵高低", VanillaCat.FaceHead) },
            { "EarPositionDepth", new SliderMeta("耳朵前后位置", VanillaCat.FaceHead) },
            { "EarRotation", new SliderMeta("招风耳/贴耳角度", VanillaCat.FaceHead) },
            { "BottomEar", new SliderMeta("耳垂大小", VanillaCat.FaceHead) },
            { "NeckSize", new SliderMeta("脖子粗细", VanillaCat.FaceHead) },
            { "AdamsApple", new SliderMeta("喉结突出度", VanillaCat.FaceHead) },

            // ========== 系统/年龄阶段内部拉杆（默认隐藏，搜索时可见） ==========
            { "Age", new SliderMeta("年龄变化（系统）", VanillaCat.BodyLimb, true) },
            { "Puberty", new SliderMeta("青春期变化（系统）", VanillaCat.BodyLimb, true) },
            { "BodyChild", new SliderMeta("儿童体型（系统）", VanillaCat.BodyLimb, true) },
            { "KidFeatures", new SliderMeta("儿童面相（系统）", VanillaCat.FaceHead, true) },
            { "ElderFeatures", new SliderMeta("老年特征（系统）", VanillaCat.FaceHead, true) },
            { "BabyDeformation", new SliderMeta("婴儿变形（系统）", VanillaCat.BodyLimb, true) },
            { "BabyDeformationBasedOnAge", new SliderMeta("婴儿年龄变形（系统）", VanillaCat.BodyLimb, true) },
            { "HeightBasedOnAge", new SliderMeta("年龄身高曲线（系统）", VanillaCat.BodyLimb, true) },
            { "HeadChildExtraSize", new SliderMeta("儿童头身比（系统）", VanillaCat.FaceHead, true) },
            { "LimbSizeBasedOnHeight", new SliderMeta("身高四肢联动（系统）", VanillaCat.BodyLimb, true) },
            { "Position", new SliderMeta("基准位置（系统）", VanillaCat.BodyLimb, true) }
        };

        // 系统内部控制拉杆（年龄/身高联动/婴儿变形等）：绝不写入预设，也不在套用预设时覆盖
        public static bool IsSystemSlider(string displayName, ulong guid)
        {
            // 4 个 0/1 体型底座开关允许随预设安全保存和切换
            if (guid == FaceLabRuntime.GUID_SLIDER_PEC
                || guid == FaceLabRuntime.GUID_SLIDER_BREAST
                || guid == FaceLabRuntime.GUID_SLIDER_BOTTOM_MASC
                || guid == FaceLabRuntime.GUID_SLIDER_BOTTOM_FEM)
                return false;

            if (string.IsNullOrEmpty(displayName)) return false;
            if (displayName.Equals("Pec", StringComparison.OrdinalIgnoreCase)
                || displayName.Equals("Breast", StringComparison.OrdinalIgnoreCase)
                || displayName.Equals("BottomMasculine", StringComparison.OrdinalIgnoreCase)
                || displayName.Equals("BottomFeminine", StringComparison.OrdinalIgnoreCase))
                return false;

            if (displayName.Equals("ShowTeeth", StringComparison.OrdinalIgnoreCase)
                || displayName.Equals("TeethDepthTemporary", StringComparison.OrdinalIgnoreCase))
                return true;

            SliderMeta m = GetMeta(displayName);
            return m != null && m.HiddenByDefault;
        }

        public static SliderMeta GetMeta(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return new SliderMeta("未知拉杆", VanillaCat.All, false);

            SliderMeta m;
            if (Map.TryGetValue(displayName, out m)) return m;

            // 幼儿专属拉杆自动识别并默认隐藏（搜 Toddler 或 幼儿 可见）
            if (displayName.StartsWith("Toddler", StringComparison.OrdinalIgnoreCase))
            {
                string sub = displayName.Substring(7);
                SliderMeta subM;
                if (Map.TryGetValue(sub, out subM))
                    return new SliderMeta("幼儿·" + subM.Zh, subM.Cat, true);
                return new SliderMeta("幼儿·" + sub, GuessCategory(sub), true);
            }

            return new SliderMeta(displayName, GuessCategory(displayName), false);
        }

        public static string GetLocalizedName(string displayName)
        {
            SliderMeta m = GetMeta(displayName);
            FaceLabLang lang = FaceLabI18n.Current;
            if (lang == FaceLabLang.ZhCN) return m.Zh;
            if (lang == FaceLabLang.ZhTW) return FaceLabI18n.ToTraditional(m.Zh);
            if (lang == FaceLabLang.En) return ToReadableEnglish(displayName);
            if (lang == FaceLabLang.Ja) return ToJapanese(displayName);
            if (lang == FaceLabLang.Ko) return ToKorean(displayName);
            return m.Zh;
        }

        private static string ToReadableEnglish(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Slider";
            string s = raw
                .Replace("Outter", "Outer")
                .Replace("Lenght", "Length")
                .Replace("Masculin", "Masculine")
                .Replace("Eyelide", "Eyelid")
                .Replace("MiddleHeigh", "MiddleHeight")
                .Replace("AdamsApple", "Adam's Apple")
                .Replace("NoseShapeTipPointy", "Pointy Nose Tip")
                .Replace("NoseShapeTipRound", "Round Nose Tip")
                .Replace("LipsTripleLobeShape", "Cupid's Bow (M-Lips)")
                .Replace("EyeEpicanthalFoldShape", "Epicanthal Fold")
                .Replace("EyeHoodedShape", "Hooded Eyelids")
                .Replace("EyeFoldHighShape", "High Eyelid Fold")
                .Replace("PecFat", "Pec Fullness / Fat")
                .Replace("PecSize", "Pec Size")
                .Replace("PecHeight", "Pec Height")
                .Replace("PecWidth", "Pec Width");

            // 驼峰拆分加空格
            System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (i > 0 && char.IsUpper(c) && (char.IsLower(s[i - 1]) || (i + 1 < s.Length && char.IsLower(s[i + 1]) && char.IsUpper(s[i - 1]))))
                    sb.Append(' ');
                else if (i > 0 && char.IsDigit(c) && !char.IsDigit(s[i - 1]))
                    sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static readonly string[,] JaTokens = new string[,]
        {
            { "Toddler", "幼児・" }, { "Eyebrow", "眉" }, { "UpperEyelid", "上まぶた" }, { "LowerEyelid", "下まぶた" },
            { "UpperLip", "上唇" }, { "LowerLip", "下唇" }, { "Cheekbones", "頬骨" }, { "Cheek", "頬" },
            { "Forehead", "額" }, { "Jawline", "フェイスライン" }, { "Jaw", "顎" }, { "DoubleChin", "二重顎" },
            { "Chin", "顎先" }, { "Nostrils", "鼻孔" }, { "Nostril", "小鼻" }, { "NoseBridge", "鼻筋" },
            { "NoseRoot", "鼻根" }, { "NoseTip", "鼻先" }, { "Nose", "鼻" }, { "Lips", "唇" }, { "Mouth", "口" },
            { "Eye", "目" }, { "Ear", "耳" }, { "Head", "頭" }, { "Face", "顔" }, { "Neck", "首" },
            { "AdamsApple", "喉仏" }, { "Teeth", "歯" }, { "Pec", "男性胸筋" }, { "Breast", "バスト" },
            { "Chest", "胸郭" }, { "RibCage", "肋骨" }, { "Shoulder", "肩" }, { "Trapezius", "僧帽筋" },
            { "Back", "背中" }, { "Posture", "姿勢" }, { "Waist", "ウエスト" }, { "Stomach", "お腹" },
            { "Navel", "へそ" }, { "Bottom", "ヒップ" }, { "Crotch", "股部" }, { "Forearm", "前腕" },
            { "Arm", "上腕" }, { "Hand", "手" }, { "Thigh", "太もも" }, { "Calves", "ふくらはぎ" },
            { "Foot", "足" }, { "OverallBody", "全身" }, { "Overall", "全体" }, { "Body", "体型" },
            { "Masculine", "(男)" }, { "Masculin", "(男)" }, { "Feminine", "(女)" },
            { "InnerCorner", "内側" }, { "OutterCorner", "外側" }, { "OuterCorner", "外側" },
            { "Middle", "中央" }, { "Position", "位置" }, { "Curvature", "カーブ" }, { "Rotation", "角度" },
            { "Height", "高さ" }, { "Width", "幅" }, { "Depth", "深さ/前後" }, { "Size", "大きさ" },
            { "Length", "長さ" }, { "Lenght", "長さ" }, { "Muscular", "筋肉" }, { "Muscle", "筋肉" },
            { "Fat", "脂肪" }, { "Weight", "体重" }, { "Bump", "隆起" }, { "Straight", "直線" },
            { "Pointy", "尖り" }, { "Round", "丸み" }, { "Shape", "形" }, { "Flat", "平坦" },
            { "Pinch", "引き締め" }, { "Fold", "二重" }, { "Hooded", "奥二重" }, { "Epicanthal", "蒙古襞" },
            { "Dimple", "えくぼ" }, { "Cleft", "割れ" }, { "Pregnant", "妊娠" }, { "MasterSlider", "全体" },
            { "Slider", "" }, { "Correction", "補正" }, { "Split", "溝" }, { "Show", "表示" },
            { "Full", "厚め" }, { "Plump", "ぷっくり" }, { "TripleLobe", "M字" }, { "ThickOutter", "外側厚め" },
            { "ThinOutter", "外側薄め" }, { "Inflated", "ふっくら" }, { "ProminentArch", "眉骨突出" }
        };

        private static readonly string[,] KoTokens = new string[,]
        {
            { "Toddler", "유아·" }, { "Eyebrow", "눈썹" }, { "UpperEyelid", "윗눈꺼풀" }, { "LowerEyelid", "아랫눈꺼풀" },
            { "UpperLip", "윗입술" }, { "LowerLip", "아랫입술" }, { "Cheekbones", "광대" }, { "Cheek", "볼" },
            { "Forehead", "이마" }, { "Jawline", "턱선" }, { "Jaw", "턱" }, { "DoubleChin", "이중턱" },
            { "Chin", "턱끝" }, { "Nostrils", "콧구멍" }, { "Nostril", "콧볼" }, { "NoseBridge", "콧대" },
            { "NoseRoot", "미간/콧뿌리" }, { "NoseTip", "코끝" }, { "Nose", "코" }, { "Lips", "입술" }, { "Mouth", "입" },
            { "Eye", "눈" }, { "Ear", "귀" }, { "Head", "머리" }, { "Face", "얼굴" }, { "Neck", "목" },
            { "AdamsApple", "목젖" }, { "Teeth", "치아" }, { "Pec", "남성 흉근" }, { "Breast", "가슴" },
            { "Chest", "흉곽" }, { "RibCage", "갈비뼈" }, { "Shoulder", "어깨" }, { "Trapezius", "승모근" },
            { "Back", "등" }, { "Posture", "자세" }, { "Waist", "허리" }, { "Stomach", "복부" },
            { "Navel", "배꼽" }, { "Bottom", "엉덩이" }, { "Crotch", "하체 중앙" }, { "Forearm", "아래팔" },
            { "Arm", "팔뚝" }, { "Hand", "손" }, { "Thigh", "허벅지" }, { "Calves", "종아리" },
            { "Foot", "발" }, { "OverallBody", "전신" }, { "Overall", "전체" }, { "Body", "체형" },
            { "Masculine", "(남)" }, { "Masculin", "(남)" }, { "Feminine", "(여)" },
            { "InnerCorner", "안쪽" }, { "OutterCorner", "바깥쪽" }, { "OuterCorner", "바깥쪽" },
            { "Middle", "중앙" }, { "Position", "위치" }, { "Curvature", "곡선" }, { "Rotation", "각도" },
            { "Height", "높이" }, { "Width", "너비" }, { "Depth", "깊이" }, { "Size", "크기" },
            { "Length", "길이" }, { "Lenght", "길이" }, { "Muscular", "근육" }, { "Muscle", "근육" },
            { "Fat", "지방" }, { "Weight", "체중" }, { "Bump", "매부리" }, { "Straight", "직선" },
            { "Pointy", "뾰족함" }, { "Round", "둥근형" }, { "Shape", "모양" }, { "Flat", "평평함" },
            { "Pinch", "조임" }, { "Fold", "쌍꺼풀" }, { "Hooded", "속쌍" }, { "Epicanthal", "몽고주름" },
            { "Dimple", "보조개" }, { "Cleft", "갈라짐" }, { "Pregnant", "임신" }, { "MasterSlider", "전체" },
            { "Slider", "" }, { "Correction", "보정" }, { "Split", "라인" }, { "Show", "노출" },
            { "Full", "도톰함" }, { "Plump", "볼륨" }, { "TripleLobe", "입술산" }, { "ThickOutter", "바깥 두껍게" },
            { "ThinOutter", "바깥 얇게" }, { "Inflated", "입가 볼륨" }, { "ProminentArch", "눈썹뼈 돌출" }
        };

        private static string ReplaceTokens(string raw, string[,] table)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            string s = raw.Replace("Eyelide", "Eyelid").Replace("MiddleHeigh", "MiddleHeight").Replace("Outter", "Outer");
            int n = table.GetLength(0);
            for (int i = 0; i < n; i++)
            {
                string k = table[i, 0];
                string v = table[i, 1];
                if (s.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                    s = ReplaceOrdinalIgnoreCase(s, k, v + " ");
            }
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            return s.Trim();
        }

        private static string ReplaceOrdinalIgnoreCase(string src, string find, string rep)
        {
            int idx = src.IndexOf(find, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return src;
            var sb = new System.Text.StringBuilder();
            int pos = 0;
            while (idx >= 0)
            {
                sb.Append(src, pos, idx - pos);
                sb.Append(rep);
                pos = idx + find.Length;
                idx = src.IndexOf(find, pos, StringComparison.OrdinalIgnoreCase);
            }
            sb.Append(src, pos, src.Length - pos);
            return sb.ToString();
        }

        private static string ToJapanese(string raw) { return ReplaceTokens(raw, JaTokens); }
        private static string ToKorean(string raw) { return ReplaceTokens(raw, KoTokens); }

        private static VanillaCat GuessCategory(string name)
        {
            string low = (name ?? "").ToLowerInvariant();
            if (low.Contains("eye") || low.Contains("brow") || low.Contains("lid")) return VanillaCat.EyeBrow;
            if (low.Contains("nose") || low.Contains("nostril")) return VanillaCat.Nose;
            if (low.Contains("lip") || low.Contains("mouth") || low.Contains("chin") || low.Contains("jaw") || low.Contains("teeth")) return VanillaCat.LipJaw;
            if (low.Contains("face") || low.Contains("head") || low.Contains("cheek") || low.Contains("ear") || low.Contains("forehead") || low.Contains("neck")) return VanillaCat.FaceHead;
            if (low.Contains("pec") || low.Contains("breast") || low.Contains("chest") || low.Contains("shoulder") || low.Contains("back") || low.Contains("rib") || low.Contains("trapez")) return VanillaCat.ChestBack;
            return VanillaCat.BodyLimb;
        }
    }
}
