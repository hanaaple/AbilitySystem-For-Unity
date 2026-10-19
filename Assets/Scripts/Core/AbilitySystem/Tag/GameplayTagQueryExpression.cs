using System.Collections.Generic;
using System.Text;
using Core.Common;
using UnityEngine;

namespace Core.AbilitySystem.Tag
{
    /// <summary><see cref="GameplayTagQuery"/>를 구성하는 식 트리의 노드.</summary>
    public class GameplayTagQueryExpression
    {
        // 토큰 하나가 byte라 개수·태그 인덱스가 이 범위를 넘을 수 없다
        private const int MaxTokenValue = byte.MaxValue;

        public GameplayTagQueryExprType ExprType;

        /// <summary>하위 식 목록. <see cref="UsesExprSet"/>인 유형에서만 쓴다.</summary>
        public List<GameplayTagQueryExpression> ExprSet = new List<GameplayTagQueryExpression>();

        /// <summary>태그 목록. <see cref="UsesTagSet"/>인 유형에서만 쓴다.</summary>
        public List<GameplayTag> TagSet = new List<GameplayTag>();

        /// <summary>태그 중 하나라도 대상에 있으면 매칭되는 식으로 설정한다.</summary>
        /// <remarks>UE: FGameplayTagQueryExpression::AnyTagsMatch</remarks>
        public GameplayTagQueryExpression AnyTagsMatch()
        {
            ExprType = GameplayTagQueryExprType.AnyTagsMatch;
            return this;
        }

        /// <summary>태그를 대상이 모두 가지면 매칭되는 식으로 설정한다.</summary>
        /// <remarks>UE: FGameplayTagQueryExpression::AllTagsMatch</remarks>
        public GameplayTagQueryExpression AllTagsMatch()
        {
            ExprType = GameplayTagQueryExprType.AllTagsMatch;
            return this;
        }

        /// <summary>태그가 하나도 대상에 없으면 매칭되는 식으로 설정한다.</summary>
        /// <remarks>UE: FGameplayTagQueryExpression::NoTagsMatch</remarks>
        public GameplayTagQueryExpression NoTagsMatch()
        {
            ExprType = GameplayTagQueryExprType.NoTagsMatch;
            return this;
        }

        /// <summary>태그 중 하나라도 대상에 정확히(부모 매칭 없이) 있으면 매칭되는 식으로 설정한다.</summary>
        public GameplayTagQueryExpression AnyTagsExactMatch()
        {
            ExprType = GameplayTagQueryExprType.AnyTagsExactMatch;
            return this;
        }

        /// <summary>태그를 대상이 모두 정확히(부모 매칭 없이) 가지면 매칭되는 식으로 설정한다.</summary>
        public GameplayTagQueryExpression AllTagsExactMatch()
        {
            ExprType = GameplayTagQueryExprType.AllTagsExactMatch;
            return this;
        }

        /// <summary>하위 식 중 하나라도 매칭되면 매칭되는 식으로 설정한다.</summary>
        /// <remarks>UE: FGameplayTagQueryExpression::AnyExprMatch</remarks>
        public GameplayTagQueryExpression AnyExprMatch()
        {
            ExprType = GameplayTagQueryExprType.AnyExprMatch;
            return this;
        }

        /// <summary>하위 식이 모두 매칭되면 매칭되는 식으로 설정한다.</summary>
        /// <remarks>UE: FGameplayTagQueryExpression::AllExprMatch</remarks>
        public GameplayTagQueryExpression AllExprMatch()
        {
            ExprType = GameplayTagQueryExprType.AllExprMatch;
            return this;
        }

        /// <summary>하위 식이 하나도 매칭되지 않으면 매칭되는 식으로 설정한다.</summary>
        /// <remarks>UE: FGameplayTagQueryExpression::NoExprMatch</remarks>
        public GameplayTagQueryExpression NoExprMatch()
        {
            ExprType = GameplayTagQueryExprType.NoExprMatch;
            return this;
        }

        /// <summary>이름으로 등록된 태그를 찾아 태그 집합에 추가한다. 태그 집합을 쓰는 식이어야 한다.</summary>
        /// <remarks>UE: FGameplayTagQueryExpression::AddTag</remarks>
        public GameplayTagQueryExpression AddTag(InternedName tagName)
        {
            GameplayTagsManager manager = GameplayTagsManager.Get();
            GameplayTag tag = manager.RequestGameplayTag(tagName);
            return AddTag(tag);
        }

        /// <summary>태그 집합에 태그를 추가한다. 태그 집합을 쓰는 식이어야 한다.</summary>
        /// <remarks>UE: FGameplayTagQueryExpression::AddTag</remarks>
        public GameplayTagQueryExpression AddTag(GameplayTag tag)
        {
            Debug.Assert(UsesTagSet(), $"GameplayTagQueryExpression: {ExprType}은 태그 집합을 쓰지 않습니다.");
            TagSet.Add(tag);
            return this;
        }

        /// <summary>태그 집합에 컨테이너의 태그를 모두 추가한다. 태그 집합을 쓰는 식이어야 한다.</summary>
        /// <remarks>UE: FGameplayTagQueryExpression::AddTags</remarks>
        public GameplayTagQueryExpression AddTags(GameplayTagContainer tags)
        {
            Debug.Assert(UsesTagSet(), $"GameplayTagQueryExpression: {ExprType}은 태그 집합을 쓰지 않습니다.");
            TagSet.AddRange(tags.GetGameplayTagArray());
            return this;
        }

