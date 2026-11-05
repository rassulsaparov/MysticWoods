
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 8f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool isGrounded;
    private bool previousMobileJump;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Keyboard and mobile movement
        float move = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(MysticGame.MobileMove) > 0.1f)
        {
            move = MysticGame.MobileMove;
        }

        rb.linearVelocity = new Vector2(
            move * moveSpeed,
            rb.linearVelocity.y
        );

        // Running animation
        if (animator != null)
        {
            animator.SetBool("IsRunning", move != 0);
        }

        // Character direction
        if (spriteRenderer != null)
        {
            if (move > 0)
                spriteRenderer.flipX = false;
            else if (move < 0)
                spriteRenderer.flipX = true;
        }

        // Jump from keyboard or mobile button
        bool keyboardJump = Input.GetKeyDown(KeyCode.Space);
        bool mobileJumpPressed =
            MysticGame.MobileJump && !previousMobileJump;

        if ((keyboardJump || mobileJumpPressed) && isGrounded)
        {
            isGrounded = false;

            rb.AddForce(
                Vector2.up * jumpForce,
                ForceMode2D.Impulse
            );

            // Jump sound
            if (MysticAudio.Instance != null)
            {
                MysticAudio.Instance.PlayJump();
            }
        }

        previousMobileJump = MysticGame.MobileJump;

        // Jump and falling animations
        if (animator != null)
        {
            animator.SetBool("IsJumping", !isGrounded);

            animator.SetBool(
                "IsFalling",
                !isGrounded && rb.linearVelocity.y < -0.1f
            );
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}
