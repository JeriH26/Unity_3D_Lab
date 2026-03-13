using UnityEngine;

/// <summary>
/// Spawns and manages objects with pooling support.
/// Uses a simple object pool to avoid excessive instantiation/destruction.
/// </summary>
public class ObjectSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private int poolSize = 20;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private float spawnHeightOffset = 0.5f;
    [SerializeField] private int maxActiveObjects = 10;

    [Header("Lifetime")]
    [SerializeField] private bool useLifetime = true;
    [SerializeField] private float minLifetime = 2f;
    [SerializeField] private float maxLifetime = 6f;

    private GameObject[] _pool;
    private float[] _lifetimes;
    private float _spawnTimer;
    private int _activeCount;

    private void Start()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        _pool = new GameObject[poolSize];
        _lifetimes = new float[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            _pool[i] = Instantiate(prefab, transform);
            _pool[i].SetActive(false);
        }
    }

    private void Update()
    {
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= spawnInterval && _activeCount < maxActiveObjects)
        {
            _spawnTimer = 0f;
            SpawnObject();
        }

        if (useLifetime)
            UpdateLifetimes();
    }

    private void SpawnObject()
    {
        int index = GetInactivePoolIndex();
        if (index < 0) return;

        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position +
            new Vector3(randomCircle.x, spawnHeightOffset, randomCircle.y);

        _pool[index].transform.position = spawnPosition;
        _pool[index].transform.rotation = Random.rotation;
        _pool[index].SetActive(true);
        _lifetimes[index] = Random.Range(minLifetime, maxLifetime);
        _activeCount++;
    }

    private void UpdateLifetimes()
    {
        for (int i = 0; i < poolSize; i++)
        {
            if (!_pool[i].activeSelf) continue;

            _lifetimes[i] -= Time.deltaTime;
            if (_lifetimes[i] <= 0f)
            {
                _pool[i].SetActive(false);
                _activeCount--;
            }
        }
    }

    private int GetInactivePoolIndex()
    {
        for (int i = 0; i < poolSize; i++)
        {
            if (!_pool[i].activeSelf)
                return i;
        }
        return -1;
    }

    /// <summary>Despawns all active pooled objects immediately.</summary>
    public void DespawnAll()
    {
        for (int i = 0; i < poolSize; i++)
        {
            if (_pool[i].activeSelf)
                _pool[i].SetActive(false);
        }
        _activeCount = 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
