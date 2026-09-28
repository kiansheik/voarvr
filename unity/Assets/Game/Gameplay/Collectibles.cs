using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoarVR.Gameplay
{
    // Numeric values are append-only because they are suitable for compact telemetry.
    public enum CollectibleType
    {
        None = 0,
        SunMoth = 1,
        MoonMoth = 2,
        EmberMoth = 3,
        CrownMoth = 4
    }

    public enum CollectibleRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3
    }

    public enum CollectibleBehavior
    {
        Swarm = 0,
        Circle = 1,
        Dart = 2,
        Flee = 3
    }

    [Serializable]
    public sealed class CollectibleDefinition
    {
        [SerializeField] private string stableId;
        [SerializeField] private string displayName;
        [SerializeField] private CollectibleType type;
        [SerializeField] private CollectibleRarity rarity;
        [SerializeField] private CollectibleBehavior behavior;
        [SerializeField] private int baseValue;

        public string StableId => stableId;
        public string DisplayName => displayName;
        public CollectibleType Type => type;
        public CollectibleRarity Rarity => rarity;
        public CollectibleBehavior Behavior => behavior;
        public int BaseValue => baseValue;

        public CollectibleDefinition(string stableId, string displayName, CollectibleType type,
            CollectibleRarity rarity, int baseValue, CollectibleBehavior behavior)
        {
            this.stableId = stableId;
            this.displayName = displayName;
            this.type = type;
            this.rarity = rarity;
            this.baseValue = baseValue;
            this.behavior = behavior;
            ValidateOrThrow();
        }

        public bool IsValid(out string error)
        {
            if (!StableIdContract.IsValid(stableId))
            {
                error = "Stable collectible ID must use lowercase ASCII letters, digits, '.' or '-'.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(displayName))
            {
                error = "Collectible display name is required.";
                return false;
            }
            if (type == CollectibleType.None || !Enum.IsDefined(typeof(CollectibleType), type))
            {
                error = "Collectible type must be a defined nonzero value.";
                return false;
            }
            if (!Enum.IsDefined(typeof(CollectibleRarity), rarity)
                || !Enum.IsDefined(typeof(CollectibleBehavior), behavior))
            {
                error = "Collectible rarity and behavior must be defined values.";
                return false;
            }
            if (baseValue <= 0)
            {
                error = "Collectible base value must be positive.";
                return false;
            }
            error = null;
            return true;
        }

        public void ValidateOrThrow()
        {
            if (!IsValid(out var error)) throw new ArgumentException(error, nameof(CollectibleDefinition));
        }
    }

    public static class CollectibleCatalog
    {
        public static readonly CollectibleDefinition SunMoth = new CollectibleDefinition(
            "sun-moth", "Sun Moth", CollectibleType.SunMoth, CollectibleRarity.Common, 10,
            CollectibleBehavior.Swarm);
        public static readonly CollectibleDefinition MoonMoth = new CollectibleDefinition(
            "moon-moth", "Moon Moth", CollectibleType.MoonMoth, CollectibleRarity.Uncommon, 20,
            CollectibleBehavior.Circle);
        public static readonly CollectibleDefinition EmberMoth = new CollectibleDefinition(
            "ember-moth", "Ember Moth", CollectibleType.EmberMoth, CollectibleRarity.Rare, 35,
            CollectibleBehavior.Dart);
        public static readonly CollectibleDefinition CrownMoth = new CollectibleDefinition(
            "crown-moth", "Crown Moth", CollectibleType.CrownMoth, CollectibleRarity.Epic, 60,
            CollectibleBehavior.Flee);

        private static readonly IReadOnlyList<CollectibleDefinition> all = Array.AsReadOnly(new[]
        {
            SunMoth, MoonMoth, EmberMoth, CrownMoth
        });

        public static IReadOnlyList<CollectibleDefinition> All => all;

        public static CollectibleDefinition Find(string stableId)
        {
            if (stableId == null) return null;
            foreach (var definition in all)
                if (string.Equals(definition.StableId, stableId, StringComparison.Ordinal)) return definition;
            return null;
        }
    }

    public readonly struct CatchResult
    {
        public readonly string CollectibleId;
        public readonly CollectibleType Type;
        public readonly CollectibleRarity Rarity;
        public readonly int BaseValue;
        public readonly int Combo;
        public readonly int AwardedValue;
        public readonly int TotalValue;
        public readonly int TypeCount;
        public readonly int TotalCaught;
        public readonly string ObjectiveId;

        public bool IsValid => !string.IsNullOrEmpty(CollectibleId) && Type != CollectibleType.None;
        public bool IsCourseObjective => !string.IsNullOrEmpty(ObjectiveId);

        public CatchResult(CollectibleDefinition definition, int combo, int awardedValue,
            int totalValue, int typeCount, int totalCaught)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            CollectibleId = definition.StableId;
            Type = definition.Type;
            Rarity = definition.Rarity;
            BaseValue = definition.BaseValue;
            Combo = combo;
            AwardedValue = awardedValue;
            TotalValue = totalValue;
            TypeCount = typeCount;
            TotalCaught = totalCaught;
            ObjectiveId = null;
        }

        private CatchResult(CatchResult source, string objectiveId)
        {
            CollectibleId = source.CollectibleId;
            Type = source.Type;
            Rarity = source.Rarity;
            BaseValue = source.BaseValue;
            Combo = source.Combo;
            AwardedValue = source.AwardedValue;
            TotalValue = source.TotalValue;
            TypeCount = source.TypeCount;
            TotalCaught = source.TotalCaught;
            ObjectiveId = objectiveId;
        }

        public CatchResult ForObjective(string objectiveId)
        {
            if (!IsValid || !StableIdContract.IsValid(objectiveId))
                throw new ArgumentException("A valid catch and stable objective id are required.", nameof(objectiveId));
            return new CatchResult(this, objectiveId);
        }
    }

    public readonly struct CollectibleTally
    {
        public readonly string CollectibleId;
        public readonly CollectibleType Type;
        public readonly int Count;
        public readonly int AwardedValue;

        public CollectibleTally(string collectibleId, CollectibleType type, int count, int awardedValue)
        {
            CollectibleId = collectibleId;
            Type = type;
            Count = count;
            AwardedValue = awardedValue;
        }
    }

    internal static class StableIdContract
    {
        public static bool IsValid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 64) return false;
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '.' && c != '-')
                    return false;
            }
            return true;
        }
    }
}
