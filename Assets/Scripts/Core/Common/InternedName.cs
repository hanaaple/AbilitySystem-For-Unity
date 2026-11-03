using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Common
{
#if TAG_NAME_INDEXED
    /// <summary> A/B 실험용 구현 — 비교·해시는 IndexedName(전역 테이블 인덱스)이 맡고, string은 Unity 직렬화·표기용으로만 든다. </summary>
    [Serializable]
    public struct InternedName : IEquatable<InternedName>, ISerializationCallbackReceiver
    {
        public const string ImplementationName = "Indexed";

        [SerializeField] private string name;

        [NonSerialized] private IndexedName _indexed;

        public InternedName(string inName)
        {
            TagProfiler.Count(TagProfileCounter.NameCreate);
            name = string.IsNullOrEmpty(inName) ? null : inName;
            _indexed = new IndexedName(inName);
        }

        public bool Equals(InternedName other)
        {
            TagProfiler.Count(TagProfileCounter.NameEquals);
            return _indexed.Equals(other._indexed);
        }

        public override bool Equals(object inName)
        {
            return inName is InternedName other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _indexed.GetHashCode();
        }

        public override string ToString()
        {
            return name;
        }

        public bool IsNullOrEmpty()
        {
            return _indexed.IsNullOrEmpty();
        }

        // Unity 역직렬화는 생성자를 우회하고 필드를 직접 채우므로, 로드 직후 테이블 인덱스를 다시 구한다.
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            TagProfiler.Count(TagProfileCounter.NameDeserialize);
            name = string.IsNullOrEmpty(name) ? null : name;
            _indexed = new IndexedName(name);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }
    }
#elif TAG_NAME_LEGACY
    /// <summary> 성능 비교 기준선 — 표기/비교 분리 전 구현(대소문자 구분, 표기 문자열 자체를 인터닝). 측정 외 용도로 쓰지 않는다. </summary>
    [Serializable]
    public struct InternedName : IEquatable<InternedName>, ISerializationCallbackReceiver
    {
        public const string ImplementationName = "Legacy";

        [SerializeField] private string name;

        [NonSerialized] private int _hash;

        public InternedName(string inName)
        {
            TagProfiler.Count(TagProfileCounter.NameCreate);
            name = Intern(inName);
            _hash = ComputeHash(name);
        }

        public bool Equals(InternedName other)
        {
            TagProfiler.Count(TagProfileCounter.NameEquals);
            return ReferenceEquals(name, other.name);
        }

        public override bool Equals(object inName)
        {
            return inName is InternedName other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public override string ToString()
        {
            return name;
        }

        public bool IsNullOrEmpty()
        {
            return name == null;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            TagProfiler.Count(TagProfileCounter.NameDeserialize);
            name = Intern(name);
            _hash = ComputeHash(name);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        private static string Intern(string inName)
        {
            return string.IsNullOrEmpty(inName) ? null : string.Intern(inName);
        }

        private static int ComputeHash(string internedName)
        {
            return internedName?.GetHashCode() ?? 0;
        }
    }
#else
    /// <summary>
    /// 표기와 비교를 분리한 이름. 표기(<see cref="ToString"/>)는 쓴 철자를 그대로 보존하고,
    /// 비교·해시는 대소문자를 무시한 정본 인스턴스의 참조로 한다 — 대소문자만 다른 이름은 같은 이름이다.
    /// </summary>
    /// <remarks>UE: FName (ComparisonIndex / DisplayIndex)</remarks>
    [Serializable]
    public struct InternedName : IEquatable<InternedName>, ISerializationCallbackReceiver
    {
        public const string ImplementationName = "Interned";

        // 비교 정본은 대문자로 접은 인턴 문자열 — 어느 철자가 먼저 들어와도 같은 인스턴스가 나와 선착순 의존이 없다.
        // 사전은 접기(ToUpperInvariant 할당)를 이름당 한 번으로 줄이는 캐시. 인턴 풀처럼 프로세스가 끝날 때까지 남는다 — 비우면 살아 있는 인스턴스의 참조 비교가 깨진다.
        private static readonly object _comparisonLock = new object();
        private static readonly Dictionary<string, string> _comparisonByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 표기. 직렬화되는 유일한 필드 — 에디터 드로어가 경로 "name"으로 읽고 쓴다.
        [SerializeField] private string name;

        [NonSerialized] private string _comparisonName;

        [NonSerialized] private int _hash;

        public InternedName(string inName)
        {
            TagProfiler.Count(TagProfileCounter.NameCreate);
            name = string.IsNullOrEmpty(inName) ? null : inName;
            _comparisonName = ToComparisonName(name);
            _hash = ComputeHash(_comparisonName);
        }

        public bool Equals(InternedName other)
        {
            TagProfiler.Count(TagProfileCounter.NameEquals);
            // 생성·역직렬화 모든 경로에서 비교 정본을 인터닝하므로 같은 이름(대소문자 무시)은 같은 인스턴스 → 참조 비교로 충분하다.
            return ReferenceEquals(_comparisonName, other._comparisonName);
        }

        public override bool Equals(object inName)
        {
            return inName is InternedName other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public override string ToString()
        {
            return name;
        }

        public bool IsNullOrEmpty()
        {
            return _comparisonName == null;
        }

        // Unity 역직렬화는 생성자를 우회하고 필드를 직접 채우므로, 로드 직후 비교 정본과 해시를 다시 만든다. 표기는 저장된 그대로 둔다.
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            TagProfiler.Count(TagProfileCounter.NameDeserialize);
            name = string.IsNullOrEmpty(name) ? null : name;
            _comparisonName = ToComparisonName(name);
            _hash = ComputeHash(_comparisonName);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        private static string ToComparisonName(string displayName)
        {
            if (displayName == null)
            {
                return null;
            }

            lock (_comparisonLock)
            {
                if (_comparisonByName.TryGetValue(displayName, out string comparisonName))
                {
                    return comparisonName;
                }

                // OrdinalIgnoreCase가 문자별 대문자 변환으로 비교하므로 정본도 같은 규칙(ToUpperInvariant)으로 접어야 사전 키와 어긋나지 않는다.
                comparisonName = string.Intern(displayName.ToUpperInvariant());
                _comparisonByName.Add(displayName, comparisonName);
                return comparisonName;
            }
        }

        // default(InternedName)의 해시(0)와 맞춰 null은 0으로 둔다.
        private static int ComputeHash(string comparisonName)
        {
            return comparisonName?.GetHashCode() ?? 0;
        }
    }
#endif
}
