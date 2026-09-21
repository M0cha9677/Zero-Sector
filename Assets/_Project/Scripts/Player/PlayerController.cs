using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visual;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform projectileParent;

    [Header("Manual Movement Physics")]
    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float deceleration = 40f;

    private PlayerStats stats;
    private Rigidbody2D rb;

    private Vector2 moveInput;

    // Direction the ship last moved/faced.
    private Vector2 moveDirection = Vector2.down;

    // Velocity calculated by OUR physics code.
    private Vector2 velocity;

    private float fireCooldownTimer;
    private float dashCooldownTimer;
    private float dashTimer;

    private bool isDashing;
    private Vector2 dashDirection;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();

        // Unity will no longer dynamically simulate this Rigidbody.
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }


    // =========================================================
    // INPUT
    // =========================================================

    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 rawInput = context.ReadValue<Vector2>();

        moveInput = new Vector2(
            Mathf.Round(rawInput.x),
            Mathf.Round(rawInput.y)
        );

        if (moveInput != Vector2.zero)
        {
            moveDirection = moveInput.normalized;
        }
    }


    private void Update()
    {
        // -----------------------------------------------------
        // PLAYER ROTATION
        // -----------------------------------------------------

        if (!isDashing && moveInput != Vector2.zero)
        {
            moveDirection = moveInput.normalized;

            float angle =
                Mathf.Atan2(moveDirection.y, moveDirection.x)
                * Mathf.Rad2Deg;

            visual.rotation =
                Quaternion.Euler(0f, 0f, angle + 90f);

            firePoint.rotation =
                Quaternion.Euler(0f, 0f, angle + 90f);
        }


        // -----------------------------------------------------
        // TIMERS
        // -----------------------------------------------------

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0f)
                isDashing = false;
        }

        if (fireCooldownTimer > 0f)
            fireCooldownTimer -= Time.deltaTime;

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;
    }


    // =========================================================
    // MANUAL PHYSICS
    // =========================================================

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (isDashing)
        {
            CalculateDashPhysics(dt);
        }
        else
        {
            CalculateMovementPhysics(dt);
        }

        // -----------------------------------------------------
        // POSITION EQUATION
        //
        // x(new) = x(old) + velocity * time
        // -----------------------------------------------------

        Vector2 newPosition =
            rb.position + velocity * dt;

        rb.MovePosition(newPosition);
    }


    private void CalculateMovementPhysics(float dt)
    {
        // If the player is providing input, accelerate the ship
        // in that direction.
        if (moveInput != Vector2.zero)
        {
            Vector2 direction = moveInput.normalized;

            // v(new) = v(old) + acceleration * time
            velocity += direction * acceleration * dt;

            // Limit the ship to its maximum movement speed.
            if (velocity.magnitude > stats.MoveSpeed)
            {
                velocity = velocity.normalized * stats.MoveSpeed;
            }
        }

        // If there is NO input, do nothing.
        //
        // The ship keeps its existing velocity because there is
        // no force/acceleration acting against its movement.
    }


    private void CalculateDashPhysics(float dt)
    {
        // During a dash we directly calculate the desired
        // velocity from direction and dash speed.
        //
        // velocity = direction * speed

        velocity =
            dashDirection * stats.DashSpeed;
    }


    // =========================================================
    // SHOOTING
    // =========================================================

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (fireCooldownTimer > 0f) return;

        Shoot();

        fireCooldownTimer = stats.FireCooldown;
    }


    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null)
            return;

        Vector3 mouseWorldPos =
            Camera.main.ScreenToWorldPoint(
                Mouse.current.position.ReadValue()
            );

        mouseWorldPos.z = 0f;

        Vector2 shootDirection =
            (mouseWorldPos - transform.position).normalized;

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation,
            projectileParent
        );

        Projectile projectile =
            bullet.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Initialize(shootDirection);
        }
    }


    // =========================================================
    // DASH
    // =========================================================

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (dashCooldownTimer > 0f) return;
        if (isDashing) return;

        Dash();
    }


    private void Dash()
    {
        isDashing = true;

        dashTimer = stats.DashDuration;
        dashCooldownTimer = stats.DashCooldown;

        if (moveInput != Vector2.zero)
        {
            dashDirection = moveInput.normalized;
        }
        else
        {
            dashDirection = moveDirection;
        }
    }
}