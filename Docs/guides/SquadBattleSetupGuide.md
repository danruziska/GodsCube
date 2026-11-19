# 3 vs 3 battle — setup guide (only the new changes)

This guide has **only what is new** for the 3 vs 3 battle. Every object has its own block with **every component and every field** you see in the Inspector, in the order Unity shows them, even when the values repeat. The names are exactly the ones in your BattleHud prefab and in the code.

Everything is inside the **BattleHud** prefab: Project window → `Assets/Game/View/Prefabs/UI` → double-click **BattleHud**.

**Reading the anchors:** in Unity **Y 0 = bottom** of the parent and **Y 1 = top**. X 0 = left, X 1 = right.
**Where the anchor fields are:** select the object → Inspector → **Rect Transform** → open the small arrow next to **Anchors** to see Min and Max.
**Point anchors vs stretch anchors:** when Min and Max are equal (for example both 0.5 / 1), the Rect Transform shows **Pos X, Pos Y, Width, Height**. When Min is 0 and Max is 1, it shows **Left, Top, Right, Bottom** instead.
**Colors:** click the color box and type the hex code in **Hexadecimal** (without #); the alpha is the **A** slider (0–255).

The part of the Hierarchy this guide uses:

```
BattleHud                        (has the Battle Ui component)
  SafeArea
    Stage                        (has the Battle Stage component and a Rect Mask 2D)
      PlayerTeam                 (Horizontal Layout Group)
        Hero0, Hero1, Hero2      (your heroes)
      EnemyTeam                  (Horizontal Layout Group)
        Foe0, Foe1, Foe2         (the rival's heroes)
    PowerBar
      PlayerSide ▸ PlayerHp      (the old big bar of your team)
      EnemySide  ▸ EnemyHp       (the old big bar of the rival)
```

**What is there now (checked in your prefab), and why the numbers below:**

- **Stage**: Anchors Min **X 0 · Y 0.73**, Max **X 1 · Y 0.87** → about **270** units tall on a 1080 × 1920 layout. It has a **Rect Mask 2D**: anything outside it is hidden.
- **PlayerTeam**: Anchors Min **X 0.03 · Y 0.04**, Max **X 0.45 · Y 0.84**. **EnemyTeam**: Min **X 0.55 · Y 0.04**, Max **X 0.97 · Y 0.84**. Both have a **Horizontal Layout Group** (Spacing **8**, Child Alignment **Lower Center**, Control Child Size **Width ON · Height ON**, Child Force Expand **Width ON · Height ON**).
- So each **Hero0–2 / Foe0–2** is sized by the layout: about **146 wide × 215 tall**. Their Rect Transform fields are **greyed out** ("Some values driven by HorizontalLayoutGroup") — that's normal, don't change them.
- Only **~43** units are left above the Hero/Foe boxes before the Stage's mask cuts.
- The figure is drawn **square** in the middle of its tall box and the sprites have some empty space above the head, so the head is roughly **55–70** units **below** the top of the box. That's why every bar uses **Pos Y -55**.
- **Foe0–2** have **Raycast Target OFF** today. None of the heroes has children yet.
- The battle texts use **Romulus** at size **32**; the bar number uses **24** to fit a 24-tall bar.

---

## Part A — Let the enemies be tapped

Only one field changes on each Foe. The rest is listed so you can compare with what you see.

#### A1. Foe0

- **Select:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ **Foe0**
- **Rect Transform** — greyed out, driven by the **Horizontal Layout Group** of EnemyTeam. **Don't change.**
  - Anchors → shows Min **X 0** · **Y 1** · Max **X 0** · **Y 1**, greyed (the layout puts them there)
  - Pivot → **X 0.5** · **Y 0.5**
  - Width and Height → set by the layout (about **146 × 215**)
  - Rotation → **X 0** · **Y 0** · **Z 0** · Scale → **X 1** · **Y 1** · **Z 1** (in Play the game sets Scale X to **-1** to mirror the figure; that's expected)
- **Canvas Renderer** — no change
- **Image**
  - Source Image → **None** (the game puts the hero figure here in Play) — no change
  - Color → **FFFFFF** · alpha **255** — no change
  - Material → **None (Material)** — no change
  - Raycast Target → **ON** ← **the only change** (already ON in your prefab — just confirm)
  - Maskable → **ON** — no change
  - Image Type, Use Sprite Mesh, Preserve Aspect → **not shown** in the editor (no sprite yet); the game sets them in Play

#### A2. Foe1

- **Select:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ **Foe1**
- **Rect Transform** — greyed out, driven by the **Horizontal Layout Group** of EnemyTeam. **Don't change.**
  - Anchors → shows Min **X 0** · **Y 1** · Max **X 0** · **Y 1**, greyed (the layout puts them there)
  - Pivot → **X 0.5** · **Y 0.5**
  - Width and Height → set by the layout (about **146 × 215**)
  - Rotation → **X 0** · **Y 0** · **Z 0** · Scale → **X 1** · **Y 1** · **Z 1** (in Play the game sets Scale X to **-1** to mirror the figure; that's expected)
- **Canvas Renderer** — no change
- **Image**
  - Source Image → **None** (the game puts the hero figure here in Play) — no change
  - Color → **FFFFFF** · alpha **255** — no change
  - Material → **None (Material)** — no change
  - Raycast Target → **ON** ← **the only change** (already ON in your prefab — just confirm)
  - Maskable → **ON** — no change
  - Image Type, Use Sprite Mesh, Preserve Aspect → **not shown** in the editor (no sprite yet); the game sets them in Play

#### A3. Foe2

- **Select:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ **Foe2**
- **Rect Transform** — greyed out, driven by the **Horizontal Layout Group** of EnemyTeam. **Don't change.**
  - Anchors → shows Min **X 0** · **Y 1** · Max **X 0** · **Y 1**, greyed (the layout puts them there)
  - Pivot → **X 0.5** · **Y 0.5**
  - Width and Height → set by the layout (about **146 × 215**)
  - Rotation → **X 0** · **Y 0** · **Z 0** · Scale → **X 1** · **Y 1** · **Z 1** (in Play the game sets Scale X to **-1** to mirror the figure; that's expected)
- **Canvas Renderer** — no change
- **Image**
  - Source Image → **None** (the game puts the hero figure here in Play) — no change
  - Color → **FFFFFF** · alpha **255** — no change
  - Material → **None (Material)** — no change
  - Raycast Target → **ON** ← **the only change** (already ON in your prefab — just confirm)
  - Maskable → **ON** — no change
  - Image Type, Use Sprite Mesh, Preserve Aspect → **not shown** in the editor (no sprite yet); the game sets them in Play

The game adds the tap by itself when a battle starts: it puts a **Stage Tap** component on each Foe. You don't add it.

Your own heroes (**PlayerTeam ▸ Hero0, Hero1, Hero2**) stay with Raycast Target **OFF**.

---

## Part B — A health bar above each hero

Each hero gets the same 4 objects: **HpBar** (dark background), **Fill** (the colored part), **Value** (the number) and **Target** (a mark). Your heroes have a **green** fill (**4CAF50**), the enemies a **red** one (**D9473B**).

The **Target** mark means two different things, so it looks different on each side:

- on a **Foe** (orange **crosshair**, **FF9E61**): the rival **you** chose to attack this turn;
- on **your hero** (red **warning**, **D9473B**): **the one hero the rival will attack on its next turn**. Like you, the rival attacks only **one** hero per turn: all its group and power damage goes there. It locks that hero at the end of its turn (and at the start of the battle) and must keep it (it only switches if that hero falls during its turn), so you can defend: hand over a face whose colours are weak against the threatened hero, or heal it first.

### Your hero Hero0

#### B1. HpBar

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero0
- **Create:** right-click **Hero0** → **Create Empty**, rename it **HpBar**
- **Rect Transform**
  - Anchors → Min **X 0.5** · **Y 1**
  - Anchors → Max **X 0.5** · **Y 1**
  - Pivot → **X 0.5** · **Y 0**
  - **Pos X 0** · **Pos Y -55** · **Pos Z 0**
  - **Width 120** · **Height 24**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Add Component → UI → Image**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the dark background of the bar)
  - Source Image → **None (Sprite)**
  - Color → **120C09** · alpha **220** (the dark background)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type, Use Sprite Mesh and Preserve Aspect → **not shown** (Unity hides them while Source Image is None) — nothing to set

#### B2. Fill

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero0 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Image**, rename it **Fill**
- **Rect Transform**
  - Anchors → Min **X 0** · **Y 0**
  - Anchors → Max **X 1** · **Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 2** · **Top 2** · **Pos Z 0**
  - **Right 2** · **Bottom 2**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the colored part that empties)
  - Source Image → **None (Sprite)**
  - Color → **4CAF50** · alpha **255** (green)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type, Use Sprite Mesh and Preserve Aspect → **not shown** (Unity hides them while Source Image is None) — nothing to set

#### B3. Value

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero0 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Text - TextMeshPro**, rename it **Value**
- **Rect Transform**
  - Anchors → Min **X 0** · **Y 0**
  - Anchors → Max **X 1** · **Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0**
  - **Right 0** · **Bottom 0**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **TextMeshPro - Text (UI)**
  - Text Input → **40** (only a preview; the game writes the real number)
  - Font Asset → **Romulus** (`Assets/Game/View/Fonts/Romulus.asset`)
  - Material Preset → the default of Romulus (leave it)
  - Font Style → **none selected** (B, I, U, S… all off)
  - Font Size → **24**
  - Auto Size → **OFF**
  - Vertex Color → **FFFFFF** · alpha **255**
  - Color Gradient → **OFF**
  - Override Tags → **OFF**
  - Spacing Options (em) → Character **0** · Word **0** · Line **0** · Paragraph **0**
  - Alignment → horizontal **Center** · vertical **Middle**
  - Wrapping → **Disabled**
  - Overflow → **Overflow**
  - Horizontal Mapping → **Character** · Vertical Mapping → **Character**
  - **Extra Settings** (click to open), in this order:
    - Margins → Left **0** · Top **0** · Right **0** · Bottom **0**
    - Geometry Sorting → **Normal**
    - Is Scale Static → **OFF**
    - Rich Text → **ON**
    - Raycast Target → **OFF**
    - Maskable → **ON**
    - Parse Escape Characters → **ON**
    - Visible Descender → **ON**
    - Emoji Fallback Support → **ON**
    - Sprite Asset → **None**
    - Style Sheet Asset → **None**
    - Font Features → leave as it is
    - Extra Padding → **OFF**

#### B4. Target

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero0 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Image**, rename it **Target**
- **Rect Transform**
  - Anchors → Min **X 0.5** · **Y 1**
  - Anchors → Max **X 0.5** · **Y 1**
  - Pivot → **X 0.5** · **Y 0**
  - **Pos X 0** · **Pos Y 4** · **Pos Z 0**
  - **Width 24** · **Height 24**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the mark above the attacked hero)
  - Source Image → **a warning sprite of yours** (a sword, a skull or "!") — different from the crosshair on the Foes
  - Color → **D9473B** · alpha **255** (red: danger)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type → **Simple**
  - Use Sprite Mesh → **OFF**
  - Preserve Aspect → **ON**
  - **Set Native Size** button → **don't click** (it would change Width/Height)
- **Active checkbox** (next to the name **Target**, top of the Inspector) → **OFF**. The game turns it on above the hero **the rival will attack on its next turn** (its warning).

#### B5. HpBar components

- **Select:** BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero0 ▸ **HpBar**
- It already has **Rect Transform** (B1), **Canvas Renderer** and **Image**. Add two more, in this order:
- **Add Component → Fill Bar**
  - Fill → drag **Hero0 ▸ HpBar ▸ Fill**
- **Add Component → Hero Hp Bar**
  - Bar → drag **Hero0 ▸ HpBar** itself (the field takes its **Fill Bar** component)
  - Label → drag **Hero0 ▸ HpBar ▸ Value**
  - Show Max → **OFF** (shows "32"; ON shows "32/40")
  - Target Mark → drag **Hero0 ▸ HpBar ▸ Target**
  - Seconds → **0.35** (how long the bar takes to slide)

> **Shortcut for the next five heroes:** select this **HpBar**, press **Cmd+D**, and drag the copy (it is called **HpBar (1)**) onto the next hero; rename it **HpBar**. Its four objects and both components come with it and already point to their own children. **Then set the copy's Rect Transform → Pos X back to 0**: when you drag an object onto another parent, Unity keeps it where it was on screen and changes Pos X (for example -139.7) — the bar would float over the wrong hero. Use the blocks below to check every value, and for the three **Foe** bars change **Fill → Image → Color** to **D9473B**.

### Your hero Hero1

#### B6. HpBar

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero1
- **Create:** right-click **Hero1** → **Create Empty**, rename it **HpBar**
- **Rect Transform**
  - Anchors → Min **X 0.5** · **Y 1**
  - Anchors → Max **X 0.5** · **Y 1**
  - Pivot → **X 0.5** · **Y 0**
  - **Pos X 0** · **Pos Y -55** · **Pos Z 0**
  - **Width 120** · **Height 24**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Add Component → UI → Image**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the dark background of the bar)
  - Source Image → **None (Sprite)**
  - Color → **120C09** · alpha **220** (the dark background)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type, Use Sprite Mesh and Preserve Aspect → **not shown** (Unity hides them while Source Image is None) — nothing to set

