# Armory screen — setup guide (by hand)

Follow it top to bottom. Every element has its own block with every value to type, even when it repeats.

**Reading the anchors:** in Unity **Y 0 = bottom** of the parent and **Y 1 = top**. X 0 = left, X 1 = right.

**Where the anchor fields are:** select the object → Inspector → **Rect Transform** → click the small arrow next to **Anchors** to open Min and Max. After typing the anchors, the top of the Rect Transform shows **Left, Top, Pos Z, Right, Bottom**.

**Colors:** click the color box and type the hex code in the **Hexadecimal** field (without #).


---

## Part 1 — Import the PixelLab image

1. Project window → `Assets/Game/View/Art` → right-click → **Create → Folder** → name it **Armory**.
2. Drag the PixelLab PNG into the **Armory** folder and rename it **armory_ui**.
3. Click **armory_ui** and set in the Inspector:
   - Texture Type **Sprite (2D and UI)**
   - Sprite Mode **Multiple**
   - Pixels Per Unit **100**
   - Mesh Type **Full Rect**
   - Generate Physics Shape **off**
   - Filter Mode **Point (no filter)**
   - Compression **None**
   - Max Size **2048**
4. Click **Apply**.

## Part 2 — Cut the pieces (Sprite Editor)

1. With **armory_ui** selected, click **Open Sprite Editor**.
2. For each piece: drag a box around it on the image, then type the name in the **Name** field of the small panel at the bottom right.
   - **frame_selected** → the glowing LEFT portrait frame (include the glow)
   - **frame** → the MIDDLE portrait frame
   - **socket** → one round socket (only the circle)
   - **socket_bar** → the short gold bar between a frame and its socket
   - **divider** → the whole gold bar with the crest
   - **medallion** → the big circle of the middle panel
   - **text_bar** → one of the two dark bars
   - **button** → the parchment button
   - **slot** → the 2nd slot of the top row (a plain one)
   - **owner_badge** → the small round badge on the 3rd slot
3. Click **Apply** (top right of the Sprite Editor) and close it.

Not needed from the image: the slate panels (we use the Court's `background_top.png`), a selected slot (the game tints the normal one).

---

## Part 3 — Build the objects

Open the **Main** scene first.


#### 1. EquipScreen (the canvas)

- **Parent:** none (scene root)
- **Create:** right-click an empty spot of the Hierarchy → **UI → Canvas**, rename it **EquipScreen**
- If Unity also created a new **EventSystem** at the root, delete that new EventSystem (the scene already has one).
- **Rect Transform:** nothing to set (the Canvas controls it)
- **Canvas:** Render Mode **Screen Space - Overlay** · Pixel Perfect **off** · **Sort Order 11** · Target Display **Display 1** · Additional Shader Channels **same as GodsScreen's Canvas**
- **Canvas Scaler:** UI Scale Mode **Scale With Screen Size** · Reference Resolution **X 1080** · **Y 1920** · Screen Match Mode **Match Width Or Height** · Match **0** · Reference Pixels Per Unit **100**
- **Graphic Raycaster:** leave the defaults (Ignore Reversed Graphics **ON**, Blocking Objects **None**)
- **Add Component → Canvas Group:** Alpha **1** · Interactable **ON** · Blocks Raycasts **ON** · Ignore Parent Groups **off**
- **Add Component → Equip Screen:** leave the fields empty for now (Part 4)

#### 2. Back

- **Parent:** EquipScreen
- **Create:** right-click **EquipScreen** → **UI → Image**, rename it **Back**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** copy it from the Court: select **GodsScreen ▸ Back**, click the **⋮** on its **Image** component → **Copy Component**; select your **Back**, click the **⋮** on its **Image** → **Paste Component Values**
- Then make sure Raycast Target is **ON** (it stops taps from going to the cube behind)

#### 3. SafeArea

- **Parent:** EquipScreen
- **Create:** right-click **EquipScreen** → **Create Empty**, rename it **SafeArea**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Add Component → Safe Area:** Min Bottom **0**

#### 4. TeamPanel

- **Parent:** EquipScreen ▸ SafeArea
- **Create:** right-click **SafeArea** → **UI → Image**, rename it **TeamPanel**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0.71875** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 5. Divider

- **Parent:** EquipScreen ▸ SafeArea
- **Create:** right-click **SafeArea** → **UI → Image**, rename it **Divider**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0.625** · **Max X 1** · **Max Y 0.71875**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 6. DetailPanel

- **Parent:** EquipScreen ▸ SafeArea
- **Create:** right-click **SafeArea** → **UI → Image**, rename it **DetailPanel**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0.375** · **Max X 1** · **Max Y 0.625**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 7. ArmoryPanel

- **Parent:** EquipScreen ▸ SafeArea
- **Create:** right-click **SafeArea** → **UI → Image**, rename it **ArmoryPanel**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0.09375** · **Max X 1** · **Max Y 0.375**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

### Hero1


#### 8. Hero1

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel
- **Create:** right-click **TeamPanel** → **Create Empty**, rename it **Hero1**
- **Rect Transform:**
  - Anchors → **Min X 0.0556** · **Min Y 0.1111** · **Max X 0.2778** · **Max Y 0.8889**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Add Component → Equip Hero Slot:** fields are filled at the end of this hero
- **Add Component → Aspect Ratio Fitter:** Aspect Mode **Width Controls Height** · Aspect Ratio **0.5714**
  - This keeps the column at its designed shape (240 × 420) on every screen height, so the portrait, bar and socket always touch. Height and Top/Bottom turn grey ("driven by AspectRatioFitter"). That is expected.

#### 9. PortraitCell

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero1
- **Create:** right-click **Hero1** → **Create Empty**, rename it **PortraitCell**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0.4286** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**

#### 10. Slot

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero1 ▸ PortraitCell
- **Create:** right-click **PortraitCell** → **Create Empty**, rename it **Slot**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Add Component → Aspect Ratio Fitter:** Aspect Mode **Fit In Parent** · Aspect Ratio **1**
  - After this the Rect Transform fields turn grey ("Some values driven by AspectRatioFitter"). That is expected.

#### 11. Frame

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero1 ▸ PortraitCell ▸ Slot
- **Create:** right-click **Slot** → **UI → Image**, rename it **Frame**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **ON** · Image Type **Simple** · Preserve Aspect **off**

#### 12. Art

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero1 ▸ PortraitCell ▸ Slot
- **Create:** right-click **Slot** → **UI → Image**, rename it **Art**
- **Rect Transform:**
  - Anchors → **Min X 0.12** · **Min Y 0.12** · **Max X 0.88** · **Max Y 0.88**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the hero portrait is set by the game) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 13. Name

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero1 ▸ PortraitCell ▸ Slot
- **Create:** right-click **Slot** → **UI → Text - TextMeshPro**, rename it **Name**
- **Rect Transform:**
  - Anchors → **Min X 0.08** · **Min Y 0.08** · **Max X 0.92** · **Max Y 0.92**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **TextMeshPro - Text (UI):**
  - Text: delete "New Text" (leave it empty; the game fills it). For a preview you can type *Bishop* and delete it later
  - Font Asset: **the same font as the Court texts** (click any text in GodsScreen to see its Font Asset)
  - Font Style **none selected** · **Auto Size ON** → Min **12** · Max **30**
  - Vertex Color **DBDBE6** (alpha 255)
  - Alignment: horizontal **Center** + vertical **Middle**
  - Wrapping **Enabled** · Overflow **Overflow**
  - Extra Settings → Raycast Target **OFF**

#### 14. Bar

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero1
- **Create:** right-click **Hero1** → **UI → Image**, rename it **Bar**
- **Rect Transform:**
  - Anchors → **Min X 0.25** · **Min Y 0.2857** · **Max X 0.75** · **Max Y 0.4286**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 15. SocketCell

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero1
- **Create:** right-click **Hero1** → **Create Empty**, rename it **SocketCell**
- **Rect Transform:**
  - Anchors → **Min X 0.25** · **Min Y 0** · **Max X 0.75** · **Max Y 0.2857**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**

#### 16. Socket

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero1 ▸ SocketCell
- **Create:** right-click **SocketCell** → **UI → Image**, rename it **Socket**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **ON** · Image Type **Simple** · Preserve Aspect **off**
- **Add Component → Aspect Ratio Fitter:** Aspect Mode **Fit In Parent** · Aspect Ratio **1**
  - After this the Rect Transform fields turn grey ("Some values driven by AspectRatioFitter"). That is expected.

#### 17. Icon

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero1 ▸ SocketCell ▸ Socket
- **Create:** right-click **Socket** → **UI → Image**, rename it **Icon**
- **Rect Transform:**
  - Anchors → **Min X 0.24** · **Min Y 0.24** · **Max X 0.76** · **Max Y 0.76**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the item icon is set by the game) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 18. Wire Hero1's Equip Hero Slot

