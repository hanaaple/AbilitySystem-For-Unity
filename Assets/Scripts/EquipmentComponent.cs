using UnityEngine;

public class EquipmentComponent : MonoBehaviour
{
    // TODO: 슬롯 기반 다중 장착으로 확장 예정 (현재 단일 무기만 지원)
    [SerializeField] private WeaponInstance _currentWeapon;

    public WeaponInstance CurrentWeapon => _currentWeapon;

    public void Equip(WeaponData weaponData)
    {
        // TODO: weaponData.weaponPrefab으로 WeaponInstance 생성 후 장착
        // TODO: 장착 무기의 스탯을 AbilitySystemComponent에 적용
    }

    public void Unequip()
    {
        if (_currentWeapon == null)
        {
            return;
        }

        // TODO: AbilitySystemComponent에서 무기 스탯 제거
        Destroy(_currentWeapon.gameObject);
        _currentWeapon = null;
    }
}