#### B7. Fill

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero1 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Image**, rename it **Fill**
- **Rect Transform**
  - Anchors → Min **X 0** · **Y 0**
  - Anchors → Max **X 1** · **Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 2** · **Top 2** · **Pos Z 0**
  - **Right 2** · **Bottom 2**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the colored part that empties)
  - Source Image → **None (Sprite)**
  - Color → **4CAF50** · alpha **255** (green)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type, Use Sprite Mesh and Preserve Aspect → **not shown** (Unity hides them while Source Image is None) — nothing to set

#### B8. Value

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero1 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Text - TextMeshPro**, rename it **Value**
- **Rect Transform**
  - Anchors → Min **X 0** · **Y 0**
  - Anchors → Max **X 1** · **Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0**
  - **Right 0** · **Bottom 0**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **TextMeshPro - Text (UI)**
  - Text Input → **40** (only a preview; the game writes the real number)
  - Font Asset → **Romulus** (`Assets/Game/View/Fonts/Romulus.asset`)
  - Material Preset → the default of Romulus (leave it)
  - Font Style → **none selected** (B, I, U, S… all off)
  - Font Size → **24**
  - Auto Size → **OFF**
  - Vertex Color → **FFFFFF** · alpha **255**
  - Color Gradient → **OFF**
  - Override Tags → **OFF**
  - Spacing Options (em) → Character **0** · Word **0** · Line **0** · Paragraph **0**
  - Alignment → horizontal **Center** · vertical **Middle**
  - Wrapping → **Disabled**
  - Overflow → **Overflow**
  - Horizontal Mapping → **Character** · Vertical Mapping → **Character**
  - **Extra Settings** (click to open), in this order:
    - Margins → Left **0** · Top **0** · Right **0** · Bottom **0**
    - Geometry Sorting → **Normal**
    - Is Scale Static → **OFF**
    - Rich Text → **ON**
    - Raycast Target → **OFF**
    - Maskable → **ON**
    - Parse Escape Characters → **ON**
    - Visible Descender → **ON**
    - Emoji Fallback Support → **ON**
    - Sprite Asset → **None**
    - Style Sheet Asset → **None**
    - Font Features → leave as it is
    - Extra Padding → **OFF**

