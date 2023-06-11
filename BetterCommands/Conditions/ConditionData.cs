using helpers.Results;

using PlayerRoles;
using PlayerStatsSystem;

using System;

namespace BetterCommands.Conditions
{
    public struct ConditionData
    {
        public ConditionFlag Flags { get; }
        public object ConditionObject { get; }

        public ConditionData(ConditionFlag flags, object condObject)
        {
            Flags = flags;
            ConditionObject = condObject;
        }

        public IResult<object> Validate(ReferenceHub hub)
        {
            if (Flags.HasFlag(ConditionFlag.RoleTypeOnly))
            {
                if (!(ConditionObject is RoleTypeId targetRole)) 
                    return new ErrorResult($"Condition failed: The condition has a RoleTypeOnly flag, but the condition's object is not a role type!");

                if (hub.GetRoleId() != targetRole) 
                    return new ErrorResult($"Condition failed: You must be a {targetRole} to run this command.");

                return new SuccessResult(null);
            }

            if (Flags.HasFlag(ConditionFlag.DisableServerPlayer))
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient) 
                    return new ErrorResult($"Condition failed: You cannot run this command as the server.");

                return new SuccessResult(null);
            }

            if (Flags.HasFlag(ConditionFlag.HealthOnly))
            {
                if (!(ConditionObject is float health)) 
                    return new ErrorResult($"Condition failed: The condition has a HealthOnly flag, but the condition's object is not a valid floating-point number!");

                if (hub.playerStats.GetModule<HealthStat>().NormalizedValue != health) 
                    return new ErrorResult($"Condition failed: You must have precisely {health} health to run this command.");

                return new SuccessResult(null);
            }

            if (Flags.HasFlag(ConditionFlag.Custom))
            {
                if (!(ConditionObject is Func<ReferenceHub, IResult<object>> func)) 
                    return new ErrorResult($"Condition failed: The condition has a Custom flag, but the condition's object is not a valid delegate!");

                return func(hub);
            }

            return new SuccessResult(null);
        }
    }
}