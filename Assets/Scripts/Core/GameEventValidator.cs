using System;
using System.Collections.Generic;
using APlaceAmongLords.Data;

namespace APlaceAmongLords.Core
{
    /// <summary>Checks event assets before the game uses them.</summary>
    public static class GameEventValidator
    {
        public static List<string> ValidateCatalog(IEnumerable<GameEventDefinition> events, int totalRounds)
        {
            List<string> errors = new List<string>();
            HashSet<string> eventIds = new HashSet<string>();
            HashSet<int> seenRounds = new HashSet<int>();
            HashSet<int> validRounds = new HashSet<int>();

            if (events == null)
            {
                errors.Add("The event catalog is null.");
                return errors;
            }

            foreach (GameEventDefinition gameEvent in events)
            {
                List<string> definitionErrors = ValidateDefinition(gameEvent, totalRounds);
                errors.AddRange(definitionErrors);
                if (gameEvent == null)
                    continue;

                bool uniqueId = !string.IsNullOrWhiteSpace(gameEvent.eventId) && eventIds.Add(gameEvent.eventId);
                if (!string.IsNullOrWhiteSpace(gameEvent.eventId) && !uniqueId)
                    errors.Add("Event asset '" + gameEvent.name + "' duplicates event ID '" + gameEvent.eventId + "'.");

                bool roundInRange = gameEvent.roundNumber >= 1 && gameEvent.roundNumber <= totalRounds;
                bool uniqueRound = roundInRange && seenRounds.Add(gameEvent.roundNumber);
                if (roundInRange && !uniqueRound)
                    errors.Add("More than one event asset is configured for round " + gameEvent.roundNumber + ".");
                if (definitionErrors.Count == 0 && uniqueId && uniqueRound)
                    validRounds.Add(gameEvent.roundNumber);
            }

            for (int round = 1; round <= totalRounds; round++)
                if (!validRounds.Contains(round))
                    errors.Add("No valid event asset is configured for round " + round + ".");

            return errors;
        }

