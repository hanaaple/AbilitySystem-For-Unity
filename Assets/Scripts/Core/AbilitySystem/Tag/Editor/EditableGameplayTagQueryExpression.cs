using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>
    /// 질의 편집 창에서 고치는 식 노드 하나. 런타임 <see cref="GameplayTagQueryExpression"/>은 직렬화되지 않아 <see cref="UnityEditor.SerializedProperty"/>로 그릴 수 없으므로,
    /// 편집 동안만 이 모양으로 바꿔 두고 끝나면 다시 질의로 만든다. 태그는 <see cref="GameplayTagContainer"/>라 컨테이너 드로어·피커를 그대로 쓴다.
    /// </summary>
    /// <remarks>UE: UEditableGameplayTagQueryExpression</remarks>
    [Serializable]
    public sealed class EditableGameplayTagQueryExpression
    {
        // 질의 편집 창이 이 직렬화 필드 이름으로 그린다. 이름을 바꾸면 GameplayTagQueryEditorWindow도 바꿔야 한다.
        [SerializeField] private GameplayTagQueryExprType _exprType;
        [SerializeField] private GameplayTagContainer _tags = new();
        [SerializeReference] private List<EditableGameplayTagQueryExpression> _expressions = new();

        public EditableGameplayTagQueryExpression()
        {
        }

        public EditableGameplayTagQueryExpression(GameplayTagQueryExprType exprType)
        {
            _exprType = exprType;
        }

        /// <summary>런타임 식을 편집용 노드로 옮긴다. 하위 식도 따라 옮긴다.</summary>
        /// <remarks>UE: FQueryEvaluator::ReadEditableQueryExpr</remarks>
        public static EditableGameplayTagQueryExpression FromQueryExpression(GameplayTagQueryExpression expr)
        {
            var editableExpr = new EditableGameplayTagQueryExpression(expr.ExprType);

            if (expr.UsesTagSet())
            {
                editableExpr._tags = GameplayTagContainer.CreateFromArray(expr.TagSet);
            }
            else if (expr.UsesExprSet())
            {
                foreach (GameplayTagQueryExpression childExpr in expr.ExprSet)
                {
                    editableExpr._expressions.Add(FromQueryExpression(childExpr));
                }
            }

            return editableExpr;
        }

        /// <summary>
        /// 편집용 노드에서 런타임 식을 만든다. 쓰지 않는 쪽(태그 유형의 하위 식, 식 유형의 태그)은 버린다.
        /// 빈 하위 식 칸(null)은 UE처럼 Undefined 식이 된다.
        /// </summary>
        /// <remarks>UE: UEditableGameplayTagQueryExpression::EmitTokens</remarks>
        public static GameplayTagQueryExpression ToQueryExpression(EditableGameplayTagQueryExpression editableExpr)
        {
            var expr = new GameplayTagQueryExpression();
            if (editableExpr == null)
            {
                return expr;
            }

            expr.ExprType = editableExpr._exprType;

            if (expr.UsesTagSet())
            {
                expr.AddTags(editableExpr._tags);
            }
            else if (expr.UsesExprSet())
            {
                foreach (EditableGameplayTagQueryExpression childExpr in editableExpr._expressions)
                {
                    expr.AddExpr(ToQueryExpression(childExpr));
                }
            }

            return expr;
        }
    }
}
