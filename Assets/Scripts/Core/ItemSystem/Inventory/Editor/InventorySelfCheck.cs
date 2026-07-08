using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Core.ItemSystem.Inventory.Editor
{
    // I1 임시 검증(D8 인프라 결정 전까지). 게임 코드가 무-asmdef라 정식 EditMode 테스트를 못 붙여,
    // Editor 어셈블리에서 Inventory를 구동해 콘솔로 결과를 확인한다. 정식 EditMode 테스트가 생기면 이 파일은 제거한다.
    public static class InventorySelfCheck
    {
        [MenuItem("Tools/Inventory/Run Self-Check")]
        public static void Run()
        {
            int passed = 0;
            int failed = 0;

            void Check(string label, bool condition)
            {
                if (condition)
                {
                    passed++;
                    Debug.Log($"[Inventory PASS] {label}");
                }
                else
                {
                    failed++;
                    Debug.LogError($"[Inventory FAIL] {label}");
                }
            }

            // --- 소비(스택): maxStack=3 으로 병합·오버플로 ---
            ConsumeItemAsset potion = MakeConsume(maxStack: 3);
            ItemSystem.Inventory.Inventory inv = new ItemSystem.Inventory.Inventory();
            int changed = 0;
            inv.OnChanged += () => changed++;

            inv.Add(potion, 2);
            Check("소비 2개 → 엔트리 1 · Count 2", inv.Entries.Count == 1 && inv.CountOf(potion) == 2);

            inv.Add(potion, 2); // 3 채우고 1 넘침
            Check("소비 누적 4(max 3) → 엔트리 2 (3+1)", inv.Entries.Count == 2 && inv.CountOf(potion) == 4);
            Check("OnChanged Add마다 1회 = 2회", changed == 2);

            // --- 장비(비스택): 개별 엔트리 + per-item ItemInstance ---
            EquipItemAsset sword = MakeEquip();
            ItemSystem.Inventory.Inventory inv2 = new ItemSystem.Inventory.Inventory();
            inv2.Add(sword, 2);
            Check("장비는 IStackable 아님", !(sword is IStackable));
            Check("장비 2개 → 엔트리 2 · 각 Count 1", inv2.Entries.Count == 2 && inv2.Entries[0].Count == 1);
            Check("장비 엔트리는 ItemInstance 보유", inv2.Entries[0].Instance != null);

            // --- 제거(원자적) ---
            Check("부족분 제거는 거부 · 무변경", !inv2.Remove(sword, 5) && inv2.Entries.Count == 2);
            Check("정상 제거", inv2.Remove(sword, 1) && inv2.Entries.Count == 1);

            // --- Query(카테고리 파생, D5) ---
            ItemSystem.Inventory.Inventory mixed = new ItemSystem.Inventory.Inventory();
            mixed.Add(potion, 1);
            mixed.Add(sword, 1);
            int equipCount = 0;
            foreach (InventoryEntry entry in mixed.Query(d => d is IEquippable))
            {
                equipCount++;
            }
            Check("Query(IEquippable) → 장비만 1", equipCount == 1);

            Object.DestroyImmediate(potion);
            Object.DestroyImmediate(sword);

            Debug.Log($"[Inventory Self-Check] passed {passed} / failed {failed}");
        }

        // maxStack은 private 직렬화 필드라 테스트 값 주입에 리플렉션을 쓴다(에디터 전용 검증 한정).
        private static ConsumeItemAsset MakeConsume(int maxStack)
        {
            ConsumeItemAsset item = ScriptableObject.CreateInstance<ConsumeItemAsset>();
            typeof(ConsumeItemAsset)
                .GetField("maxStack", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(item, maxStack);
            return item;
        }

        private static EquipItemAsset MakeEquip()
        {
            return ScriptableObject.CreateInstance<EquipItemAsset>();
        }
    }
}
