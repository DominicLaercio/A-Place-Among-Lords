using System;
using System.Collections.Generic;
using APlaceAmongLords.Data;

namespace APlaceAmongLords.Core
{
    [Serializable]
    public sealed class EventProgress
    {
        public string eventId;
        public List<string> selectedChoiceIds = new List<string>();
        public List<string> completedStageIds = new List<string>();
        public bool isComplete;
    }

    [Serializable]
    public sealed class GameState
    {
        public const int TotalRounds = 5;
        public int CurrentRound { get; private set; } = 1;
        public bool IsComplete { get; private set; }
        public AttributeSet Attributes { get; private set; }
        public ResourceSet Resources { get; private set; }
        public RelationshipSet Relationships { get; private set; }

        private readonly bool[] conversationResponses = new bool[TotalRounds * 6];
        private readonly List<EventProgress> eventProgress = new List<EventProgress>();

        public GameState()
        {
            Attributes = new AttributeSet();
            Resources = new ResourceSet();
            Relationships = new RelationshipSet();
            Resources.SetStartingValues(2, 10, 8, 0, 20);
        }

        public bool HasRespondedTo(RelationshipTarget target)
        {
            if (CurrentRound < 1 || CurrentRound > TotalRounds)
                return false;
            return conversationResponses[(CurrentRound - 1) * 6 + (int)target];
        }

        public bool RespondToCharacter(RelationshipTarget target, int relationshipChange)
        {
            if (CurrentRound < 1 || CurrentRound > TotalRounds ||
                relationshipChange < -1 || relationshipChange > 1 || HasRespondedTo(target))
                return false;

            conversationResponses[(CurrentRound - 1) * 6 + (int)target] = true;
            if (relationshipChange != 0)
                Relationships.Change(target, relationshipChange);
            return true;
        }

        public bool HasChoice(GameEventDefinition gameEvent, string choiceId)
        {
            if (gameEvent == null || string.IsNullOrEmpty(choiceId))
                return false;
            EventProgress progress = FindProgress(gameEvent.eventId);
            return progress != null && progress.selectedChoiceIds.Contains(choiceId);
        }

        public List<string> GetSelectedChoiceIds(GameEventDefinition gameEvent)
        {
            EventProgress progress = gameEvent == null ? null : FindProgress(gameEvent.eventId);
            return progress == null ? new List<string>() : new List<string>(progress.selectedChoiceIds);
        }

        public int GetStageSelectionCount(GameEventDefinition gameEvent, string stageId)
        {
            if (gameEvent == null)
                return 0;

            EventProgress progress = FindProgress(gameEvent.eventId);
            if (progress == null)
                return 0;

            int count = 0;
            foreach (GameChoiceDefinition choice in gameEvent.choices)
            {
                if (choice != null && choice.stageId == stageId && progress.selectedChoiceIds.Contains(choice.choiceId))
                    count++;
            }
            return count;
        }

        public GameEventStage GetActiveStage(GameEventDefinition gameEvent)
        {
            if (gameEvent == null || gameEvent.stages == null || IsEventComplete(gameEvent))
                return null;

            EventProgress progress = FindProgress(gameEvent.eventId);
            foreach (GameEventStage stage in gameEvent.stages)
            {
                if (stage != null && (progress == null || !progress.completedStageIds.Contains(stage.stageId)))
                    return stage;
            }
            return null;
        }

        public bool IsEventComplete(GameEventDefinition gameEvent)
        {
            if (gameEvent == null)
                return false;
            EventProgress progress = FindProgress(gameEvent.eventId);
            return progress != null && progress.isComplete;
        }

        public bool TryChooseEvent(GameEventDefinition gameEvent, GameChoiceDefinition choice, out string failureMessage)
        {
            if (!GameEventRules.CanChoose(this, gameEvent, choice, out failureMessage))
                return false;
            if (!GameEventRules.ApplyEffects(this, choice))
            {
                failureMessage = "This choice contains an invalid effect.";
                return false;
            }

            EventProgress progress = GetOrCreateProgress(gameEvent.eventId);
            progress.selectedChoiceIds.Add(choice.choiceId);

            GameEventStage stage = GetActiveStage(gameEvent);
            int selectionCount = GetStageSelectionCount(gameEvent, stage.stageId);
            if (!stage.manualCompletion && (stage.selectionMode == EventSelectionMode.SingleChoice || selectionCount >= stage.minimumSelections))
                CompleteStage(gameEvent, stage, progress);

            return true;
        }

        public bool CanCompleteCurrentStage(GameEventDefinition gameEvent, out string failureMessage)
        {
            failureMessage = string.Empty;
            GameEventStage stage = GetActiveStage(gameEvent);
            if (stage == null || !stage.manualCompletion)
            {
                failureMessage = "There is no manually completed stage to finish.";
                return false;
            }

            if (GetStageSelectionCount(gameEvent, stage.stageId) < stage.minimumSelections)
            {
                failureMessage = "Choose at least " + stage.minimumSelections + " option(s) before continuing.";
                return false;
            }

            return true;
        }

