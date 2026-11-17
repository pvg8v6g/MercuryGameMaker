# Dialogue System: Engine Design

Party conversations, influence and the talk board for Mercury Game Maker.

This is an engine feature, so nothing in it is specific to one game. Tale of Betrayal is the first game to use it,
and its settings and scripts live in the game folder:
- `C:\Users\philb\Documents\Tale Of Betrayal 1\Dialogue\PartyTalk.md` (the game's rules and numbers)
- `C:\Users\philb\Documents\Tale Of Betrayal 1\Dialogue\Rusk.md` (the first script, used as the test case)

Status: design only. No code yet.

---

## 1. Goals

1. **Talks can happen anywhere.** A talk can also be limited to particular places, such as camp, when the story
   needs it.
2. **Everything is optional.** A game that wants plain NPC text uses none of this. Influence is a separate module a
   game can switch off, and so are the notes, tiers and talk board.
3. **Reuse what the engine already has.** A conversation is made of the same `PageAction`s and `Condition`s as a map
   event, so a talk can do anything an event can (give items, start battles, set switches) and the editor reuses
   the same action and condition editors.
4. **Scale to hundreds of talks.** Talks live in their own database, not on map events.
5. **Two ways to write.** The editor, for building and tweaking, and script import, for writing big batches in plain
   text. After import, the editor's data is the source of truth.

## 2. The pieces

```
GameLibrary (data, shared by editor and engine)
  Conversation              a talk: settings + a list of PageActions
  RelationshipSettings      optional: who has influence, ranges, tiers
  new Conditions            Influence, Tier, Seen, Location, Chapter-style comparisons
  new PageActions           Line, Choice, Change Influence, Play Conversation, Set Seen

GameEngine (runtime)
  ActionRunner              runs any PageAction list one step at a time (shared with map events)
  ConversationService       plays one conversation
  InfluenceService          reads and changes influence, fires notes
  TalkBoardService          decides what each character has ready to say right now
  UI                        text window, choice window, influence note, talk menu, camp balloons

GameMaker (editor)
  Conversations page        list + node tree editor
  Relationships page        influence setup per actor
  Script import             reads .md talk scripts into Conversations
```

**Prerequisite:** the engine doesn't run `PageAction` lists yet (map events pick a page, but nothing executes its
actions). The `ActionRunner` in section 5 is needed for map events anyway, and the dialogue system is built on it.
Building it first gives both features at once.

---

## 3. Data (GameLibrary)

All data is JSON, one `.data` file per record, the same as Items and Fighters.

| Data | Game folder path |
|---|---|
| Conversations | `Conversations\<guid>.data` |
| Relationship settings | `Settings\Relationships.data` |
| Location tags | on each map (`GameMap`) and map area (`MapArea`) |

### 3.1 Conversation

`Conversation : BaseModel` (so it has Guid, Id, Name, Icon).

| Property | Type | Meaning |
|---|---|---|
| `Key` | string | Unique readable id from the script, e.g. `rusk_05_pits`. Used by scripts, flags and Play Conversation. |
| `Title` | string | Shown in the talk log. |
| `Kind` | `ConversationKind` | Story, Quest, Influence, Banter, Scene, Idle. Used for sorting and UI only; it doesn't change behaviour. |
| `Owner` | Guid? (actor) | The character this talk belongs to. Talk board uses it. Empty for scene conversations. |
| `Participants` | Guid list (actors) | Everyone who must be in the active party. The owner is always included. |
| `Conditions` | `Condition` list | All must pass for the talk to be available ("when:"). |
| `ExpiryConditions` | `Condition` list | If any passes, the talk is gone for good ("until:"). Checked before Conditions. |
| `Where` | `LocationRule` | Anywhere (default), or only/never at certain location tags. See 3.4. |
| `Requirement` | `Condition` list | The influence gate ("needs:"). Separate from Conditions so the board can tell "not unlocked yet" (hidden) from "unlocked but they won't talk to you" (refused). |
| `RefuseActions` | `PageAction` list | What plays when Conditions pass but Requirement fails ("refuse:"). Empty means the talk stays hidden instead. |
| `Priority` | int | Higher plays first when one character has several talks ready. |
| `Repeatable` | bool | Default false. Seen talks don't come back. |
| `Actions` | `PageAction` list | The talk itself. |

