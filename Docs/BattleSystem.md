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

- Can a projectile detonate early by hitting something (an enemy, a wall), or only on its Detonate step?
- When a projectile activates another arte, is that arte's `Cost` ignored? It should be, since the user already paid for the arte that fired it.
- Does an activated arte's own `CastTime` apply? It probably shouldn't, since it isn't being cast.
- Concentration: does a hit on the user after the projectiles are fired do anything? The cast is already finished, so probably not.

## Engine status

- `Arte.Projectiles` exists and is edited on the Artes page.
- `DetonateMovement` and `ActivateArteMovement` (with its `ArteId`) exist in move routes, but `MovementRouteSystem` skips both for now.
- The projectile setting is built: `Entity.DetonateOnContact` (`ContactTarget`: None, Enemies, Allies, Anyone), picked in the projectile editor's header. The engine doesn't use it yet.
- The projectile editor's missing-Detonate warning is built (`EntityEditorViewModel.ShowsDetonateWarning`). The in-battle safety net is part of the battle system, so it isn't built yet.

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
  `ScreenLayers`, with `BattleHud` as the first layer. Attributes from equipment aren't added up yet (growths only).
- **Step 3a, using artes (2026-11-21).** `BattleRunner`: the leader's arte keys (1-6, Q-Y, A-H; slot 0 is the weapon's
  arte, then the actor's artes), Mana paid on use, the cast locks the caster (`BattleLockState`) and can't be
  cancelled, then the arte detonates at the user: its hitboxes from the user's feet tile, turned to the way they face.
  `HitCalculator` runs the hit order (hit roll, multiplier × Power, Defense ratio, elements, 3x crit, Variance) on an
  `AttackerSnapshot`. Damage numbers, Threat, falling (enemies fade out, the party dims), victory and defeat (both just
  end the battle for now; no game over yet). The HUD shows the leader's arte keys with a clock-wipe cooldown, red when
  short of Mana, and a cast bar. Artes have a **Cooldown** field (60 = 1 second, 0 for
  none) on the Artes page next to Cast Time; projectiles land at the user until step 5; equipment element resistances aren't counted yet.
- **Step 4, controllers and a placeholder AI (2026-11-21).** `IFighterController` decides what a fighter does through
  `IBattleActions` (the battle runner), so the rules are the same for the player and the AI. `PlayerController` drives
  the leader (the arte keys; the arrow keys already walk the player). `PlaceholderAiController` drives everyone else,
  party members and enemies alike: a Threat-weighted target (re-rolled every ~2 s, switched at once when it falls),
  chases it, shuffles a little once close, and turns and uses its first ready arte whenever it would land. The real AI
  system replaces it later without touching the rest.
- **Debug: Show Arte Hitboxes (2026-11-21).** A test option in the editor's Test menu (`ShowArteHitboxes` in
  config.json). When an arte lands, its hitboxes flash in bright yellow for 20 updates (1/3 second) where they landed,
  whether they hit anyone or not (`DebugOverlay.FlashArteHitboxes`, called from `BattleRunner.Detonate`).
