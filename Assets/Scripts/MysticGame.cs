
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MysticGame : MonoBehaviour
{
    public static MysticGame Instance;

    public static float MobileMove;
    public static bool MobileJump;
    public static bool MobileFire;

    int level = 1;
    int hp = 3;
    int coins = 0;
    int required = 4;
    int bossHP = 8;

    GameObject player;
    GameObject boss;
    GameObject overlay;

    Text status;
    Text message;

    readonly List<GameObject> generated = new List<GameObject>();

    float lastShot;
    float bossShot;
    float invulnerableUntil;

    bool ended;

    static Sprite squareSprite;

    // ============================
    // AUTOMATIC GAME START
    // ============================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene")
            return;

        if (FindFirstObjectByType<MysticGame>() == null)
        {
            new GameObject("MysticGame_AutoSetup")
                .AddComponent<MysticGame>();
        }
    }

    void Start()
    {
        Instance = this;

        player = GameObject.Find("Player");

        if (player == null)
        {
            Debug.LogError("Mystic Woods: Player not found!");
            enabled = false;
            return;
        }

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule)
            );
        }

        BuildUI();
        LoadLevel(1);
    }

    // ============================
    // NEW SPRITES
    // ============================

    Sprite GetMysticSprite(string spriteName)
    {
        Sprite[] sprites =
            Resources.LoadAll<Sprite>("MysticSprites");

        foreach (Sprite sprite in sprites)
        {
            if (sprite.name == spriteName)
                return sprite;
        }

        Debug.LogWarning(
            "Mystic Woods: Sprite not found: " + spriteName
        );

        return null;
    }

    void ApplyMysticSprite(GameObject obj, string spriteName)
    {
        if (obj == null)
            return;

        SpriteRenderer sr =
            obj.GetComponent<SpriteRenderer>();

        if (sr == null)
            return;

        Sprite newSprite = GetMysticSprite(spriteName);

        if (newSprite == null)
            return;

        sr.sprite = newSprite;
        sr.color = Color.white;
        sr.drawMode = SpriteDrawMode.Simple;

        // Keep the original game object size
        Vector2 spriteSize = newSprite.bounds.size;

        if (spriteSize.x > 0 && spriteSize.y > 0)
        {
            Vector3 currentScale = obj.transform.localScale;

            // Scale sprite to the existing object dimensions
            // without changing its collision box.
            float targetWidth = Mathf.Abs(currentScale.x);
            float targetHeight = Mathf.Abs(currentScale.y);

            float spriteAspect = spriteSize.x / spriteSize.y;
            float targetAspect = targetWidth / targetHeight;

            float width = targetWidth;
            float height = targetHeight;

            if (spriteAspect > targetAspect)
                height = targetWidth / spriteAspect;
            else
                width = targetHeight * spriteAspect;

            // SpriteRenderer.size works only for tiled/sliced,
            // so use a visual child to preserve collider dimensions.
            GameObject visual = new GameObject(
                spriteName + "_Visual"
            );

            visual.transform.SetParent(obj.transform, false);

            SpriteRenderer visualRenderer =
                visual.AddComponent<SpriteRenderer>();

            visualRenderer.sprite = newSprite;
            visualRenderer.color = Color.white;
            visualRenderer.sortingOrder = sr.sortingOrder + 1;

            visual.transform.localPosition = Vector3.zero;

            visual.transform.localScale = new Vector3(
                width / (spriteSize.x * targetWidth),
                height / (spriteSize.y * targetHeight),
                1f
            );

            sr.enabled = false;
        }
    }

    // ============================
    // BASIC SHAPES
    // ============================

    Sprite SquareSprite()
    {
        if (squareSprite == null)
        {
            Texture2D texture = Texture2D.whiteTexture;

            squareSprite = Sprite.Create(
                texture,
                new Rect(
                    0,
                    0,
                    texture.width,
                    texture.height
                ),
                new Vector2(0.5f, 0.5f),
                texture.width
            );
        }

        return squareSprite;
    }

    GameObject Shape(
        string name,
        Vector2 position,
        Vector2 size,
        Color color,
        bool solid = false,
        bool trigger = false)
    {
        GameObject obj = new GameObject(name);

        obj.transform.position = position;
        obj.transform.localScale =
            new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer =
            obj.AddComponent<SpriteRenderer>();

        renderer.sprite = SquareSprite();
        renderer.color = color;
        renderer.sortingOrder = 4;

        if (solid || trigger)
        {
            BoxCollider2D collider =
                obj.AddComponent<BoxCollider2D>();

            collider.isTrigger = trigger;
        }

        if (trigger)
        {
            Rigidbody2D rb =
                obj.AddComponent<Rigidbody2D>();

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        generated.Add(obj);

        return obj;
    }

    // ============================
    // PLATFORMS
    // ============================

    void Platform(float x, float y, float width)
    {
        GameObject platform = Shape(
            "Ground_Platform",
            new Vector2(x, y),
            new Vector2(width, 0.45f),
            new Color(0.30f, 0.22f, 0.13f),
            true
        );

        platform.tag = "Ground";

        GameObject grass = Shape(
            "Grass_Top",
            new Vector2(x, y + 0.24f),
            new Vector2(width, 0.12f),
            new Color(0.33f, 0.78f, 0.27f)
        );

        grass.GetComponent<SpriteRenderer>().sortingOrder = 5;
    }

    // ============================
    // COINS
    // ============================

    void Coin(float x, float y)
    {
        GameObject obj = Shape(
            "Coin",
            new Vector2(x, y),
            new Vector2(0.36f, 0.36f),
            new Color(1f, 0.82f, 0.08f),
            false,
            true
        );

        ApplyMysticSprite(obj, "Coin");

        obj.AddComponent<MysticObject>().kind = "coin";
    }

    // ============================
    // HAZARDS
    // ============================

    void Hazard(float x, float y)
    {
        GameObject obj = Shape(
            "Hazard",
            new Vector2(x, y),
            new Vector2(0.55f, 0.55f),
            new Color(0.95f, 0.2f, 0.25f),
            false,
            true
        );

        obj.AddComponent<MysticObject>().kind = "hazard";
    }

    // ============================
    // ENEMIES
    // ============================

    void Enemy(float x, float y)
    {
        GameObject obj = Shape(
            "Enemy",
            new Vector2(x, y),
            new Vector2(0.85f, 0.7f),
            new Color(0.68f, 0.2f, 0.77f),
            false,
            true
        );

        ApplyMysticSprite(obj, "Enemy");

        MysticObject enemy =
            obj.AddComponent<MysticObject>();

        enemy.kind = "enemy";
        enemy.move = true;
        enemy.baseX = x;
    }

    // ============================
    // LEVELS
    // ============================

    void LoadLevel(int n)
    {
        level = n;
        hp = 3;
        coins = 0;
        bossHP = 8;
        ended = false;

        invulnerableUntil = 0f;
        bossShot = Time.time;

        MobileMove = 0f;
        MobileJump = false;
        MobileFire = false;

        foreach (GameObject obj in generated)
        {
            if (obj != null)
                Destroy(obj);
        }

        generated.Clear();
        boss = null;

        if (overlay != null)
            Destroy(overlay);

        Rigidbody2D rb =
            player.GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        player.transform.position =
            new Vector3(-6.8f, -2f, 0f);

        SpriteRenderer sprite =
            player.GetComponent<SpriteRenderer>();

        if (sprite != null)
        {
            sprite.color = Color.white;
            sprite.sortingOrder = 10;
        }

        required = n == 3 ? 0 : 4;

        // LEVEL 1
        if (n == 1)
        {
            Platform(-4f, -1.8f, 2.1f);
            Platform(0f, -1.05f, 2f);
            Platform(4f, -1.6f, 2f);

            Coin(-4f, -0.9f);
            Coin(0f, -0.15f);
            Coin(4f, -0.7f);
            Coin(6.7f, -2.6f);

            Hazard(-1.9f, -3.15f);
            Enemy(2.3f, -2.9f);
        }

        // LEVEL 2
        else if (n == 2)
        {
            Platform(-4f, -1.5f, 1.6f);
            Platform(-0.9f, -0.6f, 1.6f);
            Platform(2.2f, -1.35f, 1.5f);
            Platform(5.4f, -0.65f, 1.6f);

            Coin(-4f, -0.65f);
            Coin(-0.9f, 0.25f);
            Coin(2.2f, -0.5f);
            Coin(5.4f, 0.2f);

            Hazard(-2.2f, -3.15f);
            Hazard(1f, -3.15f);
            Enemy(3.5f, -2.9f);

            GameObject movingPlatform = Shape(
                "Moving_Platform",
                new Vector2(0.5f, -2.5f),
                new Vector2(1.8f, 0.35f),
                new Color(0.3f, 0.7f, 0.8f),
                true
            );

            movingPlatform.tag = "Ground";

            MysticObject movement =
                movingPlatform.AddComponent<MysticObject>();

            movement.kind = "platform";
            movement.move = true;
            movement.baseX = 0.5f;
        }

        // LEVEL 3 - BOSS
        else
        {
            Platform(-4f, -1.5f, 2f);
            Platform(4f, -1.5f, 2f);

            boss = Shape(
                "BOSS",
                new Vector2(5f, -2.65f),
                new Vector2(1.2f, 1.65f),
                new Color(0.75f, 0.13f, 0.24f),
                false,
                true
            );

            ApplyMysticSprite(boss, "Boss");

            MysticObject bossObject =
                boss.AddComponent<MysticObject>();

            bossObject.kind = "boss";
            bossObject.move = true;
            bossObject.baseX = 5f;

            Hazard(-0.3f, -3.15f);
        }

        // EXIT
        GameObject exit = Shape(
            "EXIT",
            new Vector2(7.6f, -2.5f),
            new Vector2(0.6f, 1.8f),
            new Color(0.18f, 0.9f, 0.9f, 0.7f),
            false,
            true
        );

        exit.AddComponent<MysticObject>().kind = "exit";

        UpdateStatus();

        message.text = n == 3
            ? "LEVEL 3 - BOSS FIGHT!"
            : "LEVEL " + n + " - COLLECT 4 COINS";

        CancelInvoke(nameof(ClearMessage));
        Invoke(nameof(ClearMessage), 3f);
    }

    void ClearMessage()
    {
        if (message != null)
            message.text = "";
    }

    // ============================
    // HUD
    // ============================

    void UpdateStatus()
    {
        if (status == null)
            return;

        status.text =
            "LEVEL " + level +
            "    HP: " + hp + "/3" +
            "    COINS: " + coins + "/" + required +
            (level == 3
                ? "    BOSS: " + bossHP + "/8"
                : "");
    }

    // ============================
    // COLLISIONS
    // ============================

    public void Interact(string kind, GameObject obj)
    {
        if (ended)
            return;

        if (kind == "coin")
        {
            coins++;

            if (MysticAudio.Instance != null)
                MysticAudio.Instance.PlayCoin();

            generated.Remove(obj);
            Destroy(obj);

            UpdateStatus();
        }
        else if (
            kind == "hazard" ||
            kind == "enemy" ||
            kind == "boss" ||
            kind == "projectile")
        {
            Damage();
        }
        else if (kind == "exit")
        {
            if (level == 3 && bossHP > 0)
            {
                message.text = "DEFEAT THE BOSS FIRST!";
                return;
            }

            if (coins < required)
            {
                message.text = "COLLECT ALL COINS!";
                return;
            }

            if (level < 3)
                LoadLevel(level + 1);
            else
                End("YOU WIN!", false);
        }
    }

    // ============================
    // PLAYER DAMAGE
    // ============================

    public void Damage()
    {
        if (ended || Time.time < invulnerableUntil)
            return;

        hp--;

        if (MysticAudio.Instance != null)
            MysticAudio.Instance.PlayDamage();

        invulnerableUntil = Time.time + 1.3f;

        UpdateStatus();

        if (hp <= 0)
            End("GAME OVER", true);
    }

    // ============================
    // UPDATE
    // ============================

    void Update()
    {
        if (ended || player == null)
            return;

        if (player.transform.position.y < -7f)
        {
            Damage();

            player.transform.position =
                new Vector3(-6.8f, -2f, 0f);

            Rigidbody2D rb =
                player.GetComponent<Rigidbody2D>();

            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }

        SpriteRenderer sprite =
            player.GetComponent<SpriteRenderer>();

        if (sprite != null)
        {
            bool flashing =
                Time.time < invulnerableUntil &&
                Mathf.FloorToInt(Time.time * 12f) % 2 == 0;

            sprite.color = flashing
                ? new Color(1f, 0.4f, 0.4f)
                : Color.white;
        }

        // SHOOTING
        if (
            (Input.GetKeyDown(KeyCode.F) || MobileFire) &&
            Time.time - lastShot > 0.35f)
        {
            lastShot = Time.time;
            Shoot();
        }

        // BOSS SHOOTING
        if (
            level == 3 &&
            boss != null &&
            Time.time - bossShot > 2f)
        {
            bossShot = Time.time;

            GameObject projectile = Shape(
                "Boss_Projectile",
                boss.transform.position +
                Vector3.left * 0.6f,
                new Vector2(0.3f, 0.3f),
                Color.magenta,
                false,
                true
            );

            MysticObject movement =
                projectile.AddComponent<MysticObject>();

            movement.kind = "projectile";
            movement.speed = -3.5f;
        }
    }

    // ============================
    // PLAYER SHOOTING
    // ============================

    void Shoot()
    {
        if (MysticAudio.Instance != null)
            MysticAudio.Instance.PlayShoot();

        SpriteRenderer sprite =
            player.GetComponent<SpriteRenderer>();

        bool right = sprite == null || !sprite.flipX;

        GameObject bullet = Shape(
            "Player_Bullet",
            player.transform.position +
            new Vector3(
                right ? 0.55f : -0.55f,
                0.1f,
                0f
            ),
            new Vector2(0.42f, 0.25f),
            Color.yellow,
            false,
            true
        );

        ApplyMysticSprite(bullet, "Bullet");

        // The sprite image faces right.
        // Flip its visual when shooting left.
        if (!right)
        {
            Transform visual =
                bullet.transform.Find("Bullet_Visual");

            if (visual != null)
            {
                SpriteRenderer visualRenderer =
                    visual.GetComponent<SpriteRenderer>();

                if (visualRenderer != null)
                    visualRenderer.flipX = true;
            }
        }

        MysticObject movement =
            bullet.AddComponent<MysticObject>();

        movement.kind = "bullet";
        movement.speed = right ? 9f : -9f;
    }

    // ============================
    // BOSS HEALTH
    // ============================

    public void HitBoss()
    {
        if (level != 3 || boss == null || ended)
            return;

        bossHP--;

        UpdateStatus();

        if (bossHP <= 0)
        {
            generated.Remove(boss);
            Destroy(boss);
            boss = null;

            message.text = "BOSS DEFEATED! GO TO EXIT";
        }
    }

    // ============================
    // GAME OVER / WIN
    // ============================

    void End(string title, bool retry)
    {
        ended = true;

        overlay = new GameObject(
            "EndPanel",
            typeof(RectTransform),
            typeof(Image)
        );

        overlay.transform.SetParent(
            status.transform.parent,
            false
        );

        RectTransform rect =
            overlay.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlay.GetComponent<Image>().color =
            new Color(0f, 0f, 0f, 0.82f);

        MakeText(
            overlay.transform,
            title,
            38,
            new Vector2(0f, 65f),
            new Vector2(600f, 90f)
        );

        MakeButton(
            overlay.transform,
            retry ? "RETRY" : "PLAY AGAIN",
            new Vector2(0f, -30f),
            new Vector2(220f, 65f),
            () => LoadLevel(retry ? level : 1)
        );

        MakeButton(
            overlay.transform,
            "MAIN MENU",
            new Vector2(0f, -115f),
            new Vector2(220f, 55f),
            () => SceneManager.LoadScene("MainMenu")
        );
    }

    // ============================
    // UI HELPERS
    // ============================

    Font UiFont()
    {
        return Resources.GetBuiltinResource<Font>(
            "LegacyRuntime.ttf"
        );
    }

    Text MakeText(
        Transform parent,
        string value,
        int fontSize,
        Vector2 position,
        Vector2 size)
    {
        GameObject go = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(Text)
        );

        go.transform.SetParent(parent, false);

        RectTransform rect =
            go.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);

        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = go.GetComponent<Text>();

        text.font = UiFont();
        text.fontSize = fontSize;
        text.text = value;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    GameObject MakeButton(
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size,
        UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject(
            label,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );

        go.transform.SetParent(parent, false);

        RectTransform rect =
            go.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);

        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        go.GetComponent<Image>().color =
            new Color(0.08f, 0.2f, 0.22f, 0.78f);

        go.GetComponent<Button>()
            .onClick.AddListener(action);

        MakeText(
            go.transform,
            label,
            19,
            Vector2.zero,
            size
        );

        return go;
    }

    // ============================
    // HUD AND MOBILE CONTROLS
    // ============================

    void BuildUI()
    {
        GameObject canvasGo = new GameObject(
            "MysticHUD",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas =
            canvasGo.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder = 100;

        CanvasScaler scaler =
            canvasGo.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1280f, 720f);

        status = MakeText(
            canvasGo.transform,
            "",
            24,
            new Vector2(0f, 320f),
            new Vector2(1100f, 55f)
        );

        message = MakeText(
            canvasGo.transform,
            "",
            24,
            new Vector2(0f, 245f),
            new Vector2(1050f, 65f)
        );

        GameObject left = MakeButton(
            canvasGo.transform,
            "LEFT",
            new Vector2(-535f, -260f),
            new Vector2(125f, 85f),
            () => { }
        );

        GameObject right = MakeButton(
            canvasGo.transform,
            "RIGHT",
            new Vector2(-395f, -260f),
            new Vector2(125f, 85f),
            () => { }
        );

        GameObject jump = MakeButton(
            canvasGo.transform,
            "JUMP",
            new Vector2(410f, -260f),
            new Vector2(125f, 85f),
            () => { }
        );

        GameObject fire = MakeButton(
            canvasGo.transform,
            "FIRE",
            new Vector2(545f, -260f),
            new Vector2(125f, 85f),
            () => { }
        );

        left.AddComponent<MysticHoldButton>()
            .direction = -1;

        right.AddComponent<MysticHoldButton>()
            .direction = 1;

        jump.AddComponent<MysticHoldButton>()
            .jump = true;

        fire.AddComponent<MysticHoldButton>()
            .fire = true;
    }
}

