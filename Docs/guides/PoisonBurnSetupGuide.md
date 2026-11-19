# Poison and Burn — setup guide

What the code already does (nothing to set up):

- The cube is now **6×6**. An old 5×5 battle that was saved mid-fight still resumes at 5×5. The next battle is 6×6.
- Two new items in the Armory:

  | Item | id | Effect |
  |---|---|---|
  | **Viper Fang** | `presa` | Poison: 3 damage per turn for 3 turns |
  | **Ember Charm** | `brasa` | Burn: 5 damage per turn for 2 turns |

  The values live in `Assets/Game/Content/Json/data/equipment.json` (`n` = damage per turn, `turns` = duration). The area rules live in `rules.json` → `hazards` (`min` 4, `area` 3, `rounds` 3).
- **Upgrades:** each item has 6 levels, offered in the run's blessings and shop like ability upgrades (only for an item your team carries). Values in `upgrades.json` → `equipment`:

  | Level | Viper Fang (poison) | Ember Charm (burn) |
  |---|---|---|
  | base | 3×3 · 3 dmg · 3 turns | 3×3 · 5 dmg · 2 turns |
  | + | area 4×4 | area 4×4 |
  | ++ | +1 dmg (4) | +2 dmg (7) |
  | +++ | +1 turn (4) | +1 turn (3) |
  | 4 | area 5×5 | area 5×5 |
  | 5 | +1 dmg (5) | +2 dmg (9) |
  | ★ | area 6×6 (the whole face) | area 6×6 (the whole face) |
- In battle: tap a group of 4+ with the item equipped → the banner says **Poison ready!** → choose the face → **hold your finger** on the cube. The 3×3 area follows your finger (your finger is the centre square) and glows. **Let go** to place it. The rival plays after that.
- The green/red glow on the cube is drawn by code. You don't need any prefab work for it.

What you set up by hand: **A** the item icons, **B** the poison and burn icons on the health bars (optional), **C** test.

---

## Part A — Item icons in the Armory

