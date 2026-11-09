using System.Collections;
using UnityEngine;

// bonificacions temporals que donen els drops
public class PlayerBuffs : MonoBehaviour
{
    public void SpeedBoost(float amount, float seconds)
    {
        StartCoroutine(SpeedRoutine(amount, seconds));
    }

    public void DamageBoost(int amount, float seconds)
    {
        StartCoroutine(DamageRoutine(amount, seconds));
    }

    IEnumerator SpeedRoutine(float amount, float seconds)
    {
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm == null) yield break;

        pm.speed += amount;
        yield return new WaitForSeconds(seconds);
        pm.speed -= amount;
    }

    IEnumerator DamageRoutine(int amount, float seconds)
    {
        PlayerShooting ps = GetComponent<PlayerShooting>();
        if (ps == null) yield break;

        ps.damage += amount;
        yield return new WaitForSeconds(seconds);
        ps.damage -= amount;
    }
}