Select **Hero1** and drag into the **Equip Hero Slot** fields:
- **Frame** ← Hero1 ▸ PortraitCell ▸ Slot ▸ **Frame**
- **Art** ← Hero1 ▸ PortraitCell ▸ Slot ▸ **Art**
- **Label** ← Hero1 ▸ PortraitCell ▸ Slot ▸ **Name**
- **Socket** ← Hero1 ▸ SocketCell ▸ **Socket**
- **Socket Icon** ← Hero1 ▸ SocketCell ▸ Socket ▸ **Icon**

### Hero2

> **Shortcut:** select **Hero1**, press **Cmd+D**, rename the copy **Hero2**, and set only block 19 (Hero2's own Rect Transform). Everything inside is copied with the same values and its Equip Hero Slot already points to Hero2's own children. Use the blocks below to check.


#### 19. Hero2

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel
- **Create:** right-click **TeamPanel** → **Create Empty**, rename it **Hero2**
- **Rect Transform:**
  - Anchors → **Min X 0.3889** · **Min Y 0.1111** · **Max X 0.6111** · **Max Y 0.8889**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Add Component → Equip Hero Slot:** fields are filled at the end of this hero
- **Add Component → Aspect Ratio Fitter:** Aspect Mode **Width Controls Height** · Aspect Ratio **0.5714**
  - This keeps the column at its designed shape (240 × 420) on every screen height, so the portrait, bar and socket always touch. Height and Top/Bottom turn grey ("driven by AspectRatioFitter"). That is expected.

#### 20. PortraitCell

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero2
- **Create:** right-click **Hero2** → **Create Empty**, rename it **PortraitCell**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0.4286** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**

#### 21. Slot

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero2 ▸ PortraitCell
- **Create:** right-click **PortraitCell** → **Create Empty**, rename it **Slot**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Add Component → Aspect Ratio Fitter:** Aspect Mode **Fit In Parent** · Aspect Ratio **1**
  - After this the Rect Transform fields turn grey ("Some values driven by AspectRatioFitter"). That is expected.

#### 22. Frame

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero2 ▸ PortraitCell ▸ Slot
- **Create:** right-click **Slot** → **UI → Image**, rename it **Frame**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **ON** · Image Type **Simple** · Preserve Aspect **off**

#### 23. Art

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero2 ▸ PortraitCell ▸ Slot
- **Create:** right-click **Slot** → **UI → Image**, rename it **Art**
- **Rect Transform:**
  - Anchors → **Min X 0.12** · **Min Y 0.12** · **Max X 0.88** · **Max Y 0.88**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the hero portrait is set by the game) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 24. Name

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero2 ▸ PortraitCell ▸ Slot
- **Create:** right-click **Slot** → **UI → Text - TextMeshPro**, rename it **Name**
- **Rect Transform:**
  - Anchors → **Min X 0.08** · **Min Y 0.08** · **Max X 0.92** · **Max Y 0.92**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **TextMeshPro - Text (UI):**
  - Text: delete "New Text" (leave it empty; the game fills it). For a preview you can type *Bishop* and delete it later
  - Font Asset: **the same font as the Court texts** (click any text in GodsScreen to see its Font Asset)
  - Font Style **none selected** · **Auto Size ON** → Min **12** · Max **30**
  - Vertex Color **DBDBE6** (alpha 255)
  - Alignment: horizontal **Center** + vertical **Middle**
  - Wrapping **Enabled** · Overflow **Overflow**
  - Extra Settings → Raycast Target **OFF**

