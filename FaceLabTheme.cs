// FaceLabTheme.cs —— 4 套完整抗锯齿圆角美化皮肤（奶油手账 / 淡雾极简 / 影棚暗房 / 樱雾甜盐）
using System;
using UnityEngine;

namespace ParaFaceLab
{
    public sealed class FaceLabPalette
    {
        public string Name;
        public string ShortName;
        public Color Bg;
        public Color HeaderBg;
        public Color Card;
        public Color SoftBox;
        public Color TabWrap;
        public Color Btn;
        public Color BtnHover;
        public Color BtnText;
        public Color Accent;
        public Color AccentSoft;
        public Color AccentSoftText;
        public Color Danger;
        public Color Track;
        public Color Border;
        public Color Text;
        public Color Sub;
        public Color InputBg;
        public Color ThumbRing;
    }

    public static class FaceLabThemes
    {
        public static readonly FaceLabPalette[] All = new FaceLabPalette[]
        {
            // 0: 奶油手账（Para 原生暖风）
            new FaceLabPalette
            {
                Name = "奶油手账",
                ShortName = "奶油",
                Bg = Hex("#FAF7F2", 0.97f),
                HeaderBg = Hex("#FFFDF9", 0.99f),
                Card = Hex("#FFFFFF", 0.99f),
                SoftBox = Hex("#F3EDE3", 0.98f),
                TabWrap = Hex("#EFE8DC", 0.98f),
                Btn = Hex("#EFE8DC", 1f),
                BtnHover = Hex("#E5DBCB", 1f),
                BtnText = Hex("#49423B", 1f),
                Accent = Hex("#5B8DBE", 1f),
                AccentSoft = Hex("#E3EEF8", 1f),
                AccentSoftText = Hex("#2F5A85", 1f),
                Danger = Hex("#D4656A", 1f),
                Track = Hex("#E5DCCF", 1f),
                Border = Hex("#E2D8C9", 1f),
                Text = Hex("#2F2A25", 1f),
                Sub = Hex("#7A7065", 1f),
                InputBg = Hex("#FFFFFF", 1f),
                ThumbRing = Hex("#FFFFFF", 1f)
            },
            // 1: 淡雾极简（低饱和白灰）
            new FaceLabPalette
            {
                Name = "淡雾极简",
                ShortName = "淡雾",
                Bg = Hex("#F3F5F8", 0.97f),
                HeaderBg = Hex("#F9FAFC", 0.99f),
                Card = Hex("#FFFFFF", 0.99f),
                SoftBox = Hex("#EAEFF5", 0.98f),
                TabWrap = Hex("#E5EAF1", 0.98f),
                Btn = Hex("#E7ECF3", 1f),
                BtnHover = Hex("#DCE4EF", 1f),
                BtnText = Hex("#3A4352", 1f),
                Accent = Hex("#6F9ED8", 1f),
                AccentSoft = Hex("#E3EEF9", 1f),
                AccentSoftText = Hex("#376398", 1f),
                Danger = Hex("#D6696E", 1f),
                Track = Hex("#DCE2EB", 1f),
                Border = Hex("#DCE3EC", 1f),
                Text = Hex("#1F2530", 1f),
                Sub = Hex("#687283", 1f),
                InputBg = Hex("#FFFFFF", 1f),
                ThumbRing = Hex("#FFFFFF", 1f)
            },
            // 2: 影棚暗房（专业 3D 护眼深灰）
            new FaceLabPalette
            {
                Name = "影棚暗房",
                ShortName = "暗房",
                Bg = Hex("#1B1F27", 0.97f),
                HeaderBg = Hex("#212630", 0.99f),
                Card = Hex("#252B36", 0.99f),
                SoftBox = Hex("#1F252F", 0.98f),
                TabWrap = Hex("#141820", 0.98f),
                Btn = Hex("#2E3543", 1f),
                BtnHover = Hex("#394254", 1f),
                BtnText = Hex("#DCE3EE", 1f),
                Accent = Hex("#72A8E8", 1f),
                AccentSoft = Hex("#26384E", 1f),
                AccentSoftText = Hex("#9AC6F8", 1f),
                Danger = Hex("#C9555A", 1f),
                Track = Hex("#374050", 1f),
                Border = Hex("#333B4B", 1f),
                Text = Hex("#EDF2F9", 1f),
                Sub = Hex("#929DB0", 1f),
                InputBg = Hex("#151921", 1f),
                ThumbRing = Hex("#1B1F27", 1f)
            },
            // 3: 樱雾甜盐（莫兰迪淡粉）
            new FaceLabPalette
            {
                Name = "樱雾甜盐",
                ShortName = "樱雾",
                Bg = Hex("#FBF6F8", 0.97f),
                HeaderBg = Hex("#FFFBFD", 0.99f),
                Card = Hex("#FFFFFF", 0.99f),
                SoftBox = Hex("#F5EAEF", 0.98f),
                TabWrap = Hex("#F1E4E9", 0.98f),
                Btn = Hex("#F1E4E9", 1f),
                BtnHover = Hex("#E8D6DE", 1f),
                BtnText = Hex("#574048", 1f),
                Accent = Hex("#D48398", 1f),
                AccentSoft = Hex("#F9E6EC", 1f),
                AccentSoftText = Hex("#A04861", 1f),
                Danger = Hex("#D65E68", 1f),
                Track = Hex("#EBD9E0", 1f),
                Border = Hex("#EAD9E0", 1f),
                Text = Hex("#36262C", 1f),
                Sub = Hex("#886E78", 1f),
                InputBg = Hex("#FFFFFF", 1f),
                ThumbRing = Hex("#FFFFFF", 1f)
            }
        };

