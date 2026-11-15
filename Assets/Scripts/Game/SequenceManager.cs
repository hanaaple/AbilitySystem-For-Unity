using UnityEngine;
using UnityEngine.Serialization;

namespace Game
{
    public class SequenceManager : MonoBehaviour
    {
        private static SequenceManager Instance;

        [SerializeField] [FormerlySerializedAs("playerControllerPrefab")] private PlayerController _playerControllerPrefab;
        [SerializeField] [FormerlySerializedAs("playerCharacterPrefab")] private PlayerCharacter _playerCharacterPrefab;
        [SerializeField] [FormerlySerializedAs("spawnPoint")] private GameObject _spawnPoint;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SpawnPlayer();
        }

        private void SpawnPlayer()
        {
            Vector3 spawnPos = _spawnPoint != null ? _spawnPoint.transform.position : Vector3.zero;

            PlayerCharacter character = Instantiate(_playerCharacterPrefab, spawnPos, Quaternion.identity);
            PlayerController controller = Instantiate(_playerControllerPrefab, Vector3.zero, Quaternion.identity);

            controller.Possess(character);
        }
    }
}