#### 25. Bar

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero2
- **Create:** right-click **Hero2** → **UI → Image**, rename it **Bar**
- **Rect Transform:**
  - Anchors → **Min X 0.25** · **Min Y 0.2857** · **Max X 0.75** · **Max Y 0.4286**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 26. SocketCell

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero2
- **Create:** right-click **Hero2** → **Create Empty**, rename it **SocketCell**
- **Rect Transform:**
  - Anchors → **Min X 0.25** · **Min Y 0** · **Max X 0.75** · **Max Y 0.2857**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**

#### 27. Socket

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero2 ▸ SocketCell
- **Create:** right-click **SocketCell** → **UI → Image**, rename it **Socket**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **ON** · Image Type **Simple** · Preserve Aspect **off**
- **Add Component → Aspect Ratio Fitter:** Aspect Mode **Fit In Parent** · Aspect Ratio **1**
  - After this the Rect Transform fields turn grey ("Some values driven by AspectRatioFitter"). That is expected.

#### 28. Icon

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero2 ▸ SocketCell ▸ Socket
- **Create:** right-click **Socket** → **UI → Image**, rename it **Icon**
- **Rect Transform:**
  - Anchors → **Min X 0.24** · **Min Y 0.24** · **Max X 0.76** · **Max Y 0.76**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the item icon is set by the game) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 29. Wire Hero2's Equip Hero Slot

Select **Hero2** and drag into the **Equip Hero Slot** fields:
- **Frame** ← Hero2 ▸ PortraitCell ▸ Slot ▸ **Frame**
- **Art** ← Hero2 ▸ PortraitCell ▸ Slot ▸ **Art**
- **Label** ← Hero2 ▸ PortraitCell ▸ Slot ▸ **Name**
- **Socket** ← Hero2 ▸ SocketCell ▸ **Socket**
- **Socket Icon** ← Hero2 ▸ SocketCell ▸ Socket ▸ **Icon**

### Hero3

> **Shortcut:** select **Hero1**, press **Cmd+D**, rename the copy **Hero3**, and set only block 30 (Hero3's own Rect Transform). Everything inside is copied with the same values and its Equip Hero Slot already points to Hero3's own children. Use the blocks below to check.


#### 30. Hero3

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel
- **Create:** right-click **TeamPanel** → **Create Empty**, rename it **Hero3**
- **Rect Transform:**
  - Anchors → **Min X 0.7222** · **Min Y 0.1111** · **Max X 0.9444** · **Max Y 0.8889**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Add Component → Equip Hero Slot:** fields are filled at the end of this hero
- **Add Component → Aspect Ratio Fitter:** Aspect Mode **Width Controls Height** · Aspect Ratio **0.5714**
  - This keeps the column at its designed shape (240 × 420) on every screen height, so the portrait, bar and socket always touch. Height and Top/Bottom turn grey ("driven by AspectRatioFitter"). That is expected.

#### 31. PortraitCell

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero3
- **Create:** right-click **Hero3** → **Create Empty**, rename it **PortraitCell**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0.4286** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**

#### 32. Slot

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero3 ▸ PortraitCell
- **Create:** right-click **PortraitCell** → **Create Empty**, rename it **Slot**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Add Component → Aspect Ratio Fitter:** Aspect Mode **Fit In Parent** · Aspect Ratio **1**
  - After this the Rect Transform fields turn grey ("Some values driven by AspectRatioFitter"). That is expected.

#### 33. Frame

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero3 ▸ PortraitCell ▸ Slot
- **Create:** right-click **Slot** → **UI → Image**, rename it **Frame**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **ON** · Image Type **Simple** · Preserve Aspect **off**

#### 34. Art

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero3 ▸ PortraitCell ▸ Slot
- **Create:** right-click **Slot** → **UI → Image**, rename it **Art**
- **Rect Transform:**
  - Anchors → **Min X 0.12** · **Min Y 0.12** · **Max X 0.88** · **Max Y 0.88**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the hero portrait is set by the game) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 35. Name

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero3 ▸ PortraitCell ▸ Slot
- **Create:** right-click **Slot** → **UI → Text - TextMeshPro**, rename it **Name**
- **Rect Transform:**
  - Anchors → **Min X 0.08** · **Min Y 0.08** · **Max X 0.92** · **Max Y 0.92**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **TextMeshPro - Text (UI):**
  - Text: delete "New Text" (leave it empty; the game fills it). For a preview you can type *Bishop* and delete it later
  - Font Asset: **the same font as the Court texts** (click any text in GodsScreen to see its Font Asset)
  - Font Style **none selected** · **Auto Size ON** → Min **12** · Max **30**
  - Vertex Color **DBDBE6** (alpha 255)
  - Alignment: horizontal **Center** + vertical **Middle**
  - Wrapping **Enabled** · Overflow **Overflow**
  - Extra Settings → Raycast Target **OFF**

