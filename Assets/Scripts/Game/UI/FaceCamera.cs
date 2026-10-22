using UnityEngine;

namespace Game.UI
{
    /// <summary>월드에 띄운 글자·막대가 늘 카메라를 보게 한다.</summary>
    public class FaceCamera : MonoBehaviour
    {
        private Transform _cameraTransform;

        private void Start()
        {
            Camera mainCamera = Camera.main;
            _cameraTransform = mainCamera != null ? mainCamera.transform : null;
        }

        private void LateUpdate()
        {
            if (_cameraTransform != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - _cameraTransform.position);
            }
        }
    }
}
