using System;
using System.Collections.Generic;
using System.Reflection;
using Character;
using Core.AbilitySystem;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Test.Editor
{
    /// <summary>
    /// <see cref="AggregatorPlayModeTest"/>가 쓸 실제 GameplayEffect·Attribute 에셋을 만들고, 현재 씬에
    /// ASC + 테스트 컴포넌트를 배선한다. 리플렉션으로 private [SerializeField]를 채워 조립하지만, 저장 후에는
    /// 인스펙터로 만든 것과 동일한 직렬화 .asset이 된다(라운드트립 검증 포함). 클릭 → Play 로 검증 완료.
    /// </summary>
    public static class AggregatorPlayModeTestSetup
    {
        private const string Folder = "Assets/_PlayModeTest";
        private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("Tools/Ability System/Setup PlayMode Test")]
        public static void Setup()
        {
            EnsureFolder();

            AttributeDefinitionAsset init = CreateAttributeInit($"{Folder}/AttrInit_PlayModeTest.asset");
            GameplayEffectAsset healthBoost = CreateGe($"{Folder}/GE_HealthBoost.asset", GameplayEffectType.Infinite,
                MakeModifier(CharacterAttributeSet.Health, GameplayModifierOperation.AddBase, 100f));
            GameplayEffectAsset speedLive = CreateGe($"{Folder}/GE_SpeedFromHealth_Live.asset", GameplayEffectType.Infinite,
                MakeAttributeBasedModifier(CharacterAttributeSet.Speed, CharacterAttributeSet.Health, snapshot: false, coefficient: 0.5f));
            GameplayEffectAsset speedSnap = CreateGe($"{Folder}/GE_SpeedFromHealth_Snapshot.asset", GameplayEffectType.Infinite,
                MakeAttributeBasedModifier(CharacterAttributeSet.Speed, CharacterAttributeSet.Health, snapshot: true, coefficient: 0.5f));
            GameplayEffectAsset instantHeal = CreateGe($"{Folder}/GE_InstantHeal.asset", GameplayEffectType.Instant,
                MakeModifier(CharacterAttributeSet.Health, GameplayModifierOperation.AddBase, 30f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            WireScene(init, healthBoost, speedLive, speedSnap, instantHeal);

            Debug.Log("[PlayMode Setup] 에셋 5개 생성 + 씬 배선 완료. Play를 눌러 검증하라 → 콘솔 '[PlayMode Self-Check] passed N / failed N'.");
        }

        // ── 씬 배선 ──
        private static void WireScene(AttributeDefinitionAsset init, GameplayEffectAsset healthBoost,
            GameplayEffectAsset speedLive, GameplayEffectAsset speedSnap, GameplayEffectAsset instantHeal)
        {
            const string goName = "AggregatorPlayModeTest";

            // 재실행 시 중복 방지 — 기존 것 제거 후 재생성.
            GameObject existing = GameObject.Find(goName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            GameObject go = new GameObject(goName);
            AbilitySystemComponent asc = go.AddComponent<AbilitySystemComponent>();
            AggregatorPlayModeTest test = go.AddComponent<AggregatorPlayModeTest>();

            // ASC.attributeInitData 주입 → Awake에서 실제 초기화 경로를 타게.
            SetSerializedRef(asc, "attributeInitData", init);

            SetSerializedRef(test, "attributeInitData", init);
            SetSerializedRef(test, "geHealthBoost", healthBoost);
            SetSerializedRef(test, "geSpeedFromHealthLive", speedLive);
            SetSerializedRef(test, "geSpeedFromHealthSnapshot", speedSnap);
            SetSerializedRef(test, "geInstantHeal", instantHeal);

            EditorSceneManager.MarkSceneDirty(go.scene);
            EditorSceneManager.SaveOpenScenes();
        }

        private static void SetSerializedRef(UnityEngine.Object target, string propName, UnityEngine.Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogError($"[PlayMode Setup] 직렬화 프로퍼티 '{propName}'을 {target.GetType().Name}에서 찾지 못함.");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 에셋 빌더 ──
        private static AttributeDefinitionAsset CreateAttributeInit(string path)
        {
            AttributeDefinitionAsset asset = LoadOrCreate<AttributeDefinitionAsset>(path);

            AttributeFieldDefinition health = MakeField("health", 100f);
            AttributeFieldDefinition speed = MakeField("speed", 0f);

            object setDef = Activator.CreateInstance(typeof(AttributeSetDefinition));
            SetPrivateField(setDef, "attributeSetTypeName", typeof(CharacterAttributeSet).AssemblyQualifiedName);
            SetPrivateField(setDef, "attributes", new List<AttributeFieldDefinition> { health, speed });

            SetPrivateField(asset, "attributeSets", new List<AttributeSetDefinition> { (AttributeSetDefinition)setDef });
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static AttributeFieldDefinition MakeField(string fieldName, float baseValue)
        {
            object field = Activator.CreateInstance(typeof(AttributeFieldDefinition));
            SetPrivateField(field, "fieldName", fieldName);
            SetPrivateField(field, "baseValue", baseValue);
            return (AttributeFieldDefinition)field;
        }

        private static GameplayEffectAsset CreateGe(string path, GameplayEffectType type, GameplayModifier modifier)
        {
            GameplayEffectAsset ge = LoadOrCreate<GameplayEffectAsset>(path);
            SetPrivateField(ge, "type", type);
            SetPrivateField(ge, "modifiers", new List<GameplayModifier> { modifier });
            EditorUtility.SetDirty(ge);
            return ge;
        }

        private static GameplayModifier MakeModifier(GameplayAttributeHandle target, GameplayModifierOperation op, float magnitude)
        {
            object boxed = default(GameplayModifier);
            SetPrivateField(boxed, "attribute", new GameplayAttribute(target));
            SetPrivateField(boxed, "operation", op);
            SetPrivateField(boxed, "magnitudeCalculationType", MagnitudeCalculationType.ScalableFloat);
            SetPrivateField(boxed, "magnitude", magnitude);
            return (GameplayModifier)boxed;
        }

        private static GameplayModifier MakeAttributeBasedModifier(GameplayAttributeHandle target, GameplayAttributeHandle captured, bool snapshot, float coefficient)
        {
            GameplayEffectAttributeCaptureDefinition captureDef =
                new GameplayEffectAttributeCaptureDefinition(captured, AttributeCaptureSource.Source, snapshot);

            AttributeBasedMagnitude attributeBased = new AttributeBasedMagnitude();
            SetPrivateField(attributeBased, "backingAttribute", captureDef);
            SetPrivateField(attributeBased, "captureValueType", AttributeCaptureValueType.CurrentValue);
            SetPrivateField(attributeBased, "coefficient", coefficient);
            SetPrivateField(attributeBased, "preMultiplyAdditive", 0f);
            SetPrivateField(attributeBased, "postMultiplyAdditive", 0f);

            object boxed = default(GameplayModifier);
            SetPrivateField(boxed, "attribute", new GameplayAttribute(target));
            SetPrivateField(boxed, "operation", GameplayModifierOperation.AddBase);
            SetPrivateField(boxed, "magnitudeCalculationType", MagnitudeCalculationType.AttributeBased);
            SetPrivateField(boxed, "attributeBased", attributeBased);
            return (GameplayModifier)boxed;
        }

        // ── 유틸 ──
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets", "_PlayModeTest");
            }
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, NonPublicInstance);
            if (field == null)
            {
                throw new InvalidOperationException($"필드 '{name}'을 {target.GetType().Name}에서 찾지 못함.");
            }

            field.SetValue(target, value);
        }
    }
}
