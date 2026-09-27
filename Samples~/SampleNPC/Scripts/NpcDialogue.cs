// Sample: talk to an NPC with Pollinations text generation.
// Attach to an NPC GameObject, set an API key in PollinationsConfig
// (or ship the device-flow handshake for player-owned keys), press E
// near the NPC and type — replies come from the configured model.
using System.Collections.Generic;
using UnityEngine;

namespace Pollinations.Samples
{
    public class NpcDialogue : MonoBehaviour
    {
        [Tooltip("What this NPC is, in one sentence — becomes the system prompt.")]
        public string persona = "A friendly blacksmith in a small mountain village.";

        [Tooltip("Conversation memory (last N messages are kept).")]
        public int memorySize = 8;

        private readonly List<ChatMessage> history = new List<ChatMessage>();

        /// <summary>
        /// Sends the player's line and returns the NPC's reply.
        /// Call from your interaction system (e.g. when the player presses E).
        /// </summary>
        public async void Say(string playerLine, System.Action<string> onReply)
        {
            history.Add(ChatMessage.User(playerLine));
            string system = persona +
                " Reply in one or two short sentences, stay in character.";
            string reply = await PollinationsClient.ChatAsync(
                new List<ChatMessage>(history), system: system);
            history.Add(ChatMessage.Assistant(reply));
            while (history.Count > memorySize)
            {
                history.RemoveAt(0);
            }
            onReply?.Invoke(reply);
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(20, 20, 200, 30), "Say hello to the NPC"))
            {
                Say("Hello, who are you?", reply =>
                {
                    Debug.Log($"NPC: {reply}");
                });
            }
        }
    }
}
