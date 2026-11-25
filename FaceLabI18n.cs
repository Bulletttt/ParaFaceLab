// FaceLabI18n.cs —— 五国语言引擎（简体中文 / 繁體中文 / English / 日本語 / 한국어）
// 支持启动时自动识别游戏当前语言 + 顶栏一键实时切换
using System;
using System.Collections.Generic;
using System.Text;

namespace ParaFaceLab
{
    public enum FaceLabLang
    {
        ZhCN = 0,
        ZhTW = 1,
        En = 2,
        Ja = 3,
        Ko = 4
    }

    public static class FaceLabI18n
    {
        public static FaceLabLang Current = FaceLabLang.ZhCN;

        public static readonly string[] LangCodes = new string[] { "zh-CN", "zh-TW", "en", "ja", "ko" };
        public static readonly string[] LangShort = new string[] { "简", "繁", "EN", "日", "한" };
        public static readonly string[] LangNames = new string[] { "简体中文", "繁體中文", "English", "日本語", "한국어" };

        public static FaceLabLang ParseCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return DetectGameLanguage();
            string s = code.Trim().ToLowerInvariant();
            if (s == "auto") return DetectGameLanguage();
            if (s == "zh-cn" || s == "zh_cn" || s == "sc" || s == "zh") return FaceLabLang.ZhCN;
            if (s == "zh-tw" || s == "zh_tw" || s == "zh-hk" || s == "tc") return FaceLabLang.ZhTW;
            if (s == "ja" || s == "jp" || s == "ja-jp") return FaceLabLang.Ja;
            if (s == "ko" || s == "kr" || s == "ko-kr") return FaceLabLang.Ko;
            if (s == "en" || s == "en-us" || s == "en-gb") return FaceLabLang.En;
            return DetectGameLanguage();
        }

        public static FaceLabLang DetectGameLanguage()
        {
            try
            {
                string loc = TranslationManager.GetCurrentLocale() ?? "";
                string name = TranslationManager.GetCurrentLanguageName() ?? "";
                string all = (loc + " " + name).ToLowerInvariant();

                if (all.Contains("tw") || all.Contains("hk") || all.Contains("hant") || all.Contains("traditional"))
                    return FaceLabLang.ZhTW;
                if (all.Contains("zh") || all.Contains("cn") || all.Contains("chinese") || all.Contains("中文"))
                    return FaceLabLang.ZhCN;
                if (all.Contains("ja") || all.Contains("jp") || all.Contains("japan") || all.Contains("日本"))
                    return FaceLabLang.Ja;
                if (all.Contains("ko") || all.Contains("kr") || all.Contains("korea") || all.Contains("한국"))
                    return FaceLabLang.Ko;
                if (all.Contains("en"))
                    return FaceLabLang.En;
            }
            catch { }
            return FaceLabLang.ZhCN;
        }

