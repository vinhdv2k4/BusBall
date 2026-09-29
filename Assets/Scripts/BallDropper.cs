using System.Collections;
using UnityEngine;
using DG.Tweening;
using BallDropParty.Gameplay;

public class BallDropper : MonoBehaviour
{
    [Header("Ball Physics Config")]
    [SerializeField] private BallPhysicsConfig ballPhysicsConfig;
    [Header("Spawn")]
    public GameObject ballPrefab;
    public Transform ballParent;
    public float ballSpawnRadius = 0.25f;
    public float spawnHeight = 0.5f;
    [SerializeField, Min(0.01f)] private float rowBallSpacing = 0.70f;
    [SerializeField, Min(0.01f)] private float rowSpacing = 0.70f;

    [Header("Arc & Physics Drop")]
    [SerializeField, Min(0f)] private float dropJumpHeight = 0.5f;
    [SerializeField, Min(0f)] private float initialFallSpeed = 2.5f;
    [Tooltip("Do cao cuc dai cua duong cong bay tu xe xuong conveyor")]
    public float arcHeight = 1.2f;
    [Tooltip("Thoi gian bay cua bong tu xe vao conveyor (giay)")]
    public float dropDuration = 0.38f;
    [Tooltip("Do lech ngau nhien cua quy dao bong")]
    public float trajectorySpread = 0.2f;
    [Tooltip("Khoang thoi gian delay giua cac bong khi tha")]
    public float spawnInterval = 0.13f;

    [Header("Free Physics Launch (No Conveyor)")]
    public float launchForceHorizontal = 1.8f;
    public float launchForceUp = 3.2f;

    public void SpawnBallsAtTransform(Transform releaseTransform, CarType carType)
    {
        SpawnBallsAtTransform(releaseTransform, carType, ColorId.Blue);
    }

    public void SpawnBallsAtTransform(Transform releaseTransform, CarType carType, ColorId colorId)
    {
        SpawnBallsAtTransform(releaseTransform, carType, colorId, null);
    }

    public void SpawnBallsAtTransform(Transform releaseTransform, CarType carType, ColorId colorId, System.Action onComplete)
    {
        EnsurePrefab();

        if (ballParent == null)
            ballParent = transform;

        int count = GetBallCount(carType);
        Vector3 origin = releaseTransform != null ? releaseTransform.position : transform.position;

        StartCoroutine(SpawnBallsSequentially(origin, count, colorId, null, 0f, onComplete));
    }

    public void SpawnBallsOnConveyor(Transform releaseTransform, Transform targetTransform, ConveyorManager conveyor, CarType carType, ColorId colorId)
    {
        SpawnBallsOnConveyor(releaseTransform, targetTransform, conveyor, carType, colorId, null);
    }

    public void SpawnBallsOnConveyor(Transform releaseTransform, Transform targetTransform, ConveyorManager conveyor, CarType carType, ColorId colorId, System.Action onComplete)
    {
        EnsurePrefab();

        if (releaseTransform == null)
            releaseTransform = transform;

        if (conveyor == null)
        {
            SpawnBallsAtTransform(releaseTransform, carType, colorId, onComplete);
            return;
        }

        int count = GetBallCount(carType);
        Vector3 zoneTarget = targetTransform != null ? targetTransform.position : conveyor.transform.position;
        StartCoroutine(SpawnBallsSequentially(releaseTransform, count, colorId, conveyor, zoneTarget, onComplete));
    }

    private void EnsurePrefab()
    {
        if (ballPrefab == null)
        {
            ballPrefab = Resources.Load<GameObject>("Ball");
        }
        GameplayObjectPool.Instance?.Configure(ballPrefab);
    }

    private int GetBallCount(CarType carType)
    {
        return carType switch
        {
            CarType.Small4Slots => 4,
            CarType.Medium6Slots => 6,
            CarType.Big9Slots => 9,
            _ => 4
        };
    }

    private IEnumerator SpawnBallsSequentially(Transform releaseTransform, int count, ColorId colorId, ConveyorManager conveyor, Vector3 zoneTarget, System.Action onComplete)
    {
        GetRowCounts(count, out int topCount, out int bottomCount);
        int columnCount = Mathf.Max(topCount, bottomCount);
        SoundManager.Instance?.PlayCarDropBall();
        for (int column = 0; column < columnCount; column++)
        {
            if (column < topCount)
            {
                int index = column;
                SpawnSingleBall(releaseTransform, index, count, colorId, conveyor, zoneTarget,
                    GetRowOffset(column, topCount, true));
            }

            if (column < bottomCount)
            {
                int index = topCount + column;
                SpawnSingleBall(releaseTransform, index, count, colorId, conveyor, zoneTarget,
                    GetRowOffset(column, bottomCount, false));
            }

            if (spawnInterval > 0f)
                yield return new WaitForSeconds(spawnInterval);
        }

        // Thả bóng xong là xe đi luôn ngay lập tức
        onComplete?.Invoke();
    }