        public bool CompleteCurrentStage(GameEventDefinition gameEvent)
        {
            string failureMessage;
            if (!CanCompleteCurrentStage(gameEvent, out failureMessage))
                return false;

            GameEventStage stage = GetActiveStage(gameEvent);
            CompleteStage(gameEvent, stage, GetOrCreateProgress(gameEvent.eventId));
            return true;
        }

        public bool AdvanceRound(GameEventDefinition currentEvent)
        {
            if (IsComplete || CurrentRound >= TotalRounds || currentEvent == null ||
                currentEvent.roundNumber != CurrentRound || !IsEventComplete(currentEvent) || currentEvent.isFinalEvent)
                return false;

            CurrentRound++;
            return true;
        }

        private void CompleteStage(GameEventDefinition gameEvent, GameEventStage stage, EventProgress progress)
        {
            if (!progress.completedStageIds.Contains(stage.stageId))
                progress.completedStageIds.Add(stage.stageId);

            if (progress.completedStageIds.Count >= gameEvent.stages.Count)
            {
                progress.isComplete = true;
                if (gameEvent.isFinalEvent)
                    IsComplete = true;
            }
        }

        private EventProgress FindProgress(string eventId)
        {
            return eventProgress.Find(item => item.eventId == eventId);
        }

        private EventProgress GetOrCreateProgress(string eventId)
        {
            EventProgress progress = FindProgress(eventId);
            if (progress != null)
                return progress;

            progress = new EventProgress { eventId = eventId };
            eventProgress.Add(progress);
            return progress;
        }

        public GameSaveData CreateSaveData(List<DialogueReplyRecord> dialogueReplies)
        {
            GameSaveData data = new GameSaveData
            {
                currentRound = CurrentRound,
                isComplete = IsComplete,
                attributesAllocated = Attributes.IsAllocated,
                diplomacy = Attributes.Diplomacy,
                warfare = Attributes.Warfare,
                management = Attributes.Management,
                combat = Attributes.Combat,
                influence = Resources.Influence,
                treasury = Resources.Treasury,
                supplies = Resources.Supplies,
                reputation = Resources.Reputation,
                troops = Resources.Troops,
                eventProgress = new List<EventProgress>(),
                conversationResponses = new List<bool>(),
                dialogueReplies = dialogueReplies == null ? new List<DialogueReplyRecord>() : new List<DialogueReplyRecord>(dialogueReplies)
            };

            foreach (RelationshipTarget target in Enum.GetValues(typeof(RelationshipTarget)))
                data.relationshipValues.Add(Relationships.Get(target));
            foreach (EventProgress progress in eventProgress)
            {
                data.eventProgress.Add(new EventProgress
                {
                    eventId = progress.eventId,
                    selectedChoiceIds = new List<string>(progress.selectedChoiceIds),
                    completedStageIds = new List<string>(progress.completedStageIds),
                    isComplete = progress.isComplete
                });
            }
            foreach (bool response in conversationResponses)
                data.conversationResponses.Add(response);
            return data;
        }

        public static GameState FromSaveData(GameSaveData data)
        {
            if (data == null)
                return null;

            GameState state = new GameState();
            state.CurrentRound = Math.Max(1, Math.Min(TotalRounds, data.currentRound));
            state.IsComplete = data.isComplete;
            if (data.attributesAllocated)
                state.Attributes.TryAllocate(data.diplomacy, data.warfare, data.management, data.combat, 8);
            state.Resources.SetStartingValues(data.influence, data.treasury, data.supplies, data.reputation, data.troops);

            int relationshipIndex = 0;
            foreach (RelationshipTarget target in Enum.GetValues(typeof(RelationshipTarget)))
            {
                if (data.relationshipValues != null && relationshipIndex < data.relationshipValues.Count)
                    state.Relationships.Change(target, data.relationshipValues[relationshipIndex]);
                relationshipIndex++;
            }

            if (data.eventProgress != null)
            {
                foreach (EventProgress progress in data.eventProgress)
                {
                    if (progress == null || string.IsNullOrEmpty(progress.eventId))
                        continue;
                    state.eventProgress.Add(new EventProgress
                    {
                        eventId = progress.eventId,
                        selectedChoiceIds = progress.selectedChoiceIds == null ? new List<string>() : new List<string>(progress.selectedChoiceIds),
                        completedStageIds = progress.completedStageIds == null ? new List<string>() : new List<string>(progress.completedStageIds),
                        isComplete = progress.isComplete
                    });
                }
            }

            if (data.conversationResponses != null)
            {
                for (int i = 0; i < state.conversationResponses.Length && i < data.conversationResponses.Count; i++)
                    state.conversationResponses[i] = data.conversationResponses[i];
            }
            return state;
        }
    }
}
