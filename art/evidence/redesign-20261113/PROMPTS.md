# 六台机器重新设计审核稿

2026-11-13。使用内置 imagegen，每台独立调用。只传入原版 purifier 与 furnace；未读取旧稿图片或绘图脚本。输出尚未达到原生尺寸与严格像素网格要求，未导入游戏。

## 共用提示词

Create a completely NEW native pixel-art inventory machine sprite for Probably Stolen. Only the two attached original-game sprites are visual-style references: purifier and furnace. They are NOT edit targets. Do not use any prior generated machine, prior mod design, or prior composition. Match their exact low-detail pixel-art visual language, not a polished modern pixel illustration.
Camera: directly in front and above, shallow downward view, no yaw. Front horizontal edges remain horizontal. Visible shallow top surfaces, elliptical top openings, thin side bevels, coherent overlapping volumes. NOT isometric, NOT three-quarter side view, NOT a flat frontal elevation with one pasted-on side.
Materials: big contiguous flat color clusters. Deep purple #292234 for outline and deep recesses; muted violet-gray #6B5269 #777081 for selected shadow transitions; gray-blue #80929B and dark #303A41 metal, warm gray #A8AEA5 #C4C4B0 with sparse #D5D2B8 highlights. Functional teal #275654 #2F756F #8ABFBB sparingly. Hue-shifted discrete transitions following form, never wash the entire object purple. Few clear highlights, very little noise, no gradients, no dithering, no tiny busy detailing, no glossy photorealism.
One machine only, no labels or text, no border, no other objects. Full silhouette. Background solid #00FF00 including holes. Native logical pixel grid, all pixels identical square size, hard edges, no antialiasing. Return exact requested native PNG dimensions if possible; never introduce detail finer than one logical pixel. Canvas origin and centered anchor retained; roughly 1-2 logical pixels outer margin, base near bottom. Fill the specified footprint with meaningful structure.

## 各机提示词

### 破碎机 — 48×48

Compact mineral crusher. Broad low cast-metal body, inset twin toothed crushing rollers visible inside a top opening, a short forward discharge throat over an integrated powder-box dock. Offset enclosed motor with a purposeful drive coupling. Clear heavy crushing function.

### 筛分机 — 48×48

Mineral powder separator. A shallow stacked vibrating sieve cassette suspended between two stout support cheeks, top receiving socket, five clearly separated small collection mouths aligned along the lower crossbeam; one compact side eccentric drive. Sieve is the main recognizable form.

### 烧结站 — 48×48

Compact sintering press furnace. A squat insulated chamber inside a rigid arch-shaped load-bearing press, top feed cartridge coupling, recessed rectangular heating door and a wide low output drawer. Strong clear press/chamber silhouette.

### 矿化机 — 48×64

Vertical mineral crystal growth machine. One tall broad sealed faceted growth chamber, a visible seed pedestal inside, asymmetric slim nutrient riser attached by two collars, robust top pressure cap and low removable harvest drawer. Transparent-looking chamber rendered in opaque muted teal color blocks.

### 酒精制备站 — 48×48

Compact alcohol preparation apparatus. A broad squat distillation kettle with a shallow elliptical lid, integrated fruit mash receiving cup at upper rear, a short bent condensation pipe feeding a narrow cooling column, bottom front liquid collection coupling, all structurally connected on one frame. Distinct from a generic cabinet.

### 双胆反应槽 — 64×48

Wide twin-vessel chemical reactor. Two broad squat reaction vessels recessed into one common chassis, shared central top feed splitter visibly connected to both lids, two distinct material windows (muted blue-teal and warm pale ivory), separate bottom taps and small gauges. Horizontal wide silhouette, clear two-vessel function.