#### 36. Bar

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero3
- **Create:** right-click **Hero3** → **UI → Image**, rename it **Bar**
- **Rect Transform:**
  - Anchors → **Min X 0.25** · **Min Y 0.2857** · **Max X 0.75** · **Max Y 0.4286**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 37. SocketCell

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero3
- **Create:** right-click **Hero3** → **Create Empty**, rename it **SocketCell**
- **Rect Transform:**
  - Anchors → **Min X 0.25** · **Min Y 0** · **Max X 0.75** · **Max Y 0.2857**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**

#### 38. Socket

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero3 ▸ SocketCell
- **Create:** right-click **SocketCell** → **UI → Image**, rename it **Socket**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **ON** · Image Type **Simple** · Preserve Aspect **off**
- **Add Component → Aspect Ratio Fitter:** Aspect Mode **Fit In Parent** · Aspect Ratio **1**
  - After this the Rect Transform fields turn grey ("Some values driven by AspectRatioFitter"). That is expected.

#### 39. Icon

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Hero3 ▸ SocketCell ▸ Socket
- **Create:** right-click **Socket** → **UI → Image**, rename it **Icon**
- **Rect Transform:**
  - Anchors → **Min X 0.24** · **Min Y 0.24** · **Max X 0.76** · **Max Y 0.76**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the item icon is set by the game) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 40. Wire Hero3's Equip Hero Slot

Select **Hero3** and drag into the **Equip Hero Slot** fields:
- **Frame** ← Hero3 ▸ PortraitCell ▸ Slot ▸ **Frame**
- **Art** ← Hero3 ▸ PortraitCell ▸ Slot ▸ **Art**
- **Label** ← Hero3 ▸ PortraitCell ▸ Slot ▸ **Name**
- **Socket** ← Hero3 ▸ SocketCell ▸ **Socket**
- **Socket Icon** ← Hero3 ▸ SocketCell ▸ Socket ▸ **Icon**

### Team nameplate (the "Team" plate on the top rail)

The plate image is `Assets/Game/View/Art/Armory/Armory_nameplate.png` (85 × 21 px). It is already imported correctly (Sprite, Point, no compression), so there is nothing to change in its import settings. Do these two blocks inside the **EquipScreen prefab** (double-click it in the Project window).

