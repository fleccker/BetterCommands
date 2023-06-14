using CustomPlayerEffects;

using System;

namespace BetterCommands.Arguments.Effects
{
    public struct EffectData
    {
        public Type EffectType { get; }
        public string EffectName { get; }

        public EffectData(Type effectType, string effectName)
        {
            EffectType = effectType;
            EffectName = effectName;
        }

        public bool TryGet(ReferenceHub target, out StatusEffectBase statusEffect) 
        {
            if (target.playerEffectsController._effectsByType.TryGetValue(EffectType, out statusEffect))
                return true;

            statusEffect = null;
            return false;
        }

        public bool IfIs<TEffect>(ReferenceHub target, Action<TEffect> execute) where TEffect : StatusEffectBase
        {
            if (target.playerEffectsController._effectsByType.TryGetValue(EffectType, out var statusEffect))
            {
                if (statusEffect is TEffect effect)
                {
                    execute?.Invoke(effect);
                    return true;
                }
            }

            return false;
        }
    }
}
