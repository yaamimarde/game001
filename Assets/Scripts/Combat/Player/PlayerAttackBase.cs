using UnityEngine;

public abstract class PlayerAttackBase : MonoBehaviour
{
    protected Character characterComponent;
    protected Animator anim;

    [Header("基类公共设置")]
    public float attackCooldown = 0.5f;
    protected float nextAttackTime = 0f;

    protected virtual void Start()
    {
        characterComponent = GetComponent<Character>();
        anim = GetComponent<Animator>();
    }

    public virtual bool CanAttack()
    {
        return characterComponent != null
            && characterComponent.hp > 0
            && Time.time >= nextAttackTime;
    }

    public abstract void ExecuteAttack();
}