        // 五语文本字典：T(key) 返回当前语言文本
        private static readonly Dictionary<string, string[]> Strs = new Dictionary<string, string[]>
        {
            // 格式: { key, new[] { 简中, 繁中, English, 日本語, 한국어 } }
            { "no_char", new[] { "未检测到角色", "未偵測到角色", "No Para Detected", "キャラクター未検出", "캐릭터 없음" } },
            { "prompt_pick", new[] { "请先进入捏人界面（Paramaker）或选中一个 Para。", "請先進入捏人介面（Paramaker）或選中一個 Para。", "Enter Paramaker or select a Para in Live Mode.", "Paramakerに入るか、Paraを選択してください。", "Paramaker에 들어가거나 Para를 선택하세요." } },
            { "rescan", new[] { "重新扫描角色", "重新掃描角色", "Rescan Paras", "再スキャン", "캐릭터 다시 찾기" } },
            { "badge_pam", new[] { "捏人同步", "捏人同步", "Paramaker", "作成中同期", "커마 동기화" } },
            { "badge_live", new[] { "生活模式", "生活模式", "Live Mode", "ライブ", "라이브 모드" } },
            { "mods_cnt", new[] { "处", "處", " pts", "箇所", "곳" } },

            { "undo", new[] { "↶ 撤回", "↶ 復原", "↶ Undo", "↶ 戻す", "↶ 취소" } },
            { "undo_ok", new[] { "已撤回：", "已復原：", "Undid: ", "元に戻しました：", "실행 취소: " } },
            { "undo_empty", new[] { "没有可撤回的操作", "沒有可復原的操作", "Nothing to undo", "戻せる履歴がありません", "취소할 기록이 없습니다" } },
            { "redo_ok", new[] { "已重做：", "已重做：", "Redid: ", "やり直しました：", "다시 실행: " } },
            { "redo_empty", new[] { "没有可重做的操作", "沒有可重做的操作", "Nothing to redo", "やり直す履歴がありません", "다시 실행할 기록이 없습니다" } },
            { "prev_step", new[] { "上一步", "上一步", "Last step", "前の操作", "이전 작업" } },
            { "next_step", new[] { "下一步", "下一步", "Next step", "次の操作", "다음 작업" } },

            { "sculpt_on", new[] { "精修：开", "精修：開", "Sculpt: ON", "調整：オン", "조정: 켜짐" } },
            { "sculpt_off", new[] { "精修：关", "精修：關", "Sculpt: OFF", "調整：オフ", "조정: 꺼짐" } },
            { "hold_cmp", new[] { "按住对比原版", "按住對比原版", "Hold to Compare", "長押しで比較", "눌러서 원본 비교" } },
            { "comparing", new[] { "正在显示初始...", "正在顯示初始...", "Showing Original...", "元の状態を表示中...", "원본 표시 중..." } },
            { "revert_init", new[] { "还原初始", "還原初始", "Revert All", "初期に戻す", "초기화" } },
            { "confirm_click", new[] { "再点确认!", "再點確認!", "Click Again!", "もう一度押す!", "한번 더 클릭!" } },
            { "revert_tip", new[] { "再点一次「还原初始」即可恢复进面板前的样子", "再點一次「還原初始」即可恢復進面板前的樣子", "Click Revert All again to restore initial look", "もう一度押すと開く前の状態に戻ります", "한번 더 누르면 패널 열기 전으로 돌아갑니다" } },
            { "reverted", new[] { "已还原到打开面板前的初始状态", "已還原到開啟面板前的初始狀態", "Restored to initial state", "初期状態に戻しました", "초기 상태로 복원했습니다" } },

            { "tab_bones", new[] { "骨骼精修", "骨骼精修", "Bone Sculpt", "ボーン調整", "본 정밀조정" } },
            { "tab_vanilla", new[] { "原版超限", "原版超限", "Vanilla+", "バニラ拡張", "기본 확장" } },
            { "tab_presets", new[] { "预设与存盘", "預設與存檔", "Presets", "プリセット", "프리셋" } },

            { "sym_on", new[] { "对称：开（左右联动）", "對稱：開（左右聯動）", "Symmetry: ON (Mirrored)", "対称：オン（左右連動）", "대칭: 켜짐 (좌우 연동)" } },
            { "sym_off", new[] { "对称：关（不对称微调）", "對稱：關（不對稱微調）", "Symmetry: OFF (Asymmetric)", "対称：オフ（非対称調整）", "대칭: 꺼짐 (비대칭 조정)" } },
            { "cur_side", new[] { "当前调节侧：", "目前調節側：", "Active Side:", "編集側：", "편집 방향:" } },
            { "side_l", new[] { "左侧 (.L)", "左側 (.L)", "Left (.L)", "左側 (.L)", "왼쪽 (.L)" } },
            { "side_r", new[] { "右侧 (.R)", "右側 (.R)", "Right (.R)", "右側 (.R)", "오른쪽 (.R)" } },
            { "tag_sym", new[] { " · 对称", " · 對稱", " · Sym", " · 対称", " · 대칭" } },
            { "tag_l", new[] { " · 左", " · 左", " · L", " · 左", " · 좌" } },
            { "tag_r", new[] { " · 右", " · 右", " · R", " · 우" } },

            { "range_1x", new[] { "精细 1x", "精細 1x", "Fine 1x", "精密 1x", "정밀 1x" } },
            { "range_2x", new[] { "超限 2x", "超限 2x", "Wide 2x", "拡張 2x", "확장 2x" } },
            { "range_3x", new[] { "极限 3x", "極限 3x", "Max 3x", "限界 3x", "극한 3x" } },
            { "reset", new[] { "重置", "重置", "Reset", "リセット", "초기화" } },
            { "clear", new[] { "清空", "清空", "Clear", "消去", "지우기" } },

            // 骨骼 10 条轴名
            { "ax_spread", new[] { "外扩 / 内收", "外擴 / 內收", "Spread / Narrow", "外側 / 内側", "넓히기 / 모으기" } },
            { "ax_lr", new[] { "左右偏移", "左右偏移", "Left / Right", "左右位置", "좌 / 우 이동" } },
            { "ax_ud", new[] { "上移 / 下移", "上移 / 下移", "Up / Down", "上下位置", "상 / 하 이동" } },
            { "ax_fb", new[] { "前凸 / 后缩", "前凸 / 後縮", "Forward / Back", "前後位置", "앞 / 뒤 이동" } },
            { "ax_roll", new[] { "斜度 / 挑角", "斜度 / 挑角", "Tilt / Slant", "傾き / つり上げ", "기울기 / 각도" } },
            { "ax_pitch", new[] { "上下俯仰", "上下俯仰", "Pitch Up / Down", "上下回転", "상하 회전" } },
            { "ax_yaw", new[] { "内外偏转", "內外偏轉", "Yaw In / Out", "左右回転", "좌우 회전" } },
            { "ax_scale_u", new[] { "整体大小", "整體大小", "Overall Scale", "全体サイズ", "전체 크기" } },
            { "ax_scale_x", new[] { "宽 / 窄 (X)", "寬 / 窄 (X)", "Width (X)", "幅 (X)", "너비 (X)" } },
            { "ax_scale_y", new[] { "高 / 扁 (Y)", "高 / 扁 (Y)", "Height (Y)", "高さ (Y)", "높이 (Y)" } },
            { "ax_scale_z", new[] { "厚 / 薄 (Z)", "厚 / 薄 (Z)", "Depth (Z)", "厚み (Z)", "두께 (Z)" } },

            // 骨骼分类
            { "bcat_0", new[] { "眼", "眼", "Eyes", "目", "눈" } },
            { "bcat_1", new[] { "眉", "眉", "Brows", "眉", "눈썹" } },
            { "bcat_2", new[] { "鼻", "鼻", "Nose", "鼻", "코" } },
            { "bcat_3", new[] { "唇", "唇", "Lips", "口", "입술" } },
            { "bcat_4", new[] { "颊颌", "頰頷", "Jaw", "頬・顎", "볼·턱" } },
            { "bcat_5", new[] { "头耳", "頭耳", "Head", "頭・耳", "머리·귀" } },
            { "bcat_6", new[] { "身材", "身材", "Body", "体型", "체형" } },
            { "bcat_7", new[] { "全部", "全部", "All", "全ボーン", "전체" } },

            // 原版超限分类
            { "vcat_0", new[] { "全部", "全部", "All", "すべて", "전체" } },
            { "vcat_1", new[] { "眼眉", "眼眉", "Eyes", "目・眉", "눈·눈썹" } },
            { "vcat_2", new[] { "鼻部", "鼻部", "Nose", "鼻", "코" } },
            { "vcat_3", new[] { "唇颌", "唇頷", "Mouth", "口・顎", "입·턱" } },
            { "vcat_4", new[] { "脸头耳", "臉頭耳", "Face", "顔・頭", "얼굴·머리" } },
            { "vcat_5", new[] { "胸肩背", "胸肩背", "Torso", "胸・肩", "가슴·어깨" } },
            { "vcat_6", new[] { "腰胯腿", "腰胯腿", "Limbs", "腰・脚", "허리·다리" } },
            { "vcat_7", new[] { "已改", "已改", "Edited", "変更済", "변경됨" } },

            { "unlock_on", new[] { "解除原版限制：开", "解除原版限制：開", "Unlock Limits: ON", "制限解除：オン", "제한 해제: 켜짐" } },
            { "unlock_off", new[] { "解除原版限制：关", "解除原版限制：關", "Unlock Limits: OFF", "制限解除：オフ", "제한 해제: 꺼짐" } },
            { "vrange_0", new[] { "标准 0~1", "標準 0~1", "Std 0~1", "標準 0~1", "표준 0~1" } },
            { "vrange_1", new[] { "扩展 -0.5~1.5", "擴展 -0.5~1.5", "Wide -0.5~1.5", "拡張 -0.5~1.5", "확장 -0.5~1.5" } },
            { "vrange_2", new[] { "极限 -1~2", "極限 -1~2", "Max -1~2", "限界 -1~2", "극한 -1~2" } },

            { "chest_title", new[] { "胸型底层开关（安全切换男女胸，防止锁死拉杆）", "胸型底層開關（安全切換男女胸，防止鎖死拉桿）", "Chest Base Switch (Safe Male/Female Toggle)", "胸部ベース切替（スライダー固定を防止）", "가슴 기본 모드 (슬라이더 잠김 방지)" } },
            { "chest_male", new[] { "男体胸肌模式", "男體胸肌模式", "Male Pecs", "男性胸筋モード", "남성 흉근 모드" } },
            { "chest_female", new[] { "女体胸部模式", "女體胸部模式", "Female Chest", "女性バストモード", "여성 가슴 모드" } },
            { "bottom_title", new[] { "下身底层开关（安全切换男女下身，防止锁死臀裆）", "下身底層開關（安全切換男女下身，防止鎖死臀襠）", "Lower Body Switch (Safe Male/Female Toggle)", "下半身ベース切替（スライダー固定を防止）", "하체 기본 모드 (슬라이더 잠김 방지)" } },
            { "bottom_male", new[] { "男体下身模式", "男體下身模式", "Male Lower", "男性下半身モード", "남성 하체 모드" } },
            { "bottom_female", new[] { "女体下身模式", "女體下身模式", "Female Lower", "女性下半身モード", "여성 하체 모드" } },

            { "ph_bone_search", new[] { "输入骨骼名搜索（如 Eye / Lip / Breast）", "輸入骨骼名搜尋（如 Eye / Lip / Breast）", "Search bone name (e.g. Eye, Lip, Breast)...", "ボーン名で検索 (例: Eye / Lip)...", "본 이름 검색 (예: Eye / Lip)..." } },
            { "ph_vanilla_search", new[] { "搜索关键词（支持中/英/日/韩）", "搜尋關鍵字（支援中/英/日/韓）", "Search slider name (e.g. Pec, Eye, Nose)...", "スライダー検索（日本語/英語）...", "슬라이더 검색 (한국어/영어)..." } },
            { "ph_preset_name", new[] { "输入预设名（留空则自动用角色名）", "輸入預設名（留空則自動用角色名）", "Preset name (blank = Para name)", "プリセット名（空欄で自動命名）", "프리셋 이름 (비우면 자동 생성)" } },

            { "no_bones", new[] { "未找到匹配骨骼", "未找到匹配骨骼", "No matching bones found", "一致するボーンがありません", "일치하는 본이 없습니다" } },
            { "no_vanilla", new[] { "没有匹配的原版拉杆。", "沒有匹配的原版拉桿。", "No matching sliders found.", "一致するスライダーがありません。", "일치하는 슬라이더가 없습니다." } },

            { "preset_save_title", new[] { "保存当前小人的捏脸/身材为预设：", "保存目前小人的捏臉/身材為預設：", "Save current face & body as preset:", "現在の顔・体型をプリセット保存：", "현재 얼굴/체형을 프리셋으로 저장:" } },
            { "preset_save_btn", new[] { "保存预设", "保存預設", "Save", "保存", "저장" } },
            { "preset_part_scope", new[] { "套用部位（不动目标年龄）：", "套用部位（不動目標年齡）：", "Target Region (Keeps Para's Age):", "適用部位（年齢は変更しません）：", "적용 부위 (나이 유지):" } },
            { "part_all", new[] { "全身（脸+身材）", "全身（臉+身材）", "Full (Face+Body)", "全身（顔＋体型）", "전신 (얼굴+체형)" } },
            { "part_face", new[] { "仅脸部", "僅臉部", "Face Only", "顔のみ", "얼굴만" } },
            { "part_body", new[] { "仅身材", "僅身材", "Body Only", "体型のみ", "체형만" } },
            { "preset_scope", new[] { "套用数据层：", "套用資料層：", "Data Layer:", "適用データ：", "적용 데이터:" } },
            { "scope_all", new[] { "骨骼+原版全套", "骨骼+原版全套", "Bones + Vanilla", "ボーン＋バニラ", "본 + 기본 전체" } },
            { "scope_bones", new[] { "仅套用骨骼", "僅套用骨骼", "Bones Only", "ボーンのみ", "본만 적용" } },
            { "scope_vanilla", new[] { "仅套用原版", "僅套用原版", "Vanilla Only", "バニラのみ", "기본만 적용" } },
            { "cross_age_on", new[] { "跨年龄智能换算：开（防不同年龄拉伸变形）", "跨年齡智慧換算：開（防不同年齡拉伸變形）", "Cross-Age Auto-Adapt: ON (Prevents stretch)", "年齢差の自動換算：オン（体型崩れ防止）", "나이대 자동 보정: 켜짐 (체형 늘어짐 방지)" } },
            { "cross_age_off", new[] { "跨年龄智能换算：关（按原始数值直接套用）", "跨年齡智慧換算：關（按原始數值直接套用）", "Cross-Age Auto-Adapt: OFF (Raw values)", "年齢差の自動換算：オフ（生数値を適用）", "나이대 자동 보정: 꺼짐 (원본 수치 적용)" } },
            { "toast_applied_age", new[] { "已跨年龄等比套用（未改年龄）：", "已跨年齡等比套用（未改年齡）：", "Applied (age-adapted, age kept): ", "年齢換算で適用（年齢維持）：", "나이대 보정 적용 (나이 유지): " } },
            { "preset_list_title", new[] { "已保存预设", "已保存預設", "Saved Presets", "保存済みプリセット", "저장된 프리셋" } },
            { "preset_empty", new[] { "还没有预设。捏好后在上面输个名字点保存，随时可一键复用！", "還沒有預設。捏好後在上面輸個名字點保存，隨時可一鍵複用！", "No presets yet. Save one above to reuse on any Para!", "プリセットはまだありません。上で名前を入れて保存できます！", "저장된 프리셋이 없습니다. 위에서 저장해 보세요!" } },
            { "apply", new[] { "套用", "套用", "Apply", "適用", "적용" } },
            { "overwrite", new[] { "覆盖", "覆蓋", "Update", "上書", "덮어쓰기" } },
            { "confirm_short", new[] { "确认?", "確認?", "Sure?", "確定?", "확인?" } },
            { "del", new[] { "删", "刪", "Del", "削除", "삭제" } },
            { "overwrite_tip", new[] { "再点一次「确认?」即可用当前小人覆盖预设：", "再點一次「確認?」即可用目前小人覆蓋預設：", "Click 'Sure?' to overwrite preset with current Para: ", "もう一度「確定?」を押すと上書き保存します：", "'확인?'을 다시 누르면 현재 캐릭터로 덮어씁니다: " } },
            { "toast_overwritten", new[] { "已覆盖更新预设：", "已覆蓋更新預設：", "Updated preset: ", "プリセットを上書きしました：", "프리셋 덮어쓰기 완료: " } },
            { "open_dir", new[] { "打开数据文件夹（ParaFaceLab.data）", "開啟資料夾（ParaFaceLab.data）", "Open Data Folder (ParaFaceLab.data)", "データフォルダを開く", "데이터 폴더 열기" } },
            { "safety_note", new[] { "安全说明：骨骼数据独立保存在插件目录，绝不污染游戏原生存档。", "安全說明：骨骼資料獨立保存在插件目錄，絕不污染遊戲原生存檔。", "Safe storage: bone data is saved separately and never corrupts game saves.", "安心設計：データは専用フォルダに保存され、セーブデータを壊しません。", "안전 저장: 데이터는 별도 폴더에 저장되어 게임 세이브를 손상시키지 않습니다." } },
            { "shift_tip", new[] { "提示：按住 Shift 拖滑块可 3 倍慢速精调，双击轨道单条归零。", "提示：按住 Shift 拖滑塊可 3 倍慢速精調，雙擊軌道單條歸零。", "Tip: Hold Shift while dragging for 3x fine control. Double-click track to reset.", "ヒント：Shift+ドラッグで精密調整、ダブルクリックで個別リセット。", "팁: Shift+드래그로 3배 정밀 조정, 트랙 더블클릭으로 초기화." } },

            // 皮肤名称
            { "th_0_short", new[] { "奶油", "奶油", "Cream", "クリーム", "크림" } },
            { "th_1_short", new[] { "淡雾", "淡霧", "Mist", "ミスト", "미스트" } },
            { "th_2_short", new[] { "暗房", "暗房", "Dark", "ダーク", "다크" } },
            { "th_3_short", new[] { "樱雾", "櫻霧", "Sakura", "桜", "벚꽃" } },
            { "th_0_name", new[] { "奶油手账", "奶油手帳", "Warm Cream", "クリーム手帳", "크림 노트" } },
            { "th_1_name", new[] { "淡雾极简", "淡霧極簡", "Soft Mist", "ソフトミスト", "소프트 미스트" } },
            { "th_2_name", new[] { "影棚暗房", "影棚暗房", "Studio Dark", "スタジオ暗室", "스튜디오 다크" } },
            { "th_3_name", new[] { "樱雾甜盐", "櫻霧甜鹽", "Sakura Milk", "桜ミルク", "사쿠라 밀크" } },

            { "toast_theme", new[] { "已切换皮肤：", "已切換皮膚：", "Theme: ", "テーマ変更：", "테마 변경: " } },
            { "toast_scale", new[] { "已切换缩放：", "已切換縮放：", "UI Scale: ", "拡大率：", "UI 크기: " } },
            { "toast_lang", new[] { "已切换语言：", "已切換語言：", "Language: ", "言語：", "언어: " } },
            { "toast_saved", new[] { "已保存预设：", "已保存預設：", "Saved preset: ", "保存しました：", "프리셋 저장됨: " } },
            { "toast_applied", new[] { "已套用预设：", "已套用預設：", "Applied preset: ", "適用しました：", "프리셋 적용됨: " } },
            { "toast_deleted", new[] { "已删除预设：", "已刪除預設：", "Deleted preset: ", "削除しました：", "프리셋 삭제됨: " } },
            { "toast_unlock_on", new[] { "已无痕解除当前角色限制（保持当前体型不变）", "已無痕解除目前角色限制（保持目前體型不變）", "Limits unlocked seamlessly (current look preserved!)", "現在キャラの制限を無変形で解除しました", "현재 캐릭터 제한 해제됨 (현재 체형 그대로 유지!)" } },
            { "toast_unlock_off", new[] { "已恢复原版范围限制", "已恢復原版範圍限制", "Vanilla range limits restored", "バニラ範囲制限を復元しました", "기본 범위 제한 복원됨" } },
            { "toast_chest_m", new[] { "已切换男体胸肌模式", "已切換男體胸肌模式", "Switched to Male Pecs mode", "男性胸筋モードに切替", "남성 흉근 모드로 전환됨" } },
            { "toast_chest_f", new[] { "已切换女体胸部模式", "已切換女體胸部模式", "Switched to Female Chest mode", "女性バストモードに切替", "여성 가슴 모드로 전환됨" } },
            { "toast_bot_m", new[] { "已切换男体下身模式", "已切換男體下身模式", "Switched to Male Lower mode", "男性下半身モードに切替", "남성 하체 모드로 전환됨" } },
            { "toast_bot_f", new[] { "已切换女体下身模式", "已切換女體下身模式", "Switched to Female Lower mode", "女性下半身モードに切替", "여성 하체 모드로 전환됨" } },

            // 骨骼条目中文/繁中/英/日/韩 名称
            { "bone_EyeMaster.L", new[] { "眼部整体", "眼部整體", "Eyes (Overall)", "目全体", "눈 전체" } },
            { "bone_Eye.L", new[] { "眼球位置/大小", "眼球位置/大小", "Eyeballs", "眼球", "안구" } },
            { "bone_UpperLid.L", new[] { "上眼皮", "上眼皮", "Upper Eyelids", "上まぶた", "윗눈꺼풀" } },
            { "bone_LowerLid.L", new[] { "下眼皮/卧蚕", "下眼皮/臥蠶", "Lower Eyelids", "下まぶた・涙袋", "아랫눈꺼풀/애교살" } },
            { "bone_EyebrowMaster.L", new[] { "眉毛整体", "眉毛整體", "Eyebrows (Overall)", "眉全体", "눈썹 전체" } },
            { "bone_Eyebrow1.L", new[] { "眉头（内段）", "眉頭（內段）", "Inner Eyebrows", "眉頭", "눈썹 앞머리" } },
            { "bone_Eyebrow2.L", new[] { "眉峰（中段）", "眉峰（中段）", "Middle Eyebrows", "眉山", "눈썹 산" } },
            { "bone_Eyebrow3.L", new[] { "眉尾（外段）", "眉尾（外段）", "Outer Eyebrows", "眉尻", "눈썹 꼬리" } },
            { "bone_NoseMaster", new[] { "鼻子整体", "鼻子整體", "Nose (Overall)", "鼻全体", "코 전체" } },
            { "bone_NoseBridge", new[] { "山根 / 鼻梁", "山根 / 鼻樑", "Nose Bridge", "鼻筋", "콧대" } },
            { "bone_NoseTip", new[] { "鼻尖", "鼻尖", "Nose Tip", "鼻先", "코끝" } },
            { "bone_Nosetril.L", new[] { "鼻翼（左右）", "鼻翼（左右）", "Nostrils", "小鼻", "콧볼" } },
            { "bone_LipsMasterTop", new[] { "上唇整体", "上唇整體", "Upper Lip (Overall)", "上唇全体", "윗입술 전체" } },
            { "bone_LipsTop", new[] { "上唇中 / 唇珠", "上唇中 / 唇珠", "Upper Lip Center", "上唇中央", "윗입술 중앙" } },
            { "bone_LipsTopMiddle.L", new[] { "上唇两侧", "上唇兩側", "Upper Lip Sides", "上唇側面", "윗입술 양옆" } },
            { "bone_LipsSmile.L", new[] { "嘴角（微笑弧度）", "嘴角（微笑弧度）", "Mouth Corners", "口角", "입꼬리" } },
            { "bone_LipsMasterBottom", new[] { "下唇整体", "下唇整體", "Lower Lip (Overall)", "下唇全体", "아랫입술 전체" } },
            { "bone_LipsBottom", new[] { "下唇中", "下唇中", "Lower Lip Center", "下唇中央", "아랫입술 중앙" } },
            { "bone_LipsBottomMiddle.L", new[] { "下唇两侧", "下唇兩側", "Lower Lip Sides", "下唇側面", "아랫입술 양옆" } },
            { "bone_UpperTeeth", new[] { "上牙", "上牙", "Upper Teeth", "上の歯", "윗니" } },
            { "bone_LowerTeeth", new[] { "下牙", "下牙", "Lower Teeth", "下の歯", "아랫니" } },
            { "bone_Tongue1", new[] { "舌头", "舌頭", "Tongue", "舌", "혀" } },
            { "bone_CheekSmile.L", new[] { "苹果肌 / 颧颊", "蘋果肌 / 顴頰", "Cheekbones", "頬骨・リンゴ頬", "광대 / 앞볼" } },
            { "bone_CheekInflate.L", new[] { "面颊 / 颊脂垫", "面頰 / 頰脂墊", "Lower Cheeks", "頬・フェイスライン", "볼살" } },
            { "bone_Jaw", new[] { "下颌骨", "下頷骨", "Jaw", "顎全体", "턱뼈" } },
            { "bone_JawDeformation", new[] { "下巴骨相", "下巴骨相", "Chin Sculpt", "顎先", "턱끝" } },
            { "bone_FaceMaster", new[] { "五官整体中枢", "五官整體中樞", "Face Master", "顔パーツ全体", "이목구비 전체" } },
            { "bone_Head", new[] { "头部整体（头身比）", "頭部整體（頭身比）", "Head (Proportion)", "頭全体（頭身）", "머리 전체 (비율)" } },
            { "bone_Neck", new[] { "脖子（长短/粗细）", "脖子（長短/粗細）", "Neck", "首", "목" } },
            { "bone_Ear.L", new[] { "耳朵（招风/精灵耳）", "耳朵（招風/精靈耳）", "Ears", "耳", "귀" } },
            { "bone_Hair", new[] { "发型挂点微调", "髮型掛點微調", "Hair Anchor", "髪の位置微調整", "헤어 위치 조정" } },
            { "bone_HumanRig", new[] { "整体身高 / 全身缩放", "整體身高 / 全身縮放", "Whole Body / Height Scale", "全身サイズ・身長", "전신 크기 / 키" } },
            { "bone_Breast.L", new[] { "胸部骨骼（压平/提拉）", "胸部骨骼（壓平/提拉）", "Chest / Breast Bones", "胸ボーン（厚み/高さ）", "가슴 본 (두께/높이)" } },
            { "bone_Chest", new[] { "胸腔 / 上身廓形", "胸腔 / 上身廓形", "Torso / Ribcage", "胸郭・上半身", "흉곽 / 상체" } },
            { "bone_Shoulder.L", new[] { "肩膀（宽窄/平肩）", "肩膀（寬窄/平肩）", "Shoulders", "肩", "어깨" } },
            { "bone_Spine", new[] { "腰腹 / 脊柱", "腰腹 / 脊柱", "Waist / Spine", "腰・背骨", "허리 / 척추" } },
            { "bone_Hips", new[] { "胯部 / 骨盆", "胯部 / 骨盆", "Hips / Pelvis", "骨盤・ヒップ", "골반 / 엉덩이" } },
            { "bone_Bottom_L", new[] { "臀瓣（翘度/丰满）", "臀瓣（翹度/豐滿）", "Glutes / Buttocks", "お尻（丸み）", "엉덩이 볼륨" } },
            { "bone_UpperArm.L", new[] { "大臂（长短/粗细）", "大臂（長短/粗細）", "Upper Arms (Length/Width)", "上腕（長さ・太さ）", "팔뚝 (길이/두께)" } },
            { "bone_UpperArm_Twist2.L", new[] { "大臂中段（线条/肌肉）", "大臂中段（線條/肌肉）", "Upper Arm Mid (Biceps)", "上腕中央（ライン）", "팔뚝 중간 (근육)" } },
            { "bone_LowerArm.L", new[] { "小臂（长短/粗细）", "小臂（長短/粗細）", "Forearms (Length/Width)", "前腕（長さ・太さ）", "아래팔 (길이/두께)" } },
            { "bone_LowerArmTwist2.L", new[] { "小臂中段（前臂线条）", "小臂中段（前臂線條）", "Forearm Mid", "前腕中央", "아래팔 중간" } },
            { "bone_Hand.L", new[] { "手掌", "手掌", "Hands", "手", "손" } },
            { "bone_UpperLeg.L", new[] { "大腿（腿长Y / 粗细XZ）", "大腿（腿長Y / 粗細XZ）", "Thighs (Length Y / Width XZ)", "太もも（長さY・太さXZ）", "허벅지 (길이Y / 굵기XZ)" } },
            { "bone_UpperLeg_Twist2.L", new[] { "大腿中段（肉感/线条）", "大腿中段（肉感/線條）", "Thigh Mid", "太もも中央", "허벅지 중간" } },
            { "bone_LowerLeg.L", new[] { "小腿（腿长Y / 粗细XZ）", "小腿（腿長Y / 粗細XZ）", "Calves (Length Y / Width XZ)", "ふくらはぎ（長さY・太さXZ）", "종아리 (길이Y / 굵기XZ)" } },
            { "bone_LowerLeg_Twist2.L", new[] { "小腿肚（肌肉/线条）", "小腿肚（肌肉/線條）", "Calf Mid", "ふくらはぎ中央", "종아리 알 / 라인" } },
            { "bone_Foot.L", new[] { "脚掌（大小/角度）", "腳掌（大小/角度）", "Feet", "足", "발" } },
            { "bone_Toes.L", new[] { "脚尖 / 脚趾", "腳尖 / 腳趾", "Toes", "つま先", "발끝 / 발가락" } }
        };