1. Make the two icons on the same **48×48** canvas as the other items (prompts at the end). Save them in **Assets/Game/View/Art/Armory/Equipments/**, for example `Viper_fang.png` and `Ember_charm.png`.
2. For each new PNG, in the Inspector (same as the other item icons):
   - Texture Type → **Sprite (2D and UI)**
   - Sprite Mode → **Single**
   - Pixels Per Unit → **100**
   - Mesh Type → **Full Rect**
   - Filter Mode → **Point (no filter)**
   - Compression → **None**
   - Click **Apply**
3. Open the **EquipScreen** prefab (Assets/Game/View/Prefabs/UI) and select the root **EquipScreen**.
4. In **Equip Screen → Icons**, click **+** twice (Size goes from 8 to 10):
   - Element 8 → Item Id **presa** · Sprite **Viper_fang**
   - Element 9 → Item Id **brasa** · Sprite **Ember_charm**
5. Save (**Cmd+S**).

Without an icon the items still work; the slot just shows no picture.

---

## Part B — Status icons on the health bars (optional)

Each hero's bar shows one small icon per active effect (poison, burn and any future one), **above the bar, starting at its left edge**, in the order the effects began. Nothing is hard-coded per effect:

- one **StatusIcon** prefab (B1),
- one empty **Status** row on each bar (B2, B3); the game creates a copy of the prefab inside it for each effect,
- one list on the **Battle Stage** that says which picture each effect uses (B4). A new effect later only needs a new line in that list.

The **first** effect is the leftmost; each newer one goes to its right. Renewing an effect that is already on keeps its place. The orange **Target** mark moves to the right end above the bar (B2b), so the two never overlap.

**Tapping an icon** (on your heroes or the rival's) opens the same balloon as the power details, for example *"Poison / Loses 3 health at the start of each round. / 2 rounds left."* Tapping the same icon again, or anywhere outside, closes it.

#### B1. The StatusIcon prefab

1. In the Project window, open **Assets/Game/View/Prefabs/UI**.
2. Right-click → **Create → Prefab**, name it **StatusIcon**, double-click it to open.
3. Select the root **StatusIcon** → **Add Component → UI → Image** (Unity turns the Transform into a Rect Transform).
4. **Rect Transform**
   - **Width 20** · **Height 20**
   - Pivot → **X 0.5** · **Y 0.5**
   - Rotation → **X 0** · **Y 0** · **Z 0** · Scale → **X 1** · **Y 1** · **Z 1**
5. **Image**
   - Source Image → **None** (the game fills it)
   - Color → **FFFFFF** · alpha **255**
   - Material → **None (Material)**
   - Raycast Target → **ON** (needed for the tap that opens the balloon)
   - Maskable → **ON**
6. **Add Component → Status Icon**
   - Icon → drag **StatusIcon** itself (its Image)
7. **Cmd+S** and leave the prefab.

If you already made the prefab with a **Turns** text, delete the Turns child: the rounds left are in the balloon now.

#### B2. The Status row on each bar

Do B2 and B3 on each of these **6 bars** inside **BattleHud** (double-click the prefab; work inside the prefab, not on the copy in the Main scene):

| Bar |
|---|
| BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero0 ▸ HpBar |
| BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero1 ▸ HpBar |
| BattleHud ▸ SafeArea ▸ Stage ▸ PlayerTeam ▸ Hero2 ▸ HpBar |
| BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe0 ▸ HpBar |
| BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe1 ▸ HpBar |
| BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ Foe2 ▸ HpBar |

If you already made **Poison** and **Burn** Images under a bar, **delete them**: the icons are created by the game now.

- **Create:** right-click **HpBar** → **Create Empty**, rename it **Status**. *(Already made it on the left side? Keep it and just change the values below.)*
- **Rect Transform**
  - Anchors → Min **X 0** · **Y 1**
  - Anchors → Max **X 0** · **Y 1**
  - Pivot → **X 0** · **Y 0**
  - **Pos X 0** · **Pos Y 4** · **Pos Z 0**
  - **Width 92** · **Height 20** (room for 4 icons of 20 with gaps of 4)
  - Rotation → **X 0** · **Y 0** · **Z 0** · Scale → **X 1** · **Y 1** · **Z 1**
- **Add Component → Horizontal Layout Group**
  - Padding → **Left 0 · Right 0 · Top 0 · Bottom 0**
  - Spacing → **4**
  - Child Alignment → **Middle Left**
  - Reverse Arrangement → **OFF** (the first effect is the leftmost)
  - Control Child Size → Width **OFF** · Height **OFF**
  - Use Child Scale → Width **OFF** · Height **OFF**
  - Child Force Expand → Width **OFF** · Height **OFF**
- Leave **Status** empty.

#### B2b. Move the Target mark to the right end

- **Select:** **HpBar ▸ Target** (the orange mark, or the red warning on your heroes)
- **Rect Transform**
  - Anchors → Min **X 1** · **Y 1**
  - Anchors → Max **X 1** · **Y 1**
  - Pivot → **X 1** · **Y 0**
  - **Pos X 0** · **Pos Y 4** · **Pos Z 0**
  - **Width 24** · **Height 24** (unchanged)

#### B3. Hero Hp Bar component

- **Select:** the **HpBar** itself
- **Hero Hp Bar → Status Row** → drag **HpBar ▸ Status**

#### B4. Battle Stage: the prefab and the pictures

- **Select:** **BattleHud ▸ SafeArea ▸ Stage** (the object with **Battle Stage**)
- Under **Efeitos nas barras (veneno, fogo e os que vierem)**:
  - **Status Prefab** → drag the **StatusIcon** prefab from the Project window
  - **Status Art** → Size **2**
    - Element 0 → Id **poison** · Sprite **your poison icon**
    - Element 1 → Id **burn** · Sprite **your fire icon**
- **Cmd+S**.

---

## Part B2 — The rival's tap area below the bar

Tapping a rival selects it as your target. So that this never fights with tapping a status icon, the tap only counts **below the health bar**. Do this on **Foe0, Foe1 and Foe2** inside **BattleHud**.

#### T1. TapZone

- **Parent:** BattleHud ▸ SafeArea ▸ Stage ▸ EnemyTeam ▸ **Foe0** (then Foe1, Foe2)
- **Create:** right-click **Foe0** → **UI → Image**, rename it **TapZone**
- Drag it so it is the **first** child of Foe0 (above **HpBar** in the list), so the bar and icons draw on top of it.
- **Rect Transform**
  - Anchor Presets → hold **Alt** and click the bottom-right square (**stretch / stretch**)
  - **Left 0** · **Top 59** · **Right 0** · **Bottom 0** (the bar's bottom edge is 55 below the top of the figure; 59 leaves a small gap)
  - Pivot → **X 0.5** · **Y 0.5**
  - Rotation → **X 0** · **Y 0** · **Z 0** · Scale → **X 1** · **Y 1** · **Z 1**
- **Image**
  - Source Image → **None (Sprite)**
  - Color → **FFFFFF** · alpha **0** (invisible, but still catches taps)
  - Material → **None (Material)**
  - Raycast Target → **ON**
  - Maskable → **ON**

#### T2. The figure itself

- **Select:** **Foe0** (then Foe1, Foe2)
- **Image → Raycast Target** → **OFF** (the figure no longer takes taps; TapZone does)

#### T3. Battle Stage

- **Select:** **BattleHud ▸ SafeArea ▸ Stage**
- **Battle Stage → Enemy Tap Areas** → Size **3**
  - Element 0 → drag **Foe0 ▸ TapZone**
  - Element 1 → drag **Foe1 ▸ TapZone**
  - Element 2 → drag **Foe2 ▸ TapZone**
- **Cmd+S**.

If Enemy Tap Areas is left empty, the whole figure counts as before.

---

## Part C — Test

1. **Armory:** equip **Viper Fang** on one hero. The text should read "Tap a group of 4+ pieces: the face you send gets a poisoned area…".
2. **Battle:**
   - The cube is 6×6.
   - Tap a group of 4 or more. The banner shows **Poison ready!**
   - Choose the face and confirm. The cube turns, and the turn text says **"Hold on the cube to aim the poison · let go to place it"**.
   - Hold your finger on the cube and move it. A green 3×3 area follows, pulsing. Near the edge it stays inside the face.
   - Let go. The area stays green and the rival plays.
   - If the rival matches through the area, the banner shows **"⟨hero⟩ is poisoned!"**. The area disappears, and the hero you were targeting loses 3 health at the start of each of the rival's next 3 turns (banner **Poison −3 · ⟨hero⟩**, plus the poison icon left of the bar if you did Part B).
   - If nobody touches the area, it fades after 3 rounds. If the face comes back to you, the area can poison **your** targeted hero instead (the one under the rival's red warning).
3. Optional: the glow colours are in the **CubeRig** prefab (Assets/Game/View/Prefabs). Open it, select the root **CubeRig** (the object with **Cube View**) and look under **Veneno e fogo** for Poison Color, Burn Color and Aim Boost. Change them in the prefab, not on the copy in the Main scene.

---

## PixelLab prompts

**Viper Fang** (48×48 item icon):

> Pixel art item icon, 48x48, single curved white viper fang with a drop of bright green venom at the tip, small dark leather cord tied at the root, dark outline, soft top-left light, centered, transparent background, same style as fantasy RPG inventory icons.
> AVOID: snake body, text, frame, background, blur.

**Ember Charm** (48×48 item icon):

> Pixel art item icon, 48x48, small round bronze amulet holding a glowing orange-red ember stone, tiny flames licking the top edge, dark outline, soft top-left light, centered, transparent background, same style as fantasy RPG inventory icons.
> AVOID: big fire, text, frame, background, blur.

**Poison status icon** (16×16 or 24×24):

> Pixel art status icon, 16x16, single bright green poison drop with a dark green outline and one white highlight, transparent background.

**Burn status icon** (16×16 or 24×24):

> Pixel art status icon, 16x16, small orange-red flame with a yellow core and dark red outline, transparent background.
