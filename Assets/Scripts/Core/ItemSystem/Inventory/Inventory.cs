using System;
using System.Collections.Generic;

namespace Core.ItemSystem.Inventory
{
    // 아이템 보유 컨테이너. 소유자를 모르는 순수 POCO(무의존) — 캐릭터·ASC·장비·UI·씬을 참조하지 않는다(INV-6/7).
    // MonoBehaviour가 아니므로 씬 수명에 묶이지 않는다: 참조를 쥔 소유자가 사는 한 살아있다(D8).
    // 소유(누가 장기 보유하는가)는 이 클래스의 관심사가 아니다 — 지속 소유자 계층이 생기면 그가 생성·보유한다.
    public class Inventory
    {
        private readonly List<InventoryEntry> _entries = new();

        // 읽기 전용 노출. 소비처(장비·UI)는 이걸 읽고 OnChanged로 갱신한다(INV-8) — 직접 변형 금지.
        public IReadOnlyList<InventoryEntry> Entries => _entries;

        // 내용이 바뀔 때마다 1회 통지. 폴링 대신 구독으로 갱신(INV-8).
        public event Action OnChanged;

        // count개를 담는다. 스택 아이템은 병합, 그 외(장비 등)는 개별 엔트리.
        public void Add(ItemData data, int count = 1)
        {
            if (data == null || count <= 0)
            {
                return;
            }

            // INV-11: 카테고리가 아니라 IStackable 능력 유무로 분기한다 — 장비는 이 능력이 없어 아래 else로 간다.
            if (data is IStackable stackable)
            {
                AddStackable(data, stackable.MaxStack, count);
            }
            else
            {
                // 비스택: per-item 상태를 공유하면 안 되므로 개수만큼 개별 엔트리 + 각자 ItemInstance(D4).
                for (int i = 0; i < count; i++)
                {
                    _entries.Add(new InventoryEntry(data, 1, new ItemInstance(data)));
                }
            }

            OnChanged?.Invoke();
        }

        // 원자적 제거: 총 보유량이 부족하면 아무것도 지우지 않고 false를 돌려준다(부분 소비 방지).
        public bool Remove(ItemData data, int count = 1)
        {
            if (data == null || count <= 0 || CountOf(data) < count)
            {
                return false;
            }

            int remaining = count;
            // 뒤에서부터 지워야 RemoveAt이 인덱스를 어긋내지 않는다.
            for (int i = _entries.Count - 1; i >= 0 && remaining > 0; i--)
            {
                InventoryEntry entry = _entries[i];
                if (entry.Data != data)
                {
                    continue;
                }

                int take = Math.Min(entry.Count, remaining);
                entry.Count -= take;
                remaining -= take;
                if (entry.Count == 0)
                {
                    _entries.RemoveAt(i);
                }
            }

            OnChanged?.Invoke();
            return true;
        }

        // 특정 엔트리 지목 제거 (예: "이 장비를" 장착/폐기). 장비는 엔트리가 곧 하나의 개체다.
        public bool Remove(InventoryEntry entry)
        {
            if (entry == null || !_entries.Remove(entry))
            {
                return false;
            }

            OnChanged?.Invoke();
            return true;
        }

        public int CountOf(ItemData data)
        {
            int sum = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Data == data)
                {
                    sum += _entries[i].Count;
                }
            }

            return sum;
        }

        // 카테고리 파생 질의(D5) — 저장은 단일 컬렉션, 필터는 질의로. 예: Query(d => d is IEquippable).
        public IEnumerable<InventoryEntry> Query(Func<ItemData, bool> predicate)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (predicate(_entries[i].Data))
                {
                    yield return _entries[i];
                }
            }
        }

        // 전량 비움. "사망 시 소실"은 소유자가 이걸 호출하는 것이지 GameObject 파괴가 아니다(D8).
        public void Clear()
        {
            if (_entries.Count == 0)
            {
                return;
            }

            _entries.Clear();
            OnChanged?.Invoke();
        }

        private void AddStackable(ItemData data, int maxStack, int count)
        {
            // 1) 같은 아이템의 미충족(Count < maxStack) 엔트리부터 채운다.
            for (int i = 0; i < _entries.Count && count > 0; i++)
            {
                InventoryEntry entry = _entries[i];
                if (entry.Data != data || entry.Count >= maxStack)
                {
                    continue;
                }

                int moved = Math.Min(maxStack - entry.Count, count);
                entry.Count += moved;
                count -= moved;
            }

            // 2) 남으면 maxStack 단위로 새 엔트리를 쌓는다. 스택 아이템은 무상태 → Instance는 null.
            while (count > 0)
            {
                int chunk = Math.Min(maxStack, count);
                _entries.Add(new InventoryEntry(data, chunk, null));
                count -= chunk;
            }
        }
    }
}
