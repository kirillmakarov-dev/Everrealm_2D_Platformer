using System;
using System.Collections.Generic;

namespace LetterHunter.SkillTree
{
    public readonly struct PurchasedSkillNode
    {
        public PurchasedSkillNode(string professionId, string nodeId)
        { ProfessionId = professionId; NodeId = nodeId; }
        public string ProfessionId { get; }
        public string NodeId { get; }
    }

    public sealed class SkillTreeProgress
    {
        private readonly HashSet<string> _purchased = new(StringComparer.Ordinal);
        public string ActiveProfessionId { get; private set; } = string.Empty;
        public event Action ProgressChanged;

        public bool IsPurchased(string professionId, string nodeId) =>
            !string.IsNullOrWhiteSpace(professionId) && !string.IsNullOrWhiteSpace(nodeId) &&
            _purchased.Contains(Key(professionId, nodeId));

        public void SetActiveProfession(string professionId, bool notify = true)
        {
            var safeId = professionId ?? string.Empty;
            if (ActiveProfessionId == safeId) return;
            ActiveProfessionId = safeId;
            if (notify) ProgressChanged?.Invoke();
        }

        public bool MarkPurchased(string professionId, string nodeId, bool notify = true)
        {
            if (string.IsNullOrWhiteSpace(professionId) || string.IsNullOrWhiteSpace(nodeId) ||
                !_purchased.Add(Key(professionId, nodeId))) return false;
            if (notify) ProgressChanged?.Invoke();
            return true;
        }

        public bool RemovePurchased(string professionId, string nodeId, bool notify = true)
        {
            var removed = _purchased.Remove(Key(professionId, nodeId));
            if (removed && notify) ProgressChanged?.Invoke();
            return removed;
        }

        public IEnumerable<PurchasedSkillNode> EnumeratePurchased()
        {
            foreach (var key in _purchased)
            {
                var separator = key.IndexOf('\u001f');
                if (separator > 0)
                    yield return new PurchasedSkillNode(key.Substring(0, separator), key.Substring(separator + 1));
            }
        }

        public void Replace(string activeProfessionId, IEnumerable<PurchasedSkillNode> purchased)
        {
            _purchased.Clear();
            ActiveProfessionId = activeProfessionId ?? string.Empty;
            if (purchased != null)
                foreach (var entry in purchased)
                    if (!string.IsNullOrWhiteSpace(entry.ProfessionId) && !string.IsNullOrWhiteSpace(entry.NodeId))
                        _purchased.Add(Key(entry.ProfessionId, entry.NodeId));
            ProgressChanged?.Invoke();
        }

        public void Clear()
        {
            _purchased.Clear();
            ProgressChanged?.Invoke();
        }

        private static string Key(string professionId, string nodeId) => $"{professionId}\u001f{nodeId}";
    }
}
