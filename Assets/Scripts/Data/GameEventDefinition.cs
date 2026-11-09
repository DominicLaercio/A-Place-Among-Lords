using System;
using System.Collections.Generic;
using UnityEngine;

namespace APlaceAmongLords.Data
{
    /// <summary>Editable definition of a narrative event and its choices.</summary>
    [CreateAssetMenu(fileName = "GameEvent", menuName = "A Place Among Lords/Game Event")]
    public sealed class GameEventDefinition : ScriptableObject
    {
        public string eventId;
        [Min(1)] public int roundNumber = 1;
        public bool isFinalEvent;
        public string title;
        [TextArea(3, 8)] public string description;
        public List<GameEventStage> stages = new List<GameEventStage>();
        public List<GameChoiceDefinition> choices = new List<GameChoiceDefinition>();
    }

    public enum EventSelectionMode
    {
        SingleChoice,
        MultipleChoices
    }

    [Serializable]
    public sealed class GameEventStage
    {
        public string stageId;
        public string title;
        public EventSelectionMode selectionMode;
        [Min(1)] public int minimumSelections = 1;
        public bool manualCompletion;
    }

    [Serializable]
    public sealed class GameChoiceDefinition
    {
        public string choiceId;
        public string stageId;
        [TextArea(1, 3)] public string buttonLabel;
        [TextArea(2, 4)] public string resultText;
        public List<GameCondition> conditions = new List<GameCondition>();
        public List<GameEffect> effects = new List<GameEffect>();
    }

    public enum GameConditionType
    {
        AttributeMinimum,
        ResourceMinimum,
        RelationshipMinimum
    }

    [Serializable]
    public sealed class GameCondition
    {
        public GameConditionType conditionType;
        public AttributeType attribute;
        public ResourceType resource;
        public RelationshipTarget relationshipTarget;
        public int minimumValue;
        public string failureMessage;
    }

    public enum GameEffectType
    {
        ResourceChange,
        RelationshipChange
    }

    [Serializable]
    public sealed class GameEffect
    {
        public GameEffectType effectType;
        public ResourceType resource;
        public RelationshipTarget relationshipTarget;
        public int amount;
    }
}