// ==================================
// GAME OBJECT BEHAVIOUR
// ==================================

public class MysticObject : MonoBehaviour
{
    public string kind;
    public bool move;
    public float baseX;
    public float speed;

    float born;

    void Start()
    {
        born = Time.time;
    }

    void Update()
    {
        // Enemy, boss and platform movement
        if (move)
        {
            transform.position = new Vector3(
                baseX +
                Mathf.Sin(Time.time * 1.4f) * 1.15f,
                transform.position.y,
                0f
            );
        }

        // Bullets and projectiles
        if (kind == "bullet" || kind == "projectile")
        {
            transform.position +=
                Vector3.right * speed * Time.deltaTime;

            if (Time.time - born > 3f)
                Destroy(gameObject);
        }

        // Rotating coins
        if (kind == "coin")
        {
            transform.Rotate(
                0f,
                0f,
                75f * Time.deltaTime
            );
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (MysticGame.Instance == null)
            return;

        if (kind == "bullet")
        {
            MysticObject target =
                other.GetComponent<MysticObject>();

            if (target != null)
            {
                if (target.kind == "boss")
                {
                    MysticGame.Instance.HitBoss();
                    Destroy(gameObject);
                }
                else if (target.kind == "enemy")
                {
                    Destroy(other.gameObject);
                    Destroy(gameObject);
                }
            }

            return;
        }

        if (other.gameObject.name == "Player")
        {
            MysticGame.Instance.Interact(
                kind,
                gameObject
            );

            if (kind == "projectile")
                Destroy(gameObject);
        }
    }
}

// ==================================
// MOBILE BUTTONS
// ==================================

public class MysticHoldButton :
    MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    public int direction;
    public bool jump;
    public bool fire;

    public void OnPointerDown(
        PointerEventData eventData)
    {
        if (direction != 0)
            MysticGame.MobileMove = direction;

        if (jump)
            MysticGame.MobileJump = true;

        if (fire)
            MysticGame.MobileFire = true;
    }

    public void OnPointerUp(
        PointerEventData eventData)
    {
        if (direction != 0)
            MysticGame.MobileMove = 0f;

        if (jump)
            MysticGame.MobileJump = false;

        if (fire)
            MysticGame.MobileFire = false;
    }
}