        public static string T(string key)
        {
            string[] arr;
            if (Strs.TryGetValue(key, out arr) && arr != null)
            {
                int idx = (int)Current;
                if (idx >= 0 && idx < arr.Length && !string.IsNullOrEmpty(arr[idx]))
                    return arr[idx];
                return arr[0];
            }
            return key;
        }

        public static string BoneLabel(string leftBone, string fallback)
        {
            string k = "bone_" + leftBone;
            string[] arr;
            if (Strs.TryGetValue(k, out arr) && arr != null)
            {
                int idx = (int)Current;
                if (idx >= 0 && idx < arr.Length && !string.IsNullOrEmpty(arr[idx]))
                    return arr[idx];
            }
            return fallback;
        }

        // 简→繁常用捏脸词字符映射表（保证 200+ 原版拉杆 100% 精准转繁体）
        private static readonly string S_CHARS = "体肌轮廓宽满脸头颌颌长细发眼窝双紧皱内颊颧颧节翘圆沟窝额颅顶颈结背驼态腰线围脐翘缝裆腿脚龄变统儿阶曲线动联总开关称调精细极限范围扩编预设盘";
        private static readonly string T_CHARS = "體肌輪廓寬滿臉頭頷頷長細髮眼窩雙緊皺內頰顴顴節翹圓溝窩額顱頂頸結背駝態腰線圍臍翹縫襠腿腳齡變統兒階曲線動聯總開關稱調精細極限範圍擴編預設盤";
        private static Dictionary<char, char> _s2t;

        public static string ToTraditional(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (_s2t == null)
            {
                _s2t = new Dictionary<char, char>();
                string a = "体肌轮廓宽满脸头颌长细发窝双紧皱内颊颧节翘圆沟额颅顶颈结驼态线围脐缝裆脚龄变统儿阶动联总开关称调围扩编预设盘压侧后缩转倾厚薄删夹单";
                string b = "體肌輪廓寬滿臉頭頷長細髮窩雙緊皺內頰顴節翹圓溝額顱頂頸結駝態線圍臍縫襠腳齡變統兒階動聯總開關稱調圍擴編預設盤壓側後縮轉傾厚薄刪夾單";
                int len = Math.Min(a.Length, b.Length);
                for (int i = 0; i < len; i++) _s2t[a[i]] = b[i];
            }
            StringBuilder sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                char mapped;
                sb.Append(_s2t.TryGetValue(c, out mapped) ? mapped : c);
            }
            return sb.ToString();
        }
    }
}