#### B9. Target

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero1 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Image**, rename it **Target**
- **Rect Transform**
  - Anchors → Min **X 0.5** · **Y 1**
  - Anchors → Max **X 0.5** · **Y 1**
  - Pivot → **X 0.5** · **Y 0**
  - **Pos X 0** · **Pos Y 4** · **Pos Z 0**
  - **Width 24** · **Height 24**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the mark above the attacked hero)
  - Source Image → **a warning sprite of yours** (a sword, a skull or "!") — different from the crosshair on the Foes
  - Color → **D9473B** · alpha **255** (red: danger)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type → **Simple**
  - Use Sprite Mesh → **OFF**
  - Preserve Aspect → **ON**
  - **Set Native Size** button → **don't click** (it would change Width/Height)
- **Active checkbox** (next to the name **Target**, top of the Inspector) → **OFF**. The game turns it on above the hero **the rival will attack on its next turn** (its warning).

#### B10. HpBar components

- **Select:** BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero1 ▸ **HpBar**
- It already has **Rect Transform** (B6), **Canvas Renderer** and **Image**. Add two more, in this order:
- **Add Component → Fill Bar**
  - Fill → drag **Hero1 ▸ HpBar ▸ Fill**
- **Add Component → Hero Hp Bar**
  - Bar → drag **Hero1 ▸ HpBar** itself (the field takes its **Fill Bar** component)
  - Label → drag **Hero1 ▸ HpBar ▸ Value**
  - Show Max → **OFF** (shows "32"; ON shows "32/40")
  - Target Mark → drag **Hero1 ▸ HpBar ▸ Target**
  - Seconds → **0.35** (how long the bar takes to slide)