        private static Color Hex(string hex, float a)
        {
            Color c;
            if (ColorUtility.TryParseHtmlString(hex, out c)) { c.a = a; return c; }
            return new Color(0.5f, 0.5f, 0.5f, a);
        }

        // 程序生成抗锯齿圆角 9-slice 贴图（零外部文件、极低内存、缩放不糊）
        public static Texture2D MakeRoundedTex(int size, int radius, Color fill, Color border, float borderPx = 1f)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] px = new Color[size * size];
            float half = (size - 1) * 0.5f;
            float r = Mathf.Clamp(radius, 1, size / 2 - 1);
            float bx = half - r;
            float by = half - r;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - half) - bx;
                    float dy = Mathf.Abs(y - half) - by;
                    float outDist = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
                    float inDist = Mathf.Min(Mathf.Max(dx, dy), 0f);
                    float sdf = outDist + inDist - r; // <= 0 内部

                    float alphaOuter = Mathf.Clamp01(0.5f - sdf);
                    if (alphaOuter <= 0.001f)
                    {
                        px[y * size + x] = new Color(0, 0, 0, 0);
                    }
                    else if (borderPx > 0.01f && border.a > 0.01f)
                    {
                        float inner = Mathf.Clamp01(0.5f - (sdf + borderPx));
                        Color col = Color.Lerp(border, fill, inner);
                        col.a *= alphaOuter;
                        px[y * size + x] = col;
                    }
                    else
                    {
                        Color col = fill;
                        col.a *= alphaOuter;
                        px[y * size + x] = col;
                    }
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // 程序生成圆形带白边阴影滑块圆点
        public static Texture2D MakeThumbTex(int size, Color fill, Color ring)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] px = new Color[size * size];
            float half = (size - 1) * 0.5f;
            float outerR = half - 2.5f;
            float innerR = outerR - Mathf.Max(2.2f, size * 0.13f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - half;
                    float dy = y - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    // 柔和投影（略向下偏 1px）
                    float ds = Mathf.Sqrt(dx * dx + (dy + 1.0f) * (dy + 1.0f));
                    float shA = Mathf.Clamp01(1f - Mathf.Max(0f, ds - outerR) / 2.5f) * 0.24f;

                    float ringCov = Mathf.Clamp01(outerR + 0.6f - d);
                    float fillCov = Mathf.Clamp01(innerR + 0.6f - d);

                    Color col = new Color(0f, 0f, 0f, shA);
                    if (ringCov > 0.001f)
                    {
                        Color fg = Color.Lerp(ring, fill, fillCov);
                        float a = fg.a * ringCov;
                        col = new Color(
                            fg.r * a + col.r * col.a * (1f - a),
                            fg.g * a + col.g * col.a * (1f - a),
                            fg.b * a + col.b * col.a * (1f - a),
                            a + col.a * (1f - a)
                        );
                        if (col.a > 0.001f)
                        {
                            col.r /= col.a; col.g /= col.a; col.b /= col.a;
                        }
                    }
                    px[y * size + x] = col;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }
}
