# Battle System (design notes, started 2026-11-19)

The battle system isn't designed yet. This document collects the rules that are already decided so nothing gets lost before the full design.

## Artes and projectiles (user, 2026-11-19)

An arte is cast, the cast takes time (`CastTime`), and when the cast finishes the arte either **detonates** or **fires its projectiles**. Which one happens depends on whether the arte has projectiles.

### An arte without projectiles

When the cast finishes, the arte detonates **at the location of the user**.

### An arte with projectiles

When the cast finishes, the arte fires **all** of its projectiles at once and then does nothing else. It doesn't detonate at the user.

Each projectile follows its own move route, and **every projectile must end with a Detonate step** (`DetonateMovement`). Detonating does two things:

1. the arte the projectile belongs to activates **at the projectile's location**, not the user's,
2. the projectile is removed from the battle screen.

A projectile can't leave the battle any other way.

**Safety nets (user, 2026-11-19):** the projectile editor shows a yellow warning when none of a projectile's movement routes has a Detonate step. In battle, a projectile whose route runs out without detonating detonates anyway, wherever it stopped.

### What a detonation is

A detonation is everything the arte does when it lands:

- its detonation animation plays (the arte's target animation),
- its hitboxes activate where the detonation happens,
- damage and healing are dealt (`HpDamage`, `ManaDamage`, `Variance`, `Elements`),
- states are added and removed (`AddStates`, `RemoveStates`).

### The user still owns the arte

When a projectile detonates, the damage is worked out exactly as if the user had cast the arte at that spot. The user's Power, Critical, Accuracy and the rest are used, and the user is the one who dealt the damage. The projectile only changes **where** the arte lands, never **whose** it is.

### Activate Arte: other artes along the way

A projectile's route can also use the **Activate Arte** step (`ActivateArteMovement`). It activates the named arte at the projectile's location, still owned by the original user, and the projectile **keeps going**. It can be used any number of times before the projectile detonates.

When a projectile only exists to activate other artes, the arte it belongs to has no hitboxes, no damage and no detonation animation. Its detonation then does nothing except remove the projectile.

**Example: Elise's healing ground.** Elise casts a healing arte that has no damage, hitboxes or animation of its own. It lays down one projectile that isn't solid and has no image, so it's invisible. The projectile's route is:

1. Wait 5 seconds, then Activate Arte: Healing Pulse (this has its own detonation animation, hitboxes and healing),
2. repeat for about 30 seconds,
3. Detonate. The healing arte does nothing, and the projectile goes away.

**Example: Volley.** An arte with no damage of its own fires three arrows. Each arrow's route moves forward, uses Activate Arte: Fire Arrow, and then detonates.

An activated arte that has projectiles of its own would fire them from that point, which allows chains like a bomb that bursts into fragments. Whether chains are allowed, and how deep, is still open.

### Place Randomly, random waits and Play Animation (user, 2026-11-22)

**Example: Meteor Shower.** The cast finishes and fires the arte's projectiles (10-12 copies of one invisible
projectile). Each projectile's route is:

1. **Place Randomly:** its feet go on a random open tile of the field.
2. **Wait, Random:** a random time between Wait Time and Max Wait Time, so the meteors don't all land at once.
3. **Play Animation, Wait For Completion:** the meteor falls from the sky. The animation is drawn so that it ends on
   impact.
4. **Detonate:** the arte lands on that tile the moment the animation ends.

The three new steps:

- **Place Randomly** (`PlaceRandomlyMovement`) moves the entity, feet first, to a random tile in the area it stands
  in. That area is every tile reachable from its own tile without crossing a collision area on its level, so in a
  battle it's the field inside the walls. Entities placed this way don't share a tile while free tiles are left. It
  works on story maps too. A contact projectile's sweep starts fresh after the jump.
- **Wait** has a **Random** option with a **Max Wait Time**. The length is picked once each time the step runs.
- **Play Animation** (`AnimationMovement`) plays an animation on the entity. **Wait For Completion** (on by default)
  holds the route until the animation has finished.
- The placeholder AI casts an arte with randomly placed projectiles whenever any target is in scope.
- An arte always fires every projectile it has, so the count is fixed by how many copies are on the arte. A random
  count (10-12) would need a new arte setting.

### Where an arte's hitboxes go (user, 2026-11-21)

- The anchor tile in the arte hitbox picker is **the tile the user stands on**, and every hitbox is drawn from it.
- **Hitboxes per direction.** An arte with Rotate Arte With Character Direction has its own hitboxes for each way the
  user faces: `Hitboxes` is Down (and the only set for artes that don't turn), plus `HitboxesLeft`, `HitboxesRight`
  and `HitboxesUp`. In battle the set for the way the user faces is used **exactly as drawn**, with no turning.
- **A direction with no hitboxes of its own** turns the Down hitboxes about the anchor's middle instead
  (`Arte.TurnedFromDown`), and facing up those are measured from the top of the user's hitbox so they don't leave a
  gap. So older artes keep working until their directions are drawn.
- **The picker (editor):** a **Hitbox Direction** box (shown only for artes that turn) picks which set is drawn, and
  **Fill from Down** gives every direction that's still empty the turned Down hitboxes as a starting point (it never
  touches a direction that already has hitboxes). A **Preview Fighter** stands on the anchor, feet on the tile like in
  battle, facing the picked direction, with their own body hitbox outlined in dashed blue. The preview fighter is only
  for the editor and isn't saved.
- Tried and rejected (user, 2026-11-21): laying every arte out from the user's hitbox, pushed out to its edge on the
  side they face. Left and right swings came out at the feet instead of the middle of the body. One drawing turned
  four ways can't fit a body that isn't square, which is why each direction is drawn by hand now.

### Rotate Arte With Character Direction (user, 2026-11-21)

An arte with **Rotate Arte With Character Direction** turns its hitboxes to the way the user faces, and it also picks
its animations by direction, **by animation Id** (not by the order of the Animations list, which loads in file
order). Facing down it plays the animation it was given, facing left the animation with Id + 1, facing right Id + 2,
and facing up Id + 3. So a sword swing has four animations in a row
(Down, Left, Right, Up), and the arte points at the first. This applies to both the caster animation and the target
animation, and the direction is the user's facing (when the cast starts for the caster animation, when it lands for the
target animation). If there's no animation with that Id, the given animation plays instead.

**One animation, moved and rotated (user, 2026-11-21).** Instead of four animations, a turning arte can set
**Direction Animations** to **MoveAndRotate** (the default, SeparateAnimations, is the Id rule above). Then each
direction has its own **Animation X, Animation Y and Rotation** (`Arte.AnimationDown/Left/Right/Up`), edited on the
Artes page for the direction picked in the Hitbox Direction box.

- The **caster animation** is moved by the offset (unscaled pixels) and turned about the middle of the character,
  clockwise in degrees: every cell's spot turns around that middle and every picture turns with it. Shockwaves and
  heat move and turn with it too (a heat box keeps its shape).
- The **target animation** turns the same way, but is never moved: it plays where the target was hit.
- The rotations start at Down 0, Left 90, Right 270 and Up 180, so one animation already faces each way before
  anything is moved.
