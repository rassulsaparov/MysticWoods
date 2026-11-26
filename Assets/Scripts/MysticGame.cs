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

    GameData data;
    LevelData levelData;
    int score;
    float timeLeft;

    public bool RoundEnded => ended;

    int level;
    int hp;
    int coins;
    int required;
    int bossHP;

    GameObject player;
    GameObject boss;
    GameObject overlay;

    Text status;
    Text message;

    readonly List<GameObject> generated = new List<GameObject>();

    float lastShot;
    float bossShot;
    float invulnerableUntil;
    bool bossWarned;
    const float BossWindUp = 0.5f;

    bool ended;

    static Sprite squareSprite;

    void Start()
    {
        Instance = this;
        data = GameData.Instance;

        player = GameObject.Find("Player");

        if (player == null)
        {
            Debug.LogError("Mystic Woods: Player not found!");
            enabled = false;
            return;
        }

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule)
            );
        }

        BuildUI();
        int selected = SaveSystem.SelectedLevel;
        if (!SaveSystem.Data.unlockedLevels.Contains(selected))
            selected = SaveSystem.Data.currentLevel;

        LoadLevel(selected);
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

    void Platform(PlatformData platformData)
    {
        float x = platformData.position.x;
        float y = platformData.position.y;
        float width = platformData.size.x;
        GameObject platform = Shape(
            "Ground_Platform",
            new Vector2(x, y),
            platformData.size,
            new Color(0.30f, 0.22f, 0.13f),
            true
        );

        platform.tag = "Ground";

        if (platformData.moving)
        {
            platform.name = "Moving_Platform";
            platform.GetComponent<SpriteRenderer>().color = new Color(0.3f, 0.7f, 0.8f);
            MysticObject movement = platform.AddComponent<MysticObject>();
            movement.kind = "platform";
            movement.move = true;
            movement.baseX = x;
            movement.movementDistance = platformData.movementDistance;
            movement.movementFrequency = platformData.movementFrequency;
            return;
        }

        GameObject grass = Shape(
            "Grass_Top",
            new Vector2(x, y + platformData.size.y * 0.5f),
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
            data.itemValues.coinSize,
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
            data.itemValues.hazardSize,
            new Color(0.95f, 0.2f, 0.25f),
            false,
            true
        );

        obj.AddComponent<MysticObject>().kind = "hazard";
    }

    // ============================
    // ENEMIES
    // ============================

    void Enemy(WoodEnemy prefab, float x, float y)
    {
        if (prefab == null)
        {
            Debug.LogError("Mystic Woods: Enemy prefab is missing.");
            return;
        }

        WoodEnemy enemy = Instantiate(prefab, new Vector2(x, y), Quaternion.identity);
        enemy.SetPlayer(player.transform);
        generated.Add(enemy.gameObject);
    }

    // ============================
    // LEVELS
    // ============================

    void LoadLevel(int n)
    {
        level = n;
        levelData = data.levels[n - 1];
        SaveSystem.SelectedLevel = n;
        SaveSystem.Data.currentLevel = n;
        hp = data.playerStats.maxHP;
        coins = 0;
        score = 0;
        timeLeft = levelData.timeLimit;
        bossHP = levelData.bossStats == null ? 0 : levelData.bossStats.maxHP;
        ended = false;

        invulnerableUntil = 0f;
        bossShot = Time.time;
        bossWarned = false;

        MobileMove = 0f;
        MobileJump = false;
        MobileFire = false;

        foreach (GameObject obj in generated)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                Destroy(obj);
            }
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
            (Vector3)levelData.playerSpawn;

        SpriteRenderer sprite =
            player.GetComponent<SpriteRenderer>();

        if (sprite != null)
        {
            sprite.color = Color.white;
            sprite.sortingOrder = 10;
        }

        required = levelData.goalAmount;

        foreach (PlatformData platformData in levelData.platforms)
            Platform(platformData);

        foreach (Vector2 position in levelData.coins)
            Coin(position.x, position.y);

        foreach (Vector2 position in levelData.hazards)
            Hazard(position.x, position.y);

        foreach (EnemySpawn spawn in levelData.enemies)
            Enemy(spawn.prefab, spawn.position.x, spawn.position.y);

        if (levelData.bossStats != null)
        {
            boss = Shape(
                "BOSS", levelData.bossPosition, levelData.bossSize,
                new Color(0.75f, 0.13f, 0.24f), false, true
            );

            ApplyMysticSprite(boss, "Boss");
            MysticObject bossObject = boss.AddComponent<MysticObject>();
            bossObject.kind = "boss";
            bossObject.move = true;
            bossObject.baseX = levelData.bossPosition.x;
            bossObject.movementDistance = levelData.bossStats.patrolDistance;
            bossObject.movementFrequency = levelData.bossStats.patrolFrequency;
        }

        // EXIT
        GameObject exit = Shape(
            "EXIT",
            levelData.exitPosition,
            levelData.exitSize,
            new Color(0.18f, 0.9f, 0.9f, 0.7f),
            false,
            true
        );

        exit.AddComponent<MysticObject>().kind = "exit";

        UpdateStatus();

        message.text = levelData.goal == LevelGoal.DefeatBoss
            ? levelData.title + " - DEFEAT THE BOSS"
            : levelData.title + " - COLLECT " + required + " COINS";

        CancelInvoke(nameof(ClearMessage));
        Invoke(nameof(ClearMessage), 3f);
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateStatus();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            MobileMove = 0f;
            MobileJump = false;
            MobileFire = false;
            SaveSystem.Save();
        }
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused)
            SaveSystem.Save();
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
            "    HP: " + hp + "/" + data.playerStats.maxHP +
            "    COINS: " + coins + "/" + required +
            (levelData.bossStats != null
                ? "    BOSS: " + bossHP + "/" + levelData.bossStats.maxHP
                : "") +
            "    SCORE: " + score + "    TIME: " + Mathf.CeilToInt(timeLeft);
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
            if (!generated.Remove(obj))
                return;

            coins += data.itemValues.coinValue;
            score += data.itemValues.coinScore;

            MysticAudio.Play(Sfx.Pickup);

            Destroy(obj);

            UpdateStatus();
        }
        else if (
            kind == "hazard" ||
            kind == "enemy" ||
            kind == "boss" ||
            kind == "projectile")
        {
            Damage(kind == "boss" || kind == "projectile"
                ? levelData.bossStats.attackDamage : data.itemValues.hazardDamage);
        }
        else if (kind == "exit")
        {
            if (levelData.goal == LevelGoal.DefeatBoss && bossHP > 0)
            {
                message.text = "DEFEAT THE BOSS FIRST!";
                return;
            }

            if (coins < required)
            {
                message.text = "COLLECT ALL COINS!";
                return;
            }

            score += levelData.completionScore;
            SaveSystem.LevelCompleted(level, score);
            MysticAudio.Play(Sfx.Win);

            if (level < data.levels.Length)
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
        Damage(data.itemValues.hazardDamage);
    }

    public void Damage(int amount)
    {
        if (ended || Time.time < invulnerableUntil)
            return;

        hp = Mathf.Max(0, hp - amount);

        // The fatal hit is voiced by the lose sound in End().
        if (hp > 0)
            MysticAudio.Play(Sfx.Hit);

        invulnerableUntil = Time.time + data.playerStats.invulnerabilityTime;

        UpdateStatus();

        if (hp <= 0)
            End("GAME OVER", true);
    }

    // ============================
    // UPDATE
    // ============================

    void Update()
    {
        if (ended || player == null || Time.timeScale == 0f)
            return;

        timeLeft = Mathf.Max(0f, timeLeft - Time.deltaTime);
        UpdateStatus();
        if (timeLeft <= 0f)
        {
            End("TIME UP", true);
            return;
        }

        if (player.transform.position.y < levelData.fallHeight)
        {
            Damage();

            player.transform.position =
                (Vector3)levelData.playerSpawn;

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
            Time.time - lastShot > data.playerStats.shotCooldown)
        {
            lastShot = Time.time;
            Shoot();
        }

        // BOSS WIND-UP: the boss is heard before each projectile appears.
        if (
            boss != null &&
            !bossWarned &&
            Time.time - bossShot > levelData.bossStats.cooldownTime - BossWindUp)
        {
            bossWarned = true;
            if (WoodEnemy.OnScreen(Camera.main, boss.transform.position))
                MysticAudio.Play(Sfx.EnemyWindUp);
        }

        // BOSS SHOOTING
        if (
            boss != null &&
            Time.time - bossShot > levelData.bossStats.cooldownTime)
        {
            bossShot = Time.time;
            bossWarned = false;

            GameObject projectile = Shape(
                "Boss_Projectile",
                boss.transform.position +
                (Vector3)levelData.bossShotOffset,
                levelData.bossProjectileSize,
                Color.magenta,
                false,
                true
            );

            MysticObject movement =
                projectile.AddComponent<MysticObject>();

            movement.kind = "projectile";
            movement.speed = -levelData.bossStats.projectileSpeed;
            movement.lifetime = levelData.bossStats.projectileLifetime;
        }
    }

    // ============================
    // PLAYER SHOOTING
    // ============================

    public void EnemyShot(Vector2 position, Vector2 direction, float speed, int damage, float lifetime)
    {
        GameObject shot = Shape(
            "Shooter_Projectile", position, new Vector2(0.32f, 0.18f),
            Color.yellow, false, true
        );

        ApplyMysticSprite(shot, "Bullet");
        shot.transform.right = direction;

        MysticObject movement = shot.AddComponent<MysticObject>();
        movement.kind = "shooterShot";
        movement.direction = direction;
        movement.speed = speed;
        movement.damage = damage;
        movement.lifetime = lifetime;
    }

    void Shoot()
    {
        MysticAudio.Play(Sfx.Shoot);

        SpriteRenderer sprite =
            player.GetComponent<SpriteRenderer>();

        bool right = sprite == null || !sprite.flipX;

        GameObject bullet = Shape(
            "Player_Bullet",
            player.transform.position +
            new Vector3(
                right ? data.playerStats.shotOffset.x : -data.playerStats.shotOffset.x,
                data.playerStats.shotOffset.y,
                0f
            ),
            data.playerStats.projectileSize,
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
        movement.speed = right ? data.playerStats.projectileSpeed : -data.playerStats.projectileSpeed;
        movement.lifetime = data.playerStats.projectileLifetime;
    }

    // ============================
    // BOSS HEALTH
    // ============================

    public void HitBoss()
    {
        if (boss == null || ended || bossHP <= 0)
            return;

        bossHP = Mathf.Max(0, bossHP - data.playerStats.attackDamage);

        UpdateStatus();

        if (bossHP <= 0)
        {
            MysticAudio.Play(Sfx.EnemyDeath);
            AddScore(data.itemValues.bossScore);
            boss.SetActive(false);
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
        CancelInvoke(nameof(ClearMessage));

        // Win is played when the exit is reached; every retry ending is a loss.
        if (retry)
            MysticAudio.Play(Sfx.Lose);
        MobileMove = 0f;
        MobileJump = false;
        MobileFire = false;

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
        UnityEngine.Events.UnityAction action,
        bool clickSound = true)
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

        Button button = go.GetComponent<Button>();
        button.onClick.AddListener(action);

        // Menu buttons click; held movement / jump / fire controls stay silent.
        if (clickSound)
            MysticAudio.RegisterButton(button);

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
            () => { },
            false
        );

        GameObject right = MakeButton(
            canvasGo.transform,
            "RIGHT",
            new Vector2(-395f, -260f),
            new Vector2(125f, 85f),
            () => { },
            false
        );

        GameObject jump = MakeButton(
            canvasGo.transform,
            "JUMP",
            new Vector2(410f, -260f),
            new Vector2(125f, 85f),
            () => { },
            false
        );

        GameObject fire = MakeButton(
            canvasGo.transform,
            "FIRE",
            new Vector2(545f, -260f),
            new Vector2(125f, 85f),
            () => { },
            false
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
    public float lifetime;
    public float movementDistance;
    public float movementFrequency;
    bool consumed;

    public Vector2 direction;
    public int damage;
    float born;
    Camera gameCamera;
    ContactFilter2D solids;
    RaycastHit2D[] hits;

    void FixedUpdate()
    {
        if (kind != "shooterShot")
            return;

        if (MysticGame.Instance == null || MysticGame.Instance.RoundEnded ||
            Time.time - born > lifetime || !WoodEnemy.OnScreen(gameCamera, transform.position))
        {
            Destroy(gameObject);
            return;
        }

        float step = speed * Time.fixedDeltaTime;
        int count = Physics2D.Raycast(transform.position, direction, solids, hits, step);
        if (count > 0)
        {
            Transform target = hits[0].transform;
            if (target.GetComponent<PlayerMovement>() != null &&
                WoodEnemy.OnScreen(gameCamera, target.position))
                MysticGame.Instance.Damage(damage);

            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(direction * step);
    }

    void Start()
    {
        born = Time.time;
        if (kind == "shooterShot")
        {
            gameCamera = Camera.main;
            hits = new RaycastHit2D[1];
            solids.useTriggers = false;
        }
    }

    void Update()
    {
        // Enemy, boss and platform movement
        if (move)
        {
            transform.position = new Vector3(
                baseX +
                Mathf.Sin(Time.time * movementFrequency) * movementDistance,
                transform.position.y,
                0f
            );
        }

        // Bullets and projectiles
        if (kind == "bullet" || kind == "projectile")
        {
            transform.position +=
                Vector3.right * speed * Time.deltaTime;

            if (Time.time - born > lifetime)
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
        if (MysticGame.Instance == null || MysticGame.Instance.RoundEnded || consumed)
            return;

        if (kind == "bullet")
        {
            WoodEnemy enemy = other.GetComponent<WoodEnemy>();
            if (enemy != null)
            {
                consumed = true;
                enemy.TakeHit(GameData.Instance.playerStats.attackDamage);
                Destroy(gameObject);
                return;
            }

            MysticObject target =
                other.GetComponent<MysticObject>();

            if (target != null)
            {
                if (target.kind == "boss")
                {
                    consumed = true;
                    MysticGame.Instance.HitBoss();
                    Destroy(gameObject);
                }
                else if (target.kind == "enemy")
                {
                    MysticAudio.Play(Sfx.EnemyDeath);
                    Destroy(other.gameObject);
                    Destroy(gameObject);
                }
            }

            return;
        }

        if (kind == "shooterShot")
            return;

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
