using UnityEngine;

/// <summary>
/// Hiệu ứng "nhảy ra" cho loot khi zombie chết: bắn loot lên theo quỹ đạo
/// parabol (vận tốc đứng + hướng ngang ngẫu nhiên), xoay trong lúc bay,
/// hạ cánh về độ cao xuất phát rồi tiếp tục xoay nhẹ. Không cần Rigidbody,
/// không sửa prefab — tương thích với trigger-based pickup (Coin, ...).
/// </summary>
public class LootPop : MonoBehaviour
{
    [Header("Launch")]
    [Tooltip("Vận tốc đứng (lên) lúc bắn ra (m/s).")]
    public float upwardSpeed = 4.5f;

    [Tooltip("Vận tốc ngang tối đa (m/s), hướng ngẫu nhiên trong vòng tròn.")]
    public float horizontalSpeed = 2.5f;

    [Tooltip("Gia tốc trọng lực áp dụng cho loot (m/s^2).")]
    public float gravity = 12f;

    [Header("Spin")]
    [Tooltip("Tốc độ xoay khi đang bay (deg/s).")]
    public float launchSpinSpeed = 540f;

    [Tooltip("Tốc độ xoay sau khi hạ cánh (deg/s).")]
    public float idleSpinSpeed = 90f;

    private Vector3 velocity;
    private float spinSpeed;
    private bool landed;
    private float startY;

    /// <summary>True khi loot đã hạ cánh về độ cao xuất phát.</summary>
    public bool Landed => landed;

    void Awake()
    {
        enabled = false;
    }

    /// <summary>Khởi động hiệu ứng bắn ra từ vị trí hiện tại.</summary>
    public void Launch(Vector3 spawnPosition)
    {
        transform.position = spawnPosition;
        startY = spawnPosition.y;

        // Hướng ngang ngẫu nhiên trong vòng tròn.
        Vector2 h = Random.insideUnitCircle.normalized * horizontalSpeed;
        velocity = new Vector3(h.x, upwardSpeed, h.y);

        spinSpeed = launchSpinSpeed;
        landed = false;
        enabled = true;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (!landed)
        {
            velocity.y -= gravity * dt;
            Vector3 p = transform.position + velocity * dt;

            if (p.y <= startY && velocity.y <= 0f)
            {
                // Hạ cánh: chốt về startY, dập nảy nhẹ rồi dừng.
                p.y = startY;
                if (Mathf.Abs(velocity.y) > 1.5f)
                {
                    velocity.y = -velocity.y * 0.35f;
                    velocity.x *= 0.5f;
                    velocity.z *= 0.5f;
                }
                else
                {
                    velocity = Vector3.zero;
                    landed = true;
                    spinSpeed = idleSpinSpeed;
                }
            }

            transform.position = p;
        }

        // Xoay quanh trục Y (và hơi nghiêng khi bay).
        transform.Rotate(Vector3.up, spinSpeed * dt, Space.World);
    }
}
