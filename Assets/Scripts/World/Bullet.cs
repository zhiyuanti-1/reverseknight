using UnityEngine;

// 炮台子弹：沿世界水平方向匀速飞行，不参与重力；靠 Hazard 触发器杀死玩家。
public class Bullet : MonoBehaviour
{
    public int direction = 1;
    public float speed = 13f;
    public float life = 4f;

    float age;

    void FixedUpdate()
    {
        age += Time.fixedDeltaTime;
        if (age >= life)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += new Vector3(direction * speed * Time.fixedDeltaTime, 0f, 0f);
    }
}