### Your hero Hero2

#### B11. HpBar

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero2
- **Create:** right-click **Hero2** → **Create Empty**, rename it **HpBar**
- **Rect Transform**
  - Anchors → Min **X 0.5** · **Y 1**
  - Anchors → Max **X 0.5** · **Y 1**
  - Pivot → **X 0.5** · **Y 0**
  - **Pos X 0** · **Pos Y -55** · **Pos Z 0**
  - **Width 120** · **Height 24**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Add Component → UI → Image**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the dark background of the bar)
  - Source Image → **None (Sprite)**
  - Color → **120C09** · alpha **220** (the dark background)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type, Use Sprite Mesh and Preserve Aspect → **not shown** (Unity hides them while Source Image is None) — nothing to set

#### B12. Fill

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero2 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Image**, rename it **Fill**
- **Rect Transform**
  - Anchors → Min **X 0** · **Y 0**
  - Anchors → Max **X 1** · **Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 2** · **Top 2** · **Pos Z 0**
  - **Right 2** · **Bottom 2**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the colored part that empties)
  - Source Image → **None (Sprite)**
  - Color → **4CAF50** · alpha **255** (green)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type, Use Sprite Mesh and Preserve Aspect → **not shown** (Unity hides them while Source Image is None) — nothing to set

#### B13. Value

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero2 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Text - TextMeshPro**, rename it **Value**
- **Rect Transform**
  - Anchors → Min **X 0** · **Y 0**
  - Anchors → Max **X 1** · **Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0**
  - **Right 0** · **Bottom 0**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **TextMeshPro - Text (UI)**
  - Text Input → **40** (only a preview; the game writes the real number)
  - Font Asset → **Romulus** (`Assets/Game/View/Fonts/Romulus.asset`)
  - Material Preset → the default of Romulus (leave it)
  - Font Style → **none selected** (B, I, U, S… all off)
  - Font Size → **24**
  - Auto Size → **OFF**
  - Vertex Color → **FFFFFF** · alpha **255**
  - Color Gradient → **OFF**
  - Override Tags → **OFF**
  - Spacing Options (em) → Character **0** · Word **0** · Line **0** · Paragraph **0**
  - Alignment → horizontal **Center** · vertical **Middle**
  - Wrapping → **Disabled**
  - Overflow → **Overflow**
  - Horizontal Mapping → **Character** · Vertical Mapping → **Character**
  - **Extra Settings** (click to open), in this order:
    - Margins → Left **0** · Top **0** · Right **0** · Bottom **0**
    - Geometry Sorting → **Normal**
    - Is Scale Static → **OFF**
    - Rich Text → **ON**
    - Raycast Target → **OFF**
    - Maskable → **ON**
    - Parse Escape Characters → **ON**
    - Visible Descender → **ON**
    - Emoji Fallback Support → **ON**
    - Sprite Asset → **None**
    - Style Sheet Asset → **None**
    - Font Features → leave as it is
    - Extra Padding → **OFF**

#### B14. Target

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero2 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Image**, rename it **Target**
- **Rect Transform**
  - Anchors → Min **X 0.5** · **Y 1**
  - Anchors → Max **X 0.5** · **Y 1**
  - Pivot → **X 0.5** · **Y 0**
  - **Pos X 0** · **Pos Y 4** · **Pos Z 0**
  - **Width 24** · **Height 24**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the mark above the attacked hero)
  - Source Image → **a warning sprite of yours** (a sword, a skull or "!") — different from the crosshair on the Foes
  - Color → **D9473B** · alpha **255** (red: danger)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type → **Simple**
  - Use Sprite Mesh → **OFF**
  - Preserve Aspect → **ON**
  - **Set Native Size** button → **don't click** (it would change Width/Height)
- **Active checkbox** (next to the name **Target**, top of the Inspector) → **OFF**. The game turns it on above the hero **the rival will attack on its next turn** (its warning).

#### B15. HpBar components

- **Select:** BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero2 ▸ **HpBar**
- It already has **Rect Transform** (B11), **Canvas Renderer** and **Image**. Add two more, in this order:
- **Add Component → Fill Bar**
  - Fill → drag **Hero2 ▸ HpBar ▸ Fill**
