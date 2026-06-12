/// <summary>
/// This script belongs to cowsins� as a part of the cowsins� FPS Engine. All rights reserved. 
/// </summary>using UnityEngine;
using UnityEngine;

namespace cowsins
{
    public class Bullet : MonoBehaviour
    {
        [HideInInspector] public float speed;
        [HideInInspector] public float damage;
        [HideInInspector] public Vector3 destination;
        [HideInInspector] public bool gravity;
        [HideInInspector] public Transform player;
        [HideInInspector] public bool hurtsPlayer;
        [HideInInspector] public bool explosionOnHit;
        [HideInInspector] public GameObject explosionVFX;
        [HideInInspector] public float explosionRadius;
        [HideInInspector] public float explosionForce;
        [HideInInspector] public float criticalMultiplier;
        [HideInInspector] public float duration;
        [HideInInspector] public GameObject prefab;
        [SerializeField] private LayerMask projectileHitLayer;

        private static Collider[] overlapColliders = new Collider[500];
        private bool projectileHasAlreadyHit = false; // Prevent from double hitting issues

        private AimSkillSystem aimSystem;

        private void OnEnable()
        {
            projectileHasAlreadyHit = false;
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(DestroyProjectile));
        }

        public void Initialize()
        {
            transform.LookAt(destination);

            if (player != null)
            {
                aimSystem = player.GetComponent<AimSkillSystem>();
            }

            Invoke(nameof(DestroyProjectile), duration);
        }

        private void Update()
        {
            transform.Translate(0.0f, 0.0f, speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (projectileHasAlreadyHit) return;

            IDamageable damageable = other.GetComponent<IDamageable>();

            if (other.CompareTag("Critical"))
            {
                DamageTarget(damageable, damage * criticalMultiplier, true);
            }
            else if (other.CompareTag("BodyShot"))
            {
                DamageTarget(CowsinsUtilities.GatherDamageableParent(other.transform), damage, false);
            }
            else if (damageable != null && !other.CompareTag("Player"))
            {
                DamageTarget(damageable, damage, false);
            }
            else if (IsGroundOrObstacleLayer(other.gameObject.layer))
            {
                DestroyProjectile();
            }
        }

        private void DamageTarget(
            IDamageable target,
            float dmg,
            bool isCritical)
        {
            if (target == null)
                return;

            float finalDamage = dmg;

            // Aim Skill Effects
            if (aimSystem != null)
            {
                ZombieAI zombie =
                    (target as MonoBehaviour)?
                    .GetComponent<ZombieAI>();

                if (zombie != null)
                {
                    // Node 5: One Shot Crook
                    if (zombie.Type == ZombieAI.EnemyType.Crook &&
                        aimSystem.OneShotCrook)
                    {
                        finalDamage = zombie.MaxHealth * 10f;
                    }

                    // Node 5: Bonus Damage vs Special
                    if (zombie.Type == ZombieAI.EnemyType.Special &&
                        aimSystem.BonusDamageVsSpecial)
                    {
                        finalDamage *= 2f;
                    }
                }

                // Crit Chance
                if (aimSystem.RollCritical())
                {
                    finalDamage =
                        aimSystem.ApplyCriticalDamage(finalDamage);

                    Debug.Log("CRITICAL HIT!");
                }
            }

            target.Damage(finalDamage, isCritical);

            projectileHasAlreadyHit = true;

            DestroyProjectile();
        }

        private bool IsGroundOrObstacleLayer(int layer)
        {
            return (projectileHitLayer.value & (1 << layer)) != 0;
        }

        private void DestroyProjectile()
        {
            if (explosionOnHit)
            {
                if (explosionVFX != null)
                {
                    var contact = GetComponent<Collider>().ClosestPoint(transform.position);
                    PoolManager.Instance.GetFromPool(explosionVFX, contact, Quaternion.identity);
                }

                int numHits = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, overlapColliders);

                for (int i = 0; i < numHits; i++)
                {
                    var collider = overlapColliders[i];
                    var damageable = collider.GetComponent<IDamageable>();
                    var playerMovement = collider.GetComponent<PlayerMovement>();
                    var rigidbody = collider.GetComponent<Rigidbody>();

                    if (damageable != null)
                    {
                        // Calculate the distance ratio and damage based on the explosion radius
                        float distanceRatio = 1 - Mathf.Clamp01(Vector3.Distance(collider.transform.position, transform.position) / explosionRadius);
                        float dmg = damage * distanceRatio;

                        // Apply damage if the collider is a player and the explosion should hurt the player
                        if (collider.CompareTag("Player") && hurtsPlayer)
                        {
                            damageable.Damage(dmg, false);
                        }
                        // Apply damage if the collider is not a player
                        else if (!collider.CompareTag("Player"))
                        {
                            damageable.Damage(dmg, false);
                        }
                    }

                    if (playerMovement != null)
                    {
                        CameraEffects cameraEffects = playerMovement.GetComponent<CameraEffects>();
                        cameraEffects.ExplosionShake(Vector3.Distance(cameraEffects.transform.position, transform.position));
                    }

                    if (rigidbody != null && collider != this)
                    {
                        rigidbody.AddExplosionForce(explosionForce, transform.position, explosionRadius, 5, ForceMode.Force);
                    }
                }
            }

            if (prefab != null)
            {
                PoolManager.Instance.ReturnToPool(gameObject, prefab);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
