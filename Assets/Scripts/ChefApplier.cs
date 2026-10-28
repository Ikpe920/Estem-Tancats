using UnityEngine;

// s'executa abans que els altres scripts, perquè PlayerHealth i LifeHearts ja vegin els cors del xef
[DefaultExecutionOrder(-200)]
public class ChefApplier : MonoBehaviour
{
    void Awake()
    {
        ChefInfo chef = Chefs.Current;

        PlayerHealth health = GetComponent<PlayerHealth>();
        PlayerMovement movement = GetComponent<PlayerMovement>();
        PlayerShooting shooting = GetComponent<PlayerShooting>();
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (health != null) health.maxLives = chef.maxLives;

        bool customSprites = false;
        if (movement != null)
        {
            movement.speed += chef.speedBonus;
            movement.dashCooldown = Mathf.Max(0.2f, movement.dashCooldown + chef.dashCooldownDelta);
            customSprites = LoadSprites(chef, movement);
        }

        if (shooting != null)
        {
            shooting.fireRate = Mathf.Clamp(shooting.fireRate + chef.fireRateDelta, 0.08f, 1f);
            shooting.damage += chef.damageBonus;
            shooting.pierce += chef.pierce;
            shooting.explosionRadius += chef.explosionRadius;
        }

        if (chef.dashDamage > 0)
        {
            DashDamage dd = GetComponent<DashDamage>();
            if (dd == null) dd = gameObject.AddComponent<DashDamage>();
            dd.damage += chef.dashDamage;
        }

        // sense sprites propis, es tenyeix el xef de sempre perquè es distingeixin
        if (sr != null) sr.color = customSprites ? Color.white : chef.tint;
    }

    // busca Resources/Chefs/<prefix>_Up, _Down, _Left i _Right
    static bool LoadSprites(ChefInfo chef, PlayerMovement movement)
    {
        if (string.IsNullOrEmpty(chef.spritePrefix)) return false;

        Sprite up = Resources.Load<Sprite>("Chefs/" + chef.spritePrefix + "_Up");
        Sprite down = Resources.Load<Sprite>("Chefs/" + chef.spritePrefix + "_Down");
        Sprite left = Resources.Load<Sprite>("Chefs/" + chef.spritePrefix + "_Left");
        Sprite right = Resources.Load<Sprite>("Chefs/" + chef.spritePrefix + "_Right");

        if (up != null) movement.spriteUp = up;
        if (down != null) movement.spriteDown = down;
        if (left != null) movement.spriteLeft = left;
        if (right != null) movement.spriteRight = right;

        return up != null || down != null || left != null || right != null;
    }
}