- **Add Component → Hero Hp Bar**
  - Bar → drag **Hero2 ▸ HpBar** itself (the field takes its **Fill Bar** component)
  - Label → drag **Hero2 ▸ HpBar ▸ Value**
  - Show Max → **OFF** (shows "32"; ON shows "32/40")
  - Target Mark → drag **Hero2 ▸ HpBar ▸ Target**
  - Seconds → **0.35** (how long the bar takes to slide)

### Enemy Foe0

#### B16. HpBar

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe0
- **Create:** right-click **Foe0** → **Create Empty**, rename it **HpBar**
- **Rect Transform**
  - Anchors → Min **X 0.5** · **Y 1**
  - Anchors → Max **X 0.5** · **Y 1**
  - Pivot → **X 0.5** · **Y 0**
  - **Pos X 0** · **Pos Y -55** · **Pos Z 0**
  - **Width 120** · **Height 24**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Add Component → UI → Image**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the dark background of the bar)
  - Source Image → **None (Sprite)**
  - Color → **120C09** · alpha **220** (the dark background)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type, Use Sprite Mesh and Preserve Aspect → **not shown** (Unity hides them while Source Image is None) — nothing to set

#### B17. Fill

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe0 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Image**, rename it **Fill**
- **Rect Transform**
  - Anchors → Min **X 0** · **Y 0**
  - Anchors → Max **X 1** · **Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 2** · **Top 2** · **Pos Z 0**
  - **Right 2** · **Bottom 2**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the colored part that empties)
  - Source Image → **None (Sprite)**
  - Color → **D9473B** · alpha **255** (red)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type, Use Sprite Mesh and Preserve Aspect → **not shown** (Unity hides them while Source Image is None) — nothing to set

#### B18. Value

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe0 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Text - TextMeshPro**, rename it **Value**
- **Rect Transform**
  - Anchors → Min **X 0** · **Y 0**
  - Anchors → Max **X 1** · **Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0**
  - **Right 0** · **Bottom 0**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **TextMeshPro - Text (UI)**
  - Text Input → **40** (only a preview; the game writes the real number)
  - Font Asset → **Romulus** (`Assets/Game/View/Fonts/Romulus.asset`)
  - Material Preset → the default of Romulus (leave it)
  - Font Style → **none selected** (B, I, U, S… all off)
  - Font Size → **24**
  - Auto Size → **OFF**
  - Vertex Color → **FFFFFF** · alpha **255**
  - Color Gradient → **OFF**
  - Override Tags → **OFF**
  - Spacing Options (em) → Character **0** · Word **0** · Line **0** · Paragraph **0**
  - Alignment → horizontal **Center** · vertical **Middle**
  - Wrapping → **Disabled**
  - Overflow → **Overflow**
  - Horizontal Mapping → **Character** · Vertical Mapping → **Character**
  - **Extra Settings** (click to open), in this order:
    - Margins → Left **0** · Top **0** · Right **0** · Bottom **0**
    - Geometry Sorting → **Normal**
    - Is Scale Static → **OFF**
    - Rich Text → **ON**
    - Raycast Target → **OFF**
    - Maskable → **ON**
    - Parse Escape Characters → **ON**
    - Visible Descender → **ON**
    - Emoji Fallback Support → **ON**
    - Sprite Asset → **None**
    - Style Sheet Asset → **None**
    - Font Features → leave as it is
    - Extra Padding → **OFF**

#### B19. Target

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe0 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Image**, rename it **Target**
- **Rect Transform**
  - Anchors → Min **X 0.5** · **Y 1**
  - Anchors → Max **X 0.5** · **Y 1**
  - Pivot → **X 0.5** · **Y 0**
  - **Pos X 0** · **Pos Y 4** · **Pos Z 0**
  - **Width 24** · **Height 24**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the mark above the attacked hero)
  - Source Image → **a small arrow or crosshair sprite of yours**
  - Color → **FF9E61** · alpha **255** (orange)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type → **Simple**
  - Use Sprite Mesh → **OFF**
  - Preserve Aspect → **ON**
  - **Set Native Size** button → **don't click** (it would change Width/Height)
- **Active checkbox** (next to the name **Target**, top of the Inspector) → **OFF**. The game turns it on above the rival **you chose to attack** this turn.

#### B20. HpBar components

- **Select:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe0 ▸ **HpBar**
- It already has **Rect Transform** (B16), **Canvas Renderer** and **Image**. Add two more, in this order:
- **Add Component → Fill Bar**
  - Fill → drag **Foe0 ▸ HpBar ▸ Fill**
- **Add Component → Hero Hp Bar**
  - Bar → drag **Foe0 ▸ HpBar** itself (the field takes its **Fill Bar** component)
  - Label → drag **Foe0 ▸ HpBar ▸ Value**
  - Show Max → **OFF** (shows "32"; ON shows "32/40")
  - Target Mark → drag **Foe0 ▸ HpBar ▸ Target**
  - Seconds → **0.35** (how long the bar takes to slide)

### Enemy Foe1

#### B21. HpBar

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe1
- **Create:** right-click **Foe1** → **Create Empty**, rename it **HpBar**
- **Rect Transform**
  - Anchors → Min **X 0.5** · **Y 1**
  - Anchors → Max **X 0.5** · **Y 1**
  - Pivot → **X 0.5** · **Y 0**
  - **Pos X 0** · **Pos Y -55** · **Pos Z 0**
  - **Width 120** · **Height 24**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Add Component → UI → Image**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the dark background of the bar)
  - Source Image → **None (Sprite)**
  - Color → **120C09** · alpha **220** (the dark background)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type, Use Sprite Mesh and Preserve Aspect → **not shown** (Unity hides them while Source Image is None) — nothing to set