- **Mirror Left/Right** and **Mirror Up/Down** (per direction) flip the animation before it's turned: each cell's
  spot is mirrored and its picture gets a negative width or height, with its own turn reversed inside one mirror
  (both together are a half turn, so the cell's turn is unchanged). The target animation
  mirrors too. The maths lives in `ArteAnimationPlacement` (Place and Turn), so the game and the editor preview
  always agree.
- **Animation Preview** on the Artes page (under the Rotate tick box): the preview fighter standing on the anchor
  tile, the hitboxes the game uses for the picked direction (red, including the turned-Down fallback) and the
  fighter's body hitbox (dashed blue). It shows the first frame with cells, and **Play** runs the caster animation
  once at the game's 60 frames a second, placed the way the battle places it (by Id, or mirrored, turned and moved).
  Sounds, shockwaves and heat aren't previewed.

### Contact projectiles, like arrows (user, 2026-11-19)

An arrow flies up to five tiles. If it touches an enemy on the way, it detonates there, and the arte's one-tile hitbox lands right on that enemy, so the arrow has to actually hit. It never detonates on allies. If it hits nothing, it still detonates at the end of its route, because detonating is how every projectile is removed, but that detonation **only removes it**: no animation, no hitbox, no damage.

A move route alone can't express "stop when you touch something", so the projectile itself gets a setting:

- **Detonate on contact with:** None (default), Enemies, Allies or Anyone. Teams are from the user's side, so an enemy archer's arrows treat the party as enemies. The projectile checks its own hitbox against fighters after every walking step of its route (a Step Forward of 60 steps is checked 60 times, not once). On a valid contact it stops its route and detonates at once, with the full arte.
- Contact projectiles pass through the fighters they ignore (an arrow flies past Rusk to reach the wolf behind him).
- **A contact projectile that reaches its Detonate step without touching anything** detonates empty: it's removed and nothing else happens. A projectile without contact detonation always detonates with the full arte, as before.
- **Walls:** a contact projectile blocked by a wall or a solid tile detonates empty too. Whether some projectiles should detonate for real on walls (a fire bolt bursting against a wall) is open.
- Every projectile route still ends in Detonate, so the missing-Detonate warning applies to contact projectiles too.

So an arrow is: contact = Enemies, route = one Step Forward of 60 walking steps (5 tiles) then Detonate, and an arte with a one-tile hitbox and the arrow's damage.

**Detonate at Route End (user, 2026-11-23).** A projectile setting next to Detonate on Contact: a contact projectile that reaches its Detonate step without touching anyone goes off with its **full arte** there instead of just being removed, so it detonates by slamming into an enemy **or** where it lands (Acid Spit's glob splats on the ground). Walls still only remove it. A detonation's animation only plays on the fighters it hits, so a projectile that should splat on empty ground plays its splat as a route step (Play Animation, Wait For Completion) just before Detonate. A Play Animation step that waits holds the route until **every** animation on the projectile has finished, so a projectile's own flying animation must last no longer than its flight.

## The battle style (user, 2026-11-20)

A **grid-based tactical battle, but in real time** instead of taking turns. Fighters stand on the 48 px tile grid, and
positions, ranges and areas are all counted in tiles.

### The field (user, 2026-11-20)

The standard battle field is the one on the **Battle Map Test** map: a 21x21 map with a collision wall around a
**13x13-tile field** (tiles 4-16 in both directions). Battles shouldn't take place on a field much larger than that,
because you shouldn't be able to run too far. Different areas may use different sizes, but this one is the standard.

Spawns on the test map: the party starts on row 14 and the enemies on row 6, so the two sides start **8 rows apart**.
Party spawns are at columns 6, 8 and 14 (there's no Actor Spawn Location 003 yet, probably column 12), and enemy spawns
at columns 6, 8, 12 and 14.

For scale: a rounded 7x7 area (Cherry Gel, Rally) covers 45 of the field's 169 tiles at most, and an arrow's five
tiles is a bit over a third of the field's width. In practice a big area covers much less than that (user,
2026-11-20): battles are fluid and spread across the whole field, so fighters are rarely in the middle, and part of a
big area often falls outside the walls and hits nobody. Big areas are fine on this field size.

- **Open fields, no obstacles (user, 2026-11-20).** Battle maps are completely separate from the story maps for this
  reason: nothing on the field blocks movement or projectiles, only the walls around it.
- **No turning on the spot.** Pressing a direction moves the fighter that way, the same as on the story maps.
- **Free movement, exactly like the story maps (user, 2026-11-20).** Fighters move in small steps and can stand
  between tiles. Tiles are the unit for sizes and ranges (a 1x1 hitbox is 48x48 px), but hitboxes are placed from the
  fighter's exact position, not snapped to the grid.
- **No running in battle (user, 2026-11-21).** Dashing (Shift) is off for everyone while a battle is on.
- **Everyone moves at the same speed (user, 2026-11-20).** There's no speed per class or stat. On entering a battle,
  every character's movement speed is lowered by 1 from their story-map speed, and raised by 1 again when they return
  to the story map. States can still change speed during a battle (Quickfoot faster, Chilled slower, stances slower):
  everyone starts from the same speed, and only temporary effects move it.
- **Speed never leaks out of a battle.** The engine remembers each character's story-map speed when the battle starts
  and restores that exact value when the battle ends. It doesn't apply "+1" to whatever the battle speed happens to be,
  so a Chilled or Quickfoot still running at the end of the fight can't leave anyone slower or faster on the story map.
  All battle states are cleared from speed when the battle ends.
- **Route steps count walking steps (user, 2026-11-20).** Move, Step Forward and Step Backward each have a
  **Steps** count in walking steps (the grid ÷ Division: 4 px, 12 to a tile here), and the edit popup shows how many
  tiles that is (60 steps = 5 tiles, 30 = 2.5 tiles). So "five tiles forward" is a single Step Forward of 60, and a
  route can stop between tiles. New move steps start at one tile (12). Bow Shot's Arrow and Rod Magic's projectile were
  converted from five one-tile steps to one step of 60 (backup in `Reference\_archive\artes_backup_2026-11-20_steps`).

## Items in battle (user, 2026-11-20)

- **Most items hit an area around the user.** A Cherry Gel heals the user by 30% of their Life **and** every ally inside
  its hitbox: a 7x7-tile area around the user, rounded (the corners are cut off, which is why the item uses two
  overlapping rectangles: 7 wide by 5 tall, and 5 wide by 7 tall). Most items work like this, so using an item next
  to your friends matters.
- **Some items affect only the user**, such as special healing items and stims. These use the **Self** scope.
- **Stims** are temporary stat boosts: an item that adds a timed State which raises attributes by a percentage. They
  must not be overpowered: small boosts (around 10-15%), short durations, one stat (or a small trade-off) each, and the
  same stim doesn't stack with itself.
- **Items can do almost anything through states**: "higher magic resistance for a while", "heals you over time" and so
  on are items that add a state.

### Items outside and inside battle (user, 2026-11-20)

- **From the menu (outside battle)**, most consumables can be used too, but there they only affect **the one actor you
  pick**. A Cherry Gel used from the menu heals that one actor by 30%, not everyone around them.
- **In battle the menu still opens, but items and equipment are locked**: nothing can be used or equipped from it.
  The menu stays available so the player can change plans mid-battle (which may be done a better way later). In battle
  an item is always used by a fighter where they stand, so it only reaches allies in its hitbox. That stops anyone from
  healing a party member across the field from a menu.
- **Items have one global cooldown in battle.** Starting point: **15 seconds**. When anyone in the party uses any item,
  nobody in the party can use any item until the cooldown runs out, so use them wisely. The real length gets tuned in
  testing.
- **Party members can use items on their own.** Each party member has battle options in the menu, and one of them is
  whether that member may use items in battle. It's a real trade-off: a low party member who heals themselves may not
  reach you with the area, and if you're low too you can die.

## Spawn points (user, 2026-11-21)

- **Troops own every spawn point.** Battle maps are plain open fields with no spawn entities, so any troop can use
  any battle map.
- **Enemies:** each enemy in a troop is **Fixed** (starts on its own spot, for bosses and set pieces) or **Random**
  (starts on one of the troop's **enemy spawn points**, a different one for each random enemy, so fights don't start
  the same way every time).
- **Party:** the troop's **Party Spawn** is **Fixed** (party member 1, the leader, on party point 1, member 2 on
  point 2 and so on) or **Random** (each member on a different party point picked at random).
- **Placement is by the feet:** a spot is the tile the fighter stands on, and the sprite rises up from it, centred,
  like characters standing on tiles on the story maps (`TroopEnemy.X/Y` and `TroopSpawnPoint.X/Y` are that tile's
  top left in battle map pixels).
- The Troops page warns (yellow) when there are fewer than 4 party points, fewer enemy points than Random enemies, or
  Fixed enemies that haven't been placed. If a battle is still short anyway, leftover fighters go to the nearest
  open tile next to their side's other points.

## Starting a battle (user, 2026-11-20)

- **Touching an enemy on the map starts a fight against a troop**, and the troop can be **random**: one enemy on the
  map might lead to troop 1, 7, 9, 15 or 23. (Today Initiate Battle takes one troop; it needs a list to pick from.)
- **Battle transitions:** the engine offers **several transition effects** to pick from, so each game can choose.
  This game uses a **screen crack and break** like the Tales series. Another the user loved: a **bright sunny yellow
  lens flare at the spot where the touch happened**, then the screen zooms in and you're on the battle map.
  - Ideas for the list: Shatter, Sun Flare, Swirl, Mosaic, Zoom Blur, Slash, Ink Bleed, Fade. Every one starts from
    the point of contact.
  - **The transition is a game-wide setting (user, 2026-11-20)**, with overrides on troops or Initiate Battle (for a
    boss) proposed on top.
  - **When the party is ambushed** (caught from behind), the transition gets a **red tint**, and the party starts the
    fight stunned for a while.
- **Random troops are weighted (user, 2026-11-20):** each troop an enemy can lead to has a weight (50% troop 1, 30%
  troop 7, 20% troop 23), so **rare encounters** are possible.
- **A General page in Settings (user, 2026-11-20)** for game-wide odds and ends like the battle transition and the
  level cap (today the level cap is on the Growths page).

- **How you touch the enemy matters.** Catching an enemy from behind on the story map starts the battle with the
  enemies stunned, and an enemy catching the party from behind starts it with the party stunned. This stun lasts a
  little longer than a normal Stunned, so it's its own state (working name **Ambushed**).

## Who you control (user, 2026-11-20)

- **Only the party leader, the first member, is controlled by the player.** The others are run by the AI.
- The leader can be changed **outside battle only**. It's locked during a battle, so when the leader falls, the
  player watches helplessly and hopes the others win.
- **Every party member keeps their own arte key layout.** Playing as Elise for a while never changes or loses
  Odette's slots.

## Fleeing (user, 2026-11-20)

- Battles marked **Can Run** (already a setting on Troops and Initiate Battle) can be fled by **holding a key**. A
  progress bar on screen shows how much longer it has to be held. You don't have to move anywhere to flee.
- **While fleeing, the party stops fighting**, but the enemies don't. Everyone is open to attack until the bar fills.
- **A small chance it fails.** When the bar fills, the escape can still fail and the fight carries on.
- **Letting go of the key resets the bar**, and the party starts fighting again straight away. There's no cooldown on
  trying again.

## Hitting things (user, 2026-11-20)

- **Damage and healing get their own planning session.** The user wants a complex system built on percentages, never a
  simple "attack minus defense". Healing uses the same kind of system (and never Defense, see the attributes).
- **Accuracy and Evasion use a ratio (user, 2026-11-20)**, the same shape as the damage formula:
  > **hit chance = 180% × Accuracy ÷ (Accuracy + Evasion)**, clamped between a floor (about 5-10%) and 100%

  Equal stats hit 90% of the time. At level 50, Aeron (320 Accuracy) always hits a slow enemy (Evasion 140), Garrick
  (170) hits an agile one (250) 73% of the time, and an enemy with 200 Accuracy hits Rusk (220 Evasion) 86% of the time.
  The 180% and the floor are starting values to tune in testing. A guaranteed dodge comes from states (Untouchable,
  Hidden), never from Evasion alone. Healing and buffs on allies never miss, and some artes can be marked as never
  missing (like Path of Exile's "Can't be Evaded"). Other systems considered (RPG Maker's two separate rolls, FFXI and
  D&D's difference, Pokémon's stages) are in the chat of 2026-11-20.
- **Critical hits do 3x** damage or healing (user, 2026-11-20, changed from 2x). **Heals can crit.** The chance comes from the attacker's Critical; the
  defender has nothing against it.
  > **crit chance = Critical ÷ (Critical + 250 + 44 × the user's level)**

  Crits should feel like crits, so they're rare: **the best party member (Rusk) tops out at 10%** at any level
  (user, 2026-11-20). With the Classes.md growths, at level 50: Elise 3.5%, Garrick 5.0%, Odette 5.4%, Tavren 6.1%,
  Sabine 6.5%, Aeron 8.6%, Rusk 9.6% (Rusk peaks at 10.0% around levels 10-20). States and gear that add Critical push
  past these, so they should be small.
- **Elements are plain percentages (user, 2026-11-20).** Every fighter has a rate per element (`Fighter.Elements`,
  default 100%), and the hit is multiplied by it: above 100% takes more, 100% is normal, below 100% takes less, 0% takes
  nothing, and **only a negative rate heals** (-50% heals them by half the hit). A rate between 0% and 100%, like
  50%, never heals; it only reduces the damage (50% takes half). There are no named tiers; the numbers are the
  design.
- **Sources stack the same way (user, 2026-11-20).** The fighter's own rate, equipment (`Equipment.ElementResist`) and
  states (wards like Fire Ward) each count as a change from 100%, and the changes are added together. Garrick at 100%
  Fire, wearing 50% Fire armor (-50) under a 50% Fire Ward (-50), ends at **0%**, immune. The fighter's total rate for
  each element is worked out this way first, then an arte with several elements combines those totals (step 7).
- **Variance** is the arte's ± random roll, so hits feel slightly different each time. It only adds randomness around
  the arte's value and never decides the damage: a high Variance means more spread, a low one less, but an arte
  that does 1-10 can never suddenly roll 10,000, however high Variance is set.
  - **Variance is a percentage, applied last**, to the finished hit after Power, Defense, elements and the rest.
    Example: Sword Swipe has a value of 1, Tavren's Power turns that into a hit of about 15, and Variance 33% spreads
    it to about 10-20.
  - **Proposed cap: 50%** (a hit of 15 could roll 7-22 at most). The editor clamps anything higher.
- **No flinching and no invincibility after a hit.** You can stand next to an enemy and swing, swing, swing while
  it hits you back. Only artes that say so (knockback, stun and so on) interrupt anyone.
- **Concentration:** damage taken during a cast adds up, and if it passes the caster's Concentration threshold the
  cast **breaks**: the Mana is spent, nothing happens, and the cooldown starts. (The player still can't cancel a cast.)

## The order of a hit (user, 2026-11-20)

Everything that happens when an arte or item lands on one fighter, in order. Before step 1, both fighters' attributes
are already final (equipment, Teamwork and stat states such as Powered Up or Ironskin are applied to the attributes
themselves), and every step reads those finished numbers.

1. **Can this fighter be affected?** Check the scope. With an Allies scope, the caster is hit too if they're inside
   the hitbox. An Incapacitated fighter is skipped unless the arte revives them.
2. **Hit or miss:** Accuracy against Evasion. **A miss stops everything:** no states and no damage. Healing and buffs
   on allies never miss.
3. **Remove states.**
4. **Add states.** Every state can be resisted on its own. **Bosses are immune to stun-lock effects** (Stunned,
   Sealed, Rooted and the like) **but can still be poisoned** and get other effects of that kind.
5. **Base value = Damage Multiplier × Power.** Every arte's `HpDamage` is its **Damage Multiplier**, used as it is:
   **1 means 1× the user's Power** (user, 2026-11-21: it isn't a percentage, Sword Swipe stays 1). A **negative**
   multiplier heals.
6. **Defense ratio:** damage is multiplied by **Power ÷ (Power + Defense)**, the contest between the attacker's Power
   and the target's Defense. Equal Power and Defense lets half through, Defense 0 lets all of it through, and huge
   Defense crushes the hit toward 0 without ever being a guaranteed wall. **Skipped for healing.**
7. **Element:** multiply by the target's rate for the arte's element (a percentage; below 0 absorbs). **An arte with
   several elements counts every one of them (user, 2026-11-20).** 100% is the base, so each rate is a change from
   100%, and the changes are added together: **rate = 100% + the sum of (each element's rate - 100%)**.
   - 150% Fire and 50% Light: +50 and -50, so the hit lands at **100%**.
   - -50% Fire and 100% Light: -150 and 0, so the hit lands at **-50%**: it heals the target by half.
   - This is not RPG Maker's rule, which keeps only the highest rate.
7b. **From behind (user, 2026-11-23):** a hit on an opponent from behind does a bit more damage (x1.25,
   `HitCalculator.BehindMultiplier`). It counts when the hit came from the back side of the target's tile: the
   user's tile for an arte that detonates at the user, or the projectile's tile for a projectile. A state's tick
   never counts, and neither does healing or a friendly.
8. **Critical:** roll it, and a crit triples the result.
9. **Variance:** the ± percentage, applied last.
10. **Round:** to a whole number. **A hit that comes out at 0 does no damage.** It isn't rounded up to 1.
11. **Apply it:** Life goes down, never below 0, and healing never goes past max Life. Mana damage works the same
    way. An absorbed hit heals instead.
12. **Aftermath:**
    - **Threat** is added for the user, as a percentage of the hit (not 1:1).
    - If the target is casting, the damage counts toward breaking their cast (Concentration).
    - If the target's Life hit 0, they become Incapacitated, all their other states are cleared and their summons
      vanish. **Enemies** also drop their loot on the floor; actors don't have drops.
    - Last, check for victory or game over.

### The damage formula (user, 2026-11-20)

> **damage = Damage Multiplier × Power × (Power ÷ (Power + Defense))**, then element, critical, Variance and rounding
> **healing = |Damage Multiplier| × Power**, then element, critical, Variance and rounding (no Defense)

It's a ratio formula, like Pokémon's Attack ÷ Defense, but bounded so a target with very low Defense can't make hits
explode. Tavren's Sword Swipe (1) at level 1 (Power 28) against Defense 20 hits for about 16, and Variance 33% spreads
that to about 11-21. At level 50 (Power 280) against Defense 200 it's about 163.

- **Healing never turns into damage (user, 2026-11-20).** Negative element rates are ignored for heals: if a heal's
  element rate comes out below 0%, it counts as 0% and the heal does nothing. So a Light heal on someone at -100%
  Light (undead, a skeleton) never hurts them. Only damage can be absorbed.
- **Mana uses the same formula (user, 2026-11-20).** `ManaDamage` is a Mana Multiplier (1 = 1× Power):
  Mana damage = Mana Multiplier × Power × (Power ÷ (Power + Defense)), and a negative Mana Multiplier restores Mana
  (no Defense). Soul Harvest and Mana-restoring artes use it.
- **Defense is never a guaranteed wall.** A ratio never truly reaches zero, so a huge Defense (99999, 999999) crushes
  most hits to 0 or 1 but a strong enough hit can still push a point through. That's accepted.
- **Guaranteed immunity uses rules built for it:** the Invincible state, an element rate of 0%, or a "can't be damaged"
  setting on the enemy, never a Defense number.
- The other formula families considered (subtraction like RPG Maker's `a.atk * 4 - b.def * 2`, FF7's level curves,
  D&D's armor-as-hit-chance, MMO percentage mitigation) are in the chat history of 2026-11-20; the ratio was picked.

## How fighters move (user, 2026-11-20)

- The player's character moves only as the player says.
- **Everyone else picks a target every so often**, weighted by Threat, so if Threat is fairly even, an enemy may switch
  between actors at random every few seconds.
- **While they have a target they chase it**, and once they're right next to it they **move around a little at
  random**, so nobody just stands still in your face.

## Winning and losing (user, 2026-11-20)

- **Victory:** when every enemy is Incapacitated, the party wins.
- **Summons go with their summoner:** when a summoner is Incapacitated, every summon they raised is removed from the
  battle at once. That works both ways: killing an enemy necromancer clears its skeletons, and when Sabine falls, her
  skeletons go too. A summon that falls is simply removed; it isn't Incapacitated and can't be revived.
- **Summons never count** toward victory or a game over. A side can't be left with only summons, since they vanish
  with their summoner, so this only keeps the rule simple.

- **Game over:** when every actor fighting in the battle is Incapacitated, the player gets a game over. Party members
  on the bench don't count; only the ones on the field do.
- **Unwinnable battles** are marked as such on the battle. When every actor falls in one, there's no game over: the
  battle ends and the story carries on (Tavren against The Shepherd in Chapter 3, The Shepherd in Chapter 8, Osric and
  Garrick at the midpoint).
- When the battle ends either way, every fallen actor revives with 1 Life (see States).
- **Scripted endings.** Some battle endings can be scripted, such as a boss that taunts the party when they lose before
  the defeat plays out, or a scene on a win. So battles need events that run when they're won or lost, before the
  battle actually ends.

### Drops and the end of a battle (user, 2026-11-20)

- **Drops land on the field where the enemy fell**, money included, and have to be collected by walking over them.
  Any party member can collect them, not only the leader.
- **The battle officially ends the moment the last enemy falls.** At that moment every state is cleared, Incapacitated
  included, and every fallen actor revives with 1 Life, so everyone is on their feet for the pickup.
- **Then there's a short pause, about 5 seconds**, for collecting drops. There's no timer on screen: players learn the
  length by playing. The AI party members go for drops during the pause. When it's over, the party leaves the field,
  and **any drops still on the field are lost**.

### EXP (user, 2026-11-20)

- **Everyone gets the full EXP**, with no splitting, except:
- **A party member who is down when the battle officially ends** (the moment the last enemy falls) **gets no EXP at
  all.** Someone revived before then gets the full EXP. The pickup pause doesn't count, since everyone is back up by
  then anyway.
- **Benched party members get full EXP**, so the whole squad stays playable even if the player sticks with four
  favourites.

## States (user, 2026-11-20)

**When a battle ends (user, 2026-11-20), every state is removed** unless that state specifically says it lasts past
the battle. That needs a setting on states (something like "Remains after battle", off by default). Incapacitated is
always cleared, and every fallen actor revives with 1 Life.

**Incapacitated (user, 2026-11-20)** is added when a fighter's Life falls to 0 or below. Life never goes below 0. In
battle it's cleared by things like the Blackberry Gel or raising artes (Second Chance, Grace of the Order). Nobody is
ever Incapacitated outside battle, because the end of every battle clears it, so revive items like the Blackberry Gel
are **BattleOnly**.

- **Becoming Incapacitated removes every other state** on the fighter, **and resists every other state** while it
  lasts: nothing can add a state to an Incapacitated fighter. Because a hit removes states before adding them, a
  revive that also adds a state (say Grace of the Order adding Regenerating) still works: Incapacitated goes first,
  then the new state goes on.
- **An Incapacitated fighter can't be healed or damaged by anything** in the battle, enemy or ally, except an arte or
  item that revives them (one that removes Incapacitated). Everything else in a hitbox passes over them. They also
  can't be targeted, so enemies stop going after them.
- **Order of a hit (user, 2026-11-20):** when an arte or item lands on a fighter, it does three things in this order:
  1. **remove states** (`RemoveStates`),
  2. **add states** (`AddStates`),
  3. **deal the damage or healing.**

  So an arte's own states count toward its own hit: an arte that adds a Defense-down state deals its damage against
  the lowered Defense. That's intended, and it allows "Sunder" style artes that crack armor and benefit at once. So a Blackberry Gel removes Incapacitated first, and then heals the
  revived fighter by 30% of their Life. Without that order the heal would hit someone who can't be healed.

**States can be anything and everything (user, 2026-11-20), and there will be a lot of them:** damage and healing,
slowing a fighter, freezing them in place (stun, frozen and so on), lowering stats, one-use powerful buffs or nerfs,
effects over time, boss buffs (resist every state of a kind, enrage when the fight takes too long), and **invisible
states** that never show on the HUD. Odette's Overcharge is a state too: when it triggers, it just gives her a state
that makes her next arte cast instantly.

**States remember who applied them, and their damage uses the normal formula (user, 2026-11-20).** When a poison (or
any state) ticks for damage or healing, it goes through the same hit order and formulas as everything else, using the
**Power of the fighter who applied the state**, not the holder's. The cleanest way to build it is for a state to
**activate artes** the way projectiles do (Burning = every second, activate Burn Tick, owned by the one who applied
it), so a state never needs a damage formula of its own.

**How a state knows its source (2026-11-20).** Each state on a fighter in battle is an instance that keeps the
source in two ways:

- **the live fighter** who applied it, for everything about *who* and *where*: a taunt chases the taunter's current
  position, Feared runs from them, Brother's Keeper redirects damage to them, and Threat from a tick goes to them;
- **a snapshot of their numbers** (Power, Critical, Accuracy, Level, team and so on, finished values) taken when it
  was applied, for any damage or healing the state deals. The damage calculator only ever reads an attacker snapshot:
  a normal hit makes a fresh one, and a state passes its saved one. Projectiles snapshot their owner when fired, the
  same way.
- **Per state: what happens when the source falls.** "Ends when its source falls" for taunts, Feared and links;
  poisons, burns and curses keep going on their saved numbers.

**Stackable (user, 2026-11-20).** Every state has a **Stackable** option, and many states in this game aren't
stackable, Poisoned for one: you can be poisoned once until it wears off. Stackable states have a **Max Stacks**
option (Rusk's Rage up to 5, for example). 
- **A non-stackable state applied again is ignored (user, 2026-11-20):** the one already there runs out as normal and
  the new one does nothing.
- **Stacks share one timer (user, 2026-11-20):** adding a stack restarts it, and when it runs out, all the stacks end
  together.

**New state abilities and UI options will be needed as the artes get built (user, 2026-11-23).** Writing each new arte
will show what states need to do, and the engine and the States page will grow new abilities and editor options to match.
That is expected, not a gap: when an arte needs a state to do something it can't yet, add the ability and its editor
option then.

**State options built so far (2026-11-23):** Duration (`Ticks`, counted in Tick Intervals: 5 ticks at an interval of 60 is 5 seconds, so a state with nothing to tick and an interval of 60 is plain seconds; 0 is permanent; user, 2026-11-23), Stackable and Max Stacks, Remains After
Battle, Ends When Source Falls, Stun-Lock (bosses resist it, and a fighter holding one can do nothing: no walking, turning or artes, and a cast is broken) with Can Still Use Artes (off by default) to let it use artes where it stands, Hidden (no HUD icon), Friendly (a good state, off by default; artes and items with Remove All Negative States clear every state that isn't Friendly, except Incapacitated; user, 2026-11-23), attribute changes (flat, per stack) and attribute percents (applied after the flat ones, per stack, so -20% Defense scales with level; user, 2026-11-23), Move Speed (percent walking speed in battle, several multiply; Chilled is 60%),
element changes (a rate like equipment's, 100% is no change, per stack), Resist States (states that can't be put on the holder while it has this one, e.g. Venom Ward keeps Poisoned off; user, 2026-11-23), and a Tick Arte and/or Tick Animation with a Tick Interval (the animation plays on the holder every tick) (the arte is activated on the
holder, owned by whoever applied the state, no cost, cast or cooldown; it lands on the holder whatever its scope says, never misses, and can still crit; user, 2026-11-23). Equipment's Resist
States and Resist All States are checked when a state is added.

The States page is a placeholder for now. **States can and should do whatever the game needs**, and options get added
to the States page as they come up (stat changes, resistances, healing or damage over time, and so on). A missing
option is never a reason to cut an idea.

## Learning artes: the arte tree (user, 2026-11-20)

- **Players choose their artes.** Each actor has a pool of **about 30 artes**, and **nobody can learn them all**.
- **Every tree arte has a level threshold** (the earliest level it can be bought at) and may have **prerequisite
  artes** that must be learned first. Branches or straight lines, either works.
- **Gaining a level gives an arte point**, spent in the menu to buy an arte.
- **Stronger artes cost more points**, by tier.
- **Granted artes** aren't on the tree and cost nothing: they're simply given at the right moment. That covers every
  actor's Threat drop arte and the side quest and story artes (The Wall, Stand Fast, The Verdict and so on).
- **New party members arrive with a pre-set list of artes** that fits who they are in the story at that point.
- **Choices are permanent.** No respec in this game. The engine should make respec an **option** (a game setting, off
  here) for other games.
- **Points per level is a game setting, not a fixed rule.** This game caps at level 50 (so about 49 points with one
  per level), but another game could cap at 99 and want fewer points per level, or points only on some levels. So
  the engine needs: points gained per level (or a per-level table like the growths), starting points, and arte point
  costs chosen per arte. The number of arte key slots should be a setting as well.
- **This game's values (user, 2026-11-20):** one point per level (49 by level 50), and costs by tier: **Quick 1,
  Standard 2, Heavy 4, Big 6, Mystic 8**. A pool of about 30 (roughly 9 Quick, 9 Standard, 6 Heavy, 4 Big, 2 Mystic)
  costs about 91 points, so a level-50 actor can afford about half of it, around 14-16 tree artes. Together with the
  weapon attack, the Threat drop and 1-2 side quest artes, that's about 18, which fills the **18 key slots**.
- Engine needs: a point cost and prerequisite list on artes (fighters already have level conditions on their artes),
  arte points on actors, granted artes, a starting list per actor, the respec setting, and a menu screen to buy artes.

## Talents (user, 2026-11-20)

- **A small second tree of talents:** unique passive bonuses for each character, learned throughout the game.
- **Each character has only a handful** to pick from.
- **Talents are equipped and unequipped freely** at any time outside battle, at no cost (unlike artes, which are
  permanent).
- **2 can be equipped at once**, rising to **3** later in the game.
- **Learned through a mix:** a couple from level milestones, one from the character's story arc, and one from their
  side quest.
- Example, Odette: when one of her artes deals damage there's about a **30% chance** she gains a state that makes her
  **next arte cast instantly**. It's rolled **once per arte**, not once per hit.
- The draft talent ideas for every character are in `Tale Of Betrayal 1\Talents.md`.
- Engine thought: a talent can be a passive state that's on the fighter for the whole battle while equipped, plus
  **triggers** ("when you deal damage, X% chance to add a state", "when your Life drops below 25%", "when an ally
  falls"). Triggers are the new piece the engine needs.

## Cooldowns, Mana and using artes (user, 2026-11-20)

- **Every arte has its own cooldown.** Some are very fast or have none at all, and others are long. Each arte's
  cooldown runs separately, so artes can be used back to back to back as long as each one is ready.
- **Artes cost Mana** (`Cost`).
- **Every arte a fighter knows is usable in battle.** The player is never limited to a set of ten. The old design
  (number keys 0-9, swapped in the menu) is rejected because people always used the same artes and switching meant
  opening the menu. No held modifier keys (Shift+number is out: too annoying).
- **Default keyboard layout (user, 2026-11-20):** the right hand moves with the arrow keys, and the left hand has
  everything else:

  ```
    1   2   3   4   5   6        arte slots 1-6   (slot 1 = weapon attack by default)
     Q   W   E   R   T   Y       arte slots 7-12
      A   S   D   F   G   H      arte slots 13-18
       Z   X   C   V   B     item slots 1-5 (reassignable at any time, mid-battle too)
    [      Space      ]      free for now (guard, dodge or switching fighters later)
  ```

  New artes fill the next free slot by themselves, and the player can rearrange slots in the menu but never has to.
  A new weapon's attack replaces the old one in the same slot. The battle bar shows the slots in the same shape as the
  keys, with each one's cooldown and Mana cost. Every key should be remappable (an engine option), and a controller
  will need its own scheme later (probably a radial menu).
- **The weapon attack is just an arte.** It comes from the equipped weapon and takes up one arte slot like any other
  arte, with no key of its own.
- **Each fighter has a tiny global arte cooldown**, only so one key press can't fire two artes. It isn't a gameplay
  limit.
- **Casting locks movement**, and a cast **can't be cancelled** once it starts, so the player has to pick their moment.
  The fighter can move again once the cast finishes.
- **Items need keys too.** The old design had three item slots on Q, W and E that could be reassigned mid-battle.
  Items keep the single global party item cooldown (see Items in battle), but the key layout shouldn't be as
  restrictive as the old one.

## Life and Mana between battles (user, 2026-11-20)

- **No Mana regeneration during battle.** Mana only comes back through items and artes (Soul Harvest, Mana-restoring
  artes).
- **Life and Mana do not refill after a battle.** Whatever a fighter ends with carries into the next fight, and fallen
  fighters get up with 1 Life. So every fight costs something, dungeons are a test of endurance, and weapon attacks
  (which cost nothing) matter.
- **Full recovery comes from:**
  - **inns**,
  - **certain story points** (after being beaten by Osric, the party is back at full the next time the player has
    control),
  - **recovery save points**: these save **and** fully recover the party, but the recovery only happens the **first**
    time each one is used; saving there works forever. Ordinary save points (like the one in an inn) only save.
  - **levelling up**: an actor who gains a level is fully recovered.
- **Mana cost target:** a full Mana bar should pay for about **15-20 mid-level spells**. For Odette at level 50 (340
  Mana) that puts a mid spell around 17-22 Mana.
- **Arte costs are fixed (user, 2026-11-20)**, priced for the level the arte is learned at, so a new arte always
  costs about the same share of the bar when it's learned. Mana grows a lot (Odette ~75 at level 9, 340 at level 50),
  so a mid spell learned around level 9 costs about 4-5, around level 25 about 9-11, and around level 50 about 17-22.
  Early artes become cheap filler later, and characters with more Mana really can cast more.

## Cast times and cooldowns (user, 2026-11-20)

- **They vary a lot from arte to arte.** Artes with much higher potential get a longer cooldown, a longer cast time,
  or both. There's no fixed tier table.
- **Cast time is the strategic one.** It's the window where the caster is locked in place and the target can move.
  - **Aimed artes keep short casts:** a fireball flies straight forward, so a long cast would let the enemy step out
    of the line before it's even thrown.
  - **Stubby hitboxes keep short casts too**, like a punch on the tile in front, for the same reason.
  - Long casts suit artes that don't need precise aim: big areas, artes centred on the caster, buffs and heals.
- Cooldowns are the main way to limit strong artes that need short casts.

## Threat and taunts (user, 2026-11-20)

- **Every fighter in battle has a Threat value.** When an enemy picks a target, the choice is random but **weighted by
  Threat**: the higher a fighter's Threat, the more likely enemies are to go after them. Nobody is ever completely safe
  just because someone else has more.
- **Tanks build Threat on purpose.** Tank artes raise the user's Threat, often with a Defense or Evasion boost on top.
- **Taunt** is a state with a source (the fighter who taunted):
  - a **taunted enemy** attacks the taunter while the taunt lasts,
  - a **taunted ally**, including the player's character, isn't forced to do anything, but does **far less damage** to
    anything that isn't the one taunting them. That's how an enemy tank's taunt works against the party.
- **Every actor has a Threat drop arte.** It removes a large amount of the user's Threat and makes them hard to hurt
  for a short time, through a big Defense boost, a big Evasion boost, or a **cloak** (enemies can't see them, so they
  can't target them). Some of these also heal the user.

**Threat details (user, 2026-11-20):**

- **One Threat value per fighter.** Every enemy weighs each actor's overall Threat and picks a target weighted by it.
  Enemies don't keep their own separate Threat lists.
- **Damage builds Threat at 100% of the damage dealt, healing at 50% of the amount actually healed** (overheal
  doesn't count). Both are settings so other games can tune them.
- **Artes have a Threat field** (default 0) that changes the user's Threat directly: positive for tank artes, negative
  for the Threat drops.
- **Threat never fades during a battle.** It only resets when the battle ends. If your Threat is high and your Threat
  drop is on cooldown, you have to stop acting until someone else takes the Threat from you.
- **Targeting has two speeds (user, 2026-11-20):**
  - enemies **check very often** (not every update, but many times a second) and switch at once when their target
    falls, becomes Hidden, a taunt starts or ends, or someone's Threat jumps well above the current target's,
  - the **weighted random pick is only re-rolled every couple of seconds**, so an enemy commits to a chase instead
    of twitching between targets, and with even Threat it still changes target every few seconds.
- (Proposed: a fighter's Threat goes to 0 when they're Incapacitated, so nobody keeps targeting the fallen.)
- **Enemy arte choice gets its own customizable AI system** (a much bigger conversation): every arte gets its own
  **"pick me if"** section of conditions.
- **The AI system is built later (user, 2026-11-20)**, but the battle system is built with it in mind so adding it
  isn't a huge lift. Every fighter in battle is driven by a **controller** behind one interface: the player's
  controller reads the keys, and the **AI controller drives both the AI party members and the enemies** (the same
  system for both sides; the party battle options feed into it for allies). The battle system only ever asks a controller "where do
  you move, which arte do you use", so the real AI later replaces a simple placeholder without touching the rest.

## Teams (user, 2026-11-20)

- **Every fighter has a Team and an Original Team** (`Fighter.Team`, `Fighter.OriginalTeam`). The party starts on one
  team and the enemies on another, and those starting values are saved as the Original Team.
- **States can change a fighter's team** during the fight: Confused, Charmed and other effects that make your artes
  hit your own side. A fighter can switch to the **enemy** team, or to **neutral**, where they hurt anyone.
- **When the state ends, the fighter goes back to their Original Team.** The end of the battle resets it too.
- **A team change only changes how that fighter sees everyone else (user, 2026-11-20).** Nobody else knows: the
  others keep treating them by their **Original Team**, so nobody suddenly turns on them. A Charmed Rusk on the enemy
  team attacks his friends, but his friends' artes still pass over him as an ally, and the enemies still go after him.
  A neutral fighter hurts everyone, and everyone still treats them as they did before.
- So whether an arte hits someone is worked out from **the user's current team against the target's Original Team**,
  for arte scopes, contact projectiles, targeting and taunts alike.
- **When the leader's team is changed, the AI takes over the leader** for as long as the state lasts, and control
  goes back to the player when it ends. So states need an **"AI takes control"** option, and the battle system can
  swap a fighter's controller at any time (which the controller design already allows).
- **With two team-changing states, the newest wins** (user, 2026-11-20), and the fighter falls back to the other
  when it ends. **Victory only counts fighters who started as enemies.**

## Party battle options (user, 2026-11-20)

Options the player sets for each AI-controlled party member in the menu:

- **May use items in battle** (see Items).
- **Magic user:** tries to stay away from the middle of the fight. Enemy casters do this by default.
- **Flee casting enemies:** moves away from enemies that are casting.

## Open questions

- ~~Can a projectile detonate early by hitting something?~~ Answered by Detonate on Contact (2026-11-19).
- ~~Is an activated arte's `Cost`, `CastTime` and cooldown ignored?~~ Answered (user, 2026-11-23): yes. An arte activated
  from a projectile has no cost, no cast time and no cooldown.
- Concentration: does a hit on the user after the projectiles are fired do anything? The cast is already finished, so
  probably not. (Nothing in step 5 links them.)
- ~~Chains~~ Answered (user, 2026-11-23): an arte whose projectile activates another arte that fires projectiles of its
  own is a chain. The cap of 4 (`ArteChains.MaxDepth`) stays. Hitting it must never be silent: the Artes page shows a
  yellow warning over the Projectiles list when an arte's chain goes deeper than 4 (`ArtesPageViewModel.ShowsChainWarning`,
  using `ArteChains.IsCutOff`), and the battle writes a debug line when it cuts a chain off.
- ~~When the user falls, do their projectiles keep flying?~~ Answered (user, 2026-11-23): **no, they all disappear.**
  Killing a boss while it has a meteor storm in the air must never kill the party during the victory screen. Built:
  `BattleRunner.Fall` removes every projectile whose attacker is the fallen fighter. (This covers every projectile the fallen fighter fired, including ones fired by arte activations.)
- Should a projectile without contact detonation be stopped by walls? Undecided (user, 2026-11-23). Battle maps differ:
  an indoor map has real walls, an outdoor grass map has none, yet both have collision around the field. So walls can't
  be a global rule; it stays a per-projectile choice (its own Collide setting).
- ~~How do enemies get inspected so their Life bars show?~~ Answered (user, 2026-11-23), see "Inspecting enemies" below.
- ~~A random number of projectiles (like 10-12 meteors)?~~ Not needed (user, 2026-11-23): copy and paste projectiles in
  the arte's projectile list.

## TODO (user, 2026-11-24)

Parked on purpose, nothing here blocks the next step.

- **Game Over, later.** `GameOverSequence` just returns the party to the story map. The title screen, the last save and
  fading the music out on the Game Over screen come when the game has them. Don't work on it before then.
- **Fleeing needs player instructions.** Hold Shift to run is hard-coded and nothing on screen says so. It goes into
  the game's instructions or help text when that exists (no HUD hint). The party's per-member flee options and a way
  for a page to tell the party ran wait too.
- **Dropped:** the fallback to the framed icon when a No Background cell is empty (user, 2026-11-24), and a separate
  "Beneficial" state flag: a state's **Friendly** checkbox already is that.

## Enemy levels and arte learning (user, 2026-11-23)

- **Enemies get harder as the party levels.** Each enemy fights at the party's average level (the fighters in the
  battle, rounded), never below the Level set on the Enemies page, which works as its minimum.
- **Enemies grow like actors.** An enemy with a Discipline gets its Life, Mana and attributes from that discipline's
  growths at its battle level, exactly as an actor does (`PartyService.SetLevel`). Static Stats keeps the numbers typed on
  the Enemies page. The slimes share the **Slime** discipline (enemy growths, not a party class).
- **Enemies learn artes by level.** An enemy's conditional artes with a Level condition are known once its battle level
  reaches it (an actor's too, against its own level). In battle only Level conditions are checked; any other condition
  keeps an arte unknown. List the strongest first: the placeholder AI uses the first ready arte that would land, so the
  basic attack goes last.
- **The placeholder AI heals.** A ready healing arte (negative damage) goes first when an ally, or the user itself, is
  below 70% Life and inside its reach.
- **The slimes (2026-11-23, `Reference\install_slimes.py` in the game folder).** Every slime knows Slime Tackle (Lv 1).
  Each colour learns a spit down a 3-tile line at Lv 8, a burst all around it that adds its state at Lv 20 (a cast), and
  a pool 5 tiles across at Lv 35 (a longer cast): Green Earth and Poisoned, Blue Water and Soaked (Lightning +30%,
  Fire -20%), Red Fire and Burning (a Fire tick), Yellow Lightning and Stunned, Purple Dark and Hexed (Dark +30%). Pink is
  the healer: Sweet Spit, then Jelly Mend (heals around it) and Royal Jelly (heals a wide area and cures every ailment).
  Each colour resists its own element (half damage, Red a quarter) and is weak to one other (x1.5, Green x1.25).

## Inspecting enemies (user, 2026-11-23)

What the player knows about a normal enemy is earned, not given:

- **Scan:** a Scan arte or item reveals an enemy on the spot, the first time you meet that type.
- **Encounters:** defeating a set number of that enemy type reveals it for good. A normal enemy doesn't get a full
  bestiary entry or a Life bar until the player has either fought enough of them or used a scan on it.
- **Bosses** are always revealed.
- Revealed enemies fill in their bestiary entry (`InspectedEnemies` in the game state) and show their small Life bar in
  battle. This replaces the TEMP `BattleHud.ShowAllEnemyBars` once built.
- **Number of defeats (decided 2026-11-23):** a field on each enemy, so a rat can ask for 3 and a dragon for 10.
- **What a scan gives (decided 2026-11-23):** the Life bar and the basics only, not the whole entry.
- **How the bestiary fills in (decided 2026-11-23):** piece by piece as the defeat count climbs (name, then stats, then
  weaknesses, then drops), so every fight teaches something. Exactly which piece unlocks at which count is still to be
  worked out when this is built.
- **Bestiary pictures (user, 2026-11-23):** the images in the game's `Graphics\Enemies` folder are the bestiary
  pictures, one per enemy type (for example `Slime_Green.png`). In battle and on the maps, enemies use their walk sheet
  from `Graphics\Characters` (the enemy's `CharacterName`), not these pictures.

## Engine status

- `Arte.Projectiles` exists and is edited on the Artes page.
- `DetonateMovement` and `ActivateArteMovement` (with its `ArteId`) run in battle (step 5). On the story maps
  `MovementRouteSystem` still skips both.
- `Entity.DetonateOnContact` (`ContactTarget`: None, Enemies, Allies, Anyone), picked in the projectile editor's
  header, is used in battle (step 5).
- The projectile editor's missing-Detonate warning is built (`EntityEditorViewModel.ShowsDetonateWarning`), and so is
  the in-battle safety net (step 5).

## Battle HUD and screen layers (user, 2026-11-21)

- **Screen layers:** everything drawn over the game (the battle HUD, the Escape menu, shops) is a layer that says whether
  it **pauses** the game. The menu pauses; the battle HUD never does. They share one look
  (`GameEngine\UX\Resources\Interface.xaml`), so the HUD isn't much different from the menu, just a layer down and never
  pausing.
- **Party panels** along the bottom: face, name, Life and Mana, the leader first with a brighter edge.
- **Bosses** get one big Life bar across the top with their name.
- **Other enemies** get a small Life bar over their heads, **but only once the player has inspected that enemy** (its
  bestiary entry is filled in, `InspectedEnemies` in the game state). How enemies get inspected is still open (an
  inspect arte or item, or fighting a number of them). For now every bar shows (`BattleHud.ShowAllEnemyBars`, TEMP) so
  the look can be checked.

## Build log

- **Step 1, into battle and back (2026-11-21).** `InitiateBattleHandler` asks for the fight (`BattleRequest`) and the
  page waits (`PageRun.WaitsForBattle`: it lasts through the battle map and back and doesn't hold the player still).
  `MapScene` saves the story map, the player's exact spot and facing and their walking speed (`ActiveBattle`), fades
  out, and loads the troop's battle map with `BattleSpawner`'s lineup: up to 4 party members on the party spawn points
  (the leader is the player) and the enemies on their fixed spots or random points, feet on their tiles. Everyone
  walks at the story speed minus 1. **F9 is a test key that ends the battle**, and the player fades back to the exact
  spot with the saved speed put back. Not yet: transitions, troop music, fighters doing anything.
- **Step 2a, fighters and the HUD (2026-11-21).** `BattleFighter` for everyone in the fight (the party's own actors, so
  Life and Mana carry over; a fresh full copy of each enemy; Team, Original Team, Threat 0). `ScreenLayer` and
  `ScreenLayers`, with `BattleHud` as the first layer. Attributes from equipment are added up by `BattleAttributes.Value`.
- **Step 3a, using artes (2026-11-21).** `BattleRunner`: the leader's arte keys (1-6, Q-Y, A-H; slot 0 is the weapon's
  arte, then the actor's artes), Mana paid on use, the cast locks the caster (`BattleLockState`) and can't be
  cancelled, then the arte detonates at the user: its hitboxes from the user's feet tile, turned to the way they face.
  `HitCalculator` runs the hit order (hit roll, multiplier × Power, Defense ratio, elements, 3x crit, Variance) on an
  `AttackerSnapshot`. Damage numbers, Threat, falling (enemies fade out, the party dims), victory and defeat (both just
  end the battle for now; no game over yet). The HUD shows the leader's arte keys with a clock-wipe cooldown, red when
  short of Mana, and a cast bar. Artes have a **Cooldown** field (60 = 1 second, 0 for
  none) on the Artes page next to Cast Time; projectiles land at the user until step 5. Equipment element resistances are counted by `BattleAttributes.ElementRate` (own rate plus each piece's change from 100%; states add theirs later).
- **Step 4, controllers and a placeholder AI (2026-11-21).** `IFighterController` decides what a fighter does through
  `IBattleActions` (the battle runner), so the rules are the same for the player and the AI. `PlayerController` drives
  the leader (the arte keys; the arrow keys already walk the player). `PlaceholderAiController` drives everyone else,
  party members and enemies alike: a Threat-weighted target (re-rolled every ~2 s, switched at once when it falls),
  chases it, shuffles a little once close, and turns and uses its first ready arte whenever it would land. The real AI
  system replaces it later without touching the rest.
- **Debug: Show Arte Hitboxes (2026-11-21).** A test option in the editor's Test menu (`ShowArteHitboxes` in
  config.json). When an arte lands, its hitboxes flash in bright yellow for 20 updates (1/3 second) where they landed,
  whether they hit anyone or not (`DebugOverlay.FlashArteHitboxes`, called from `BattleRunner.Detonate`).
- **Step 5, projectiles (2026-11-22).** When the cast finishes, an arte with projectiles fires all of them and does
  nothing else (`BattleRunner.Release` → `Fire`), and one without detonates at the user as before.
  - **Firing:** each projectile is a fresh copy of the arte's projectile, put on the battle map with its feet on the
    user's feet tile, facing the way the user faces, on the user's level. It carries an `AttackerSnapshot` of the user
    taken at that moment (`BattleProjectile`).
  - **Routes:** it walks the top-level movement routes of its page, in order, driven by the battle runner. The page's
    actions are taken off the copy so the page runner never restarts them. Routes inside branches aren't used.
  - **Detonate step:** `MovementRouteSystem` notes it on the projectile's `ProjectileState` and ends the route. The
    battle runner then lands the arte at the projectile (its hitboxes from the projectile's feet tile, turned the way
    it faces), owned by the user, and removes it. Landing never fires projectiles.
  - **Activate Arte step:** the named arte lands at the projectile, owned by the user (no Cost, no cast, no cooldown),
    and the projectile keeps going. An activated arte with projectiles of its own fires them from there, up to 4 deep.
  - **Contact:** checked once an update after everyone has moved, against the fighters' feet hitboxes. The check
    covers everything between last update and this one, so a fast projectile can't skip anyone. It never goes off on
    the fighter who fired it, and it ignores anyone who's down. On contact it goes off at once with the full arte.
  - **Walls:** a contact projectile on a collision area of its level goes away empty.
  - **Safety net:** a projectile whose routes run out without a Detonate step goes off where it stopped. A contact
    projectile that never touched anyone only goes away, like at its Detonate step.
  - **Pictures:** projectile pictures are measured when the battle begins, so they show and stand on their tile from
    the first shot.
  - **Battle end:** the end of the battle removes any projectiles still flying.
  - **Placeholder AI:** an arte with projectiles counts as "would hit" when the target is in a tile-wide line, the way
    the fighter faces, as far as the projectile's Step Forward steps reach.
- **Step 6, states (2026-11-23).** `BattleState` is a state on a fighter (its stacks, time left, next tick, and the
  source with a snapshot of their numbers). `BattleStates` adds (resisted by equipment, bosses resisting stun-lock,
  ignored when already on and not stackable, stacked otherwise), removes, wears down, ticks, and clears them. A hit
  now goes: hit roll, remove states, add states, then damage (`HitCalculator.Resolve`'s `afterHit`), so an arte's own
  states count toward its damage. Falling clears every state and puts Incapacitated on (`Fall`), a revive takes it
  off, a fallen source ends the states that end with it, and a battle's end clears everything not marked to remain.
  State attribute and element changes feed `BattleAttributes`. A Tick Arte lands on the holder, owned by the source.
  The old placeholder fields HP/Mana Damage and Percent were removed from states (and Element Effect, because a state's damage element comes from its Tick Arte's Element Type, so weaknesses and resistances already apply to it). Not yet: state icons on the HUD (Hidden does nothing yet), states that remain after battle aren't saved onto actors,
  Duration is in updates for now, and none of the control states (stun, slow, taunt, fear) do anything yet because
  they need their own abilities.
- **Step 7a, winning (2026-11-23).** The moment the last enemy falls the battle is officially won (`BattleRunner.Win`):
  every state is cleared, every fallen party member on the field gets up with 1 Life, and the EXP of every enemy that
  was in the fight is added to every owned party member's `Experience`, benched ones included. Party members who were
  down at that moment get none. A "+N EXP" popup shows over each one on the field. Then the existing short pause runs
  and the party goes back to the story map. **Not built yet:** money and item drops on the field with their
  pickup (and the 5 second pause for them), and the victory screen.
- **Step 7b, levelling up (2026-11-23).** `Experience` is a running total, and the total needed for a level is
  `ExperienceBase * (level - 1) ^ ExperienceExponent` (defaults 20 and 2: level 2 takes 20, level 10 takes 1,620,
  level 99 takes 192,080). Both numbers are game settings, set beside the Level Cap on the Growths page with a preview
  of the totals. `PartyService.GainExperience` adds the EXP and raises the level once for every level the total now
  reaches (up to the cap), so a big gain can give several. Each level-up goes through `SetLevel` (growths set the new
  Life, Mana and attributes) and then recovers the actor's Life and Mana in full and clears every state on the fighter (states have no good/bad flag
  yet, so buffs go too, as do ones marked Remains After Battle). A "Level N!" popup shows over the
  fighter. The Actor Experience event command uses the same method, so it levels actors too.
- **Step 7c, drops and pickups (2026-11-23).** When an enemy falls, its money (`MonetaryValue`) and every drop whose
  percent roll comes up are laid down as pickups around where it fell (`BattleDrops`). A party member who is up and
  walks over one takes it (within 0.6 of a tile of their feet): money goes to the party's money, an item into the bag,
  and a popup shows over them. **Anything not picked up is lost.** After the last enemy falls the battle waits up to
  5 seconds (`PickupDelay`, 300 updates) and ends the moment everything is taken; with nothing on the map it ends after
  the usual short pause. Each pickup is drawn from the icon sheet's **No Background twin**
  (`Graphics\Icons\No Background\<same sheet name>`, same cell), picked automatically from the item's icon or the
  game's Money icon (Terms page); `IGraphicsService.GetDropSheetPath` falls back to the framed sheet only when a sheet
  has no twin. A cell that is empty on the twin shows nothing, so add the bare version when adding an item icon.
  The victory screen follows (step 7d).
- **Step 7d, the victory screen (2026-11-23).** When the pickup time is over (everything taken, or the 5 seconds up,
  and anything left is lost) the **victory screen** opens (`VictoryScreen`, a screen layer that pauses the game). It
  lists each party member with their face, level (`Level 3 → 4` and a "LEVEL UP!" note when they went up) and the EXP
  they got (a fallen member shows "Down: no EXP"), then the spoils: the money picked up and each item with its count.
  Enter or Space closes it (after about 0.75 seconds, so a key held from the fight can't skip it), and the battle ends
  then, returning to the story map. The rewards are gathered as they happen in `BattleRewards`. When it opens it plays
  the game's **Victory SFX** (renamed from Victory ME in step 9), chosen on the editor's Terms page (a file from
  `Media\SFX` with volume and pitch, saved as `VictorySfx` in `GameSettings.data`; none means silence). The face and icon cutting is shared with the battle HUD
  through `InterfaceImages`.
- **Step 8, defeat and game over (2026-11-24).** When every party member on the field is down and the enemies aren't,
  the party has lost.
  - **A win comes first.** If the last enemy and the last party member fall in the same update, the party won
    (`BattleRunner.CheckForEnd`). Once the party is lost it **stays lost**: a lingering poison that kills the last enemy
    afterwards can't turn it into a victory, so defeat never shows the Victory screen, gives no EXP and hands out no
    spoils. Whatever the fallen enemies had dropped is lost at once (`Lose`), and nobody can pick anything up.
  - **Can Lose.** A battle whose troop is marked **Can Lose** (Troops page) is never a game over. It lives only on the
    troop (user, 2026-11-24), like Can Run: the Initiate Battle command has neither box any more. After the usual short pause (`EndDelay`) it just ends: the fallen get up with 1 Life and the
    story carries on. This is the override for tutorials and scripted losses (the Shepherd fights, Osric and Garrick),
    and it needs no game-wide setting. (The troop's Can Lose box already existed; the runner now reads it.)
  - **Any other defeat** opens the **Game Over screen** (`GameOverScreen`, a screen layer like the Victory screen, view
    `GameOverScreenView`) after the same pause. It pauses the game, dims the field darker than Victory does, plays the
    game's **Defeat SFX** and closes on Enter or Space (also after about 0.75 seconds, so a held key can't skip it).
    Both screens now share `PromptScreen` for that.
  - **The game's choice (`GameSettings.Defeat`, `DefeatOutcome`)**, set on the Terms page in a new **Defeat** panel next
    to Victory: **Game Over** (back to the title or the last save) or **Retry** (the same battle again). A third
    choice, "continue the story anyway", was left out: Can Lose already does that per battle.
  - **Defeat SFX (`GameSettings.DefeatSfx`)** is chosen exactly like Victory SFX: a file from `Media\SFX` with volume and
    pitch, none means silence, with Play and Stop on the Terms page.
  - **Retry.** When the screen closes, the battle ends and the party goes back to the story map, where
    `BattleRunner.Remember` has kept what the fight began with: each party member's Life and Mana, the money and the
    bag, all put back (so nothing picked up before the wipe can be farmed twice). `ActiveBattle.Retry` makes `MapScene`
    ask for the same `BattleRequest` once the story map has faded in, so the fight starts again from the exact spot the
    first one started. The page that began it never notices: it waits through both fights.
  - **TEMP: Game Over has nowhere to go yet.** The engine has no title screen and no save and load, so after the
    screen closes the party is simply back on the story map with 1 Life, as the old defeat did. That is
    `GameOverSequence` (step 9), the one place to change when the title or the saves exist. The editor's Game Over page
    command (`GameOverAction`) has no handler either, and should end up doing the same thing.
  - **Not built:** scripted events that run when a battle is lost before it ends (see "Winning and losing"), and a way
    for a page to ask whether a Can Lose battle was won or lost afterwards.
- **Step 9, the end of a battle (user, 2026-11-24).**
  - **The pause always runs out.** After the last enemy falls the battle waits its whole pause: `PickupDelay` (300
    updates, 5 seconds) when anything dropped, the short `EndDelay` when nothing did. Picking up the last drop no longer
    ends it early (this replaces "ends the moment everything is taken" in step 7c). The length is fixed when the last
    enemy falls (`_endWait`).
  - **Victory and Game Over use a sound effect, not an ME.** An ME in this engine loops until stopped, which makes it
    music, not a jingle, so the two screen sounds are **SFX** files from `Media\SFX` (`VictorySfx`, `DefeatSfx`, chosen on
    the Terms page, which now says SFX). `PlayMe` is unchanged. A game that saved a Victory ME needs to pick its sound
    again. The `Cache.Me` list is no longer used by the Terms page.
  - **The Victory screen closes itself when its sound effect finishes** (`PromptScreen.ClosesItself`, the sound is
    `IAudioService.PlaySfxToEnd`, which finishes when the sound has played out or been stopped, or at once if it can't
    play). Enter or Space still closes it early (after the 0.75 second guard) and stops the sound. With no Victory SFX
    chosen it behaves as before and waits for a key, and its prompt says so. The Game Over screen still waits for a
    key, because it tells the player what happens next. Closing either screen calls `StopSfx`.
  - **Game Over has its own class.** `GameOverSequence.Play` is what runs when the Game Over screen is closed and the
    setting is Game Over. Today it holds a TODO and the one call that returns the party to the story map. Everything
    the real game over needs (title, last save, clearing state) is built there later.
  - **Default volume and pitch are game settings (user, 2026-11-24).** `GameSettings.DefaultVolume` (0.8) and
    `DefaultPitch` (1.0) are set in a new **Sound** panel on the Terms page. A new sound that nothing was set on starts
    there: every `AudioFile`, animation sound, Play Media action and route step, and the Media Player page read
    `AudioDefaults` (copied from the settings whenever they change or a game loads). Sounds already saved keep their own
    numbers. The audio service no longer has default values of its own: every call passes its volume and pitch.
  - **Battle music (user, 2026-11-24).** The troop's **Battle Music** (Troops page) plays on its battle map in place of
    the map's own BGM, which is never played (`MapAudio.Play` and `Leave` take `isBattle` and the troop's track), fading
    in with the battle like any map's music. A troop with none picked has silence, never the battle map's BGM (user,
    2026-11-24). The story map's music starts again from the top when the party returns.
  - **The music fades out for the victory (user, 2026-11-24).** The moment the last enemy falls (`BattleRunner.Win`), the
    battle music fades out over 6 seconds (`MapAudio.FadeOutBgm`), a little longer than the 5 second pickup pause, so the Victory SFX
    starts on silence. The game over screen doesn't fade it yet.
- **Step 10, fleeing (2026-11-24).** The design in "Fleeing" is built (`BattleRunner.UpdateFlee`).
  - **The key is Shift (hold).** Dashing is already off in battle, so Shift was free. It's hard-coded for now; key
    remapping is its own job later.
  - **Who can run:** a battle whose troop has **Can Run** ticked (Troops page). It lives only on the troop (user,
    2026-11-24): the Initiate Battle command no longer has a Can Run box, so it can't disagree with the troop. Only while
    the leader is on their feet: when the leader is down the player can only watch.
  - **The bar** (`ActiveBattle.FleeFraction`, drawn over the leader's head in red) fills in 3 seconds (`FleeUpdates`,
    180). Letting go empties it at once, with no cooldown on trying again.
  - **While it's held the party doesn't fight.** Nobody on the party's side uses an arte or decides anything (`CanUse`
    is false for them and their controllers don't run), a cast already under way still finishes, and the enemies carry
    on as usual. The leader can still walk, since walking is the input system's.
  - **A full bar can still fail** (`FleeFailChance`, 10%, a tuning value): "Couldn't escape!" pops up, the bar empties
    and the fight carries on (hold again to retry).
  - **A successful escape** pops up "Escaped!" and ends the battle (`End`): no EXP, no Victory screen, whatever lies on
    the floor is lost, anything already picked up is kept, and the fallen get up with 1 Life. The story map's music
    comes back with the map as it does after a win.
  - **Not built:** a hint on screen about the key, the party's per-member flee battle options, and a way for a page to
    tell the party ran.
- **Step 11, items in battle (user, 2026-11-24).** The design in "Items in battle" is built (`BattleRunner.UseItem`).
  - **Consumables have no arte** (a mistake in the earlier plan, corrected by the user): they carry their own effect
    (scope, hitboxes, flat and percent Life and Mana, Variance, states, caster and target animation), so an item lands
    through its own path (`LandItem` / `LandItemOn`), not the arte's `Detonate`.
  - **Five item keys, Z X C V B**, in a row under the arte keys on the HUD (`ActiveBattle.ItemSlots`). Until the menu can
    reassign them, they fill when the battle starts with the first five consumables in the bag that can be used in battle
    (Use Anywhere or Battle Only), in the Items list's order. Only the leader's keys are read for now: the party's "may
    use items" option comes with the real AI.
  - **Using one costs the item and nothing else.** No Mana, no cast time, no lock. One item leaves the bag and the
    party's **shared item cooldown** (`ActiveBattle.ItemCooldown`, 15 seconds, `ItemCooldownUpdates`) starts for every
    item. A fighter that is down, running, casting or stunned can't use one, and neither can anyone once the fight is won
    or lost.
  - **The HUD:** each filled box shows the item's icon, how many are left (in the corner) and the same clock wipe as an
    arte while the shared cooldown runs. Using the last of an item empties its box (the cooldown still applies).
  - **How it lands.** Its hitboxes (they don't turn with the user) from the user's feet tile, on whoever its scope lets it
    touch, the user included (Self touches only the user, Party everyone on the side). An item never misses, never
    crits, and ignores Power, Defense and elements: Life (and Mana) change by the flat amount plus the percent of the
    target's maximum, spread by Variance. **Negative restores, positive hurts** (a Firebomb is a positive 60).
    States go first (remove, remove negative, then add), so a revive brings the fallen up before healing them (with 1
    Life if nothing heals them). **Clear All States** also gets the fallen up (the Phoenix Feather).
  - **Not built:** Attribute Changes (permanent stat boosts) are menu-only and skipped in battle, the menu to reassign
    the keys (the HUD loads each box's icon when the battle begins, so reassigning will need to refresh it), the AI
    using items, and a Mana popup that uses the Mana icon.
- **Step 12, states outside battle (user, 2026-11-24).** A stim or salve used in the menu starts a state before the fight,
  and the user wants the time it spent to be lost (option 4): states keep a real clock outside battle, and this is
  also what "Remains After Battle" needed. (`PartyStates`, `CarriedState`, `IGameStateService.CarriedStates`.)
  - **The clock runs on the story map.** Each party member's states wear down one tick per update while the game runs
    (a menu or a message holds it). A state with a Tick Arte ticks too: with no battlefield, the arte's Life amount (its
    multiplier times the Power of whoever applied the state, against the holder's Defense for damage, spread by Variance)
    heals or hurts the holder, and a hurt never takes them below 1 Life. A reserve party member's clock keeps running
    on the story map and stops during a fight they aren't in.
  - **Into a fight:** every state a member carries goes in with what's left of its clock, stacks and ticking, whether
    or not it remains after battle.
  - **Out of a fight:** as before everything not marked Remains After Battle goes, and Incapacitated always does. What
    stays is carried back out with its remaining time and keeps running on the story map. A **retry** puts the carried
    states back as they were when the fight began, like Life and the bag.
  - **Putting one on from outside** is `PartyStates.Add` (and `Remove`, `RemoveNegative`), which the menu's Use Item
    will call. Resisting, stacking and the Incapacitated rule are the same as in battle (`BattleStates.Resists`). Who
    applied it is the holder unless someone else is given.
  - **Outside battle a state is only its clock and its tick.** Attribute changes, speed, stuns and animations only
    mean something in a fight. A state applied by an enemy keeps the enemy's numbers, and its source becomes the holder
    once the enemy is gone, and damage a fighter does to itself never builds Threat, so that costs nothing.
  - **Not built:** the menu that calls `Add`, showing carried states anywhere, and the tick animation outside battle.
- **Step 13, state icons and chances (user, 2026-11-24).**
  - **Icons:** every state that isn't Hidden shows its icon on its holder: over a party member's panel, over an enemy's
    Life bar, and under a boss's bar. The clock wipe is the arte keys' own and covers how much of its time is left; a
    permanent state has none. Stacks show in the corner. (`HudState`, `HudFighter.SetStates`.)
  - **Chance:** every state an arte or consumable adds has a percent chance of going on (`StateChances`, 100 when it
    isn't listed), set beside its tick box. It's rolled for each target, after resistances (`BattleStates.Add`). The
    menu's Use Item will need to roll it too, since `PartyStates.Add` doesn't.
  - **Test keys** (`BattleStateTester`, TEST): F5 picks the next state (Shift+F5 the one before), F6 puts it on the
    leader, F7 on every enemy, F8 takes every state off everyone. They skip the chance roll.
- **Step 14, silence and no items (user, 2026-11-24).** Two new checkboxes on a state, the only finer stun flags wanted
  (Can't Move and Can't Turn were turned down, and Stun-Lock with Can Still Use Artes stays as it is).
  - **Silence:** the holder can't use artes (`BattleStates.BlocksArtes`, so the player, the AI and a cast in progress all
    obey it). It can still walk, turn and use items, and bosses aren't immune (the stun-lock immunity is unchanged).
  - **No Items:** the holder can't use items (`BattleStates.BlocksItems`), and can do everything else.
  - **A plain stun still stops items too** (it stops everything), but a Stun-Lock state that lets the holder use artes (a
    root) no longer stops items, which fixes the old side effect of Step 11.
