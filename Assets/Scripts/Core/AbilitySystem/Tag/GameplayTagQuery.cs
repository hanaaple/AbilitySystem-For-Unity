using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Core.AbilitySystem.Tag
{
    /// <summary>태그 컨테이너를 상대로 평가하는 논리 질의(All/Any/None 등의 조합 조건).</summary>
    [Serializable]
    public struct GameplayTagQuery : IEquatable<GameplayTagQuery>
    {
        // 토큰 형식을 바꾸면 저장된 질의를 이 값으로 구분해 옮긴다. 필드가 없던 에셋은 0(InitialVersion)으로 읽혀 지금 형식과 같다
        [SerializeField] private int _tokenStreamVersion;

        // 토큰 스트림은 태그를 직접 담지 않고 이 목록의 인덱스로 참조한다
        // struct 복사본끼리 리스트를 공유하므로, 두 리스트는 제자리 수정하지 않고 항상 새로 만들어 교체한다
        // 질의 편집 창이 사용자 설명을 _userDescription 이름으로 읽는다. 이름을 바꾸면 GameplayTagQueryEditorWindow도 바꿔야 한다.
        [SerializeField] private List<GameplayTag> _tagDictionary;
        [SerializeField] private List<byte> _queryTokenStream;
        [SerializeField] private string _userDescription;
        [SerializeField] private string _autoDescription;

        /// <remarks>UE: FGameplayTagQuery::operator==</remarks>
        public static bool operator ==(GameplayTagQuery left, GameplayTagQuery right)
        {
            return left.Equals(right);
        }

        /// <remarks>UE: FGameplayTagQuery::operator!=</remarks>
        public static bool operator !=(GameplayTagQuery left, GameplayTagQuery right)
        {
            return !left.Equals(right);
        }

        /// <summary>토큰 형식 버전·참조 태그 목록·토큰 스트림·사용자 설명·자동 설명이 모두 같으면 같다. 빈 목록과 null, 빈 문자열과 null은 같게 본다.</summary>
        /// <remarks>UE: FGameplayTagQuery::operator==</remarks>
        public bool Equals(GameplayTagQuery other)
        {
            return _tokenStreamVersion == other._tokenStreamVersion && ListEquals(_tagDictionary, other._tagDictionary) && ListEquals(_queryTokenStream, other._queryTokenStream) && (_userDescription ?? string.Empty) == (other._userDescription ?? string.Empty) && (_autoDescription ?? string.Empty) == (other._autoDescription ?? string.Empty);
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayTagQuery other && Equals(other);
        }

        // 빈 목록과 null을 같게 보는 Equals와 맞추려고 개수와 설명만으로 만든다
        public override int GetHashCode()
        {
            return HashCode.Combine(_tagDictionary?.Count ?? 0, _queryTokenStream?.Count ?? 0, GetDescription());
        }

        /// <summary>주어진 루트 식으로 질의를 만든다.</summary>
        /// <remarks>UE: FGameplayTagQuery::BuildQuery</remarks>
        public static GameplayTagQuery BuildQuery(GameplayTagQueryExpression rootQueryExpr, string description = "")
        {
            var query = new GameplayTagQuery();
            query.Build(rootQueryExpr, description);
            return query;
        }

        /// <summary>임의의 태그가 대상과 겹치면 매칭되는 질의를 만든다.</summary>
        /// <remarks>UE: FGameplayTagQuery::MakeQuery_MatchAnyTags</remarks>
        public static GameplayTagQuery MakeQuery_MatchAnyTags(GameplayTagContainer inTags)
        {
            return BuildQuery(new GameplayTagQueryExpression().AnyTagsMatch().AddTags(inTags));
        }

        /// <summary>모든 태그를 대상이 가지면 매칭되는 질의를 만든다.</summary>
        /// <remarks>UE: FGameplayTagQuery::MakeQuery_MatchAllTags</remarks>
        public static GameplayTagQuery MakeQuery_MatchAllTags(GameplayTagContainer inTags)
        {
            return BuildQuery(new GameplayTagQueryExpression().AllTagsMatch().AddTags(inTags));
        }

        /// <summary>어느 태그도 대상에 없으면 매칭되는 질의를 만든다.</summary>
        /// <remarks>UE: FGameplayTagQuery::MakeQuery_MatchNoTags</remarks>
        public static GameplayTagQuery MakeQuery_MatchNoTags(GameplayTagContainer inTags)
        {
            return BuildQuery(new GameplayTagQueryExpression().NoTagsMatch().AddTags(inTags));
        }

        /// <summary>단일 태그를 대상이 가지면 매칭되는 질의를 만든다.</summary>
        /// <remarks>UE: FGameplayTagQuery::MakeQuery_MatchTag</remarks>
        public static GameplayTagQuery MakeQuery_MatchTag(GameplayTag inTag)
        {
            return BuildQuery(new GameplayTagQueryExpression().AllTagsMatch().AddTag(inTag));
        }

        /// <summary>질의 로직은 유지한 채 참조 태그만 교체한다. 기존 태그 수와 같아야 한다.</summary>
        /// <remarks>UE: FGameplayTagQuery::ReplaceTagsFast</remarks>
        public void ReplaceTagsFast(GameplayTagContainer tags)
        {
            var newTags = new List<GameplayTag>(tags.GetGameplayTagArray());
            Debug.Assert(newTags.Count == (_tagDictionary?.Count ?? 0), "ReplaceTagsFast: 태그 수가 기존과 다릅니다.");
            _tagDictionary = newTags;
        }

        /// <summary>질의 로직은 유지한 채 단일 참조 태그를 교체한다. 기존 태그가 1개여야 한다.</summary>
        /// <remarks>UE: FGameplayTagQuery::ReplaceTagFast</remarks>
        public void ReplaceTagFast(GameplayTag tag)
        {
            Debug.Assert((_tagDictionary?.Count ?? 0) == 1, "ReplaceTagFast: 기존 태그가 1개가 아닙니다.");
            _tagDictionary = new List<GameplayTag> { tag };
        }

        /// <summary>주어진 태그가 이 질의를 만족하는지 평가한다. 빈 질의는 항상 false.</summary>
        /// <remarks>UE: FGameplayTagQuery::Matches</remarks>
        public bool Matches(GameplayTagContainer tags)
        {
            if (IsEmpty())
            {
                return false;
            }

            int cursor = 0;
            bool result = EvalExpr(tags, ref cursor, false);
            Debug.Assert(cursor == _queryTokenStream.Count, "GameplayTagQuery: 토큰 스트림을 끝까지 읽지 못했습니다.");
            return result;
        }

        /// <summary>질의가 비어 있는지 여부.</summary>
        /// <remarks>UE: FGameplayTagQuery::IsEmpty</remarks>
        public bool IsEmpty()
        {
            return _queryTokenStream == null || _queryTokenStream.Count == 0;
        }

        /// <summary>질의를 빈 기본 상태로 되돌린다.</summary>
        /// <remarks>UE: FGameplayTagQuery::Clear</remarks>
        public void Clear()
        {
            this = default;
        }

        /// <summary>주어진 루트 식으로 이 질의를 다시 구성한다. 자동 설명도 함께 만든다.</summary>
        /// <remarks>UE: FGameplayTagQuery::Build / FGameplayTagQuery::BuildFromEditableQuery</remarks>
        public void Build(GameplayTagQueryExpression rootQueryExpr, string userDescription = "")
        {
            var tokenStream = new List<byte>();
            var tagDictionary = new List<GameplayTag>();
            var autoDescription = new StringBuilder();
            rootQueryExpr.EmitTokens(tokenStream, tagDictionary, autoDescription);

            _tokenStreamVersion = (int)GameplayTagQueryStreamVersion.LatestVersion;
            _queryTokenStream = tokenStream;
            _tagDictionary = tagDictionary;
            _userDescription = userDescription;
            _autoDescription = autoDescription.ToString();
        }

        /// <summary>이 질의를 식 트리로 되돌린다. 빈 질의면 유형이 Undefined인 식.</summary>
        /// <remarks>UE: FGameplayTagQuery::GetQueryExpr</remarks>
        public void GetQueryExpr(out GameplayTagQueryExpression outExpr)
        {
            outExpr = new GameplayTagQueryExpression();
            if (IsEmpty())
            {
                return;
            }

            int cursor = 0;
            ReadExpr(outExpr, ref cursor);
        }

        /// <summary>사용자 지정 설명을 설정한다.</summary>
        /// <remarks>UE: FGameplayTagQuery::SetUserDescription</remarks>
        public void SetUserDescription(string userDescription)
        {
            _userDescription = userDescription;
        }

        /// <summary>사용자 지정 설명. 비었으면 <see cref="Build"/>가 만든 자동 설명. 둘 다 없으면 빈 문자열.</summary>
        /// <remarks>UE: FGameplayTagQuery::GetDescription</remarks>
        public string GetDescription()
        {
            if (!string.IsNullOrEmpty(_userDescription))
            {
                return _userDescription;
            }

            return _autoDescription ?? string.Empty;
        }

        /// <summary>질의가 참조하는 고유 태그 목록.</summary>
        /// <remarks>UE: FGameplayTagQuery::GetGameplayTagArray</remarks>
        public IReadOnlyList<GameplayTag> GetGameplayTagArray()
        {
            return _tagDictionary ?? (IReadOnlyList<GameplayTag>)Array.Empty<GameplayTag>();
        }

        private static bool ListEquals<T>(List<T> left, List<T> right)
        {
            int leftCount = left?.Count ?? 0;
            int rightCount = right?.Count ?? 0;
            if (leftCount != rightCount)
            {
                return false;
            }

            for (int i = 0; i < leftCount; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(left[i], right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // 토큰 레이아웃: [식 유형][개수][태그 인덱스… | 하위 식…] — GameplayTagQueryExpression.EmitTokens와 짝
        /// <remarks>UE: FQueryEvaluator::EvalExpr</remarks>
        private bool EvalExpr(GameplayTagContainer tags, ref int cursor, bool skip)
        {
            var exprType = (GameplayTagQueryExprType)_queryTokenStream[cursor++];
            switch (exprType)
            {
                case GameplayTagQueryExprType.AnyTagsMatch:
                {
                    return EvalTagSet(tags, ref cursor, skip, true, false);
                }
                case GameplayTagQueryExprType.AllTagsMatch:
                {
                    return EvalTagSet(tags, ref cursor, skip, false, false);
                }
                case GameplayTagQueryExprType.AnyTagsExactMatch:
                {
                    return EvalTagSet(tags, ref cursor, skip, true, true);
                }
                case GameplayTagQueryExprType.AllTagsExactMatch:
                {
                    return EvalTagSet(tags, ref cursor, skip, false, true);
                }
                case GameplayTagQueryExprType.NoTagsMatch:
                {
                    return !EvalTagSet(tags, ref cursor, skip, true, false);
                }
                case GameplayTagQueryExprType.AnyExprMatch:
                {
                    return EvalExprSet(tags, ref cursor, skip, true);
                }
                case GameplayTagQueryExprType.AllExprMatch:
                {
                    return EvalExprSet(tags, ref cursor, skip, false);
                }
                case GameplayTagQueryExprType.NoExprMatch:
                {
                    return !EvalExprSet(tags, ref cursor, skip, true);
                }
                default:
                {
                    // Undefined는 [유형][개수 0]으로 기록되므로 개수 토큰을 마저 소비한다
                    cursor++;
                    Debug.LogError($"GameplayTagQuery: 평가할 수 없는 식 유형 {exprType}");
                    return false;
                }
            }
        }

        // 결과가 정해져도 남은 토큰은 끝까지 소비해야 다음 식의 위치가 맞는다
        /// <remarks>UE: FQueryEvaluator::EvalAnyTagsMatch / EvalAllTagsMatch / EvalNoTagsMatch</remarks>
        private bool EvalTagSet(GameplayTagContainer tags, ref int cursor, bool skip, bool any, bool exact)
        {
            int count = _queryTokenStream[cursor++];
            bool result = !any;
            bool decided = skip;

            for (int i = 0; i < count; i++)
            {
                int tagIndex = _queryTokenStream[cursor++];
                if (decided)
                {
                    continue;
                }

                GameplayTag tag = _tagDictionary[tagIndex];
                bool hasTag = exact ? tags.HasTagExact(tag) : tags.HasTag(tag);
                if (hasTag == any)
                {
                    result = any;
                    decided = true;
                }
            }

            return result;
        }

        /// <remarks>UE: FQueryEvaluator::EvalAnyExprMatch / EvalAllExprMatch / EvalNoExprMatch</remarks>
        private bool EvalExprSet(GameplayTagContainer tags, ref int cursor, bool skip, bool any)
        {
            int count = _queryTokenStream[cursor++];
            bool result = !any;
            bool decided = skip;

            for (int i = 0; i < count; i++)
            {
                bool exprResult = EvalExpr(tags, ref cursor, decided);
                if (!decided && exprResult == any)
                {
                    result = any;
                    decided = true;
                }
            }

            return result;
        }

        /// <remarks>UE: FQueryEvaluator::ReadExpr</remarks>
        private void ReadExpr(GameplayTagQueryExpression expr, ref int cursor)
        {
            expr.ExprType = (GameplayTagQueryExprType)_queryTokenStream[cursor++];
            int count = _queryTokenStream[cursor++];

            if (expr.UsesTagSet())
            {
                for (int i = 0; i < count; i++)
                {
                    int tagIndex = _queryTokenStream[cursor++];
                    expr.AddTag(_tagDictionary[tagIndex]);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    var child = new GameplayTagQueryExpression();
                    ReadExpr(child, ref cursor);
                    expr.AddExpr(child);
                }
            }
        }
    }
}