#### B22. Fill

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe1 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Image**, rename it **Fill**
- **Rect Transform**
  - Anchors → Min **X 0** · **Y 0**
  - Anchors → Max **X 1** · **Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 2** · **Top 2** · **Pos Z 0**
  - **Right 2** · **Bottom 2**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the colored part that empties)
  - Source Image → **None (Sprite)**
  - Color → **D9473B** · alpha **255** (red)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type, Use Sprite Mesh and Preserve Aspect → **not shown** (Unity hides them while Source Image is None) — nothing to set

#### B23. Value

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe1 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Text - TextMeshPro**, rename it **Value**
- **Rect Transform**
  - Anchors → Min **X 0** · **Y 0**
  - Anchors → Max **X 1** · **Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0**
  - **Right 0** · **Bottom 0**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **TextMeshPro - Text (UI)**
  - Text Input → **40** (only a preview; the game writes the real number)
  - Font Asset → **Romulus** (`Assets/Game/View/Fonts/Romulus.asset`)
  - Material Preset → the default of Romulus (leave it)
  - Font Style → **none selected** (B, I, U, S… all off)
  - Font Size → **24**
  - Auto Size → **OFF**
  - Vertex Color → **FFFFFF** · alpha **255**
  - Color Gradient → **OFF**
  - Override Tags → **OFF**
  - Spacing Options (em) → Character **0** · Word **0** · Line **0** · Paragraph **0**
  - Alignment → horizontal **Center** · vertical **Middle**
  - Wrapping → **Disabled**
  - Overflow → **Overflow**
  - Horizontal Mapping → **Character** · Vertical Mapping → **Character**
  - **Extra Settings** (click to open), in this order:
    - Margins → Left **0** · Top **0** · Right **0** · Bottom **0**
    - Geometry Sorting → **Normal**
    - Is Scale Static → **OFF**
    - Rich Text → **ON**
    - Raycast Target → **OFF**
    - Maskable → **ON**
    - Parse Escape Characters → **ON**
    - Visible Descender → **ON**
    - Emoji Fallback Support → **ON**
    - Sprite Asset → **None**
    - Style Sheet Asset → **None**
    - Font Features → leave as it is
    - Extra Padding → **OFF**

#### B24. Target

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe1 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Image**, rename it **Target**
- **Rect Transform**
  - Anchors → Min **X 0.5** · **Y 1**
  - Anchors → Max **X 0.5** · **Y 1**
  - Pivot → **X 0.5** · **Y 0**
  - **Pos X 0** · **Pos Y 4** · **Pos Z 0**
  - **Width 24** · **Height 24**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the mark above the attacked hero)
  - Source Image → **a small arrow or crosshair sprite of yours**
  - Color → **FF9E61** · alpha **255** (orange)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type → **Simple**
  - Use Sprite Mesh → **OFF**
  - Preserve Aspect → **ON**
  - **Set Native Size** button → **don't click** (it would change Width/Height)
- **Active checkbox** (next to the name **Target**, top of the Inspector) → **OFF**. The game turns it on above the rival **you chose to attack** this turn.

#### B25. HpBar components

- **Select:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe1 ▸ **HpBar**
- It already has **Rect Transform** (B21), **Canvas Renderer** and **Image**. Add two more, in this order:
- **Add Component → Fill Bar**
  - Fill → drag **Foe1 ▸ HpBar ▸ Fill**
- **Add Component → Hero Hp Bar**
  - Bar → drag **Foe1 ▸ HpBar** itself (the field takes its **Fill Bar** component)
  - Label → drag **Foe1 ▸ HpBar ▸ Value**
  - Show Max → **OFF** (shows "32"; ON shows "32/40")
  - Target Mark → drag **Foe1 ▸ HpBar ▸ Target**
  - Seconds → **0.35** (how long the bar takes to slide)

### Enemy Foe2

#### B26. HpBar

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe2
- **Create:** right-click **Foe2** → **Create Empty**, rename it **HpBar**
- **Rect Transform**
  - Anchors → Min **X 0.5** · **Y 1**
  - Anchors → Max **X 0.5** · **Y 1**
  - Pivot → **X 0.5** · **Y 0**
  - **Pos X 0** · **Pos Y -55** · **Pos Z 0**
  - **Width 120** · **Height 24**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Add Component → UI → Image**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the dark background of the bar)
  - Source Image → **None (Sprite)**
  - Color → **120C09** · alpha **220** (the dark background)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type, Use Sprite Mesh and Preserve Aspect → **not shown** (Unity hides them while Source Image is None) — nothing to set

#### B27. Fill

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe2 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Image**, rename it **Fill**
- **Rect Transform**
  - Anchors → Min **X 0** · **Y 0**
  - Anchors → Max **X 1** · **Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 2** · **Top 2** · **Pos Z 0**
  - **Right 2** · **Bottom 2**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the colored part that empties)
  - Source Image → **None (Sprite)**
  - Color → **D9473B** · alpha **255** (red)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type, Use Sprite Mesh and Preserve Aspect → **not shown** (Unity hides them while Source Image is None) — nothing to set

