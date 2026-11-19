# Shop screen — setup guide (by hand)

Follow it top to bottom. Every element has its own block with every value to type, even when it repeats.

**Reading the anchors:** in Unity **Y 0 = bottom** of the parent and **Y 1 = top**. X 0 = left, X 1 = right.

**Where the anchor fields are:** select the object → Inspector → **Rect Transform** → click the small arrow next to **Anchors** to open Min and Max. After typing stretched anchors, the top of the Rect Transform shows **Left, Top, Pos Z, Right, Bottom**.

**Colors:** click the color box and type the hex code in the **Hexadecimal** field (without #).

**Fonts** (the same as the Armory): titles and buttons use **alagard**, body text uses **Romulus**.

**What the code already does** (nothing to set up): prices, gold, health, floor, the merchant's lines, which offer is on sale, the old price crossed out, Sold, red prices you can't pay, the Buy button and its reason when it's off, the restock (new offers, price going up), the potion, your rune bag, the gold counting down after a purchase, the bubble hopping when the merchant speaks, and the purchased offer bouncing.

The screen, top to bottom:

| Part | Share of the screen | What's in it |
|---|---|---|
| **MerchantPanel** | top 30% | the shop scene (merchant behind his counter), the **Shop** nameplate, gold, health, floor, the speech bubble |
| **Shelves** | next 45% | Upgrades (2), Runes (3), Services (Healing Potion and Restock), each row on a wooden plank |
| **DetailPanel** | next 14% | the offer you tapped: picture, name, effect, **Buy** button |
| **BottomBar** | bottom 11% | your 3 rune sockets and the **Next floor** button |

---

## Part 0 — The art in PixelLab

Reused from the Armory (nothing to generate): **Armory_cabinet_panel**, **Armory_cabinet_rail**, **Armory_nameplate**, **Armory-square-item-slot**, **hero_equipment_column_2** (the round socket), **tab** (the button), **background_dark**, and the **fang** and **ember** item icons.

New pieces: the merchant scene, the speech bubble, the price tag, the shelf plank, the coin and heart, the sale badge, the sold seal, the potion and restock icons and the 5 rune icons.

> **Already done (2026-11-13):** all 15 new pieces were generated with PixelLab and are in `Assets/Game/View/Art/Shop`, already imported and placed in the **ShopOffer** and **ShopScreen** prefabs by **Cubo ▸ Telas ▸ Aplicar a arte da Loja (Art ▸ Shop)**. You can skip Parts 0, 1 and 2. To swap a piece, save the new PNG over the old one with the **same file name** (trimmed to its drawing, no empty margin) and run that menu again. The prompts below stay as the recipe for regenerating a piece.

### 0.1 Full-screen mockup (for the look only)

This image is **not used in the game**: it fixes the style and where things go.

1. Click **Clear** to remove the example.
2. Click **Screen Layout** and choose a **phone portrait** size.
3. Draw these rectangles, top to bottom:
   - top **30%**: one big rectangle (the shop scene), with a small one at the top centre (nameplate) and one in the lower right half (speech bubble)
   - next **45%**: three horizontal bands (shelves); 2 squares in the first band, 3 in the second, 2 in the third
   - next **14%**: a square on the left (picture), a wide box in the middle (text), a button on the right
   - bottom **11%**: 3 small circles on the left, a wide button on the right

Prompt:

```text
Mobile game UI screen, pixel art, portrait, a medieval fantasy merchant shop between battles. Top: a cosy shop interior seen from the front, a bearded merchant in a green hood and leather apron standing behind a heavy wooden counter, shelves of potions, scrolls and crates behind him, hanging oil lamps with warm light. A copper nameplate at the top centre. A parchment speech bubble on the right side of the scene. Middle: a dark slate-blue wooden cabinet with copper riveted rails, three wooden shelves, square copper-framed item slots standing on the planks, small parchment price tags with a gold coin under each slot. Below: a detail panel with a square framed picture on the left, empty text space in the middle and a wooden button on the right. Bottom: three round sockets on the left and a wide wooden button on the right. Palette: dark slate blue, warm copper, aged wood, parchment, gold accents. Crisp pixel outlines, flat shading, no blur. All text areas left empty.
AVOID: letters, words, numbers, logos, watermark, 3D render, photo, gradients, blurry edges, characters outside the top scene.
```

### 0.2 The pieces

Each piece uses the same layout recipe as the Armory: set **Output size**, draw **one rectangle** of the given size **centred**, corner radius **0**, and paste the prompt. Generate them in one session and use the first good piece (or the mockup) as the **style reference** for the rest.

#### a. Merchant scene — `shop_merchant_scene`

- Output size **304 × 168**, rectangle **304 × 168** (full canvas: it's a picture, not a stretching piece). 304 px across the panel gives the same pixel size as the cabinet under it.

```text
Pixel art scene, 304x168, front view of a medieval fantasy shop interior. A bearded middle-aged merchant in a dark green hood and brown leather apron stands behind a long heavy wooden counter, seen from the waist up, on the LEFT third of the image, hands resting on the counter, friendly sly smile. Behind him wooden shelves with colourful potion bottles, rolled scrolls, small crates and a brass scale. Two hanging oil lamps give warm orange light. The RIGHT half of the scene stays calm and simple (shelves and wall only) because a speech bubble will cover it. The top 20 pixels stay plain dark wooden beams. The counter runs across the whole bottom edge. Palette: dark slate blue walls, warm copper, aged brown wood, gold highlights, small green and red potion accents. Crisp 1 px dark outlines, flat shading, readable at small size.
AVOID: text, letters, signs with words, a second character, weapons, blur, gradients, 3D, photo, cropped head.
```

Optional, the merchant breathing (like the Joker's idle): use PixelLab's **animate** on the scene with a short prompt such as `merchant breathes and blinks slowly, counter and shelves stay still`, 4–6 frames.

#### b. Speech bubble — `shop_speech_bubble`

- Output size **72 × 48**, rectangle **64 × 40** centred (4 px empty margin on every side)

```text
Pixel art game UI speech bubble on a 72x48 transparent canvas. The bubble fills exactly the centred 64x40 rectangle: aged parchment, rounded corners, 2 px dark brown outline, a small pointed tail at the BOTTOM-LEFT corner pointing down-left, inside the bottom-left corner area only. Straight top, right and bottom edges so the middle can stretch. Plain flat parchment inside, no texture lines. 4 px of empty transparent margin around the rectangle.
AVOID: text, letters, shadows outside the rectangle, gradients, blur, 3D.
```

#### c. Price tag — `shop_price_tag`

- Output size **56 × 24**, rectangle **48 × 16** centred

```text
Pixel art game UI price tag on a 56x24 transparent canvas, filling exactly the centred 48x16 rectangle. A small horizontal parchment label with a 1 px dark brown outline, slightly rounded corners, a tiny copper rivet at the left and the right end. Straight edges so it can stretch horizontally. Plain inside. 4 px of empty transparent margin around it.
AVOID: text, numbers, coin, string, shadows, gradients, blur.
```

#### d. Shelf plank — `shop_shelf_plank`

- Output size **136 × 16**, rectangle **128 × 8** centred

```text
Pixel art game UI wooden shelf plank on a 136x16 transparent canvas, filling exactly the centred 128x8 rectangle. Dark aged wood seen from the front, a lighter top edge, a copper bracket at the far left and the far right end. Straight horizontal edges so the middle can repeat. 4 px of empty transparent margin.
AVOID: items on the shelf, text, shadows below, gradients, blur.
```

#### e. Coin and heart — `shop_coin`, `shop_heart`

- Output size **16 × 16**, no rectangle (each icon fills the canvas)

```text
Pixel art icon, 16x16, a single shiny gold coin seen from the front, 1 px dark outline, one white highlight, transparent background.
```

```text
Pixel art icon, 16x16, a single red heart, 1 px dark red outline, one white highlight, transparent background.
```

#### f. Sale badge and sold seal — `shop_sale_badge`, `shop_sold_seal`

- Output size **24 × 24**, no rectangle

```text
Pixel art icon, 24x24, a small red ribbon rosette badge with two short tails, gold edge, 1 px dark outline, transparent background.
AVOID: text, numbers, percent sign.
```

```text
Pixel art icon, 24x24, a dark red wax seal with a pressed X mark in the middle, slightly irregular round edge, 1 px dark outline, transparent background.
AVOID: text, letters.
```

#### g. Icons on the 48 × 48 canvas (like the Armory items)

Output size **48 × 48**; keep the drawing about **28–30 px tall**, centred, so they match the Armory items. One session, the first good one as reference.

| File | Prompt (add the AVOID line below to each) |
|---|---|
| `shop_potion` | `Pixel art item icon, 48x48, a round glass healing potion bottle with red liquid and a cork, small heart shine, dark outline, centered, transparent background.` |
| `shop_restock` | `Pixel art item icon, 48x48, a small wooden crate with two curved copper arrows circling around it, dark outline, centered, transparent background.` |
| `rune_folego` | `Pixel art item icon, 48x48, a flat grey rune stone with a glowing blue swirl of breath carved on it, dark outline, centered, transparent background.` |
| `rune_estrela` | `Pixel art item icon, 48x48, a flat grey rune stone with a glowing gold four-pointed star carved on it, dark outline, centered, transparent background.` |
| `rune_fluxo` | `Pixel art item icon, 48x48, a flat grey rune stone with a glowing teal flowing wave carved on it, dark outline, centered, transparent background.` |
| `rune_escudo` | `Pixel art item icon, 48x48, a flat grey rune stone with a glowing silver shield carved on it, dark outline, centered, transparent background.` |
| `rune_vida` | `Pixel art item icon, 48x48, a flat grey rune stone with a glowing red heart carved on it, dark outline, centered, transparent background.` |

AVOID line: `AVOID: text, letters, background, frame, blur, gradients.`

---

## Part 1 — Import the PNGs

1. Project window → `Assets/Game/View/Art` → right-click → **Create → Folder** → name it **Shop**.
2. Drag every new PNG into **Shop**, with the file names above.
3. Select **all of them** (click the first, **Shift+click** the last) and set in the Inspector:
   - Texture Type **Sprite (2D and UI)**
   - Sprite Mode **Single**
   - Pixels Per Unit **100**
   - Mesh Type **Full Rect**
   - Generate Physics Shape **off**
   - Filter Mode **Point (no filter)**
   - Compression **None**
   - Max Size **2048**
4. Click **Apply**.

## Part 2 — Borders (Sprite Editor), so pieces stretch without bending

For each of these: select it → **Open Sprite Editor** → in the small panel at the bottom right type the **Border** values → **Apply** (top right) → close.

| Sprite | L | T | R | B |
|---|---|---|---|---|
| shop_speech_bubble | 18 | 5 | 5 | 12 |
| shop_price_tag | 8 | 0 | 8 | 0 |
| shop_shelf_plank | 24 | 0 | 18 | 0 |

(The bubble's bigger left and bottom borders keep the tail from stretching; if your tail is bigger, raise L and B until the green lines leave the whole tail in the bottom-left corner. The tag and the plank only have left and right borders: their ends stay whole and the middle stretches sideways. The **Aplicar a arte da Loja** menu sets these borders by itself.)

---

## Part 3 — The ShopOffer prefab (one offer on a shelf)

#### 1. Create the prefab

1. Project window → `Assets/Game/View/Prefabs/UI` → right-click → **Create → Prefab** → name it **ShopOffer** → double-click to open it.
2. Select the root **ShopOffer** → **Add Component → UI → Image** (Unity turns it into a Rect Transform). This Image catches the tap.
3. **Rect Transform**: **Width 150** · **Height 186** · Pivot **X 0.5 · Y 0.5** · Rotation **0, 0, 0** · Scale **1, 1, 1**
4. **Image**
   - Source Image → **None (Sprite)**
   - Color → **FFFFFF** · alpha **0** (invisible, still catches taps)
   - Raycast Target → **ON**

#### 2. Frame

- **Create:** right-click **ShopOffer** → **UI → Image**, rename it **Frame**
- **Rect Transform**
  - Anchors → Min **X 0 · Y 1**, Max **X 1 · Y 1**
  - Pivot → **X 0.5 · Y 1**
  - **Left 0 · Pos Y 0 · Right 0 · Height 150**
  - Rotation **0, 0, 0** · Scale **1, 1, 1**
- **Image**
  - Source Image → **Armory-square-item-slot**
  - Color → **FFFFFF** · alpha **255** (the game tints it when it's the offer in focus)
  - Raycast Target → **OFF**
  - Image Type → **Simple** · Preserve Aspect → **ON**

#### 3. Icon

- **Create:** right-click **Frame** → **UI → Image**, rename it **Icon**
- **Rect Transform**: Anchors Min **0.5, 0.5**, Max **0.5, 0.5** · Pivot **0.5, 0.5** · **Pos X 0 · Pos Y 0** · **Width 96 · Height 96**
- **Image**: Source Image **None** · Color **FFFFFF** 255 · Raycast Target **OFF** · Preserve Aspect **ON**

#### 4. Portrait (the hero's face on ability upgrades)

- **Create:** right-click **Frame** → **UI → Image**, rename it **Portrait**
- **Rect Transform**: Anchors Min **0.5, 0.5**, Max **0.5, 0.5** · Pivot **0.5, 0.5** · **Pos X 0 · Pos Y 0** · **Width 110 · Height 110**
- **Image**: Source Image **None** · Color **FFFFFF** 255 · Raycast Target **OFF** · Preserve Aspect **ON**

#### 5. Tier (the "++" in the corner)

- **Create:** right-click **Frame** → **UI → Text - TextMeshPro**, rename it **Tier**
- **Rect Transform**: Anchors Min **1, 1**, Max **1, 1** · Pivot **1, 1** · **Pos X -8 · Pos Y -6** · **Width 70 · Height 36**
- **TextMeshPro**: Text Input **empty** · Font Asset **alagard** · Font Size **28** · Color **F2CC73** · Alignment **Right** and **Top** · Wrapping **Disabled** · Raycast Target **OFF**

#### 6. SaleBadge

- **Create:** right-click **Frame** → **UI → Image**, rename it **SaleBadge**
- **Rect Transform**: Anchors Min **0, 1**, Max **0, 1** · Pivot **0, 1** · **Pos X -8 · Pos Y 8** · **Width 56 · Height 56**
- **Image**: Source Image **shop_sale_badge** · Color **FFFFFF** 255 · Raycast Target **OFF** · Preserve Aspect **ON**
- **Active checkbox** → **OFF**

#### 7. SoldSeal

- **Create:** right-click **Frame** → **UI → Image**, rename it **SoldSeal**
- **Rect Transform**: Anchors Min **0.5, 0.5**, Max **0.5, 0.5** · Pivot **0.5, 0.5** · **Pos X 0 · Pos Y 0** · **Width 84 · Height 84** · Rotation **Z -12**
- **Image**: Source Image **shop_sold_seal** · Color **FFFFFF** 255 · Raycast Target **OFF** · Preserve Aspect **ON**
- **Active checkbox** → **OFF**

#### 8. PriceTag

- **Create:** right-click **ShopOffer** → **UI → Image**, rename it **PriceTag**
- **Rect Transform**
  - Anchors → Min **X 0 · Y 0**, Max **X 1 · Y 0**
  - Pivot → **X 0.5 · Y 0**
  - **Left 8 · Pos Y 0 · Right 8 · Height 44**
- **Image**: Source Image **shop_price_tag** · Color **FFFFFF** 255 · Raycast Target **OFF** · Image Type **Sliced** · Fill Center **ON** · Pixels Per Unit Multiplier **0.386** (the art's height ÷ the Image's height: 17 ÷ 44, so the rivets keep their shape)
- **Add Component → Horizontal Layout Group**
  - Padding **Left 8 · Right 8 · Top 4 · Bottom 4** · Spacing **6**
  - Child Alignment **Middle Center**
  - Control Child Size → Width **ON** · Height **ON**
  - Use Child Scale → Width **OFF** · Height **OFF**
  - Child Force Expand → Width **OFF** · Height **OFF**

#### 9. Coin (inside PriceTag)

- **Create:** right-click **PriceTag** → **UI → Image**, rename it **Coin**
- **Image**: Source Image **shop_coin** · Color **FFFFFF** 255 · Raycast Target **OFF** · Preserve Aspect **ON**
- **Add Component → Layout Element**: Preferred Width **28** · Preferred Height **28**

#### 10. Price (inside PriceTag)

- **Create:** right-click **PriceTag** → **UI → Text - TextMeshPro**, rename it **Price**
- **TextMeshPro**: Text Input **00** · Font Asset **Romulus** · Font Size **28** · Color **3A2A1A** (the game colours it) · Alignment **Center** and **Middle** · Wrapping **Disabled** · Raycast Target **OFF**

#### 11. OldPrice (inside PriceTag)

- **Create:** right-click **PriceTag** → **UI → Text - TextMeshPro**, rename it **OldPrice**
- **TextMeshPro**: Text Input **00** · Font Asset **Romulus** · Font Size **20** · Color **7A6A5A** · Alignment **Center** and **Middle** · Wrapping **Disabled** · Raycast Target **OFF**
- **Active checkbox** → **OFF** (the game turns it on for the item on sale, crossed out)

#### 12. Shop Offer Slot component

- **Select:** the root **ShopOffer** → **Add Component → Shop Offer Slot**
  - Frame → **Frame**
  - Icon → **Frame ▸ Icon**
  - Portrait → **Frame ▸ Portrait**
  - Tier Tag → **Frame ▸ Tier**
  - Price → **PriceTag ▸ Price**
  - Old Price → **PriceTag ▸ OldPrice**
  - Sale Badge → **Frame ▸ SaleBadge**
  - Sold Stamp → **Frame ▸ SoldSeal**
  - Price Tag → **PriceTag**
  - Sold Alpha → **0.35**
- **Cmd+S** and leave the prefab.

---

## Part 4 — The ShopScreen prefab

#### 13. ShopScreen (the canvas)

1. Project window → `Assets/Game/View/Prefabs/UI` → right-click → **Create → Prefab** → name it **ShopScreen** → double-click.
2. Select the root **ShopScreen** and add, in this order:
   - **Add Component → Canvas**: Render Mode **Screen Space - Overlay** · Pixel Perfect **OFF** · Sort Order **11** (the same as the Armory)
   - **Add Component → Canvas Scaler**: UI Scale Mode **Scale With Screen Size** · Reference Resolution **X 1080 · Y 1920** · Screen Match Mode **Match Width Or Height** · Match **0** (width) · Reference Pixels Per Unit **100**
   - **Add Component → Graphic Raycaster** (defaults)
   - **Add Component → Canvas Group**: Alpha **1** · Interactable **ON** · Blocks Raycasts **ON**

#### 14. Back

- **Create:** right-click **ShopScreen** → **UI → Image**, rename it **Back**
- **Rect Transform**: Anchor Presets → **Alt + click** the bottom-right square (**stretch / stretch**) · **Left 0 · Top 0 · Right 0 · Bottom 0**
- **Image**: Source Image **background_dark** · Color **FFFFFF** 255 · Raycast Target **ON** (blocks taps to anything behind) · Image Type **Simple**

#### 15. SafeArea

- **Create:** right-click **ShopScreen** → **Create Empty**, rename it **SafeArea**
- **Rect Transform**: **stretch / stretch** · **Left 0 · Top 0 · Right 0 · Bottom 0**
- **Add Component → Safe Area**: Min Bottom **0** · Side Padding **24** · Top Padding **16** · Bottom Padding **16**

### Merchant

#### 16. MerchantPanel

- **Create:** right-click **SafeArea** → **Create Empty**, rename it **MerchantPanel**
- **Rect Transform**: Anchors Min **X 0 · Y 0.70**, Max **X 1 · Y 1** · **Left 0 · Top 0 · Right 0 · Bottom 0** · Pivot **0.5, 0.5**
- **Add Component → Rect Mask 2D** (cuts off the part of the scene that grows past the panel on taller phones)

#### 17. Scene

- **Create:** right-click **MerchantPanel** → **UI → Image**, rename it **Scene**
- **Rect Transform**: **stretch / stretch** · **Left 0 · Top 0 · Right 0 · Bottom 0**
- **Image**: Source Image **shop_merchant_scene** · Color **FFFFFF** 255 · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **OFF**
- **Add Component → Aspect Ratio Fitter**: Aspect Mode **Envelope Parent** · Aspect Ratio **1.8095** (304 ÷ 168). The picture always covers the whole panel and never gets squashed.
- Optional, the animated merchant: **Add Component → Sprite Flipbook** and fill its frames the same way as the Joker's idle.

#### 18. Nameplate

- **Create:** right-click **MerchantPanel** → **UI → Image**, rename it **Nameplate**
- **Rect Transform**: Anchors Min **0.5, 1**, Max **0.5, 1** · Pivot **0.5, 1** · **Pos X 0 · Pos Y 0** · **Width 294 · Height 72**
- **Image**: Source Image **Armory_nameplate** · Color **FFFFFF** 255 · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **OFF**

#### 19. Title

- **Create:** right-click **Nameplate** → **UI → Text - TextMeshPro**, rename it **Title**
- **Rect Transform**: **stretch / stretch** · **Left 0 · Top 0 · Right 0 · Bottom 4**
- **TextMeshPro**: Text Input **Shop** · Font Asset **alagard** · Font Size **36** · Color **FFD27A** · Alignment **Center** and **Middle** · Wrapping **Disabled** · Raycast Target **OFF**

#### 20. Floor

- **Create:** right-click **MerchantPanel** → **UI → Text - TextMeshPro**, rename it **Floor**
- **Rect Transform**: Anchors Min **0.5, 1**, Max **0.5, 1** · Pivot **0.5, 1** · **Pos X 0 · Pos Y -76** · **Width 400 · Height 36**
- **TextMeshPro**: Text Input **Floor 2/6** · Font Asset **Romulus** · Font Size **24** · Color **DBDBE6** · Alignment **Center** and **Middle** · Raycast Target **OFF**

#### 21. GoldChip

- **Create:** right-click **MerchantPanel** → **UI → Image**, rename it **GoldChip**
- **Rect Transform**: Anchors Min **0, 1**, Max **0, 1** · Pivot **0, 1** · **Pos X 12 · Pos Y -12** · **Width 190 · Height 56**
- **Image**: Source Image **shop_price_tag** · Color **FFFFFF** 255 · Raycast Target **OFF** · Image Type **Sliced** · Fill Center **ON** · Pixels Per Unit Multiplier **0.304** (17 ÷ 56)
- **Add Component → Horizontal Layout Group**: Padding **Left 12 · Right 12 · Top 6 · Bottom 6** · Spacing **8** · Child Alignment **Middle Center** · Control Child Size Width **ON** Height **ON** · Child Force Expand Width **OFF** Height **OFF**
- Inside it, in this order:
  - **Coin**: right-click GoldChip → **UI → Image** · Source Image **shop_coin** · Raycast **OFF** · Preserve Aspect **ON** · **Add Component → Layout Element**: Preferred Width **32** · Preferred Height **32**
  - **Gold**: right-click GoldChip → **UI → Text - TextMeshPro** · Text Input **85** · Font **Romulus** · Size **30** · Color **3A2A1A** · Alignment **Center / Middle** · Wrapping **Disabled** · Raycast **OFF**

#### 22. HealthChip

- **Create:** right-click **MerchantPanel** → **UI → Image**, rename it **HealthChip**
- **Rect Transform**: Anchors Min **1, 1**, Max **1, 1** · Pivot **1, 1** · **Pos X -12 · Pos Y -12** · **Width 230 · Height 56**
- **Image**: the same as GoldChip (shop_price_tag · Sliced · Multiplier **0.304** · Raycast **OFF**)
- **Horizontal Layout Group**: the same values as GoldChip
- Inside it, in this order:
  - **Heart**: UI → Image · Source Image **shop_heart** · Raycast **OFF** · Preserve Aspect **ON** · **Layout Element** Preferred Width **32** · Preferred Height **32**
  - **Health**: UI → Text - TextMeshPro · Text Input **72/125** · Font **Romulus** · Size **30** · Color **3A2A1A** · Alignment **Center / Middle** · Wrapping **Disabled** · Raycast **OFF**

#### 23. Bubble

- **Create:** right-click **MerchantPanel** → **UI → Image**, rename it **Bubble**
- **Rect Transform**: Anchors Min **X 0.50 · Y 0.10**, Max **X 0.97 · Y 0.48** · **Left 0 · Top 0 · Right 0 · Bottom 0** · Pivot **X 0 · Y 0** (it hops from the tail's corner)
- **Image**: Source Image **shop_speech_bubble** · Color **FFFFFF** 255 · Raycast Target **OFF** · Image Type **Sliced** · Fill Center **ON** · Pixels Per Unit Multiplier **0.29** (the same as the cabinet)

#### 24. Line

- **Create:** right-click **Bubble** → **UI → Text - TextMeshPro**, rename it **Line**
- **Rect Transform**: **stretch / stretch** · **Left 26 · Top 20 · Right 26 · Bottom 46** (the bigger Bottom keeps the text above the tail)
- **TextMeshPro**: Text Input **Welcome, traveller.** · Font Asset **Romulus** · Font Size **26** · Color **3A2A1A** · Alignment **Center** and **Middle** · Wrapping **Enabled** · **Auto Size ON** (Min **18**, Max **26**) · Raycast Target **OFF**

### Shelves

#### 25. Cabinet (behind the shelves and the detail)

- **Create:** right-click **SafeArea** → **UI → Image**, rename it **Cabinet**
- **Rect Transform**: Anchors Min **X 0 · Y 0.11**, Max **X 1 · Y 0.70** · **Left 0 · Top 0 · Right 0 · Bottom 0**
- **Image**: Source Image **Armory_cabinet_panel** · Color **FFFFFF** 255 · Raycast Target **OFF** · Image Type **Tiled** · Fill Center **ON** · Pixels Per Unit Multiplier **0.29** (the same as the Armory)

#### 26. Shelves

- **Create:** right-click **SafeArea** → **Create Empty**, rename it **Shelves**
- **Rect Transform**: Anchors Min **X 0 · Y 0.25**, Max **X 1 · Y 0.70** · **Left 0 · Top 0 · Right 0 · Bottom 0**
- **Add Component → Vertical Layout Group**
  - Padding **Left 50 · Right 50 · Top 52 · Bottom 26** · Spacing **4**
  - Child Alignment **Upper Center**
  - Control Child Size → Width **ON** · Height **ON**
  - Use Child Scale → Width **OFF** · Height **OFF**
  - Child Force Expand → Width **ON** · Height **OFF**

Now 6 children, in this order (27 to 32).

#### 27. UpgradesTitle

- **Create:** right-click **Shelves** → **UI → Text - TextMeshPro**, rename it **UpgradesTitle**
- **TextMeshPro**: Text Input **Upgrades** · Font **alagard** · Size **28** · Color **F2CC73** · Alignment **Left / Middle** · Raycast **OFF**
- **Add Component → Layout Element**: Preferred Height **38**

#### 28. UpgradeRow

- **Create:** right-click **Shelves** → **Create Empty**, rename it **UpgradeRow**
- **Add Component → Layout Element**: Preferred Height **190**
- **Add Component → Horizontal Layout Group**
  - Padding **Left 0 · Right 0 · Top 0 · Bottom 4** · Spacing **40**
  - Child Alignment **Lower Center** (the offers stand on the plank)
  - Control Child Size → Width **OFF** · Height **OFF**
  - Use Child Scale → Width **OFF** · Height **OFF**
  - Child Force Expand → Width **OFF** · Height **OFF**
- **Plank** (the first and only child you add; the offers are created after it):
  - right-click **UpgradeRow** → **UI → Image**, rename it **Plank**
  - **Add Component → Layout Element** → Ignore Layout **ON**
  - Rect Transform: Anchors Min **0, 0**, Max **1, 0** · Pivot **0.5, 0** · **Left -10 · Pos Y 0 · Right -10 · Height 44**
  - Image: Source Image **shop_shelf_plank** · Color **FFFFFF** 255 · Raycast **OFF** · Image Type **Sliced** · Fill Center **ON** · Pixels Per Unit Multiplier **0.273** (12 ÷ 44). The price tags hang on the front of the plank.

#### 29. RunesTitle

- The same as 27, named **RunesTitle**, Text Input **Runes**

#### 30. RuneRow

- The same as 28 (with its own **Plank**), named **RuneRow**

#### 31. ServicesTitle

- The same as 27, named **ServicesTitle**, Text Input **Services**

#### 32. ServiceRow

- The same as 28 (with its own **Plank**), named **ServiceRow**

#### 33. ShelfRail (between the shelves and the detail)

- **Create:** right-click **SafeArea** → **UI → Image**, rename it **ShelfRail**. Drag it **below Shelves** in the list.
- **Rect Transform**: Anchors Min **X 0 · Y 0.25**, Max **X 1 · Y 0.25** · Pivot **0.5, 0.5** · **Left 0 · Pos Y 0 · Right 0 · Height 44.83**
- **Image**: Source Image **Armory_cabinet_rail** · Raycast **OFF** · Image Type **Tiled** · Fill Center **ON** · Pixels Per Unit Multiplier **0.29**

### Detail

#### 34. DetailPanel

- **Create:** right-click **SafeArea** → **Create Empty**, rename it **DetailPanel**
- **Rect Transform**: Anchors Min **X 0 · Y 0.11**, Max **X 1 · Y 0.25** · **Left 0 · Top 0 · Right 0 · Bottom 0**

#### 35. IconFrame

- **Create:** right-click **DetailPanel** → **UI → Image**, rename it **IconFrame**
- **Rect Transform**: Anchors Min **0, 0.5**, Max **0, 0.5** · Pivot **0, 0.5** · **Pos X 60 · Pos Y 0** · **Width 150 · Height 150**
- **Image**: Source Image **Armory-square-item-slot** · Raycast **OFF** · Preserve Aspect **ON**
- Inside it:
  - **Icon**: UI → Image · Anchors **0.5, 0.5** (Min and Max) · Pivot **0.5, 0.5** · Pos **0, 0** · **96 × 96** · Source **None** · Raycast **OFF** · Preserve Aspect **ON**
  - **Portrait**: UI → Image · Anchors **0.5, 0.5** · Pivot **0.5, 0.5** · Pos **0, 0** · **110 × 110** · Source **None** · Raycast **OFF** · Preserve Aspect **ON**

#### 36. Name

- **Create:** right-click **DetailPanel** → **UI → Text - TextMeshPro**, rename it **Name**
- **Rect Transform**: Anchors Min **X 0.23 · Y 0.58**, Max **X 0.66 · Y 0.92** · **Left 0 · Top 0 · Right 0 · Bottom 0**
- **TextMeshPro**: Text Input **Name** · Font **alagard** · Size **32** · Color **F2CC73** · Alignment **Left / Middle** · Wrapping **Disabled** · **Auto Size ON** (Min **20**, Max **32**) · Raycast **OFF**

#### 37. Effect

- **Create:** right-click **DetailPanel** → **UI → Text - TextMeshPro**, rename it **Effect**
- **Rect Transform**: Anchors Min **X 0.23 · Y 0.08**, Max **X 0.66 · Y 0.58** · **Left 0 · Top 0 · Right 0 · Bottom 0**
- **TextMeshPro**: Text Input **Effect** · Font **Romulus** · Size **24** · Color **DBDBE6** · Alignment **Left / Top** · Wrapping **Enabled** · **Auto Size ON** (Min **16**, Max **24**) · Raycast **OFF**

#### 38. BuyButton

- **Create:** right-click **DetailPanel** → **UI → Button - TextMeshPro**, rename it **BuyButton**
- **Rect Transform**: Anchors Min **X 0.68 · Y 0.22**, Max **X 0.96 · Y 0.78** · **Left 0 · Top 0 · Right 0 · Bottom 0**
- **Image**: Source Image **tab** · Color **FFFFFF** 255 · Image Type **Sliced** · Fill Center **ON** · Pixels Per Unit Multiplier **1**
- **Button**: Transition **Color Tint** · Disabled Color **808080** alpha **200** (a greyed button when you can't buy)
- Select **BuyButton ▸ Text (TMP)**, rename it **Label**: Font **alagard** · Size **30** · Color **FFD27A** · Alignment **Center / Middle** · **Auto Size ON** (Min **18**, Max **30**) · Raycast **OFF**

### Bottom bar

#### 39. BottomBar

- **Create:** right-click **SafeArea** → **Create Empty**, rename it **BottomBar**
- **Rect Transform**: Anchors Min **X 0 · Y 0**, Max **X 1 · Y 0.11** · **Left 0 · Top 0 · Right 0 · Bottom 0**

#### 40. BagTitle

- **Create:** right-click **BottomBar** → **UI → Text - TextMeshPro**, rename it **BagTitle**
- **Rect Transform**: Anchors Min **0, 1**, Max **0, 1** · Pivot **0, 1** · **Pos X 8 · Pos Y -4** · **Width 360 · Height 34**
- **TextMeshPro**: Text Input **Your runes** · Font **Romulus** · Size **24** · Color **DBDBE6** · Alignment **Left / Middle** · Raycast **OFF**

#### 41. Bag

- **Create:** right-click **BottomBar** → **Create Empty**, rename it **Bag**
- **Rect Transform**: Anchors Min **0, 0**, Max **0, 0** · Pivot **0, 0** · **Pos X 8 · Pos Y 10** · **Width 340 · Height 100**
- **Add Component → Horizontal Layout Group**: Spacing **12** · Child Alignment **Middle Left** · Control Child Size Width **OFF** Height **OFF** · Force Expand Width **OFF** Height **OFF**
- Inside it, three sockets **Socket0**, **Socket1**, **Socket2** (do each):
  - right-click **Bag** → **UI → Image** · **Width 100 · Height 100** · Source Image **hero_equipment_column_2** (the round socket) · Raycast **OFF** · Preserve Aspect **ON**
  - inside each socket: **Icon** → UI → Image · Anchors **0.5, 0.5** · Pivot **0.5, 0.5** · Pos **0, 0** · **64 × 64** · Source **None** · Raycast **OFF** · Preserve Aspect **ON**

#### 42. LeaveButton

- **Create:** right-click **BottomBar** → **UI → Button - TextMeshPro**, rename it **LeaveButton**
- **Rect Transform**: Anchors Min **X 0.52 · Y 0.14**, Max **X 1 · Y 0.86** · **Left 0 · Top 0 · Right 0 · Bottom 0**
- **Image**: Source Image **tab** · Image Type **Sliced** · Fill Center **ON** · Pixels Per Unit Multiplier **1**
- **Label** (rename **Text (TMP)**): Font **alagard** · Size **32** · Color **FFD27A** · Alignment **Center / Middle** · **Auto Size ON** (Min **20**, Max **32**) · Raycast **OFF**

---

## Part 5 — Wire the Shop Screen component

#### 43. Shop Screen

- **Select:** the root **ShopScreen** → **Add Component → Shop Screen**
- **Grupos**
  - Screen → the root **ShopScreen** (its Canvas Group)
  - Court → leave **None** for now (set it in the scene, step 45)
- **Topo**: Title → **Nameplate ▸ Title** · Gold → **GoldChip ▸ Gold** · Health → **HealthChip ▸ Health** · Floor → **Floor**
- **Vendedor**: Line → **Bubble ▸ Line** · Bubble → **Bubble**
- **Prateleiras**: Upgrades Title → **UpgradesTitle** · Runes Title → **RunesTitle** · Services Title → **ServicesTitle** · Upgrade Row → **UpgradeRow** · Rune Row → **RuneRow** · Service Row → **ServiceRow** · Offer Prefab → the **ShopOffer** prefab from the **Project** window
- **Detalhe**: Detail Icon → **IconFrame ▸ Icon** · Detail Portrait → **IconFrame ▸ Portrait** · Detail Name → **Name** · Detail Effect → **Effect** · Action → **BuyButton** · Action Label → **BuyButton ▸ Label**
- **Bolsa de runas e saída**: Bag Title → **BagTitle** · Bag Icons → Size **3**: **Socket0 ▸ Icon**, **Socket1 ▸ Icon**, **Socket2 ▸ Icon** · Leave → **LeaveButton** · Leave Label → **LeaveButton ▸ Label**
- **Ícones** → Size **9**:

  | Element | Id | Sprite |
  |---|---|---|
  | 0 | folego | rune_folego |
  | 1 | estrela | rune_estrela |
  | 2 | fluxo | rune_fluxo |
  | 3 | escudo | rune_escudo |
  | 4 | vida | rune_vida |
  | 5 | presa | fang |
  | 6 | brasa | ember |
  | 7 | potion | shop_potion |
  | 8 | reroll | shop_restock |

- Icon Canvas → **48**
- **Cores** (keep the defaults): Focus Tint **FF9E61** · Price Color **3A2A1A** (dark ink on the parchment tag) · Poor Color **B0302A** (dark red) · Highlight Numbers **ON** · Number Color **F2C759**
- **Animação**: Fade In Seconds **0.2** · Gold Seconds **0.35**
- **Cmd+S** and leave the prefab.

#### 44. Put it in the scene

1. Open the **Main** scene.
2. Drag the **ShopScreen** prefab from the Project window into the Hierarchy, as a sibling of **EquipScreen** (directly under Main).

#### 45. Court (on the scene copy)

- **Select:** **Main ▸ ShopScreen** → **Shop Screen → Court** → drag **Main ▸ GodsScreen** (the ability upgrades show the hero's face from the Court).

#### 46. Game Bootstrap

- **Select:** **Main ▸ Game** → **Game Bootstrap → Shop** → drag **Main ▸ ShopScreen**.
- **Cmd+S** (save the scene).

If **Shop** is left empty, the game keeps using the old list screen for the shop.

---

## Part 6 — Test

Press **Play**, start a run and win the first battle (after the blessing and any event):

- The shop scene fills the top with the merchant, the **Shop** plate, your gold on the left, health on the right, the floor under the plate, and a greeting in the bubble.
- Upgrades (2), Runes (3) and Services (Potion, Restock) stand on their planks; exactly **one** of the upgrades or runes shows the **sale badge** and the old price crossed out next to the new one.
- The first offer is already in the detail panel. Tap another: it lights up (orange tint) and the detail changes; ability upgrades show the hero's face and the "→ ++" change.
- Prices you can't pay are **red**; tapping one makes the merchant say "Come back with more coin." and the button reads **Not enough gold**.
- **Buy**: the gold counts down, the offer bounces and gets the **sold seal**, the merchant thanks you; a rune appears in **Your runes**.
- **Restock**: new runes and upgrades, a new sale, the price goes up by 5 each time; a bought potion stays bought.
- **Next floor** leaves the shop.