        /// <summary>하위 식을 추가한다. 하위 식 목록을 쓰는 식이어야 한다.</summary>
        /// <remarks>UE: FGameplayTagQueryExpression::AddExpr</remarks>
        public GameplayTagQueryExpression AddExpr(GameplayTagQueryExpression expr)
        {
            Debug.Assert(UsesExprSet(), $"GameplayTagQueryExpression: {ExprType}은 하위 식 목록을 쓰지 않습니다.");
            ExprSet.Add(expr);
            return this;
        }

        /// <summary>이 식을 토큰 스트림으로 기록하고, 참조 태그를 태그 사전에 등록한다. <paramref name="description"/>을 주면 사람이 읽을 설명(예: <c> ANY( A, B )</c>)을 덧붙인다.</summary>
        /// <remarks>UE: FGameplayTagQueryExpression::EmitTokens / UEditableGameplayTagQueryExpression::EmitTokens</remarks>
        // 토큰 레이아웃: [식 유형][개수][태그 인덱스… | 하위 식…] — GameplayTagQuery의 평가·복원과 짝
        public void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary, StringBuilder description = null)
        {
            tokenStream.Add((byte)ExprType);

            if (UsesTagSet())
            {
                Debug.Assert(TagSet.Count <= MaxTokenValue, "GameplayTagQueryExpression: 한 식의 태그 수가 너무 많습니다.");
                tokenStream.Add((byte)TagSet.Count);
                description?.Append(GetDescriptionOpening());

                bool isFirstTag = true;
                foreach (GameplayTag tag in TagSet)
                {
                    int tagIndex = tagDictionary.IndexOf(tag);
                    if (tagIndex < 0)
                    {
                        tagIndex = tagDictionary.Count;
                        tagDictionary.Add(tag);
                    }

                    Debug.Assert(tagIndex <= MaxTokenValue, "GameplayTagQueryExpression: 질의의 고유 태그 수가 너무 많습니다.");
                    tokenStream.Add((byte)tagIndex);

                    AppendListSeparator(description, isFirstTag);
                    description?.Append(tag.ToString());
                    isFirstTag = false;
                }

                description?.Append(" )");
                return;
            }

            // Undefined도 개수 0으로 기록해, 읽는 쪽이 유형과 무관하게 [유형][개수]를 소비하게 한다
            int exprCount = UsesExprSet() ? ExprSet.Count : 0;
            Debug.Assert(exprCount <= MaxTokenValue, "GameplayTagQueryExpression: 하위 식 수가 너무 많습니다.");
            tokenStream.Add((byte)exprCount);

            if (!UsesExprSet())
            {
                description?.Append("undefined");
                return;
            }

            description?.Append(GetDescriptionOpening());

            for (int i = 0; i < exprCount; i++)
            {
                AppendListSeparator(description, i == 0);
                ExprSet[i].EmitTokens(tokenStream, tagDictionary, description);
            }

            description?.Append(" )");
        }

        /// <summary>이 식이 태그 집합을 쓰는 유형인지 여부.</summary>
        /// <remarks>UE: FGameplayTagQueryExpression::UsesTagSet</remarks>
        public bool UsesTagSet()
        {
            return ExprType == GameplayTagQueryExprType.AnyTagsMatch || ExprType == GameplayTagQueryExprType.AllTagsMatch || ExprType == GameplayTagQueryExprType.NoTagsMatch || ExprType == GameplayTagQueryExprType.AnyTagsExactMatch || ExprType == GameplayTagQueryExprType.AllTagsExactMatch;
        }

        /// <summary>이 식이 하위 식 목록을 쓰는 유형(Any/All/NoExprMatch)인지 여부.</summary>
        /// <remarks>UE: FGameplayTagQueryExpression::UsesExprSet</remarks>
        public bool UsesExprSet()
        {
            return ExprType == GameplayTagQueryExprType.AnyExprMatch || ExprType == GameplayTagQueryExprType.AllExprMatch || ExprType == GameplayTagQueryExprType.NoExprMatch;
        }

        // Exact 두 유형은 UE 5.6 원문에 문구가 없어 같은 꼴로 지었다
        /// <remarks>UE: UEditableGameplayTagQueryExpression_*::EmitTokens</remarks>
        private string GetDescriptionOpening()
        {
            switch (ExprType)
            {
                case GameplayTagQueryExprType.AnyTagsMatch:
                case GameplayTagQueryExprType.AnyExprMatch:
                {
                    return " ANY(";
                }
                case GameplayTagQueryExprType.AllTagsMatch:
                case GameplayTagQueryExprType.AllExprMatch:
                {
                    return " ALL(";
                }
                case GameplayTagQueryExprType.NoTagsMatch:
                case GameplayTagQueryExprType.NoExprMatch:
                {
                    return " NONE(";
                }
                case GameplayTagQueryExprType.AnyTagsExactMatch:
                {
                    return " ANY EXACT(";
                }
                case GameplayTagQueryExprType.AllTagsExactMatch:
                {
                    return " ALL EXACT(";
                }
                default:
                {
                    return "undefined";
                }
            }
        }

        /// <remarks>UE: UEditableGameplayTagQueryExpression::EmitTagTokens / EmitExprListTokens</remarks>
        private static void AppendListSeparator(StringBuilder description, bool isFirst)
        {
            if (description == null)
            {
                return;
            }

            if (!isFirst)
            {
                description.Append(',');
            }

            description.Append(' ');
        }
    }
}