    private IEnumerator SpawnBallsSequentially(Vector3 origin, int count, ColorId colorId, ConveyorManager conveyor, float startProgress, System.Action onComplete)
    {
        Vector3 defaultZone = conveyor != null ? conveyor.transform.position : Vector3.zero;
        return SpawnBallsSequentially(transform, count, colorId, conveyor, defaultZone, onComplete);
    }

    private void GetRowCounts(int total, out int topCount, out int bottomCount)
    {
        topCount = total / 2;
        bottomCount = total - topCount;
    }

    private Vector3 GetRowOffset(int column, int rowCount, bool isTopRow)
    {
        float centeredX = (column - (rowCount - 1) * 0.5f) * rowBallSpacing;
        float rowY = isTopRow ? rowSpacing * 0.5f : -rowSpacing * 0.5f;
        return new Vector3(centeredX, rowY, 0f);
    }

    private void SpawnSingleBall(Transform releaseTransform, int index, int total, ColorId colorId, ConveyorManager conveyor, Vector3 zoneTarget, Vector3 rowOffset)
    {
        if (ballPrefab == null) return;

        float angle = (index + UnityEngine.Random.Range(-0.2f, 0.2f)) * Mathf.PI * 2f / Mathf.Max(1, total);
        
        // Sắp xếp các quả bóng theo trục Ox và Oy tại vị trí Ball Drop
        Vector3 basePosition = releaseTransform != null ? releaseTransform.position : transform.position;
        Vector3 spawnPos = basePosition + rowOffset;

        var ball = GameplayObjectPool.Instance != null
            ? GameplayObjectPool.Instance.GetBall(spawnPos, UnityEngine.Random.rotation, ballParent)
            : Instantiate(ballPrefab, spawnPos, UnityEngine.Random.rotation, ballParent);
        ball.name = $"Ball_{colorId}_{index + 1}";

        var ballItem = ball.GetComponent<BallItem>();
        if (ballItem == null) ballItem = ball.AddComponent<BallItem>();
        ballItem.Init(colorId);

        var ballCtrl = ball.GetComponent<BallController>();
        if (ballCtrl != null) ballCtrl.Init(colorId);

        var rb = ball.GetComponent<Rigidbody>();
        if (ballPhysicsConfig != null)
        {
            ballPhysicsConfig.ApplyTo(rb);
        }
        else if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezePositionZ;
            rb.linearDamping = 0.08f;
            rb.angularDamping = 0.05f;
        }

        if (rb != null)
        {
            // Đảm bảo luôn khóa trục Z để bóng không bị bay lệch ra khỏi mặt phẳng màn hình
            rb.constraints = RigidbodyConstraints.FreezePositionZ;
            rb.isKinematic = false;
            rb.useGravity = true;

            // Hướng bay từ vị trí Ball Drop trên xe về phía Zone hứng bóng
            Vector3 toTarget = zoneTarget != Vector3.zero ? (zoneTarget - spawnPos) : Vector3.down;
            Vector3 horizontalDir = new Vector3(toTarget.x, 0f, 0f);
            if (Mathf.Abs(horizontalDir.x) > 0.001f)
                horizontalDir = horizontalDir.normalized;
            else if (Mathf.Abs(rowOffset.x) > 0.001f)
                horizontalDir = new Vector3(Mathf.Sign(rowOffset.x), 0f, 0f);
            else
                horizontalDir = new Vector3(Mathf.Cos(angle), 0f, 0f).normalized;

            float upForce = Mathf.Max(2.8f, launchForceUp);
            float hForce = Mathf.Max(1.4f, launchForceHorizontal);

            Vector3 launchVelocity = horizontalDir * hForce + Vector3.up * upForce;
            launchVelocity.z = 0f;
            rb.linearVelocity = launchVelocity;
            rb.angularVelocity = new Vector3(0f, 0f, UnityEngine.Random.Range(-15f, 15f));
        }
    }
}