#### B28. Value

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe2 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Text - TextMeshPro**, rename it **Value**
- **Rect Transform**
  - Anchors → Min **X 0** · **Y 0**
  - Anchors → Max **X 1** · **Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0**
  - **Right 0** · **Bottom 0**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **TextMeshPro - Text (UI)**
  - Text Input → **40** (only a preview; the game writes the real number)
  - Font Asset → **Romulus** (`Assets/Game/View/Fonts/Romulus.asset`)
  - Material Preset → the default of Romulus (leave it)
  - Font Style → **none selected** (B, I, U, S… all off)
  - Font Size → **24**
  - Auto Size → **OFF**
  - Vertex Color → **FFFFFF** · alpha **255**
  - Color Gradient → **OFF**
  - Override Tags → **OFF**
  - Spacing Options (em) → Character **0** · Word **0** · Line **0** · Paragraph **0**
  - Alignment → horizontal **Center** · vertical **Middle**
  - Wrapping → **Disabled**
  - Overflow → **Overflow**
  - Horizontal Mapping → **Character** · Vertical Mapping → **Character**
  - **Extra Settings** (click to open), in this order:
    - Margins → Left **0** · Top **0** · Right **0** · Bottom **0**
    - Geometry Sorting → **Normal**
    - Is Scale Static → **OFF**
    - Rich Text → **ON**
    - Raycast Target → **OFF**
    - Maskable → **ON**
    - Parse Escape Characters → **ON**
    - Visible Descender → **ON**
    - Emoji Fallback Support → **ON**
    - Sprite Asset → **None**
    - Style Sheet Asset → **None**
    - Font Features → leave as it is
    - Extra Padding → **OFF**

#### B29. Target

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe2 ▸ HpBar
- **Create:** right-click **HpBar** → **UI → Image**, rename it **Target**
- **Rect Transform**
  - Anchors → Min **X 0.5** · **Y 1**
  - Anchors → Max **X 0.5** · **Y 1**
  - Pivot → **X 0.5** · **Y 0**
  - **Pos X 0** · **Pos Y 4** · **Pos Z 0**
  - **Width 24** · **Height 24**
  - Rotation → **X 0** · **Y 0** · **Z 0**
  - Scale → **X 1** · **Y 1** · **Z 1**
- **Canvas Renderer** (added by Unity with the Image)
  - Cull Transparent Mesh → **ON** (default; leave it)
- **Image** (the mark above the attacked hero)
  - Source Image → **a small arrow or crosshair sprite of yours**
  - Color → **FF9E61** · alpha **255** (orange)
  - Material → **None (Material)**
  - Raycast Target → **OFF**
  - Raycast Padding → **0 · 0 · 0 · 0** (leave closed)
  - Maskable → **ON**
  - Image Type → **Simple**
  - Use Sprite Mesh → **OFF**
  - Preserve Aspect → **ON**
  - **Set Native Size** button → **don't click** (it would change Width/Height)
- **Active checkbox** (next to the name **Target**, top of the Inspector) → **OFF**. The game turns it on above the rival **you chose to attack** this turn.

#### B30. HpBar components

- **Select:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe2 ▸ **HpBar**
- It already has **Rect Transform** (B26), **Canvas Renderer** and **Image**. Add two more, in this order:
- **Add Component → Fill Bar**
  - Fill → drag **Foe2 ▸ HpBar ▸ Fill**
- **Add Component → Hero Hp Bar**
  - Bar → drag **Foe2 ▸ HpBar** itself (the field takes its **Fill Bar** component)
  - Label → drag **Foe2 ▸ HpBar ▸ Value**
  - Show Max → **OFF** (shows "32"; ON shows "32/40")
  - Target Mark → drag **Foe2 ▸ HpBar ▸ Target**
  - Seconds → **0.35** (how long the bar takes to slide)

> **The enemy bars are not mirrored:** in Play, Foe0–2 get Scale X **-1** to face left, and the game gives their HpBar Scale X **-1** too, so the bar and the number read normally. In the editor you'll see Scale X **1** on both — that's correct.

> **Tuning the height:** press Play and look at a battle. If the bar covers the head, raise it (HpBar **Pos Y -45**, **-35**…); if it floats too high, lower it (**-65**…). Change it on one HpBar and copy the value to the other five. Don't go above **Pos Y 0**: with the Target mark on top, the ~43 units above the box are all there is before the **Rect Mask 2D** on **Stage** cuts it.

> **The heroes may stand at slightly different heights:** the stage draws each figure square in its box, so a sprite exported on a smaller canvas (the Bishop's 68 or the Joker's 64 instead of 80) looks a bit bigger, with its head a little higher. If one bar touches its head, give just that HpBar a slightly higher Pos Y.

> **If the number looks blurry:** Romulus is a pixel font; try **Font Size 16** or **32** instead of 24.

---

## Part C — Connect the bars to the stage

#### C1. Stage — Battle Stage component

- **Select:** BattleHud ▸ SafeArea ▸ Stage
- **Rect Transform** — no change (Anchors Min **X 0 · Y 0.73**, Max **X 1 · Y 0.87**, Left/Top/Right/Bottom **0**)
- **Rect Mask 2D** — no change (Padding **0 · 0 · 0 · 0**, Softness **0 · 0**)
- **Battle Stage** — the existing sections (Cenário, Os lugares dos heróis, As figuras) stay as they are. Fill only the new section **"3 contra 3: as barras de vida em cima das cabeças"**:
  - Player Bars → size **3**
    - Element 0 → drag **BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero0 ▸ HpBar**
    - Element 1 → drag **BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero1 ▸ HpBar**
    - Element 2 → drag **BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero2 ▸ HpBar**
  - Enemy Bars → size **3**
    - Element 0 → drag **BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe0 ▸ HpBar**
    - Element 1 → drag **BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe1 ▸ HpBar**
    - Element 2 → drag **BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe2 ▸ HpBar**
  - Fallen Alpha → **0.3** (a slider from 0 to 1: how faded a defeated hero looks)

