using System;
using APlaceAmongLords.Data;

namespace APlaceAmongLords.Core
{
    /// <summary>Evaluates requirements and applies effects defined by event assets.</summary>
    public static class GameEventRules
    {
        public static bool CanChoose(GameState state, GameEventDefinition gameEvent, GameChoiceDefinition choice, out string failureMessage)
        {
            failureMessage = string.Empty;
            if (state == null || gameEvent == null || choice == null)
            {
                failureMessage = "Event or choice is not configured.";
                return false;
            }

            if (state.CurrentRound != gameEvent.roundNumber || state.IsEventComplete(gameEvent) ||
                gameEvent.choices == null || !gameEvent.choices.Contains(choice))
            {
                failureMessage = "This event or choice is not currently available.";
                return false;
            }

            GameEventStage activeStage = state.GetActiveStage(gameEvent);
            if (activeStage == null || choice.stageId != activeStage.stageId)
            {
                failureMessage = "This choice belongs to a different event stage.";
                return false;
            }

            if (state.HasChoice(gameEvent, choice.choiceId))
            {
                failureMessage = "This choice has already been selected.";
                return false;
            }

            int stageSelectionCount = state.GetStageSelectionCount(gameEvent, activeStage.stageId);
            if (activeStage.selectionMode == EventSelectionMode.SingleChoice && stageSelectionCount > 0)
            {
                failureMessage = "Only one choice can be selected in this stage.";
                return false;
            }

            if (choice.conditions == null)
                return true;

            foreach (GameCondition condition in choice.conditions)
            {
                if (condition == null)
                    continue;

                int currentValue;
                switch (condition.conditionType)
                {
                    case GameConditionType.AttributeMinimum:
                        if (!Enum.IsDefined(typeof(AttributeType), condition.attribute))
                        {
                            failureMessage = "Unknown attribute in condition.";
                            return false;
                        }
                        currentValue = state.Attributes.Get(condition.attribute);
                        break;
                    case GameConditionType.ResourceMinimum:
                        if (!Enum.IsDefined(typeof(ResourceType), condition.resource))
                        {
                            failureMessage = "Unknown resource in condition.";
                            return false;
                        }
                        currentValue = state.Resources.Get(condition.resource);
                        break;
                    case GameConditionType.RelationshipMinimum:
                        if (!Enum.IsDefined(typeof(RelationshipTarget), condition.relationshipTarget))
                        {
                            failureMessage = "Unknown relationship in condition.";
                            return false;
                        }
                        currentValue = state.Relationships.Get(condition.relationshipTarget);
                        break;
                    default:
                        failureMessage = "Unknown choice condition.";
                        return false;
                }

                if (currentValue < condition.minimumValue)
                {
                    failureMessage = string.IsNullOrEmpty(condition.failureMessage)
                        ? "The requirements for this choice have not been met."
                        : condition.failureMessage;
                    return false;
                }
            }

            return true;
        }

        public static bool ApplyEffects(GameState state, GameChoiceDefinition choice)
        {
            if (state == null || choice == null)
                return false;

            if (choice.effects == null)
                return true;

            // Validate all effect types before changing state to avoid partial application.
            foreach (GameEffect effect in choice.effects)
            {
                if (effect == null)
                    continue;

                switch (effect.effectType)
                {
                    case GameEffectType.ResourceChange:
                        if (!Enum.IsDefined(typeof(ResourceType), effect.resource))
                            return false;
                        break;
                    case GameEffectType.RelationshipChange:
                        if (!Enum.IsDefined(typeof(RelationshipTarget), effect.relationshipTarget))
                            return false;
                        break;
                    default:
                        return false;
                }
            }

            foreach (GameEffect effect in choice.effects)
            {
                if (effect == null)
                    continue;

                if (effect.effectType == GameEffectType.ResourceChange)
                    state.Resources.Change(effect.resource, effect.amount);
                else
                    state.Relationships.Change(effect.relationshipTarget, effect.amount);
            }

            return true;
        }
    }
}