#### 40a. Nameplate

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel
- **Create:** right-click **TeamPanel** → **UI → Image**, rename it **Nameplate**
- **Order:** drag it to the **bottom of TeamPanel's children** (below Hero3), so it is drawn on top of everything in the panel
- **Rect Transform:**
  - Anchors → **Min X 0.5** · **Min Y 1** · **Max X 0.5** · **Max Y 1** (shortcut: Anchor Presets → **top-center**)
  - Pivot → **X 0.5** · **Y 1**
  - **Pos X 0** · **Pos Y 0** · **Pos Z 0** · **Width 294** · **Height 72**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **Armory_nameplate** · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **on** (don't click Set Native Size: it would shrink the plate to 85 × 21)
  - 294 × 72 is the plate at about 3.45× its pixel size, the same pixel size as the cabinet and the divider. It covers the top rail in the middle and reaches about 27 units below it, which stays clear of Hero2's portrait.

#### 40b. Title

- **Parent:** EquipScreen ▸ SafeArea ▸ TeamPanel ▸ Nameplate
- **Create:** right-click **Nameplate** → **UI → Text - TextMeshPro**, rename it **Title**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1** (shortcut: Anchor Presets → Alt + **stretch-stretch**)
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 24** · **Top 10** · **Pos Z 0** · **Right 24** · **Bottom 10**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **TextMeshPro - Text (UI):**
  - Text Input: **Team**
  - Font Asset: **the same font as the Court texts** (click any text in GodsScreen to see its Font Asset)
  - Font Style: none
  - Font Size **34** · Auto Size **on** · Min **18** · Max **36**
  - Vertex Color **FFD27A** (alpha 255) — the warm gold of the item title
  - Alignment: **Center** (horizontal) and **Middle** (vertical)
  - Wrapping **Disabled** · Overflow **Overflow**
  - Extra Settings → Raycast Target **OFF**
- **Check:** the word sits in the middle of the dark steel face of the plate, not on the wooden ends. If it touches the ends, raise Left and Right (for example 30). If it looks too low or too high, change Top and Bottom by the same amount in opposite directions (for example Top 8, Bottom 12 moves it up).
- **Wire it:** select **EquipScreen** (the root) and drag **TeamPanel ▸ Nameplate ▸ Title** into the **Team Title** field of the **Equip Screen** component (at the top, under Grupos).
  - In Play the script writes the word in the game's language: **Time** (Portuguese), **Team** (English), **Equipo** (Spanish). The "Team" you typed is only what you see while editing.
  - The field is optional: if it is empty, the plate keeps whatever you typed and nothing breaks.

### Detail panel

#### 40c. Armory nameplate

The same plate image as the Team plate, now at the top of the cabinet. It goes inside **DetailPanel** (not inside Cabinet), because DetailPanel is drawn after Cabinet, so the plate stays on top of the wood.

- **Parent:** EquipScreen ▸ SafeArea ▸ DetailPanel
- **Create:** right-click **DetailPanel** → **UI → Image**, rename it **Nameplate**
- **Order:** drag it to the **bottom of DetailPanel's children**, so it is drawn on top of everything in the panel. If you build the detail panel from scratch, do this block after block 49 (the button's Label) so it stays last
- **Rect Transform:**
  - Anchors → **Min X 0.5** · **Min Y 1** · **Max X 0.5** · **Max Y 1** (shortcut: Anchor Presets → **top-center**)
  - Pivot → **X 0.5** · **Y 1**
  - **Pos X 0** · **Pos Y 0** · **Pos Z 0** · **Width 294** · **Height 72**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **Armory_nameplate** · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **on** (don't click Set Native Size: it would shrink the plate to 85 × 21)
- **Shortcut:** instead of building it, select **TeamPanel ▸ Nameplate**, press **Cmd+D**, drag the copy onto **DetailPanel** (to the bottom of its children) and check the values above. Then in 40d only change the Text Input.

#### 40d. Title

- **Parent:** EquipScreen ▸ SafeArea ▸ DetailPanel ▸ Nameplate
- **Create:** right-click **Nameplate** → **UI → Text - TextMeshPro**, rename it **Title**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1** (shortcut: Anchor Presets → Alt + **stretch-stretch**)
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 24** · **Top 10** · **Pos Z 0** · **Right 24** · **Bottom 10**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **TextMeshPro - Text (UI):**
  - Text Input: **Armory**
  - Font Asset: **the same font as the Court texts** (click any text in GodsScreen to see its Font Asset)
  - Font Style: none
  - Font Size **34** · Auto Size **on** · Min **18** · Max **36**
  - Vertex Color **FFD27A** (alpha 255)
  - Alignment: **Center** (horizontal) and **Middle** (vertical)
  - Wrapping **Disabled** · Overflow **Overflow**
  - Extra Settings → Raycast Target **OFF**
- **Wire it:** select **EquipScreen** (the root) and drag **DetailPanel ▸ Nameplate ▸ Title** into the **Title** field of the **Equip Screen** component (under Grupos). In Play it shows **Arsenal** (Portuguese), **Armory** (English), **Arsenal** (Spanish).
- **Make room under the plate:** the plate reaches 72 units down into the detail panel, and the item name bar starts about 60 units down, so they would overlap. Select **DetailPanel ▸ NameBar** and change its anchor **Max Y 0.875 → 0.84**. The medallion is on the left, outside the plate, so it needs no change.


#### 41. MedallionCell

- **Parent:** EquipScreen ▸ SafeArea ▸ DetailPanel
- **Create:** right-click **DetailPanel** → **Create Empty**, rename it **MedallionCell**
- **Rect Transform:**
  - Anchors → **Min X 0.0556** · **Min Y 0.25** · **Max X 0.3333** · **Max Y 0.875**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**

#### 42. Medallion

- **Parent:** EquipScreen ▸ SafeArea ▸ DetailPanel ▸ MedallionCell
- **Create:** right-click **MedallionCell** → **UI → Image**, rename it **Medallion**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**
- **Add Component → Aspect Ratio Fitter:** Aspect Mode **Fit In Parent** · Aspect Ratio **1**
  - After this the Rect Transform fields turn grey ("Some values driven by AspectRatioFitter"). That is expected.

#### 43. Icon

- **Parent:** EquipScreen ▸ SafeArea ▸ DetailPanel ▸ MedallionCell ▸ Medallion
- **Create:** right-click **Medallion** → **UI → Image**, rename it **Icon**
- **Rect Transform:**
  - Anchors → **Min X 0.22** · **Min Y 0.22** · **Max X 0.78** · **Max Y 0.78**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the item icon is set by the game) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 44. NameBar

