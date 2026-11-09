using System;
using System.Collections.Generic;
using System.IO;
using APlaceAmongLords.Data;
using UnityEngine;

namespace APlaceAmongLords.Core
{
    /// <summary>Temporary prototype interface for validating decisions and consequences.</summary>
    public sealed class GameLoopController : MonoBehaviour
    {
        private const int AttributePointBudget = 8;
        // IMGUI dimensions in pixels. Adjust these values to resize the panels.
        private const float RoomMinWidth = 320f;
        private const float RoomMaxWidth = 850f;
        private const float RoomReservedScreenWidth = 520f;
        private const float RoomWidthScale = 1.5f;
        private const float RoomHeight = 360f;
        private const float StatusPanelWidth = 630f;
        private const float ConsultationWidth = 585f;
        private const float ConsultationMinHeight = 150f;
        private GameState state;
        private readonly Dictionary<int, GameEventDefinition> eventsByRound = new Dictionary<int, GameEventDefinition>();
        private RoomConsultationContent roomConsultationContent;
        private readonly Dictionary<string, DialogueDefinition> dialogues = new Dictionary<string, DialogueDefinition>();
        private readonly Dictionary<string, string> dialogueReplies = new Dictionary<string, string>();
        private int diplomacy;
        private int warfare;
        private int management;
        private int combat;
        private Vector2 scrollPosition;
        private string selectedConversation = "Context";
        private string notice = "Assign 8 points to your attributes to begin.";

        private void Awake()
        {
            state = new GameState();
            GameEventDefinition[] loadedEvents = Resources.LoadAll<GameEventDefinition>("Data/Events");
            HashSet<string> eventIds = new HashSet<string>();
            foreach (GameEventDefinition gameEvent in loadedEvents)
            {
                if (gameEvent == null || gameEvent.roundNumber < 1 || gameEvent.roundNumber > GameState.TotalRounds || string.IsNullOrEmpty(gameEvent.eventId))
                {
                    Debug.LogError("An event asset has an invalid round number or event ID.");
                    continue;
                }
                if (!ValidateEventAsset(gameEvent) || !eventIds.Add(gameEvent.eventId))
                {
                    Debug.LogError("Event " + gameEvent.eventId + " has invalid or duplicate configuration.", gameEvent);
                    continue;
                }
                if (eventsByRound.ContainsKey(gameEvent.roundNumber))
                {
                    Debug.LogError("More than one event is configured for round " + gameEvent.roundNumber + ".");
                    continue;
                }
                eventsByRound.Add(gameEvent.roundNumber, gameEvent);
            }
            roomConsultationContent = Resources.Load<RoomConsultationContent>("Data/RoomConsultationContent");
            DialogueDefinition[] loadedDialogues = Resources.LoadAll<DialogueDefinition>("Data/Conversations");
            foreach (DialogueDefinition dialogue in loadedDialogues)
            {
                if (dialogue == null || dialogue.roundNumber < 1 || string.IsNullOrEmpty(dialogue.conversationKey))
                    continue;

                string lookupKey = DialogueLookupKey(dialogue.roundNumber, dialogue.conversationKey);
                if (dialogues.ContainsKey(lookupKey))
                {
                    Debug.LogError("More than one dialogue is configured for " + lookupKey + ".");
                    continue;
                }

                dialogues.Add(lookupKey, dialogue);
            }
        }