**Seen tracking:** when a conversation finishes, the engine records its Key in the save (`SeenConversations`). A
talk that ends partway (game closed mid-talk) is not marked seen.

### 3.2 New PageActions

The talk body uses existing actions (`BranchAction` already has nested `BranchActions` and `ElseActions`, so
`if:` blocks need nothing new) plus these:

| Action | Fields | Why it's needed |
|---|---|---|
| `ShowLineAction` | `Speaker` (actor Guid or free name), `Face` (face set + index, or an expression name), `Text`, `Portrait side` | `ShowTextAction` works but makes the writer pick a face file and index. A line knows its speaker, so it can find the face by expression name ("eyeroll") from the actor's face list. |
| `ConversationChoiceAction` | list of `ConversationOption` | `ShowChoicesAction` only stores an index in a variable, and then every option needs its own Branch. For writing, each option should carry its own lines. |
| `ConversationOption` | `Text`, `ShowConditions` (Condition list), `Effects` (influence changes), `Actions` (PageAction list) | One option: what it says, when it appears, what it changes, what follows. |
| `ChangeInfluenceAction` | `Actor`, `Amount` (+/-), `ShowNote` (bool) | Also usable from any map event, e.g. a story scene. |
| `SetInfluenceAction` | `Actor`, `Mode` (set / at least / at most), `Value` | Story events like "Rusk -40, not below 5" or "Garrick reset to 0". |
| `PlayConversationAction` | `Conversation` (Key), or `FirstMatching` (list of Keys) | Lets cutscenes play conversations. `FirstMatching` plays the first one whose conditions pass, for scene payoffs like Rusk's four versions of his last words. |
| `RandomLineAction` | list of `ShowLineAction`, optional Condition per line | Idle lines. |

### 3.3 New Conditions

The `Condition` list is polymorphic (`@type`), so these slot in beside the existing ones and work on map event
pages too.

| Condition | Fields | Example |
|---|---|---|
| `InfluenceCondition` | `Actor`, `Comparison`, `Value` | Rusk >= 40 |
| `TierCondition` | `Actor`, `Comparison`, `Tier` | Rusk at Blood Brothers or above |
| `SeenCondition` | `Key`, `Seen` (bool) | seen rusk_05_pits |
| `LocationCondition` | `Tag`, `Is` (bool) | at camp |
| `VariableCondition` (change) | add `Comparison` (=, !=, <, <=, >, >=) | chapter >= 5 |

`VariableCondition` only does "equals" today. Adding a `Comparison` (defaulting to equals, so existing data loads
unchanged) is needed for chapter ranges, and is useful well beyond dialogue. Variables are stored as strings, so
comparisons parse both sides as numbers and fall back to text comparison.

**The existing `Comparison` enum** (used by `BranchAction` for timers) should be reused for all of these.

### 3.4 Location tags (talks anywhere, sometimes camp only)

Each map and map area gets a `Tags` list of free text: `camp`, `town`, `dungeon`, `cortel`, `capital_camp`. A game
defines whatever tags it wants.

- The **current location tags** are the active map's tags plus the tags of any area the player is standing in.
- A game can also add tags at runtime with a new `LocationTagAction` (add/remove), for a "make camp" command that
  turns any spot into a camp.
- `LocationRule` on a conversation: `Anywhere`, `OnlyAt` (any of these tags), or `NeverAt` (none of these tags).

So most talks are `Anywhere`, and the ones a story path needs at camp are `OnlyAt: camp`. Rusk's cradle talk works
on the road or at camp, but a game could insist on camp.

**Blocking talks entirely:** a map or area tagged `no_talk` (name set in Relationship settings) hides the talk menu
and balloons, for cutscenes, boss rooms and similar. A switch can do the same.

### 3.5 Relationship settings (optional module)

`Settings\Relationships.data`. If there are no entries, influence is off and none of its UI appears.

| Property | Meaning |
|---|---|
| `Enabled` | Master switch. |
| `Entries` | One `RelationshipEntry` per actor that has influence. |
| `ShowNotes` | Show "Rusk approves." when influence changes. |
| `NoteTexts` | Wording for up and down, with `{name}`. Defaults "{name} approves." / "{name} disapproves." |
| `ShowTierInMenu` | Show each actor's tier name in the party menu. |
| `ShowNumbers` | Show the raw number too (off by default, useful for debugging). |
| `OneNewTalkAtATime` | Talk board pacing rule (section 6). |
| `NoTalkTag` | The location tag that blocks talking. Default `no_talk`. |