- **Parent:** EquipScreen ▸ SafeArea ▸ DetailPanel
- **Create:** right-click **DetailPanel** → **UI → Image**, rename it **NameBar**
- **Rect Transform:**
  - Anchors → **Min X 0.3889** · **Min Y 0.625** · **Max X 0.9444** · **Max Y 0.84** (0.875 before the Armory plate; lowered so the plate doesn't cover it)
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 45. Name

- **Parent:** EquipScreen ▸ SafeArea ▸ DetailPanel ▸ NameBar
- **Create:** right-click **NameBar** → **UI → Text - TextMeshPro**, rename it **Name**
- **Rect Transform:**
  - Anchors → **Min X 0.05** · **Min Y 0.12** · **Max X 0.95** · **Max Y 0.88**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **TextMeshPro - Text (UI):**
  - Text: delete "New Text" (leave it empty; the game fills it). For a preview you can type *Iron Helm* and delete it later
  - Font Asset: **the same font as the Court texts** (click any text in GodsScreen to see its Font Asset)
  - Font Style **none selected** · **Auto Size ON** → Min **12** · Max **44**
  - Vertex Color **F2CC73** (alpha 255)
  - Alignment: horizontal **Left** + vertical **Bottom** (sits on the line just above the effect, so the two read as one block)
  - Wrapping **Enabled** · Overflow **Overflow**
  - Extra Settings → Raycast Target **OFF**

#### 46. EffectBar

- **Parent:** EquipScreen ▸ SafeArea ▸ DetailPanel
- **Create:** right-click **DetailPanel** → **UI → Image**, rename it **EffectBar**
- **Rect Transform:**
  - Anchors → **Min X 0.3889** · **Min Y 0.375** · **Max X 0.9444** · **Max Y 0.625**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 47. Effect

- **Parent:** EquipScreen ▸ SafeArea ▸ DetailPanel ▸ EffectBar
- **Create:** right-click **EffectBar** → **UI → Text - TextMeshPro**, rename it **Effect**
- **Rect Transform:**
  - Anchors → **Min X 0.05** · **Min Y 0.1** · **Max X 0.95** · **Max Y 0.9**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **TextMeshPro - Text (UI):**
  - Text: delete "New Text" (leave it empty; the game fills it). For a preview you can type *+15 max health.* and delete it later
  - Font Asset: **the same font as the Court texts** (click any text in GodsScreen to see its Font Asset)
  - Font Style **none selected** · **Auto Size ON** → Min **12** · Max **30**
  - Vertex Color **DBDBE6** (alpha 255)
  - Alignment: horizontal **Left** + vertical **Top** (starts right under the name; "Carried by …" adds lines downward)
  - Wrapping **Enabled** · Overflow **Overflow**
  - Extra Settings → Raycast Target **OFF**

#### 48. ActionButton

- **Parent:** EquipScreen ▸ SafeArea ▸ DetailPanel
- **Create:** right-click **DetailPanel** → **UI → Image**, rename it **ActionButton**
- **Rect Transform:**
  - Anchors → **Min X 0.6111** · **Min Y 0.125** · **Max X 0.9444** · **Max Y 0.375**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **ON** · Image Type **Simple** · Preserve Aspect **off** · **Pixels Per Unit Multiplier 0.29** (appears once the tab art is set and the type turns Sliced; makes the wood frame as chunky as the cabinet)
- **Add Component → Button:**
  - Interactable **ON** · Transition **Color Tint** · Target Graphic ← **ActionButton** (drag the object itself)
  - Normal Color **FFFFFF** · Highlighted Color **F5F5F5** · Pressed Color **C8C8C8** · Selected Color **F5F5F5** · Disabled Color **C8C8C8** with alpha **128**
  - Color Multiplier **1** · Fade Duration **0.1** · Navigation **Automatic**
  - On Click (): leave the list empty (the game connects it)

#### 49. Label

- **Parent:** EquipScreen ▸ SafeArea ▸ DetailPanel ▸ ActionButton
- **Create:** right-click **ActionButton** → **UI → Text - TextMeshPro**, rename it **Label**
- **Rect Transform:**
  - Anchors → **Min X 0.08** · **Min Y 0.15** · **Max X 0.92** · **Max Y 0.85**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **TextMeshPro - Text (UI):**
  - Text: delete "New Text" (leave it empty; the game fills it). For a preview you can type *Equip* and delete it later
  - Font Asset: **the same font as the Court texts** (click any text in GodsScreen to see its Font Asset)
  - Font Style **none selected** · **Auto Size ON** → Min **12** · Max **36**
  - Vertex Color **FFD27A** (alpha 255) — warm gold, the same as the titles, readable on the dark wood
  - If the letters still look thin, turn on **B** (Bold) in Font Style. Don't turn on Outline in the material at the bottom: that material is shared, so every text in the game would get the outline
  - Alignment: horizontal **Center** + vertical **Middle**
  - Wrapping **Enabled** · Overflow **Overflow**
  - Extra Settings → Raycast Target **OFF**

### Item grid (scrolls when there are more items than fit)


#### 50. Scroll

- **Parent:** EquipScreen ▸ SafeArea ▸ ArmoryPanel
- **Create:** right-click **ArmoryPanel** → **UI → Image**, rename it **Scroll**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** · Color **FFFFFF** with alpha **0** (invisible, but it catches the finger for scrolling) · Material **None** · Raycast Target **ON** · Image Type **Simple**
- **Add Component → Rect Mask 2D:** Padding **0, 0, 0, 0** · Softness **X 0 · Y 0** (hides slots that scroll out of the panel)
- **Add Component → Scroll Rect:**
  - Content: leave empty for now (block after next)
  - Horizontal **off** · Vertical **ON**
  - Movement Type **Elastic** · Elasticity **0.1**
  - Inertia **ON** · Deceleration Rate **0.135** · Scroll Sensitivity **1**
  - Viewport ← **Scroll** (drag the object itself)
  - Horizontal Scrollbar **None** · Vertical Scrollbar **None**

#### 51. Grid

- **Parent:** EquipScreen ▸ SafeArea ▸ ArmoryPanel ▸ Scroll
- **Create:** right-click **Scroll** → **Create Empty**, rename it **Grid**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 1** · **Max X 1** · **Max Y 1** (stretched sideways, glued to the top)
  - Pivot → **X 0.5** · **Y 1**
  - The fields now read Left, Pos Y, Pos Z, Right, Height: **Left 0** · **Pos Y 0** · **Pos Z 0** · **Right 0** · **Height 540**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Add Component → Grid Layout Group:**
  - Padding → **Left 90** · **Right 90** · **Top 60** · **Bottom 60**
  - Cell Size → **X 180** · **Y 180**
  - Spacing → **X 60** · **Y 60**
  - Start Corner **Upper Left** · Start Axis **Horizontal** · Child Alignment **Upper Left**
  - Constraint **Fixed Column Count** · Constraint Count **4**
- **Add Component → Content Size Fitter:** Horizontal Fit **Unconstrained** · Vertical Fit **Preferred Size** (the Height turns grey: it now grows with the number of rows)

#### 52. Connect the Scroll Rect

Select **Scroll** → **Scroll Rect** → **Content** ← drag **Grid**.

### The item slot (built once, then turned into a prefab)

You build one slot inside Grid so you can see it, turn it into a prefab, then delete it from the scene. In Play, the game creates one copy per item in `equipment.json`, in rows of 4.


#### 53. EquipItemSlot

- **Parent:** EquipScreen ▸ SafeArea ▸ ArmoryPanel ▸ Scroll ▸ Grid
- **Create:** right-click **Grid** → **Create Empty**, rename it **EquipItemSlot**
- **Rect Transform:** nothing to type: the Grid Layout Group controls its position and size (180 × 180). The fields are grey, that is expected.
  - Only check Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Add Component → Equip Item Slot:** fields are filled at the end of this section

#### 54. Frame

- **Parent:** EquipScreen ▸ SafeArea ▸ ArmoryPanel ▸ Scroll ▸ Grid ▸ EquipItemSlot
- **Create:** right-click **EquipItemSlot** → **UI → Image**, rename it **Frame**
- **Rect Transform:**
  - Anchors → **Min X 0** · **Min Y 0** · **Max X 1** · **Max Y 1**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **ON** · Image Type **Simple** · Preserve Aspect **off**

#### 55. Icon

- **Parent:** EquipScreen ▸ SafeArea ▸ ArmoryPanel ▸ Scroll ▸ Grid ▸ EquipItemSlot
- **Create:** right-click **EquipItemSlot** → **UI → Image**, rename it **Icon**
- **Rect Transform:**
  - Anchors → **Min X 0.18** · **Min Y 0.18** · **Max X 0.82** · **Max Y 0.82**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the item icon is set by the game) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 56. Name

