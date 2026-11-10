using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =====================================================================
//  EINES DELS CAPS (els comportaments són a BossBrain.cs)
// =====================================================================

// frena el jugador una estona (sense acumular-se)
public static class PlayerSlow
{
    static float until;

    public static void Apply(float fraction, float seconds, string text)
    {
        PlayerMovement pm = PlayerRef.Movement;
        if (pm == null || Time.time < until) return;
        until = Time.time + seconds;
        PlayerBuffs buffs = pm.GetComponent<PlayerBuffs>();
        if (buffs == null) buffs = pm.gameObject.AddComponent<PlayerBuffs>();
        buffs.SpeedBoost(-pm.speed * fraction, seconds);
        if (!string.IsNullOrEmpty(text))
            HazardFx.Popup((Vector2)pm.transform.position + Vector2.up * 1.2f, text, new Color(0.7f, 0.85f, 1f), 0.9f);
    }
}

// un tret de cap (el projectil dels clients)
static class BossShot
{
    public static void Fire(GameObject prefab, Vector2 from, Vector2 dir, float speed)
    {
        if (prefab == null) return;
        GameObject s = Object.Instantiate(prefab, from + dir * 0.8f, Quaternion.identity);
        Rigidbody2D rb = s.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = dir.normalized * speed;
    }
}
