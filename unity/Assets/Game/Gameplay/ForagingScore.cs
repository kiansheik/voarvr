using System;
using System.Collections.Generic;
using UnityEngine;
namespace VoarVR.Gameplay
{
    // Session points are separate from expedition medals and never modify flight stats.
    public sealed class ForagingScore
    {
        private sealed class MutableTally
        {
            public CollectibleType Type;
            public int Count;
            public int AwardedValue;
        }

        private readonly Dictionary<string, MutableTally> tallies =
            new Dictionary<string, MutableTally>(StringComparer.Ordinal);

        public int Points { get; private set; }
        public int Caught { get; private set; }
        public int Combo { get; private set; }
        private float lastCatch=-100;

        // Compatibility entry point for the shipped Sun Moth v1 score contract.
        public int Catch(float time)
        {
            return Catch(CollectibleCatalog.SunMoth, time).AwardedValue;
        }

        public CatchResult Catch(CollectibleDefinition definition, float time, string courseObjectiveId = null)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            definition.ValidateOrThrow();
            if(tallies.TryGetValue(definition.StableId,out var existing) && existing.Type!=definition.Type)
                throw new InvalidOperationException("A stable collectible ID cannot change type within a score.");
            Combo=time>=lastCatch && time-lastCatch<6?Mathf.Min(Combo+1,5):1;
            lastCatch=time;
            Caught++;
            int reward=definition.BaseValue*Combo;
            Points+=reward;
            if(!tallies.TryGetValue(definition.StableId,out var tally))
            {
                tally=new MutableTally { Type=definition.Type };
                tallies.Add(definition.StableId,tally);
            }
            tally.Count++;
            tally.AwardedValue+=reward;
            var result = new CatchResult(definition,Combo,reward,Points,tally.Count,Caught);
            return string.IsNullOrEmpty(courseObjectiveId) ? result : result.ForObjective(courseObjectiveId);
        }

        public CollectibleTally Tally(string collectibleId)
        {
            return collectibleId!=null && tallies.TryGetValue(collectibleId,out var tally)
                ? new CollectibleTally(collectibleId,tally.Type,tally.Count,tally.AwardedValue)
                : new CollectibleTally(collectibleId,CollectibleType.None,0,0);
        }

        public IReadOnlyList<CollectibleTally> Tallies
        {
            get
            {
                var result=new List<CollectibleTally>(tallies.Count);
                foreach(var pair in tallies)
                    result.Add(new CollectibleTally(pair.Key,pair.Value.Type,pair.Value.Count,pair.Value.AwardedValue));
                result.Sort((a,b)=>string.CompareOrdinal(a.CollectibleId,b.CollectibleId));
                return result;
            }
        }

        public static bool Touches(Vector3 from,Vector3 to,Vector3 food,float radius)
        {
            var delta=to-from;float t=delta.sqrMagnitude>1e-6f?Mathf.Clamp01(Vector3.Dot(food-from,delta)/delta.sqrMagnitude):0;
            return (food-(from+delta*t)).sqrMagnitude<=radius*radius;
        }
    }
}