- **Parent:** EquipScreen ▸ SafeArea ▸ ArmoryPanel ▸ Scroll ▸ Grid ▸ EquipItemSlot
- **Create:** right-click **EquipItemSlot** → **UI → Text - TextMeshPro**, rename it **Name**
- **Rect Transform:**
  - Anchors → **Min X 0.1** · **Min Y 0.1** · **Max X 0.9** · **Max Y 0.9**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **TextMeshPro - Text (UI):**
  - Text: delete "New Text" (leave it empty; the game fills it). For a preview you can type *Buckler* and delete it later
  - Font Asset: **the same font as the Court texts** (click any text in GodsScreen to see its Font Asset)
  - Font Style **none selected** · **Auto Size ON** → Min **12** · Max **24**
  - Vertex Color **DBDBE6** (alpha 255)
  - Alignment: horizontal **Center** + vertical **Middle**
  - Wrapping **Enabled** · Overflow **Overflow**
  - Extra Settings → Raycast Target **OFF**

#### 57. (removed)

There is no Check badge any more: an equipped item shows only the face of the hero who carries it (block 58). If you already built a **Check** object in the EquipItemSlot prefab, delete it.

#### 58. Owner

- **Parent:** EquipScreen ▸ SafeArea ▸ ArmoryPanel ▸ Scroll ▸ Grid ▸ EquipItemSlot
- **Create:** right-click **EquipItemSlot** → **UI → Image**, rename it **Owner**
- **Rect Transform:**
  - Anchors → **Min X 0.62** · **Min Y -0.05** · **Max X 1.05** · **Max Y 0.38**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the art is set later through EquipScreen) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 59. Face

- **Parent:** EquipScreen ▸ SafeArea ▸ ArmoryPanel ▸ Scroll ▸ Grid ▸ EquipItemSlot ▸ Owner
- **Create:** right-click **Owner** → **UI → Image**, rename it **Face**
- **Rect Transform:**
  - Anchors → **Min X 0.18** · **Min Y 0.18** · **Max X 0.82** · **Max Y 0.82**
  - Pivot → **X 0.5** · **Y 0.5**
  - **Left 0** · **Top 0** · **Pos Z 0** · **Right 0** · **Bottom 0**
  - Rotation **X 0 · Y 0 · Z 0** · Scale **X 1 · Y 1 · Z 1**
- **Image:** Source Image **None** (the hero face is set by the game) · Color **FFFFFF** (alpha 255) · Material **None** · Raycast Target **OFF** · Image Type **Simple** · Preserve Aspect **off**

#### 60. Wire the Equip Item Slot

Select **EquipItemSlot** and drag into the **Equip Item Slot** fields:
- **Frame** ← EquipItemSlot ▸ **Frame**
- **Icon** ← EquipItemSlot ▸ **Icon**
- **Label** ← EquipItemSlot ▸ **Name**
- **Owner** ← EquipItemSlot ▸ **Owner**
- **Owner Face** ← EquipItemSlot ▸ Owner ▸ **Face**

#### 61. Turn it into a prefab

1. In the Project window open `Assets/Game/View/Prefabs/UI`.
2. Drag **EquipItemSlot** from the Hierarchy into that folder. A prefab **EquipItemSlot** appears there (blue cube icon), and the one in the Hierarchy turns blue.
3. Delete **EquipItemSlot** from the Hierarchy (select it → **Delete**). **Grid must be empty**: the game fills it.
4. From now on, to change the slot (sizes, colors, text), double-click the **EquipItemSlot** prefab in the Project window to edit it.

---

## Part 4 — Fill the Equip Screen component

Select **EquipScreen** (the root). Fill every field of **Equip Screen** in this order. For lists: type the size, press Enter, then drag into each Element.

