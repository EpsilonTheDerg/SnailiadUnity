﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health;
    public int maxHealth;
    public int attack;
    public int defense;
    public List<int> resistances;
    public bool letsPermeatingShotsBy;
    public bool stunInvulnerability = false;
    public string elementType; // Currently supports "ice" and "fire"

    public BoxCollider2D box;
    public Rigidbody2D rb;
    public SpriteRenderer sprite;
    public AnimationModule anim;
    public SpriteMask mask;

    private int pingPlayer = 0;

    public List<float> spawnConditions;
    public Vector2 origin;
    private bool intersectingPlayer = false;
    private List<GameObject> intersectingBullets = new List<GameObject>();
    public LayerMask enemyCollide;
    
    public void Spawn(int hp, int atk, int def, bool piercable, Vector2 hitboxSize, List<int> res = null)
    {
        box = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        mask = gameObject.AddComponent<SpriteMask>();
        BuildMask();
        anim = GetComponent<AnimationModule>();
        anim.updateMask = true;

        origin = transform.localPosition;

        enemyCollide = LayerMask.GetMask("PlayerCollide", "EnemyCollide");

        health = hp;
        maxHealth = hp;
        attack = atk;
        defense = def;
        resistances = res;
        letsPermeatingShotsBy = piercable;
        if (box != null)
            box.size = hitboxSize;

        if (resistances == null)
            resistances = new List<int> { -1 };
    }

    private void LateUpdate()
    {
        if (intersectingPlayer && !PlayState.playerScript.stunned)
        {
            PlayState.playerScript.health = Mathf.RoundToInt(Mathf.Clamp(PlayState.playerScript.health - attack, 0, Mathf.Infinity));
            if (PlayState.playerScript.health <= 0)
                PlayState.playerScript.Die();
            else
                PlayState.playerScript.BecomeStunned();
        }

        if (!stunInvulnerability)
        {
            List<GameObject> bulletsToDespawn = new List<GameObject>();
            bool killFlag = false;
            foreach (GameObject bullet in intersectingBullets)
            {
                Bullet bulletScript = bullet.GetComponent<Bullet>();
                if (!resistances.Contains(bulletScript.bulletType) && bulletScript.damage - defense > 0)
                {
                    health -= bulletScript.damage - defense;
                    if (health <= 0)
                        killFlag = true;
                    else
                        StartCoroutine(nameof(Flash));
                }
                else
                {
                    if (!PlayState.armorPingPlayedThisFrame)
                    {
                        PlayState.armorPingPlayedThisFrame = true;
                        PlayState.PlaySound("Ping");
                    }
                    pingPlayer -= 1;
                }
                if (!letsPermeatingShotsBy || bulletScript.bulletType == 1)
                    bulletsToDespawn.Add(bullet);
            }
            while (bulletsToDespawn.Count > 0)
            {
                intersectingBullets.RemoveAt(0);
                bulletsToDespawn[0].GetComponent<Bullet>().Despawn(true);
                bulletsToDespawn.RemoveAt(0);
            }
            if (killFlag)
                Kill();
        }

        if (intersectingBullets.Count == 0)
        {
            pingPlayer = 0;
        }
    }

    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        switch (collision.tag)
        {
            case "PlayerBullet":
                intersectingBullets.Add(collision.gameObject);
                break;
            case "Player":
                intersectingPlayer = true;
                break;
            default:
                break;
        }
    }

    public virtual void OnTriggerExit2D(Collider2D collision)
    {
        switch (collision.tag)
        {
            case "PlayerBullet":
                intersectingBullets.Remove(collision.gameObject);
                break;
            case "Player":
                intersectingPlayer = false;
                break;
            default:
                break;
        }
    }

    public IEnumerator Flash()
    {
        mask.enabled = true;
        stunInvulnerability = true;
        PlayState.PlaySound("Explode" + Random.Range(1, 5));
        yield return new WaitForSeconds(0.0125f);
        mask.enabled = false;
        yield return new WaitForSeconds(0.0125f);
        stunInvulnerability = false;
    }

    public virtual void Kill()
    {
        PlayState.PlaySound("EnemyKilled1");
        box.enabled = false;
        sprite.enabled = false;
        for (int i = Random.Range(1, 4); i > 0; i--)
            PlayState.RequestParticle(new Vector2(Random.Range(transform.position.x - 0.5f, transform.position.x + 0.5f),
                Random.Range(transform.position.y - 0.5f, transform.position.y + 0.5f)), "explosion", new float[] { 2 });
    }

    public bool OnScreen()
    {
        float boxAdjust = box != null ? box.size.x * 0.5f : 8;
        return Vector2.Distance(new Vector2(transform.position.x, 0), new Vector2(PlayState.cam.transform.position.x, 0)) - boxAdjust < 12.5f &&
            Vector2.Distance(new Vector2(0, transform.position.y), new Vector2(0, PlayState.cam.transform.position.y)) - boxAdjust < 7.5f;
    }

    private void BuildMask()
    {
        mask.isCustomRangeActive = true;
        mask.frontSortingOrder = -2;
        mask.backSortingOrder = -3;
        mask.enabled = false;
    }
}