`RelationshipEntry`:

| Property | Meaning |
|---|---|
| `Actor` | Guid of the actor. |
| `Min`, `Max`, `Start` | Range and starting value. Tale of Betrayal uses 0-100. |
| `Tiers` | Ordered list of `{ Name, From }`. A value belongs to the highest tier whose `From` it reaches. |
| `BurnAt` | Optional. If influence ever reaches this value (Tale of Betrayal: 0), set the burn switch. |
| `BurnSwitch` | Switch name, e.g. `rusk_burned`. Stays on even if influence recovers. |
| `TierSets` | Optional extra tier lists, switched by a switch. For Garrick's Act 2 / after-betrayal names. |

Burned bridges are just a switch, so quest talks check it with an ordinary `SwitchCondition`. The engine doesn't
need to know what "burned" means.

### 3.6 Save data

Added to the save file (and to `IGameStateService` at runtime):

| Field | Type |
|---|---|
| `Influence` | `Dictionary<Guid, int>` |
| `SeenConversations` | `HashSet<string>` |
| `LocationTagsAdded` | `HashSet<string>` (runtime tags from `LocationTagAction`) |
| `TalkLog` | ordered list of seen conversation Keys, for an optional "past talks" menu |

---

## 4. How a talk becomes available

For each conversation, in order:

1. **Expired?** If `SeenConversations` has it and it isn't Repeatable, or any `ExpiryCondition` passes, it's gone.
2. **Unlocked?** All `Conditions` pass, every `Participant` is in the active party, and the `Where` rule matches the
   current location tags. If not, it's hidden (it may unlock later).
3. **Allowed?** All `Requirement` conditions pass. If they do, the talk is **Ready**. If not, it's **Refused**
   (when it has RefuseActions) or hidden (when it doesn't).

Ready and Refused both show the character as having something to talk about; Refused just plays the refuse line
instead. That's how the player learns they're locked out.

## 5. Running a talk (GameEngine)

### ActionRunner

Runs a `PageAction` list one action at a time. Some actions finish instantly (Set Switch, Change Influence); others
wait (Show Line waits for the confirm key, Choice waits for a pick, Battle waits for the battle to end).

- One runner instance per running list, with a stack for nested lists (branches, choice options).
- While a conversation runs, the map is paused, the same way the existing `RunsWhilePaused` systems expect.
- Map event pages and conversations both use it. This is the shared piece.

### ConversationService

- `Play(conversation)`: runs `Actions` through the ActionRunner. On finish, marks seen, adds to the talk log.
- `PlayRefusal(conversation)`: runs `RefuseActions`. Doesn't mark anything seen.
- `PlayFirstMatching(keys)`: for scene payoffs.

### InfluenceService

- `Get(actor)`, `Change(actor, amount, showNote)`, `Set(actor, mode, value)`, `Tier(actor)`.
- Clamps to Min/Max. Checks `BurnAt` after every change. Raises an `InfluenceChanged` event the UI listens to for
  notes.

### UI

| Piece | Notes |
|---|---|
| Text window | Speaker name, face (left or right), text with a typing effect, confirm to continue. Shared with map events. |
| Choice window | Options from `ConversationChoiceAction`, hiding options whose `ShowConditions` fail. |
| Influence note | Small, in a corner, fades after a couple of seconds. Stacks if several change at once. |
| Talk menu | A menu entry listing party members, with a marker on anyone Ready or Refused. Picking one asks the talk board what to play. |
| Field balloons | Party followers (or camp NPC stand-ins) show a speech balloon when they have a Ready or Refused talk. Uses the existing balloon icons. |

---

## 6. The talk board (TalkBoardService)

Answers one question: **"The player wants to talk to this character. What plays?"**

1. Collect every conversation whose `Owner` is this character and which is Ready or Refused (section 4).
2. Highest `Priority` first; ties go to the lowest Id (the order they were written in).
3. If one is Ready, play it. If only Refused ones exist, play the top one's refusal.
4. If nothing, play the character's Idle conversation (Kind = Idle, which can use RandomLineAction and tier
   conditions).

