using System;
using UnityEngine;

namespace Game.Settings
{
    /// <summary> 유저 설정값. PlayerPrefs에 저장되어 다음 실행에도 유지된다. </summary>
    public static class GameSettings
    {
        public const float MinMouseSensitivity = 0.1f;
        public const float MaxMouseSensitivity = 3f;
        private const float DefaultMouseSensitivity = 1f;
        private const string MouseSensitivityKey = "Settings.MouseSensitivity";

        /// <summary> 마우스 감도 배율이 바뀌면 새 값과 함께 알린다. </summary>
        public static event Action<float> OnMouseSensitivityChanged;

        /// <summary> 카메라 기본 회전 속도에 곱하는 배율. </summary>
        public static float MouseSensitivity
        {
            get => PlayerPrefs.GetFloat(MouseSensitivityKey, DefaultMouseSensitivity);
            set
            {
                float clamped = Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity);
                PlayerPrefs.SetFloat(MouseSensitivityKey, clamped);
                PlayerPrefs.Save();
                OnMouseSensitivityChanged?.Invoke(clamped);
            }
        }
    }
}