        public static List<string> ValidateDefinition(GameEventDefinition gameEvent, int totalRounds)
        {
            List<string> errors = new List<string>();
            if (gameEvent == null)
            {
                errors.Add("An event asset is null.");
                return errors;
            }

            string label = "Event '" + (string.IsNullOrEmpty(gameEvent.eventId) ? gameEvent.name : gameEvent.eventId) + "'";
            if (string.IsNullOrWhiteSpace(gameEvent.eventId)) errors.Add(label + " must have an event ID.");
            if (gameEvent.roundNumber < 1 || gameEvent.roundNumber > totalRounds)
                errors.Add(label + " has a round number outside 1–" + totalRounds + ".");
            if (gameEvent.roundNumber == totalRounds && !gameEvent.isFinalEvent)
                errors.Add(label + " is in the last round but is not marked as the final event.");
            if (gameEvent.isFinalEvent && gameEvent.roundNumber != totalRounds)
                errors.Add(label + " is marked as final but is not in the last round.");
            if (string.IsNullOrWhiteSpace(gameEvent.title)) errors.Add(label + " must have a title.");
            if (string.IsNullOrWhiteSpace(gameEvent.description)) errors.Add(label + " must have a description.");
            if (gameEvent.stages == null || gameEvent.stages.Count == 0)
                errors.Add(label + " must define at least one stage.");
            if (gameEvent.choices == null || gameEvent.choices.Count == 0)
                errors.Add(label + " must define at least one choice.");
            if (gameEvent.stages == null || gameEvent.choices == null)
                return errors;

            HashSet<string> stageIds = new HashSet<string>();
            Dictionary<string, GameEventStage> stagesById = new Dictionary<string, GameEventStage>();
            foreach (GameEventStage stage in gameEvent.stages)
            {
                if (stage == null)
                {
                    errors.Add(label + " contains a null stage.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(stage.stageId))
                {
                    errors.Add(label + " contains a stage without an ID.");
                    continue;
                }
                if (!stageIds.Add(stage.stageId))
                {
                    errors.Add(label + " contains duplicate stage ID '" + stage.stageId + "'.");
                    continue;
                }
                stagesById.Add(stage.stageId, stage);
                if (string.IsNullOrWhiteSpace(stage.title)) errors.Add(label + " stage '" + stage.stageId + "' must have a title.");
                if (stage.minimumSelections < 1) errors.Add(label + " stage '" + stage.stageId + "' must require at least one selection.");
                if (!Enum.IsDefined(typeof(EventSelectionMode), stage.selectionMode))
                    errors.Add(label + " stage '" + stage.stageId + "' has an unknown selection mode.");
                if (stage.selectionMode == EventSelectionMode.SingleChoice && stage.minimumSelections != 1)
                    errors.Add(label + " single-choice stage '" + stage.stageId + "' must require exactly one selection.");
            }

            HashSet<string> choiceIds = new HashSet<string>();
            Dictionary<string, int> stageChoiceCounts = new Dictionary<string, int>();
            foreach (string stageId in stageIds)
                stageChoiceCounts.Add(stageId, 0);

            foreach (GameChoiceDefinition choice in gameEvent.choices)
            {
                if (choice == null)
                {
                    errors.Add(label + " contains a null choice.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(choice.choiceId)) errors.Add(label + " contains a choice without an ID.");
                else if (!choiceIds.Add(choice.choiceId)) errors.Add(label + " contains duplicate choice ID '" + choice.choiceId + "'.");
                if (string.IsNullOrWhiteSpace(choice.buttonLabel)) errors.Add(label + " choice '" + choice.choiceId + "' must have a button label.");
                if (string.IsNullOrWhiteSpace(choice.resultText)) errors.Add(label + " choice '" + choice.choiceId + "' must have result text.");

                int currentCount;
                if (string.IsNullOrWhiteSpace(choice.stageId))
                    errors.Add(label + " choice '" + choice.choiceId + "' must have a stage ID.");
                else if (stageChoiceCounts.TryGetValue(choice.stageId, out currentCount))
                    stageChoiceCounts[choice.stageId] = currentCount + 1;
                else
                    errors.Add(label + " choice '" + choice.choiceId + "' refers to unknown stage '" + choice.stageId + "'.");

                ValidateConditions(choice, label, errors);
                ValidateEffects(choice, label, errors);
            }

            foreach (KeyValuePair<string, GameEventStage> item in stagesById)
            {
                int choiceCount = stageChoiceCounts[item.Key];
                if (choiceCount == 0)
                    errors.Add(label + " stage '" + item.Key + "' has no choices.");
                if (item.Value.minimumSelections > choiceCount)
                    errors.Add(label + " stage '" + item.Key + "' requires more choices than it contains.");
            }

            return errors;
        }

        private static void ValidateConditions(GameChoiceDefinition choice, string eventLabel, List<string> errors)
        {
            if (choice.conditions == null)
                return;

            foreach (GameCondition condition in choice.conditions)
            {
                if (condition == null)
                {
                    errors.Add(eventLabel + " choice '" + choice.choiceId + "' contains a null condition.");
                    continue;
                }

                switch (condition.conditionType)
                {
                    case GameConditionType.AttributeMinimum:
                        if (!Enum.IsDefined(typeof(AttributeType), condition.attribute))
                            errors.Add(eventLabel + " choice '" + choice.choiceId + "' has an unknown attribute condition.");
                        if (condition.minimumValue < 0 || condition.minimumValue > GameState.AttributePointBudget)
                            errors.Add(eventLabel + " choice '" + choice.choiceId + "' has an impossible attribute minimum.");
                        break;
                    case GameConditionType.ResourceMinimum:
                        if (!Enum.IsDefined(typeof(ResourceType), condition.resource))
                            errors.Add(eventLabel + " choice '" + choice.choiceId + "' has an unknown resource condition.");
                        if (condition.minimumValue < 0)
                            errors.Add(eventLabel + " choice '" + choice.choiceId + "' has a negative resource minimum.");
                        break;
                    case GameConditionType.RelationshipMinimum:
                        if (!Enum.IsDefined(typeof(RelationshipTarget), condition.relationshipTarget))
                            errors.Add(eventLabel + " choice '" + choice.choiceId + "' has an unknown relationship condition.");
                        if (condition.minimumValue < -5 || condition.minimumValue > 5)
                            errors.Add(eventLabel + " choice '" + choice.choiceId + "' has a relationship minimum outside −5 to +5.");
                        break;
                    default:
                        errors.Add(eventLabel + " choice '" + choice.choiceId + "' has an unknown condition type.");
                        break;
                }
            }
        }

        private static void ValidateEffects(GameChoiceDefinition choice, string eventLabel, List<string> errors)
        {
            if (choice.effects == null)
                return;

            foreach (GameEffect effect in choice.effects)
            {
                if (effect == null)
                {
                    errors.Add(eventLabel + " choice '" + choice.choiceId + "' contains a null effect.");
                    continue;
                }

                switch (effect.effectType)
                {
                    case GameEffectType.ResourceChange:
                        if (!Enum.IsDefined(typeof(ResourceType), effect.resource))
                            errors.Add(eventLabel + " choice '" + choice.choiceId + "' has an unknown resource effect.");
                        break;
                    case GameEffectType.RelationshipChange:
                        if (!Enum.IsDefined(typeof(RelationshipTarget), effect.relationshipTarget))
                            errors.Add(eventLabel + " choice '" + choice.choiceId + "' has an unknown relationship effect.");
                        break;
                    default:
                        errors.Add(eventLabel + " choice '" + choice.choiceId + "' has an unknown effect type.");
                        break;
                }
            }
        }
    }
}