**Pacing (`OneNewTalkAtATime`):** when on, a character only offers one new Ready talk until the player either sees
it or the board refreshes. The board refreshes on: resting, a chapter variable change, a map change, or a
`RefreshTalkBoardAction`. Without this, a player who ignored Rusk for three chapters would get a backlog all at once.

**When the board is recalculated:** after any switch, variable, influence, party or location change. It's cheap
(conditions only), so recalculating per change is fine; it shouldn't run every frame.

**Debug view:** in a debug build, a panel lists every conversation for a character with its state (Expired, Hidden,
Refused, Ready) and which condition failed. This will matter: with hundreds of talks, "why isn't this showing up?"
will be the most common question.

---

## 7. Editor (GameMaker)

### Conversations page

- **Left:** list of conversations, filterable by owner, kind and text search. Grouped by owner.
- **Top right:** the settings from 3.1 (key, title, kind, owner, participants, where, priority, repeatable), with
  the existing condition editor for Conditions, Expiry and Requirement.
- **Bottom right:** the action list, using the existing event action editor, plus editors for the new actions.
  Lines show as `Rusk [eyeroll]: You said it was a small cave.` so a talk reads like the script.
- **Preview:** plays the conversation in a small window with a panel to fake influence, switches and variables,
  without running the game.

### Relationships page

The settings from 3.5: the actor list, each actor's range, start value, tiers and burn rule.

### Actor face names

`ShowLineAction` finds faces by expression name, so each actor needs a list of face names in order (normal, happy,
...). The face sheets already follow this order (`<name>_face_<n>_<expression>.png`), so the editor can read the
names from the face set, or the Actor page can store them.

### Script import

`Import Script...` on the Conversations page reads a Markdown (`.md`) talk file in the format from `PartyTalk.md` section 5.

- Each `== key ==` block becomes a Conversation. If one with the same Key exists, the importer shows a diff and asks
  before replacing it.
- Unknown faces, actors, flags or keys are listed as warnings, not errors, so a half-finished script still imports.
- Flags in `set:` become `FlowAction` switch changes. `{Rusk +3}` becomes `ChangeInfluenceAction`. `if` blocks
  become `BranchAction`s.
- Lines in a script that aren't inside a talk (notes, the payoff sections) are ignored unless they're in a
  `-- if ... --` payoff block, which becomes a Scene conversation.

**Export Script** does the reverse, so a talk edited in the editor can be sent back out for rewriting. Export
should exist, but the editor's data is the source of truth once imported.

---

## 8. Build order

Each step ends with something playable, using Rusk's script as the test.

| Step | Build | Test |
|---|---|---|
| 1 | ActionRunner, text window, `ShowLineAction`, `ConversationChoiceAction`, Conversation data, `PlayConversationAction` | A map event plays `rusk_01_crab` |
| 2 | `VariableCondition` comparisons, `SeenCondition`, save data | `rusk_04_crossing` plays its "I said tomorrow" line only after `rusk_03_tomorrow` |
| 3 | Relationship settings, InfluenceService, influence actions and conditions, notes | `rusk_02_ships` choices move Rusk's influence; `rusk_07_cradle` extra line appears at 60+ |
| 4 | Location tags, TalkBoardService, talk menu, balloons, refusals, burn switch | Rusk's quest talks lock at 20/30/40, refuse, and burn at 0 |
| 5 | Editor: Conversations page, Relationships page, preview | Build a talk by hand in the editor |
| 6 | Script import (and export) | Import all of `Rusk.md` with no warnings |

## 9. Open questions

- **Camp:** a real camp scene (party sitting around a fire) versus "camp" being just a tag on rest spots. The design
  supports both; the scene itself is a separate feature.
- **Banter:** talks between party members that start on their own while walking. Likely a second board mode
  (automatic, chance based, no player choice), designed after the basic board works.
- **Voice:** whether lines will ever have voice clips. `ShowLineAction` can take an optional audio file later
  without changing anything else.
- **Localization:** text is stored inline for now. If the engine is sold, translatable text tables may be wanted;
  the Key on each conversation plus a line index would be enough to build that later.
- **Tavren speaking on his own:** a story question, not an engine one. Fixed lines and choice options are both
  supported either way.