        private bool ValidateEventAsset(GameEventDefinition gameEvent)
        {
            if (gameEvent.stages == null || gameEvent.stages.Count == 0 || gameEvent.choices == null || gameEvent.choices.Count == 0)
                return false;

            HashSet<string> stageIds = new HashSet<string>();
            foreach (GameEventStage stage in gameEvent.stages)
            {
                if (stage == null || string.IsNullOrEmpty(stage.stageId) || stage.minimumSelections < 1 ||
                    !Enum.IsDefined(typeof(EventSelectionMode), stage.selectionMode) || !stageIds.Add(stage.stageId))
                    return false;
            }

            HashSet<string> choiceIds = new HashSet<string>();
            foreach (GameChoiceDefinition choice in gameEvent.choices)
            {
                if (choice == null || string.IsNullOrEmpty(choice.choiceId) || string.IsNullOrEmpty(choice.buttonLabel) ||
                    !stageIds.Contains(choice.stageId) || !choiceIds.Add(choice.choiceId))
                    return false;
            }

            foreach (GameEventStage stage in gameEvent.stages)
            {
                int stageChoiceCount = 0;
                foreach (GameChoiceDefinition choice in gameEvent.choices)
                    if (choice.stageId == stage.stageId) stageChoiceCount++;
                if (stage.minimumSelections > stageChoiceCount ||
                    (stage.selectionMode == EventSelectionMode.SingleChoice && stage.minimumSelections != 1))
                    return false;
            }
            return true;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height), GUI.skin.box);
            ConfigureTextSizes();
            GUILayout.Label("A PLACE AMONG LORDS", TitleStyle());
            GUILayout.Space(8);

