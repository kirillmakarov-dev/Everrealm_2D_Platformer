using System;
using System.Collections.Generic;

namespace LetterHunter.SkillTree
{
    public sealed class SkillTreeProgress
    {
        private readonly Dictionary<string, int> _nodeRanks = new();

        public event Action ProgressChanged;
        public IReadOnlyCollection<string> UnlockedNodeIds => _nodeRanks.Keys;
        public IReadOnlyDictionary<string, int> NodeRanks => _nodeRanks;

        public bool IsUnlocked(string nodeId) =>
            GetRank(nodeId) > 0;

        public int GetRank(string nodeId) =>
            !string.IsNullOrWhiteSpace(nodeId) && _nodeRanks.TryGetValue(nodeId, out var rank) ? rank : 0;

        public bool MarkUnlocked(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || IsUnlocked(nodeId))
                return false;

            _nodeRanks[nodeId] = 1;
            ProgressChanged?.Invoke();
            return true;
        }

        public bool SetRank(string nodeId, int rank)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return false;

            var safeRank = Math.Max(0, rank);
            var currentRank = GetRank(nodeId);
            if (currentRank == safeRank)
                return false;

            if (safeRank == 0)
                _nodeRanks.Remove(nodeId);
            else
                _nodeRanks[nodeId] = safeRank;

            ProgressChanged?.Invoke();
            return true;
        }

        public void ReplaceUnlocked(IEnumerable<string> nodeIds)
        {
            _nodeRanks.Clear();
            if (nodeIds != null)
            {
                foreach (var nodeId in nodeIds)
                    if (!string.IsNullOrWhiteSpace(nodeId))
                        _nodeRanks[nodeId] = 1;
            }

            ProgressChanged?.Invoke();
        }

        public void ReplaceRanks(IEnumerable<KeyValuePair<string, int>> nodeRanks)
        {
            _nodeRanks.Clear();
            if (nodeRanks != null)
            {
                foreach (var pair in nodeRanks)
                    if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value > 0)
                        _nodeRanks[pair.Key] = pair.Value;
            }

            ProgressChanged?.Invoke();
        }
    }
}