---

## Part D — The old team bars (optional)

The two big bars still work and now show each **team's total**:

- **BattleHud ▸ SafeArea ▸ PowerBar ▸ PlayerSide ▸ PlayerHp**
- **BattleHud ▸ SafeArea ▸ PowerBar ▸ EnemySide ▸ EnemyHp**

Choose one:

- **Keep them:** nothing to do.
- **Remove them:**

#### D1. Delete the bars

- Right-click **PlayerHp** → **Delete**. Right-click **EnemyHp** → **Delete**.

#### D2. BattleHud — Battle Ui component

- **Select:** the root **BattleHud**
- **Battle Ui**
  - Enemy Hp → shows **None (Hp Bar)** — leave it empty (allowed now)
  - Player Hp → shows **None (Hp Bar)** — leave it empty (allowed now)
  - every other field — no change

---

## Part E — Save and test

1. Save the prefab (**Cmd+S**) and leave it (the **<** arrow at the top of the Hierarchy).
2. Press **Play**, start a battle and check:
   - the turn text says **"Your turn · tap a rival to attack"**, and tapping the cube does nothing yet;
   - tapping an enemy (Foe) shows the **Target** mark above it; tapping another enemy moves the mark;
   - a group lowers **only that enemy's** bar; a pair (2 pieces) only fills mana;
   - an enemy at 0 fades out (**Fallen Alpha**) and you must choose a new target;
   - from the start, a red **warning** mark is above **one** of your heroes: on its turn the rival attacks only that hero, then moves the warning to its next target;
   - you win when all three enemies fall, and lose when all three of yours fall.

---

## Part F — Golden frame on the chosen rival

When you tap a rival, its row in the power panel (face + its two power bars) gets a golden frame, so you see exactly who you are attacking, how full its bars are and which elements it has. The frame sprite is **Assets/Game/View/Art/target_frame.png** (already 9-sliced: Border 6 on every side, Mesh Type Full Rect).

Open **BattleHud** (double-click the prefab). Repeat F1 for each of the three rows:

| Step | Row |
|---|---|
| F1a | BattleHud ▸ SafeArea ▸ PowerBar ▸ EnemySide ▸ Grid ▸ FoeRow0 |
| F1b | BattleHud ▸ SafeArea ▸ PowerBar ▸ EnemySide ▸ Grid ▸ FoeRow1 |
| F1c | BattleHud ▸ SafeArea ▸ PowerBar ▸ EnemySide ▸ Grid ▸ FoeRow2 |

#### F1. TargetFrame (inside each FoeRow)

1. Right-click the row → **UI → Image**. Rename it **TargetFrame** (TargetFrame0, TargetFrame1, TargetFrame2 also work).
2. Drag it so it is the **last** child of the row (below Foe0/Foe1): last child = drawn on top.
3. **Add Component → Layout Element**:
   - **Ignore Layout**: ✔ (otherwise the row's Horizontal Layout Group treats it as a fourth item).
4. **Rect Transform** (after Ignore Layout the fields unlock):
   - Anchor Presets: hold **Alt** and click the bottom-right square (**stretch / stretch**).
   - **Left** −3, **Top** −3, **Right** −3, **Bottom** −3.
   - **Pivot** X 0.5, Y 0.5. **Rotation** 0, 0, 0. **Scale** 1, 1, 1.
5. **Image**:
   - **Source Image**: target_frame
   - **Color**: white (FFFFFF, A 255)
   - **Material**: None
   - **Raycast Target**: ☐ (off — otherwise it eats the taps on the power bars)
   - **Maskable**: ✔
   - **Image Type**: Sliced
   - **Fill Center**: ✔ (the centre of the sprite is transparent anyway)
   - **Pixels Per Unit Multiplier**: 1 (use **0.5** for a frame twice as thick)
6. Untick the checkbox next to the object's name at the top of the Inspector (**inactive**). The game turns on only the chosen one.

#### F2. PowerBar — Power Board component

Select **BattleHud ▸ SafeArea ▸ PowerBar** (the object with the **Power Board** component). Under **O rival escolhido como alvo (3 contra 3)**:

- **Enemy Target Frames**: Size **3**
  - Element 0: drag **FoeRow0 ▸ TargetFrame**
  - Element 1: drag **FoeRow1 ▸ TargetFrame**
  - Element 2: drag **FoeRow2 ▸ TargetFrame**

#### F3. Test

Save (**Cmd+S**), press **Play**, start a battle:

- no frame at the start of your turn (no target yet);
- tap an enemy on the stage: its row in the panel gets the golden frame; tap another: the frame moves;
- when that enemy falls, the frame goes away until you choose again.

---

## Reference — the elements

Each colour beats one and loses to one:

**Water → Fire → Leaf → Air → Earth → Lightning → Water**

Against a hero, both of its colours count: **+50%** damage for each target colour the attack beats, **−25%** for each target colour that beats the attack. Groups use the **group's colour**, powers use **their own colour**. Values in `rules.json` → `elements` (`beats`, `strong`, `weak`).
