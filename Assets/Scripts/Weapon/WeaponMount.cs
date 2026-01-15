using UnityEngine;

public class WeaponMount : MonoBehaviour
{
    private WeaponBase _currentWeapon;

    public void Equip(GameObject weaponPrefab, Collider2D ownerCollider, Transform realOwner)
    {
        if (_currentWeapon != null)
        {
            _currentWeapon.ForceDrop();
        }

        if (weaponPrefab == null) return;

        GameObject wpObj = Instantiate(weaponPrefab, transform);
        wpObj.transform.localPosition = Vector3.zero;
        wpObj.transform.localRotation = Quaternion.identity;

        if (wpObj.TryGetComponent<WeaponBase>(out var weaponLogic))
        {
            _currentWeapon = weaponLogic;
            _currentWeapon.Initialize(realOwner, ownerCollider);
        }
        else
        {
            Destroy(wpObj);
        }
    }

    public void ForceDrop()
    {
        if (_currentWeapon != null)
        {
            _currentWeapon.ForceDrop();
            _currentWeapon = null;
        }
    }
}