using UnityEngine;

public class SequenceManager : MonoBehaviour
{
    private static SequenceManager _instance;

    [SerializeField] private PlayerController playerControllerPrefab;
    [SerializeField] private PlayerCharacter playerCharacterPrefab;
    [SerializeField] private GameObject spawnPoint;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;

        PlayerCharacter character = Instantiate(playerCharacterPrefab, spawnPos, Quaternion.identity);
        PlayerController controller = Instantiate(playerControllerPrefab, Vector3.zero, Quaternion.identity);

        controller.Possess(character);
    }
}
