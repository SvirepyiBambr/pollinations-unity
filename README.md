# Pollinations for Unity

Generate **text**, **images** and **speech** in your Unity game with your
own [Pollinations](https://pollinations.ai) account — every generation is
billed to the API key's own Pollen (BYOP), with the device-flow handshake
planned so players can pay with their own Pollen.

## Install (UPM)

Via Package Manager → *Add package from git URL*:

```
https://github.com/SvirepyiBambr/pollinations-unity.git
```

Requires Unity **2021.3+**. No dependencies — the runtime is a single
`UnityWebRequest`-based client with a tiny built-in JSON helper.

## Quick start

```csharp
using Pollinations;

// once at startup
PollinationsConfig.ApiKey = "polli_…"; // https://enter.pollinations.ai/keys

// text (any model from https://gen.pollinations.ai/text/models)
string reply = await PollinationsClient.GenerateTextAsync(
    "Write a two-sentence innkeeper greeting.", system: "You are a fantasy innkeeper.");

// chat with memory
var messages = new List<ChatMessage> { ChatMessage.User("Who are you?") };
string reply2 = await PollinationsClient.ChatAsync(messages);

// image (any model from https://gen.pollinations.ai/image/models)
Texture2D texture = await PollinationsClient.GenerateImageAsync(
    "a glowing sword on a stone altar", 512, 512);

// speech (https://gen.pollinations.ai/audio/models)
byte[] audio = await PollinationsClient.GenerateSpeechAsync("Welcome, traveler!");
// play it with AudioSource: WAV/MP3 decode is up to your audio pipeline
```

See `Samples~/SampleNPC` for a ready NPC dialogue component: give it a
`persona`, call `Say(playerLine, onReply)` from your interaction system,
and it keeps a rolling conversation memory per NPC.

## How players use their own Pollen (BYOP)

The billed account is whichever key the game presents, so never bundle a
shared secret key:

- **Development**: the author's own `sk_` key from
  <https://enter.pollinations.ai/keys>.
- **Shipped games**: let each player paste **their own** key in your
  settings UI (device-flow handshake per
  [BRING_YOUR_OWN_POLLEN](https://github.com/pollinations/pollinations/blob/main/BRING_YOUR_OWN_POLLEN.md#%EF%B8%8F-clis--headless-apps-device-flow)
  — open the auth URL, player approves, key goes into player prefs —
  is the no-embedding option the quest points to).

## Models

Every method takes an optional model override; defaults are configured in
`PollinationsConfig`. Browse the live lists:

- Text: https://gen.pollinations.ai/text/models
- Image: https://gen.pollinations.ai/image/models
- Speech: https://gen.pollinations.ai/audio/models

## Demo

1. Create an empty scene, add a plane + capsule, attach `NpcDialogue`.
2. Set `PollinationsConfig.ApiKey` in a bootstrap script.
3. Press Play → click **Say hello to the NPC** → the reply appears in the
   Console, e.g.:
   > NPC: Ah, a traveler! I'm Marla, the village blacksmith — need a blade
   > sharpened before the road takes you?

4. Swap `GenerateTextAsync` for `GenerateImageAsync` to drop a generated
   prop texture onto a `Material` at runtime.

## Install steps

1. *Window → Package Manager → + → Add package from git URL* (above), or
   add to `Packages/manifest.json`:
   ```json
   "ai.pollinations.unity": "https://github.com/SvirepyiBambr/pollinations-unity.git"
   ```
2. Import the sample via Package Manager → Pollinations → Samples.
3. Set your API key (or wire the device flow from
   [BRING_YOUR_OWN_POLLEN](https://github.com/pollinations/pollinations/blob/main/BRING_YOUR_OWN_POLLEN.md)).

## CI

`.github/workflows/ci.yml` syntax- and semantics-checks the runtime with
Mono's `mcs` against minimal UnityEngine stubs (kept in a hidden
`Stubs~` folder that Unity ignores), so every commit compiles.

## License

MIT
