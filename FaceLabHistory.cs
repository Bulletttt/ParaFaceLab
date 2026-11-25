// FaceLabHistory.cs —— 50 步撤回/重做 + 进面板前初始快照（同时覆盖骨骼精修与原版拉杆）
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParaFaceLab
{
    public sealed class FaceLabSnapshot
    {
        public string Label;
        public Dictionary<string, BoneOffset> Bones = new Dictionary<string, BoneOffset>();
        public Dictionary<ulong, float> Vanilla = new Dictionary<ulong, float>();
        public bool Enabled = true;
        public bool UnlockLimits = false;
    }

    public static class FaceLabHistory
    {
        private const int MAX_STEPS = 50;

        private sealed class CharHistory
        {
            public FaceLabSnapshot Initial;
            public readonly List<FaceLabSnapshot> Undo = new List<FaceLabSnapshot>();
            public readonly List<FaceLabSnapshot> Redo = new List<FaceLabSnapshot>();
        }

        private static readonly Dictionary<ulong, CharHistory> Map = new Dictionary<ulong, CharHistory>();

        private static CharHistory Get(ulong guid)
        {
            CharHistory h;
            if (!Map.TryGetValue(guid, out h))
            {
                h = new CharHistory();
                Map[guid] = h;
            }
            return h;
        }

        public static FaceLabSnapshot Capture(AssetCharacter ch, List<Setting.DeformationSlider> allSliders, string label = "")
        {
            var snap = new FaceLabSnapshot { Label = label ?? "" };
            if (ch == null) return snap;

            FaceProfile prof = FaceLabStorage.TryGetProfile(ch.GUID);
            if (prof != null)
            {
                snap.Enabled = prof.enabled;
                snap.UnlockLimits = prof.unlockVanillaLimits;
                if (prof.bones != null)
                {
                    foreach (var kv in prof.bones)
                    {
                        if (kv.Value != null && !kv.Value.IsZero())
                            snap.Bones[kv.Key] = kv.Value.Clone();
                    }
                }
            }

            try
            {
                CharacterDeformationSliderManager mgr = CharacterDeformationSliderManager.Instance;
                if (mgr != null && allSliders != null)
                {
                    for (int i = 0; i < allSliders.Count; i++)
                    {
                        Setting.DeformationSlider s = allSliders[i];
                        if (s == null) continue;
                        snap.Vanilla[s.GUID] = mgr.GetCharacterSliderValue(ch, s.GUID);
                    }
                }
            }
            catch { }

            return snap;
        }

        public static void EnsureInitialSnapshot(AssetCharacter ch, List<Setting.DeformationSlider> allSliders)
        {
            if (ch == null) return;
            CharHistory h = Get(ch.GUID);
            if (h.Initial == null)
                h.Initial = Capture(ch, allSliders, "初始状态");
        }

        public static void PushUndo(ulong guid, FaceLabSnapshot beforeSnap, string label)
        {
            if (guid == 0 || beforeSnap == null) return;
            beforeSnap.Label = label ?? "";
            CharHistory h = Get(guid);
            h.Undo.Add(beforeSnap);
            if (h.Undo.Count > MAX_STEPS) h.Undo.RemoveAt(0);
            h.Redo.Clear();
        }

        public static int UndoCount(ulong guid)
        {
            CharHistory h;
            return Map.TryGetValue(guid, out h) ? h.Undo.Count : 0;
        }

        public static int RedoCount(ulong guid)
        {
            CharHistory h;
            return Map.TryGetValue(guid, out h) ? h.Redo.Count : 0;
        }

        public static bool HasInitial(ulong guid)
        {
            CharHistory h;
            return Map.TryGetValue(guid, out h) && h.Initial != null;
        }

        public static FaceLabSnapshot GetInitialSnapshot(ulong guid)
        {
            CharHistory h;
            return Map.TryGetValue(guid, out h) ? h.Initial : null;
        }

        public static string Undo(AssetCharacter ch, List<Setting.DeformationSlider> allSliders)
        {
            if (ch == null) return null;
            CharHistory h = Get(ch.GUID);
            if (h.Undo.Count == 0) return null;

            FaceLabSnapshot prev = h.Undo[h.Undo.Count - 1];
            h.Undo.RemoveAt(h.Undo.Count - 1);

            FaceLabSnapshot cur = Capture(ch, allSliders, prev.Label);
            h.Redo.Add(cur);
            if (h.Redo.Count > MAX_STEPS) h.Redo.RemoveAt(0);

            ApplySnapshot(ch, prev);
            return prev.Label;
        }

        public static string Redo(AssetCharacter ch, List<Setting.DeformationSlider> allSliders)
        {
            if (ch == null) return null;
            CharHistory h = Get(ch.GUID);
            if (h.Redo.Count == 0) return null;

            FaceLabSnapshot next = h.Redo[h.Redo.Count - 1];
            h.Redo.RemoveAt(h.Redo.Count - 1);

            FaceLabSnapshot cur = Capture(ch, allSliders, next.Label);
            h.Undo.Add(cur);
            if (h.Undo.Count > MAX_STEPS) h.Undo.RemoveAt(0);

            ApplySnapshot(ch, next);
            return next.Label;
        }

        public static bool RevertToInitial(AssetCharacter ch, List<Setting.DeformationSlider> allSliders)
        {
            if (ch == null) return false;
            CharHistory h = Get(ch.GUID);
            if (h.Initial == null) return false;

            FaceLabSnapshot cur = Capture(ch, allSliders, "还原初始前");
            PushUndo(ch.GUID, cur, "还原初始状态");
            ApplySnapshot(ch, h.Initial);
            return true;
        }

        public static void ApplySnapshot(AssetCharacter ch, FaceLabSnapshot snap)
        {
            if (ch == null || snap == null) return;

            FaceProfile prof = FaceLabStorage.GetOrCreateProfile(ch.GUID, "");
            prof.enabled = snap.Enabled;
            prof.unlockVanillaLimits = snap.UnlockLimits;
            prof.bones.Clear();
            foreach (var kv in snap.Bones)
            {
                if (kv.Value != null && !kv.Value.IsZero())
                    prof.bones[kv.Key] = kv.Value.Clone();
            }
            FaceLabStorage.MarkDirty(ch.GUID);

            if (snap.Vanilla != null && snap.Vanilla.Count > 0)
            {
                FaceLabRuntime.ApplyVanillaBatch(ch, snap.Vanilla);
            }
        }
    }
}
