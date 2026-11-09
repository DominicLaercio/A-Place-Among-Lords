using System.Collections.Generic;
using System.Linq;
using APlaceAmongLords.Core;
using APlaceAmongLords.Data;
using NUnit.Framework;
using UnityEngine;

namespace APlaceAmongLords.Tests.EditMode
{
    public sealed class GameStateTests
    {
        private readonly List<GameEventDefinition> createdEvents = new List<GameEventDefinition>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameEventDefinition gameEvent in createdEvents)
                if (gameEvent != null)
                    Object.DestroyImmediate(gameEvent);
            createdEvents.Clear();
        }

        [Test]
        public void ValidateCatalog_AllProjectEventAssetsAreWellFormed()
        {
            GameEventDefinition[] events = Resources.LoadAll<GameEventDefinition>("Data/Events");
            List<string> errors = GameEventValidator.ValidateCatalog(events, GameState.TotalRounds);

            Assert.That(events.Length, Is.EqualTo(GameState.TotalRounds));
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void ValidateCatalog_ReportsEveryRoundWithoutAValidEvent()
        {
            GameEventDefinition roundOneEvent = CreateEvent(1, false, SingleChoiceStage("decision"));
            roundOneEvent.choices.Add(CreateChoice("choice", "decision"));

            List<string> errors = GameEventValidator.ValidateCatalog(
                new[] { roundOneEvent }, GameState.TotalRounds);

            Assert.That(errors.Count(error => error.Contains("No valid event asset is configured for round")), Is.EqualTo(4), string.Join("\n", errors));
        }

        [Test]
        public void ValidateDefinition_ReportsDuplicateIdsAndInvalidConditionValues()
        {
            GameEventDefinition gameEvent = CreateEvent(1, false, SingleChoiceStage("decision"));
            GameChoiceDefinition invalidChoice = CreateChoice("same_id", "decision");
            invalidChoice.conditions.Add(new GameCondition
            {
                conditionType = GameConditionType.AttributeMinimum,
                attribute = AttributeType.Warfare,
                minimumValue = GameState.AttributePointBudget + 1
            });
            gameEvent.choices.Add(invalidChoice);
            gameEvent.choices.Add(CreateChoice("same_id", "decision"));

            List<string> errors = GameEventValidator.ValidateDefinition(gameEvent, GameState.TotalRounds);

            Assert.That(errors.Exists(error => error.Contains("duplicate choice ID")), Is.True, string.Join("\n", errors));
            Assert.That(errors.Exists(error => error.Contains("impossible attribute minimum")), Is.True, string.Join("\n", errors));
        }

        [Test]
        public void CanChoose_EnforcesAttributeMinimum()
        {
            GameEventDefinition gameEvent = CreateEvent(1, false, new GameEventStage
            {
                stageId = "decision",
                selectionMode = EventSelectionMode.SingleChoice,
                minimumSelections = 1
            });
            GameChoiceDefinition choice = CreateChoice("requires_warfare", "decision");
            choice.conditions.Add(new GameCondition
            {
                conditionType = GameConditionType.AttributeMinimum,
                attribute = AttributeType.Warfare,
                minimumValue = 2,
                failureMessage = "Requires Warfare 2."
            });
            gameEvent.choices.Add(choice);

            GameState belowRequirement = CreateAllocatedState(3, 1, 2, 2);
            string failureMessage;
            Assert.That(GameEventRules.CanChoose(belowRequirement, gameEvent, choice, out failureMessage), Is.False);
            Assert.That(failureMessage, Is.EqualTo("Requires Warfare 2."));

            GameState meetingRequirement = CreateAllocatedState(2, 2, 2, 2);
            Assert.That(GameEventRules.CanChoose(meetingRequirement, gameEvent, choice, out failureMessage), Is.True);
            Assert.That(failureMessage, Is.Empty);
        }

        [Test]
        public void TryChooseEvent_AppliesResourceAndRelationshipEffectsOnlyOnce()
        {
            GameEventDefinition gameEvent = CreateEvent(1, false, SingleChoiceStage("decision"));
            GameChoiceDefinition choice = CreateChoice("aid_rebels", "decision");
            choice.effects.Add(new GameEffect { effectType = GameEffectType.ResourceChange, resource = ResourceType.Treasury, amount = -3 });
            choice.effects.Add(new GameEffect { effectType = GameEffectType.RelationshipChange, relationshipTarget = RelationshipTarget.Rebels, amount = 2 });
            gameEvent.choices.Add(choice);
            GameState state = CreateAllocatedState(2, 2, 2, 2);

            string failureMessage;
            Assert.That(state.TryChooseEvent(gameEvent, choice, out failureMessage), Is.True, failureMessage);
            Assert.That(state.Resources.Treasury, Is.EqualTo(7));
            Assert.That(state.Relationships.Get(RelationshipTarget.Rebels), Is.EqualTo(2));
            Assert.That(state.IsEventComplete(gameEvent), Is.True);
            Assert.That(state.TryChooseEvent(gameEvent, choice, out failureMessage), Is.False);
            Assert.That(state.Resources.Treasury, Is.EqualTo(7), "A repeated choice must not apply effects twice.");
        }

        [Test]
        public void ApplyEffects_InvalidEffectDoesNotPartiallyChangeState()
        {
            GameState state = new GameState();
            GameChoiceDefinition choice = CreateChoice("invalid_effect", "decision");
            choice.effects.Add(new GameEffect { effectType = GameEffectType.ResourceChange, resource = ResourceType.Treasury, amount = 5 });
            choice.effects.Add(new GameEffect { effectType = GameEffectType.ResourceChange, resource = (ResourceType)99, amount = -100 });

            Assert.That(GameEventRules.ApplyEffects(state, choice), Is.False);
            Assert.That(state.Resources.Treasury, Is.EqualTo(10));
        }

        [Test]
        public void MultipleChoiceStage_RequiresMinimumAndManualCompletion()
        {
            GameState state = CreateAllocatedState(2, 2, 2, 2);
            ResolveRound(state, 1);
            ResolveRound(state, 2);

            GameEventDefinition gameEvent = CreateEvent(3, false, new GameEventStage
            {
                stageId = "actions",
                selectionMode = EventSelectionMode.MultipleChoices,
                minimumSelections = 2,
                manualCompletion = true
            });
            GameChoiceDefinition evacuate = CreateChoice("evacuate", "actions");
            GameChoiceDefinition defend = CreateChoice("defend", "actions");
            gameEvent.choices.Add(evacuate);
            gameEvent.choices.Add(defend);

            string failureMessage;
            Assert.That(state.CanCompleteCurrentStage(gameEvent, out failureMessage), Is.False);
            Assert.That(state.TryChooseEvent(gameEvent, evacuate, out failureMessage), Is.True, failureMessage);
            Assert.That(state.IsEventComplete(gameEvent), Is.False);
            Assert.That(state.CanCompleteCurrentStage(gameEvent, out failureMessage), Is.False);
            Assert.That(state.TryChooseEvent(gameEvent, defend, out failureMessage), Is.True, failureMessage);
            Assert.That(state.CanCompleteCurrentStage(gameEvent, out failureMessage), Is.True, failureMessage);

            Assert.That(state.CompleteCurrentStage(gameEvent), Is.True);
            Assert.That(state.IsEventComplete(gameEvent), Is.True);
            Assert.That(state.AdvanceRound(gameEvent), Is.True);
            Assert.That(state.CurrentRound, Is.EqualTo(4));
        }

        [Test]
        public void FinalEvent_CompletesAfterFactionAndPreparationStagesWithoutStartingBattle()
        {
            GameState state = CreateAllocatedState(2, 2, 2, 2);
            for (int round = 1; round <= 4; round++)
                ResolveRound(state, round);

            GameEventDefinition finalEvent = CreateEvent(5, true,
                SingleChoiceStage("faction"),
                new GameEventStage
                {
                    stageId = "preparations",
                    selectionMode = EventSelectionMode.MultipleChoices,
                    minimumSelections = 1,
                    manualCompletion = true
                });
            GameChoiceDefinition chooseCrown = CreateChoice("choose_crown", "faction");
            GameChoiceDefinition fortify = CreateChoice("fortify", "preparations");
            finalEvent.choices.Add(chooseCrown);
            finalEvent.choices.Add(fortify);

            string failureMessage;
            Assert.That(state.TryChooseEvent(finalEvent, chooseCrown, out failureMessage), Is.True, failureMessage);
            Assert.That(state.GetActiveStage(finalEvent).stageId, Is.EqualTo("preparations"));
            Assert.That(state.TryChooseEvent(finalEvent, fortify, out failureMessage), Is.True, failureMessage);
            Assert.That(state.IsComplete, Is.False);
            Assert.That(state.CompleteCurrentStage(finalEvent), Is.True);
            Assert.That(state.IsComplete, Is.True);
            Assert.That(state.AdvanceRound(finalEvent), Is.False);
            Assert.That(state.CurrentRound, Is.EqualTo(5), "The prototype ends before the battle begins.");
        }

        [Test]
        public void SaveRoundTrip_RestoresValuesEventProgressAndConversationReplies()
        {
            GameState state = CreateAllocatedState(2, 2, 2, 2);
            GameEventDefinition gameEvent = CreateEvent(1, false, new GameEventStage
            {
                stageId = "actions",
                selectionMode = EventSelectionMode.MultipleChoices,
                minimumSelections = 2,
                manualCompletion = true
            });
            GameChoiceDefinition action = CreateChoice("send_aid", "actions");
            action.effects.Add(new GameEffect { effectType = GameEffectType.ResourceChange, resource = ResourceType.Supplies, amount = -2 });
            gameEvent.choices.Add(action);
            gameEvent.choices.Add(CreateChoice("hold_aid", "actions"));

            string failureMessage;
            Assert.That(state.TryChooseEvent(gameEvent, action, out failureMessage), Is.True, failureMessage);
            Assert.That(state.RespondToCharacter(RelationshipTarget.Mother, -1), Is.True);
            List<DialogueReplyRecord> replies = new List<DialogueReplyRecord>
            {
                new DialogueReplyRecord { dialogueId = "round1_mother", replyText = "I will remember your answer." }
            };

            string json = JsonUtility.ToJson(state.CreateSaveData(replies));
            GameSaveData restoredData = JsonUtility.FromJson<GameSaveData>(json);
            GameState restored = GameState.FromSaveData(restoredData);

            Assert.That(restored.Attributes.IsAllocated, Is.True);
            Assert.That(restored.Attributes.Diplomacy, Is.EqualTo(2));
            Assert.That(restored.Resources.Supplies, Is.EqualTo(6));
            Assert.That(restored.Relationships.Get(RelationshipTarget.Mother), Is.EqualTo(-1));
            Assert.That(restored.HasRespondedTo(RelationshipTarget.Mother), Is.True);
            Assert.That(restored.HasChoice(gameEvent, "send_aid"), Is.True);
            Assert.That(restored.IsEventComplete(gameEvent), Is.False);
            Assert.That(restored.GetActiveStage(gameEvent).stageId, Is.EqualTo("actions"));
            Assert.That(restoredData.dialogueReplies, Has.Count.EqualTo(1));
            Assert.That(restoredData.dialogueReplies[0].replyText, Is.EqualTo("I will remember your answer."));
        }

        private GameState CreateAllocatedState(int diplomacy, int warfare, int management, int combat)
        {
            GameState state = new GameState();
            Assert.That(state.Attributes.TryAllocate(diplomacy, warfare, management, combat, 8), Is.True);
            return state;
        }

        private void ResolveRound(GameState state, int round)
        {
            GameEventDefinition gameEvent = CreateEvent(round, false, SingleChoiceStage("decision"));
            GameChoiceDefinition choice = CreateChoice("round_" + round + "_choice", "decision");
            gameEvent.choices.Add(choice);
            string failureMessage;
            Assert.That(state.TryChooseEvent(gameEvent, choice, out failureMessage), Is.True, failureMessage);
            Assert.That(state.AdvanceRound(gameEvent), Is.True);
        }

        private GameEventDefinition CreateEvent(int round, bool isFinalEvent, params GameEventStage[] stages)
        {
            GameEventDefinition gameEvent = ScriptableObject.CreateInstance<GameEventDefinition>();
            gameEvent.eventId = "test_event_round_" + round;
            gameEvent.roundNumber = round;
            gameEvent.isFinalEvent = isFinalEvent;
            gameEvent.title = "Test event " + round;
            gameEvent.description = "Test event description.";
            gameEvent.stages = new List<GameEventStage>(stages);
            gameEvent.choices = new List<GameChoiceDefinition>();
            createdEvents.Add(gameEvent);
            return gameEvent;
        }

        private GameChoiceDefinition CreateChoice(string choiceId, string stageId)
        {
            return new GameChoiceDefinition
            {
                choiceId = choiceId,
                stageId = stageId,
                buttonLabel = choiceId,
                resultText = choiceId + " selected.",
                conditions = new List<GameCondition>(),
                effects = new List<GameEffect>()
            };
        }

        private GameEventStage SingleChoiceStage(string stageId)
        {
            return new GameEventStage
            {
                stageId = stageId,
                title = "Test stage",
                selectionMode = EventSelectionMode.SingleChoice,
                minimumSelections = 1
            };
        }
    }
}
