using UnityEngine;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>
    /// 질의 편집 창이 편집 동안만 쓰는 질의 사본. <see cref="UnityEditor.SerializedObject"/>로 그리려면 Unity 오브젝트여야 해서 ScriptableObject다.
    /// 저장하지 않으며 창이 닫힐 때 지운다.
    /// </summary>
    /// <remarks>UE: UEditableGameplayTagQuery</remarks>
    public sealed class EditableGameplayTagQuery : ScriptableObject
    {
        // 질의 편집 창이 이 직렬화 필드 이름으로 그린다. 이름을 바꾸면 GameplayTagQueryEditorWindow도 바꿔야 한다.
        [Tooltip("비우면 식에서 만든 설명을 쓴다.")]
        [SerializeField] private string _userDescription;
        [SerializeReference] private EditableGameplayTagQueryExpression _rootExpression;

        /// <summary>질의를 편집용 사본으로 옮긴다. 빈 질의면 루트 식이 없다.</summary>
        /// <param name="userDescription">질의의 사용자 설명. <see cref="GameplayTagQuery.GetDescription"/>은 자동 설명으로 대신할 수 있어 따로 받는다.</param>
        /// <remarks>UE: FGameplayTagQuery::CreateEditableQuery</remarks>
        public static EditableGameplayTagQuery CreateEditableQuery(GameplayTagQuery query, string userDescription)
        {
            var editableQuery = CreateInstance<EditableGameplayTagQuery>();
            editableQuery.hideFlags = HideFlags.DontSave;

            // UE는 사용자 설명만 옮긴다. 자동 설명은 다시 만들 때 새로 생긴다
            editableQuery._userDescription = userDescription;

            if (!query.IsEmpty())
            {
                query.GetQueryExpr(out GameplayTagQueryExpression rootExpr);
                editableQuery._rootExpression = EditableGameplayTagQueryExpression.FromQueryExpression(rootExpr);
            }

            return editableQuery;
        }

        /// <summary>편집용 사본으로 질의를 만든다. 루트 식이 없으면 사용자 설명까지 버린 빈 질의다.</summary>
        /// <remarks>UE: SGameplayTagQueryWidget::SaveToTagQuery</remarks>
        public GameplayTagQuery BuildQuery()
        {
            if (_rootExpression == null)
            {
                return new GameplayTagQuery();
            }

            GameplayTagQueryExpression rootExpr = EditableGameplayTagQueryExpression.ToQueryExpression(_rootExpression);
            return GameplayTagQuery.BuildQuery(rootExpr, _userDescription ?? string.Empty);
        }
    }
}