**Grupos**
- **Screen** ← EquipScreen (drag the root object; it uses its Canvas Group)
- **Court** ← **GodsScreen** (the Court screen object in the Hierarchy)
- **Team Title** ← TeamPanel ▸ Nameplate ▸ **Title** (optional: it shows "Time" / "Team" / "Equipo" in the game's language)
- **Title** ← DetailPanel ▸ Nameplate ▸ **Title** (optional: it shows "Arsenal" / "Armory" in the game's language)

**Time**
- **Heroes** size **3** → Element 0 ← **Hero1** · Element 1 ← **Hero2** · Element 2 ← **Hero3**

**Painel do meio**
- **Detail Icon** ← DetailPanel ▸ MedallionCell ▸ Medallion ▸ **Icon**
- **Detail Name** ← DetailPanel ▸ NameBar ▸ **Name**
- **Detail Effect** ← DetailPanel ▸ EffectBar ▸ **Effect**
- **Action** ← DetailPanel ▸ **ActionButton**
- **Action Label** ← DetailPanel ▸ ActionButton ▸ **Label**
- **Owner Color** **D99E59** (alpha 255)
- **Reveal Seconds** **0.18**

**Grade**
- **Scroll** ← ArmoryPanel ▸ **Scroll**
- **Grid** ← ArmoryPanel ▸ Scroll ▸ **Grid**
- **Slot Prefab** ← the **EquipItemSlot** prefab from the **Project window** (`Assets/Game/View/Prefabs/UI`), not from the Hierarchy

**Ícones dos equipamentos**
This list doesn't decide how many slots appear. The grid makes one slot per item in `equipment.json` by itself. This list only pairs an item's id with its picture, and an item missing from it shows its name instead of an icon. The order doesn't matter, and the size is however many icons you have.
- You can leave **Icons** at size **0** for now: every slot will show its item's name.
- When you get an icon, press **+** under Icons, type the **Item Id** exactly as below (lowercase), and drag the picture into **Sprite**.
- When you add a new item to `equipment.json` later, its slot already appears. Add an entry here only once it has an icon.
- The ids of today's items:
  - **lamina** — Keen Blade
  - **anel** — Mana Ring
  - **cetro** — Light Scepter
  - **elmo** — Iron Helm
  - **broquel** — Buckler
  - **trevo** — Four-Leaf Clover
  - **corrente** — Golden Chain
  - **bolsa** — Deep Purse

**Arte (os recortes da imagem do PixelLab)** — expand **armory_ui** in the Project window to see the pieces
- **Panel Art** ← **background_top** (Assets/Game/View/Art/background_top.png — the Court's slate panel)
- **Portrait Frame Art** ← **frame**
- **Portrait Frame Selected Art** ← **frame_selected**
- **Socket Art** ← **socket**
- **Socket Bar Art** ← **socket_bar**
- **Divider Art** ← **divider**
- **Medallion Art** ← **medallion**
- **Text Bar Art** ← **text_bar**
- **Button Art** ← **tab** (Assets/Game/View/Art/Tab — the dark wood of the tab buttons)
- **Slot Art** ← **slot**
- **Slot Selected Art** ← leave **None** (the normal slot gets tinted)
- **Owner Badge Art** ← **owner_badge**
- **Selected Tint** **FF9E61** (alpha 255)

**Onde a arte vai**
- **Panels** size **3** → Element 0 ← **TeamPanel** · Element 1 ← **DetailPanel** · Element 2 ← **ArmoryPanel**
- **Socket Bars** size **3** → Element 0 ← **Hero1 ▸ Bar** · Element 1 ← **Hero2 ▸ Bar** · Element 2 ← **Hero3 ▸ Bar**
- **Divider** ← **Divider**
- **Medallion** ← DetailPanel ▸ MedallionCell ▸ **Medallion**
- **Text Bars** size **2** → Element 0 ← **NameBar** · Element 1 ← **EffectBar**
- **Button Image** ← **ActionButton**

**Entrada da tela**
- **Fade In Seconds** **0.2**


---

## Part 5 — Make EquipScreen a prefab

Like GodsScreen, the whole screen lives in a prefab. Then you can edit it on its own, and it stays safe if the scene gets messy.

1. In the Project window open `Assets/Game/View/Prefabs/UI`.
2. Drag **EquipScreen** from the Hierarchy into that folder. A prefab **EquipScreen** appears there, and the EquipScreen in the Hierarchy turns **blue**.
3. Select the **EquipScreen** in the Hierarchy and check **Equip Screen → Court**: it must still point to **GodsScreen**. It is shown in **bold**, because a prefab can't store links to objects of the scene, so this one link is saved in the scene. That is normal.
4. If Unity asks **Original Prefab or Prefab Variant**, choose **Original Prefab**.

**From now on:**
- To change the screen (positions, sizes, colors, art), **double-click the EquipScreen prefab** in the Project window, edit it, and click **Save** at the top of the Scene view (or leave with the **<** arrow and save).
- The **Court** field is set only on the EquipScreen in the **scene**. In the prefab it stays empty, and that is correct. Without it, the heroes show their names instead of their portraits.
- The **EquipItemSlot** prefab stays separate: the EquipScreen prefab only points to it through **Slot Prefab**.

## Part 6 — Connect, save and test

1. Select **GameBootstrap** in the Hierarchy → field **Equip** ← drag **EquipScreen** (from the Hierarchy).
2. Save the scene: **Cmd+S**.
3. Check that the **Grid** inside the prefab is empty: double-click the EquipScreen prefab → ArmoryPanel ▸ Scroll ▸ **Grid** has no children.
4. Press **Play** and tap the third tab (Armory). Check:
   - tapping an item shows it in the middle panel and lights its slot orange;
   - **Equip** puts the item in the hero's socket and that hero's face appears in the slot's bottom-right corner;
   - tapping another hero and then an item the first hero carries shows **Take**, and pressing it moves the item;
   - **Remove** takes it off;
   - the grid shows one slot per item (8 today); with more than 8 the panel scrolls;
   - the Battle tab starts the run with those items.

## If something is off

- **Console says "EquipScreen: falta ligar no Inspector: …"** — a field from Part 3 or 4 is empty; the message ends with its name.
- **Panel borders too thick or too thin** — select TeamPanel / DetailPanel / ArmoryPanel → Image → **Pixels Per Unit Multiplier**: try **2** (thinner) or **0.5** (thicker).
- **The bottom panel goes under the tab bar on the phone** — select **EquipScreen ▸ SafeArea** → Safe Area → **Min Bottom 60**.
- **Slots too big or too small** — Grid → Grid Layout Group → **Cell Size** (keep X and Y equal) and **Spacing**; with 4 columns, Left + Right padding + 4 × Cell + 3 × Spacing should be **1080**.
- **The Armory covers the other screens while you edit** — EquipScreen → Canvas Group → **Alpha 0**. In Play the game shows and hides it by itself.
