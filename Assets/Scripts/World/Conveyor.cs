using UnityEngine;

// 传送带：贴在实心块上，玩家站在上面时把水平速度目标推向带子方向。
// 方向是世界方向，不随重力翻转改变；总速度由 PlayerMotor 限制在最大跑速内。
public class Conveyor : MonoBehaviour
{
    public int direction = 1;
    public float speed = 3f;
    public float speedLimitBonus;

    public float BeltSpeed { get { return direction * speed; } }
}
