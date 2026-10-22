using Cinemachine;
using UnityEngine;

namespace Game.Settings
{
    /// <summary> FreeLook 카메라에 마우스 감도 설정을 적용하고, 설정 창이 열린 동안엔 카메라 회전 입력을 막는다. </summary>
    [RequireComponent(typeof(CinemachineFreeLook))]
    public class CameraLookSettings : MonoBehaviour
    {
        private CinemachineFreeLook _freeLook;
        private CinemachineInputProvider _inputProvider;
        private float _baseXSpeed;
        private float _baseYSpeed;

        private void Awake()
        {
            _freeLook = GetComponent<CinemachineFreeLook>();
            _inputProvider = GetComponent<CinemachineInputProvider>();

            // 프리팹에 설정된 속도를 배율 1의 기준으로 삼는다
            _baseXSpeed = _freeLook.m_XAxis.m_MaxSpeed;
            _baseYSpeed = _freeLook.m_YAxis.m_MaxSpeed;
        }

        private void OnEnable()
        {
            GameSettings.OnMouseSensitivityChanged += ApplySensitivity;
            SettingsMenu.OnOpenChanged += SetLookBlocked;
            ApplySensitivity(GameSettings.MouseSensitivity);
        }

        private void OnDisable()
        {
            GameSettings.OnMouseSensitivityChanged -= ApplySensitivity;
            SettingsMenu.OnOpenChanged -= SetLookBlocked;
        }

        private void ApplySensitivity(float sensitivity)
        {
            _freeLook.m_XAxis.m_MaxSpeed = _baseXSpeed * sensitivity;
            _freeLook.m_YAxis.m_MaxSpeed = _baseYSpeed * sensitivity;
        }

        private void SetLookBlocked(bool isBlocked)
        {
            if (_inputProvider != null)
            {
                _inputProvider.enabled = !isBlocked;
            }
        }
    }
}