            if (!state.Attributes.IsAllocated)
            {
                DrawAttributeSetup();
            }
            else
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                DrawGameState();
                GUILayout.EndScrollView();
            }

            GUILayout.Space(8);
            GUILayout.Label(notice, GUI.skin.box);
            GUILayout.EndArea();
        }

        private void DrawAttributeSetup()
        {
            if (File.Exists(SavePath) && GUILayout.Button("Load Saved Game", GUILayout.Height(34)))
                LoadGame();

            GUILayout.Label("Allocate your attributes");
            GUILayout.Label("Points remaining: " + (AttributePointBudget - TotalAllocated()));
            DrawAttributeRow("Diplomacy", ref diplomacy);
            DrawAttributeRow("Warfare", ref warfare);
            DrawAttributeRow("Management", ref management);
            DrawAttributeRow("Combat", ref combat);

            GUI.enabled = TotalAllocated() == AttributePointBudget;
            if (GUILayout.Button("Begin the first round", GUILayout.Height(36)))
            {
                if (state.Attributes.TryAllocate(diplomacy, warfare, management, combat, AttributePointBudget))
                    notice = "The succession begins. Read the letters and speak with your advisors.";
            }
            GUI.enabled = true;
        }

        private void DrawAttributeRow(string label, ref int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(230));
            if (GUILayout.Button("−", GUILayout.Width(36)) && value > 0) value--;
            GUILayout.Label(value.ToString(), GUILayout.Width(36));
            if (GUILayout.Button("+", GUILayout.Width(36)) && TotalAllocated() < AttributePointBudget) value++;
            GUILayout.EndHorizontal();
        }

        private int TotalAllocated()
        {
            return diplomacy + warfare + management + combat;
        }

        private void DrawGameState()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save Game", GUILayout.Width(140))) SaveGame();
            if (GUILayout.Button("Load Game", GUILayout.Width(140))) LoadGame();
            GUILayout.EndHorizontal();
            GUILayout.Label("Round " + state.CurrentRound + " of " + GameState.TotalRounds, TitleStyle());
            GUILayout.Label(RoundTitle(state.CurrentRound));
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            DrawRoom();
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(StatusPanelWidth));
            GUILayout.Label("Domain Status", TitleStyle());
            DrawStatus();
            GUILayout.Space(8);
            GUILayout.Label("Consultation", TitleStyle());
            GUILayout.Label(ConversationText(), ConsultationStyle(), GUILayout.Width(ConsultationWidth), GUILayout.MinHeight(ConsultationMinHeight));
            DrawConversationResponses();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            DrawCurrentRound();
        }

        private void DrawRoom()
        {
            float roomWidth = Mathf.Max(RoomMinWidth, Mathf.Min(RoomMaxWidth, Screen.width - RoomReservedScreenWidth)) * RoomWidthScale;
            Rect room = GUILayoutUtility.GetRect(roomWidth, RoomHeight, GUILayout.Width(roomWidth), GUILayout.Height(RoomHeight));
            GUI.Box(room, "");
            GUI.Label(new Rect(room.x + 12, room.y + 8, room.width - 24, 28), "MAIN HALL — MONDNA CASTLE", SectionStyle());
            GUI.Box(new Rect(room.x + room.width * 0.34f, room.y + 42, room.width * 0.32f, 42), "View of the Mondna Lands");
            GUI.Box(new Rect(room.x + room.width * 0.29f, room.y + 88, room.width * 0.42f, 34), "Council Table");

            List<string> people = new List<string> { "Mother", "Steward", "General" };
            if (state.CurrentRound == 2) people.Add("Rebel Messenger");
            GameEventDefinition refugeeEvent = GetEvent(4);
            if (state.CurrentRound == 4 || (state.CurrentRound == 5 && refugeeEvent != null && !state.HasChoice(refugeeEvent, "refuse_entry"))) people.Add("Refugee Noble");
            if (state.CurrentRound == 5)
            {
                people.Add("Crown Messenger");
                people.Add("Rebel Envoy");
            }

            float margin = 14f;
            float gap = 7f;
            float cellWidth = (room.width - margin * 2f - gap * 2f) / 3f;
            for (int i = 0; i < people.Count; i++)
            {
                int row = i / 3;
                int column = i % 3;
                Rect button = new Rect(room.x + margin + column * (cellWidth + gap), room.y + 132 + row * 68, cellWidth, 60);
                DrawRoomButton(button, people[i], people[i]);
            }

            float objectY = room.y + 282;
            float objectWidth = (room.width - margin * 2f - gap * 2f) / 3f;
            DrawRoomButton(new Rect(room.x + margin, objectY, objectWidth, 54), "Lands Map", "Lands Map");
            if (state.CurrentRound == 1)
            {
                DrawRoomButton(new Rect(room.x + margin + objectWidth + gap, objectY, objectWidth, 54), "Crown Letter", "Crown Letter");
                DrawRoomButton(new Rect(room.x + margin + (objectWidth + gap) * 2f, objectY, objectWidth, 54), "Rebel Letter", "Rebel Letter");
            }
            else
            {
                DrawRoomButton(new Rect(room.x + margin + objectWidth + gap, objectY, objectWidth, 54), "Records Book", "Records Book");
                GUI.Box(new Rect(room.x + margin + (objectWidth + gap) * 2f, objectY, objectWidth, 54), "Hearth");
            }
        }

        private void DrawRoomButton(Rect rect, string topic, string label)
        {
            if (GUI.Button(rect, label))
                selectedConversation = topic;
        }

        private void DrawConversationResponses()
        {
            DialogueDefinition dialogue;
            if (!TryGetSelectedDialogue(out dialogue))
                return;

            DrawAuthoredDialogueResponses(dialogue);
        }

        private void DrawAuthoredDialogueResponses(DialogueDefinition dialogue)
        {
            if (state.HasRespondedTo(dialogue.relationshipTarget))
                return;

            GUILayout.Label("Choose how to respond to " + dialogue.speakerName + ":");
            foreach (DialogueResponse response in dialogue.responses)
            {
                if (!GUILayout.Button(response.playerResponse, GUILayout.Height(38)))
                    continue;

                if (!state.RespondToCharacter(dialogue.relationshipTarget, response.relationshipChange))
                    continue;

                dialogueReplies[dialogue.dialogueId] = response.characterReply;
                if (response.relationshipChange > 0)
                    notice = "Your response improved your relationship with " + dialogue.speakerName + ". Relationship: " + Relation(dialogue.relationshipTarget) + ".";
                else if (response.relationshipChange < 0)
                    notice = "Your response worsened your relationship with " + dialogue.speakerName + ". Relationship: " + Relation(dialogue.relationshipTarget) + ".";
                else
                    notice = "The conversation continued without changing your relationship with " + dialogue.speakerName + ".";
            }
        }
        private bool TryGetSelectedDialogue(out DialogueDefinition dialogue)
        {
            string conversationKey;
            switch (selectedConversation)
            {
                case "Mother": conversationKey = "mother"; break;
                case "Steward": conversationKey = "administrator"; break;
                case "General": conversationKey = "general"; break;
                case "Refugee Noble": conversationKey = "refugee_noble"; break;
                case "Crown Letter":
                    conversationKey = "crown_letter"; break;
                case "Crown Messenger": conversationKey = "crown_messenger"; break;
                case "Rebel Letter":
                    conversationKey = "rebels_letter"; break;
                case "Rebel Messenger": conversationKey = "rebel_messenger"; break;
                case "Rebel Envoy": conversationKey = "rebel_emissary"; break;
                default:
                    dialogue = null;
                    return false;
            }

            return dialogues.TryGetValue(DialogueLookupKey(state.CurrentRound, conversationKey), out dialogue);
        }

        private string DialogueLookupKey(int roundNumber, string conversationKey)
        {
            return roundNumber + ":" + conversationKey;
        }
        private void DrawStatus()
        {
            GUILayout.Label("Attributes");
            GUILayout.Label("Diplomacy: " + state.Attributes.Diplomacy + "   Warfare: " + state.Attributes.Warfare);
            GUILayout.Label("Management: " + state.Attributes.Management + "   Combat: " + state.Attributes.Combat);
            GUILayout.Space(5);
            GUILayout.Label("Resources");
            GUILayout.Label("Influence: " + state.Resources.Influence + "   Treasury: " + state.Resources.Treasury);
            GUILayout.Label("Supplies: " + state.Resources.Supplies + "   Reputation: " + state.Resources.Reputation);
            GUILayout.Label("Troops: " + state.Resources.Troops);
            GUILayout.Space(5);
            GUILayout.Label("Relationships (−5 to +5)");
            GUILayout.Label("Steward " + Relation(RelationshipTarget.Administrator) +
                " | General " + Relation(RelationshipTarget.General) + " | Mother " + Relation(RelationshipTarget.Mother));
            GUILayout.Label("Crown " + Relation(RelationshipTarget.Crown) + " | Rebels " + Relation(RelationshipTarget.Rebels));
            GUILayout.Label("Refugee Noble " + Relation(RelationshipTarget.RefugeeNoble));
        }

        private void DrawCurrentRound()
        {
            if (state.IsComplete)
            {
                DrawEnding();
                return;
            }
            GameEventDefinition gameEvent = GetEvent(state.CurrentRound);
            if (gameEvent == null)
            {
                GUILayout.Label("No event is configured for this round.", GUI.skin.box);
                return;
            }
            DrawEvent(gameEvent);
        }

        private void DrawEvent(GameEventDefinition gameEvent)
        {
            GUILayout.Label(gameEvent.description);
            GUILayout.Space(8);
            if (state.IsEventComplete(gameEvent))
            {
                GUILayout.Label("Event decisions recorded. Effects have been applied.", GUI.skin.box);
                if (!gameEvent.isFinalEvent && GUILayout.Button("Advance to Round " + (state.CurrentRound + 1), GUILayout.Height(38))) AdvanceRound();
                return;
            }

            GameEventStage stage = state.GetActiveStage(gameEvent);
            if (stage == null)
            {
                GUILayout.Label("This event has no available stage. Check its event asset.", GUI.skin.box);
                return;
            }
            if (!string.IsNullOrEmpty(stage.title)) GUILayout.Label(stage.title, SectionStyle());
            GUILayout.Label("Resources — Influence: " + state.Resources.Influence + " | Treasury: " + state.Resources.Treasury +
                " | Supplies: " + state.Resources.Supplies + " | Troops: " + state.Resources.Troops);

            GameChoiceDefinition selectedChoice = null;
            foreach (GameChoiceDefinition choice in gameEvent.choices)
            {
                if (choice == null || choice.stageId != stage.stageId) continue;
                if (state.HasChoice(gameEvent, choice.choiceId))
                {
                    GUILayout.Label("✓ " + choice.resultText, GUI.skin.box);
                    continue;
                }
                if (stage.selectionMode == EventSelectionMode.SingleChoice && state.GetStageSelectionCount(gameEvent, stage.stageId) > 0)
                    continue;
                string choiceFailure;
                bool canChoose = GameEventRules.CanChoose(state, gameEvent, choice, out choiceFailure);
                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && canChoose;
                if (GUILayout.Button(choice.buttonLabel, GUILayout.Height(38))) selectedChoice = choice;
                GUI.enabled = previousEnabled;
                if (!canChoose) GUILayout.Label(choiceFailure);
            }

            if (selectedChoice != null)
            {
                string choiceFailureMessage;
                if (state.TryChooseEvent(gameEvent, selectedChoice, out choiceFailureMessage))
                    notice = selectedChoice.resultText;
                else
                    notice = choiceFailureMessage;
                return;
            }

            if (!stage.manualCompletion) return;
            string failureMessage;
            bool canFinish = state.CanCompleteCurrentStage(gameEvent, out failureMessage);
            GUI.enabled = canFinish;
            string finishLabel = gameEvent.isFinalEvent ? "Finish preparations and end the prototype" : "Complete this stage and advance";
            if (GUILayout.Button(finishLabel, GUILayout.Height(40)) && state.CompleteCurrentStage(gameEvent))
            {
                if (gameEvent.isFinalEvent)
                    notice = "Preparations complete. The battle has not yet begun.";
                else
                    AdvanceRound();
            }
            GUI.enabled = true;
            if (!canFinish) GUILayout.Label(failureMessage);
        }
        private string ConversationText()
        {
            if (selectedConversation == "Lands Map")
                return roomConsultationContent != null ? MapDescription() : "Room consultation text is not loaded.";
            if (selectedConversation == "Records Book")
                return roomConsultationContent != null ? DecisionRecord() : "Room consultation text is not loaded.";
            DialogueDefinition dialogue;
            if (TryGetSelectedDialogue(out dialogue))
            {
                string dialogueText = dialogue.speakerName + ":\n" + dialogue.openingText;
                string reply;
                if (dialogueReplies.TryGetValue(dialogue.dialogueId, out reply) && !string.IsNullOrEmpty(reply))
                    dialogueText += "\n\n" + dialogue.speakerName + ": " + reply;
                return dialogueText;
            }
            return roomConsultationContent != null
                ? roomConsultationContent.noSelectionText
                : "Select a person or message in the room to view the conversation.";
        }
        private string MapDescription()
        {
            foreach (RoundConsultationText entry in roomConsultationContent.mapDescriptions)
            {
                if (entry != null && entry.roundNumber == state.CurrentRound)
                    return entry.text;
            }

            return roomConsultationContent.missingContentText;
        }

        private string DecisionRecord()
        {
            List<string> records = new List<string>();
            for (int round = 1; round <= GameState.TotalRounds; round++)
            {
                GameEventDefinition gameEvent = GetEvent(round);
                if (gameEvent == null) continue;
                if (gameEvent.stages == null) continue;
                foreach (GameEventStage stage in gameEvent.stages)
                    if (stage != null)
                        records.Add(gameEvent.title + " — " + stage.title + ": " + FormatSelectedChoices(gameEvent, stage.stageId));
            }
            return string.Join(". ", records) + ".";
        }

        private string FormatSelectedChoices(GameEventDefinition gameEvent, string stageId = null)
        {
            if (gameEvent == null) return "Unavailable";
            List<string> labels = new List<string>();
            if (gameEvent.choices == null) return "Undecided";
            foreach (GameChoiceDefinition choice in gameEvent.choices)
                if (choice != null && (stageId == null || choice.stageId == stageId) && state.HasChoice(gameEvent, choice.choiceId))
                    labels.Add(choice.buttonLabel);
            return labels.Count == 0 ? "Undecided" : string.Join(", ", labels);
        }
        private GameEventDefinition GetEvent(int roundNumber)
        {
            GameEventDefinition gameEvent;
            return eventsByRound.TryGetValue(roundNumber, out gameEvent) ? gameEvent : null;
        }

        private string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, "a-place-among-lords-save.json"); }
        }

        private void SaveGame()
        {
            try
            {
                List<DialogueReplyRecord> replies = new List<DialogueReplyRecord>();
                foreach (KeyValuePair<string, string> item in dialogueReplies)
                    replies.Add(new DialogueReplyRecord { dialogueId = item.Key, replyText = item.Value });
                GameSaveData save = state.CreateSaveData(replies);
                File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
                notice = "Game saved to " + SavePath;
            }
            catch (Exception exception)
            {
                Debug.LogError("Could not save game: " + exception);
                notice = "Could not save the game. See the Unity Console for details.";
            }
        }

        private void LoadGame()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    notice = "No saved game was found.";
                    return;
                }
                GameSaveData save = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SavePath));
                if (save == null || save.saveVersion != 1)
                {
                    notice = "The save file is empty or uses an unsupported version.";
                    return;
                }
                GameState loadedState = GameState.FromSaveData(save);
                if (loadedState == null)
                {
                    notice = "The save file could not be read.";
                    return;
                }
                state = loadedState;
                dialogueReplies.Clear();
                if (save.dialogueReplies != null)
                    foreach (DialogueReplyRecord reply in save.dialogueReplies)
                        if (reply != null && !string.IsNullOrEmpty(reply.dialogueId))
                            dialogueReplies[reply.dialogueId] = reply.replyText;
                diplomacy = state.Attributes.Diplomacy;
                warfare = state.Attributes.Warfare;
                management = state.Attributes.Management;
                combat = state.Attributes.Combat;
                selectedConversation = "Context";
                scrollPosition = Vector2.zero;
                notice = "Saved game loaded.";
            }
            catch (Exception exception)
            {
                Debug.LogError("Could not load game: " + exception);
                notice = "Could not load the game. See the Unity Console for details.";
            }
        }

        private void DrawEnding()
        {
            GUILayout.Label("PREPARATIONS COMPLETE", TitleStyle());
            GameEventDefinition finalEvent = GetEvent(GameState.TotalRounds);
            if (finalEvent != null && finalEvent.stages != null)
            {
                foreach (GameEventStage stage in finalEvent.stages)
                    if (stage != null)
                        GUILayout.Label(stage.title + ": " + FormatSelectedChoices(finalEvent, stage.stageId) + ".", GUI.skin.box);
            }
            GUILayout.Label("Your forces are ready to march. The battle has not yet begun.", GUI.skin.box);
        }

        private void AdvanceRound()
        {
            GameEventDefinition currentEvent = GetEvent(state.CurrentRound);
            if (state.AdvanceRound(currentEvent))
            {
                selectedConversation = "Context";
                notice = "Round advanced. Previous decisions remain recorded.";
            }
        }

        private int Relation(RelationshipTarget target)
        {
            return state.Relationships.Get(target);
        }

        private string RoundTitle(int round)
        {
            GameEventDefinition gameEvent = GetEvent(round);
            return gameEvent != null ? gameEvent.title : "The story is complete";
        }

        private void ConfigureTextSizes()
        {
            GUI.skin.label.fontSize = 20;
            GUI.skin.button.fontSize = 18;
            GUI.skin.box.fontSize = 20;
            GUI.skin.label.wordWrap = true;
            GUI.skin.box.wordWrap = true;
            GUI.skin.button.wordWrap = true;
        }

        private GUIStyle ConsultationStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = GUI.skin.box.fontSize;
            style.wordWrap = true;
            style.alignment = TextAnchor.UpperLeft;
            style.padding = new RectOffset(10, 10, 8, 8);
            return style;
        }

        private GUIStyle SectionStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            return style;
        }

        private GUIStyle TitleStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 30;
            style.fontStyle = FontStyle.Bold;
            return style;
        }
    }
}



























