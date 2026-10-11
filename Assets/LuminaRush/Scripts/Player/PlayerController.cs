using System;
using UnityEngine;

namespace LuminaRush
{
    public sealed class PlayerController : MonoBehaviour
    {
        public event Action Hit;
        public event Action<int> CoinCollected;
        public event Action Jumped;
        public event Action Slid;
        public event Action<PowerKind> PowerCollected;

        public bool Shielded { get; private set; }
        public bool Dead { get; private set; }
        public bool Grounded { get; private set; }
        public bool Sliding { get; private set; }
        public float MagnetTime { get; private set; }
        public float BoostTime { get; private set; }
        public int Lane { get; private set; }

        CharacterVisual visual;
        CapsuleCollider body;
        Rigidbody bodyRigidbody;
        float vertical;
        float verticalSpeed;
        float slideTimer;
        float invuln;
        float landSquash;
        float laneX;
        bool running;

        public Vector3 CollectCenter => transform.position + Vector3.up * 0.8f;
        public bool MagnetActive => MagnetTime > 0f;
        public bool BoostActive => BoostTime > 0f;

        public void Build(Material material)
        {
            body = GetComponent<CapsuleCollider>();
            if (body == null)
                body = gameObject.AddComponent<CapsuleCollider>();
            body.isTrigger = true;
            body.direction = 1;
            bodyRigidbody = GetComponent<Rigidbody>();
            if (bodyRigidbody == null)
                bodyRigidbody = gameObject.AddComponent<Rigidbody>();
            bodyRigidbody.isKinematic = true;
            bodyRigidbody.useGravity = false;
            visual = GetComponent<CharacterVisual>();
            if (visual == null)
                visual = gameObject.AddComponent<CharacterVisual>();
            visual.Build(material, transform);
            ApplyCollider();
        }

        public void ApplyLook(int character, int skin)
        {
            if (visual != null)
                visual.Apply(character, skin);
        }

        public void ResetAt(Vector3 position)
        {
            Dead = false;
            Shielded = false;
            Grounded = true;
            Sliding = false;
            running = true;
            Lane = 0;
            laneX = 0f;
            vertical = 0f;
            verticalSpeed = 0f;
            slideTimer = 0f;
            invuln = 1.15f;
            MagnetTime = 0f;
            BoostTime = 0f;
            landSquash = 0f;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            ApplyCollider();
            if (visual != null)
                visual.SetShield(false);
        }

        public void Stop()
        {
            running = false;
        }

        public void Tick(float dt, float speed, SwipeFrame swipe)
        {
            if (!running || Dead)
            {
                if (visual != null)
                    visual.Animate(dt, 0f, Grounded, Sliding, vertical, landSquash, Dead);
                return;
            }

            if (swipe.Left && Lane > -1) Lane--;
            if (swipe.Right && Lane < 1) Lane++;
            if (swipe.Up)
                TryJump();
            if (swipe.Down)
                TrySlide();

            float target = Lane * Tune.LaneWidth;
            laneX = Mathf.MoveTowards(laneX, target, Tune.LaneSpeed * dt);
            transform.position += Vector3.forward * speed * dt;

            if (!Grounded)
            {
                verticalSpeed -= Tune.Gravity * dt;
                vertical += verticalSpeed * dt;
                if (vertical <= 0f)
                {
                    vertical = 0f;
                    verticalSpeed = 0f;
                    Grounded = true;
                    landSquash = 0.12f;
                    if (slideTimer > 0f)
                        BeginSlide();
                }
            }
            else if (Sliding)
            {
                slideTimer -= dt;
                if (slideTimer <= 0f)
                    Sliding = false;
            }

            if (landSquash > 0f)
                landSquash -= dt;
            if (invuln > 0f)
                invuln -= dt;
            if (MagnetTime > 0f)
                MagnetTime -= dt;
            if (BoostTime > 0f)
                BoostTime -= dt;

            Vector3 pos = transform.position;
            pos.x = laneX;
            pos.y = vertical;
            transform.position = pos;
            ApplyCollider();
            if (visual != null)
            {
                visual.SetShield(Shielded);
                visual.Animate(dt, speed, Grounded, Sliding, vertical, landSquash, false);
            }
        }

        public void ActivateShield()
        {
            Shielded = true;
            if (visual != null)
                visual.SetShield(true);
        }

        public void ActivateMagnet(float seconds)
        {
            MagnetTime = Mathf.Max(MagnetTime, seconds);
        }

        public void ActivateBoost(float seconds)
        {
            BoostTime = Mathf.Max(BoostTime, seconds);
        }

        public void Revive()
        {
            Dead = false;
            running = true;
            Shielded = true;
            invuln = 1.4f;
            Sliding = false;
            slideTimer = 0f;
            if (vertical < 0.2f)
            {
                vertical = 0f;
                verticalSpeed = 0f;
                Grounded = true;
            }
            if (visual != null)
                visual.SetShield(true);
        }

        void TryJump()
        {
            if (!Grounded || Sliding)
                return;
            Grounded = false;
            verticalSpeed = Tune.JumpSpeed;
            if (Jumped != null)
                Jumped();
        }

        void TrySlide()
        {
            if (!Grounded)
            {
                verticalSpeed = -Tune.FastFall;
                slideTimer = Tune.SlideTime;
                return;
            }
            if (Sliding)
                return;
            BeginSlide();
            if (Slid != null)
                Slid();
        }

        void BeginSlide()
        {
            Sliding = true;
            slideTimer = Tune.SlideTime;
        }

        void ApplyCollider()
        {
            if (body == null)
                return;
            if (Sliding)
            {
                body.height = 0.72f;
                body.radius = 0.36f;
                body.center = new Vector3(0f, 0.38f, 0f);
            }
            else
            {
                body.height = 1.6f;
                body.radius = 0.36f;
                body.center = new Vector3(0f, 0.86f, 0f);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (Dead)
                return;
            Coin coin = other.GetComponent<Coin>();
            if (coin != null)
            {
                CollectCoin(coin);
                return;
            }
            PowerOrb orb = other.GetComponent<PowerOrb>();
            if (orb != null)
            {
                CollectPower(orb);
                return;
            }
            Hazard hazard = other.GetComponent<Hazard>();
            if (hazard != null)
                ResolveHazard(hazard);
        }

        public void CollectCoin(Coin coin)
        {
            if (coin == null || coin.Taken)
                return;
            int value = coin.Take(BoostActive ? 2 : 1);
            if (value > 0 && CoinCollected != null)
                CoinCollected(value);
        }

        void CollectPower(PowerOrb orb)
        {
            if (orb == null || orb.Taken)
                return;
            PowerKind kind = orb.Kind;
            orb.Take();
            if (kind == PowerKind.Shield) ActivateShield();
            else if (kind == PowerKind.Magnet) ActivateMagnet(8f);
            else ActivateBoost(10f);
            if (PowerCollected != null)
                PowerCollected(kind);
        }

        void ResolveHazard(Hazard hazard)
        {
            if (invuln > 0f)
                return;
            if (Shielded)
            {
                Shielded = false;
                invuln = 0.8f;
                hazard.DisableHit();
                if (visual != null)
                    visual.SetShield(false);
                return;
            }
            Dead = true;
            running = false;
            if (Hit != null)
                Hit();
        }
    }
}
