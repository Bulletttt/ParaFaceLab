// FaceLabStorage.cs —— 按角色 GUID 独立 JSON 存盘 & 预设管理（绝不污染游戏原版存档）
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace ParaFaceLab
{
    public static class FaceLabStorage
    {
        private static string _rootDir;
        private static string _charDir;
        private static string _presetDir;

        public static readonly Dictionary<ulong, FaceProfile> Profiles = new Dictionary<ulong, FaceProfile>();
        private static readonly HashSet<ulong> DirtyGuids = new HashSet<ulong>();
        private static float _nextSaveAt = 0f;

        public static string RootDir { get { EnsureDirs(); return _rootDir; } }
        public static string PresetDir { get { EnsureDirs(); return _presetDir; } }

        public static void Init(string pluginPath)
        {
            string baseDir = Path.GetDirectoryName(pluginPath);
            if (string.IsNullOrEmpty(baseDir)) baseDir = ".";
            _rootDir = Path.Combine(baseDir, "ParaFaceLab.data");
            _charDir = Path.Combine(_rootDir, "characters");
            _presetDir = Path.Combine(_rootDir, "presets");
            EnsureDirs();
            LoadAllCharacters();
        }

        private static void EnsureDirs()
        {
            try
            {
                if (!string.IsNullOrEmpty(_rootDir) && !Directory.Exists(_rootDir)) Directory.CreateDirectory(_rootDir);
                if (!string.IsNullOrEmpty(_charDir) && !Directory.Exists(_charDir)) Directory.CreateDirectory(_charDir);
                if (!string.IsNullOrEmpty(_presetDir) && !Directory.Exists(_presetDir)) Directory.CreateDirectory(_presetDir);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ParaFaceLab] EnsureDirs failed: " + e.Message);
            }
        }

        public static void LoadAllCharacters()
        {
            Profiles.Clear();
            EnsureDirs();
            if (string.IsNullOrEmpty(_charDir) || !Directory.Exists(_charDir)) return;
            try
            {
                string[] files = Directory.GetFiles(_charDir, "*.json");
                for (int i = 0; i < files.Length; i++)
                {
                    try
                    {
                        string txt = File.ReadAllText(files[i], Encoding.UTF8);
                        FaceProfile p = JsonConvert.DeserializeObject<FaceProfile>(txt);
                        if (p != null && p.characterGuid != 0)
                        {
                            if (p.bones == null) p.bones = new Dictionary<string, BoneOffset>();
                            if (p.vanillaSliders == null) p.vanillaSliders = new Dictionary<string, float>();
                            if (p.effectiveVanillaSliders == null) p.effectiveVanillaSliders = new Dictionary<string, float>();
                            Profiles[p.characterGuid] = p;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[ParaFaceLab] Load profile failed (" + files[i] + "): " + e.Message);
                    }
                }
                Debug.Log("[ParaFaceLab] Loaded " + Profiles.Count + " character face profiles.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ParaFaceLab] LoadAllCharacters failed: " + e.Message);
            }
        }

        public static FaceProfile GetOrCreateProfile(ulong guid, string name)
        {
            FaceProfile p;
            if (!Profiles.TryGetValue(guid, out p) || p == null)
            {
                p = new FaceProfile { characterGuid = guid, characterName = name ?? "" };
                Profiles[guid] = p;
            }
            else if (!string.IsNullOrEmpty(name))
            {
                p.characterName = name;
            }
            return p;
        }

        public static FaceProfile TryGetProfile(ulong guid)
        {
            FaceProfile p;
            return Profiles.TryGetValue(guid, out p) ? p : null;
        }

        public static void MarkDirty(ulong guid)
        {
            if (guid == 0) return;
            DirtyGuids.Add(guid);
            _nextSaveAt = Time.unscaledTime + 0.6f; // 防抖 0.6 秒落盘，拖动拉杆零卡顿
        }

        public static void TickAutoSave()
        {
            if (DirtyGuids.Count == 0) return;
            if (Time.unscaledTime < _nextSaveAt) return;
            FlushAll();
        }

        public static void FlushAll()
        {
            if (DirtyGuids.Count == 0) return;
            EnsureDirs();
            List<ulong> list = new List<ulong>(DirtyGuids);
            DirtyGuids.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                SaveCharacterNow(list[i]);
            }
        }

        public static void SaveCharacterNow(ulong guid)
        {
            if (guid == 0) return;
            FaceProfile p;
            if (!Profiles.TryGetValue(guid, out p) || p == null) return;
            EnsureDirs();
            string path = Path.Combine(_charDir, "c" + guid.ToString() + ".json");
            try
            {
                p.PruneZeros();
                p.updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                if (p.ActiveBoneCount() == 0 && !p.unlockVanillaLimits)
                {
                    if (File.Exists(path)) File.Delete(path);
                    return;
                }
                string json = JsonConvert.SerializeObject(p, Formatting.Indented);
                File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ParaFaceLab] SaveCharacter failed: " + e.Message);
            }
        }

        public static List<string> ListPresets()
        {
            var res = new List<string>();
            EnsureDirs();
            if (string.IsNullOrEmpty(_presetDir) || !Directory.Exists(_presetDir)) return res;
            try
            {
                string[] files = Directory.GetFiles(_presetDir, "*.json");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < files.Length; i++)
                {
                    res.Add(Path.GetFileNameWithoutExtension(files[i]));
                }
            }
            catch { }
            return res;
        }

        public static string SanitizeFileName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "preset";
            char[] bad = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                bool ok = true;
                for (int j = 0; j < bad.Length; j++)
                {
                    if (c == bad[j]) { ok = false; break; }
                }
                sb.Append(ok ? c : '_');
            }
            string s = sb.ToString().Trim();
            return string.IsNullOrEmpty(s) ? "preset" : s;
        }

        public static bool SavePreset(string name, FaceProfile snapshot, out string error)
        {
            error = "";
            EnsureDirs();
            string safe = SanitizeFileName(name);
            string path = Path.Combine(_presetDir, safe + ".json");
            try
            {
                FaceProfile copy = snapshot.Clone();
                copy.characterGuid = 0;
                copy.characterName = safe;
                copy.updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string json = JsonConvert.SerializeObject(copy, Formatting.Indented);
                File.WriteAllText(path, json, Encoding.UTF8);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public static FaceProfile LoadPreset(string name, out string error)
        {
            error = "";
            EnsureDirs();
            string safe = SanitizeFileName(name);
            string path = Path.Combine(_presetDir, safe + ".json");
            try
            {
                if (!File.Exists(path)) { error = "预设文件不存在"; return null; }
                string json = File.ReadAllText(path, Encoding.UTF8);
                FaceProfile p = JsonConvert.DeserializeObject<FaceProfile>(json);
                if (p == null) { error = "预设内容为空"; return null; }
                if (p.bones == null) p.bones = new Dictionary<string, BoneOffset>();
                if (p.vanillaSliders == null) p.vanillaSliders = new Dictionary<string, float>();
                if (p.effectiveVanillaSliders == null) p.effectiveVanillaSliders = new Dictionary<string, float>();
                return p;
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }
        }

        public static bool DeletePreset(string name)
        {
            EnsureDirs();
            string safe = SanitizeFileName(name);
            string path = Path.Combine(_presetDir, safe + ".json");
            try
            {
                if (File.Exists(path)) File.Delete(path);
                return true;
            }
            catch { return false; }
        }
    }
}
