using System;
using System.Collections.Generic;

namespace Core.Common
{
    /// <summary>
    /// 이름을 전역 테이블의 정수 인덱스 두 개로 드는 이름 타입. 비교는 대소문자를 무시하고, 표기는 처음 들어온 철자를 보존한다.
    /// InternedName(인터닝된 string 참조)과의 비교 실험용 — 직렬화는 지원하지 않는다.
    /// </summary>
    /// <remarks>UE: FName</remarks>
    public readonly struct IndexedName : IEquatable<IndexedName>
    {
        // 인덱스 0은 None 자리 — default(IndexedName)이 곧 None이 되게 비워 둔다.
        private static readonly object _tableLock = new object();
        private static readonly Dictionary<string, int> _comparisonIndexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> _displayIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
        private static readonly List<string> _displayNames = new List<string> { null };
        private static readonly List<int> _displayComparisonIndices = new List<int> { 0 };
        private static int _comparisonCount = 1;

        private readonly int _comparisonIndex;
        private readonly int _displayIndex;

        public static int ComparisonEntryCount
        {
            get
            {
                lock (_tableLock)
                {
                    return _comparisonCount;
                }
            }
        }

        public static int DisplayEntryCount
        {
            get
            {
                lock (_tableLock)
                {
                    return _displayNames.Count;
                }
            }
        }

        public int ComparisonIndex => _comparisonIndex;
        public int DisplayIndex => _displayIndex;

        public IndexedName(string inName)
        {
            if (string.IsNullOrEmpty(inName))
            {
                _comparisonIndex = 0;
                _displayIndex = 0;
                return;
            }

            lock (_tableLock)
            {
                // 같은 철자가 이미 있으면 조회 한 번으로 끝 — 대소문자 무시 사전까지 가지 않는다.
                if (_displayIndexByName.TryGetValue(inName, out int displayIndex))
                {
                    _displayIndex = displayIndex;
                    _comparisonIndex = _displayComparisonIndices[displayIndex];
                    return;
                }

                if (!_comparisonIndexByName.TryGetValue(inName, out int comparisonIndex))
                {
                    comparisonIndex = _comparisonCount;
                    _comparisonCount++;
                    _comparisonIndexByName.Add(inName, comparisonIndex);
                }

                _displayIndex = _displayNames.Count;
                _comparisonIndex = comparisonIndex;
                _displayNames.Add(inName);
                _displayComparisonIndices.Add(comparisonIndex);
                _displayIndexByName.Add(inName, _displayIndex);
            }
        }

        public bool Equals(IndexedName other)
        {
            return _comparisonIndex == other._comparisonIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is IndexedName other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _comparisonIndex;
        }

        public override string ToString()
        {
            lock (_tableLock)
            {
                return _displayNames[_displayIndex];
            }
        }

        public bool IsNullOrEmpty()
        {
            return _comparisonIndex == 0;
        }
    }
}
