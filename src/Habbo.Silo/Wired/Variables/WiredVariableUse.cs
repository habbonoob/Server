using System.Numerics;

namespace Habbo.Silo.Wired.Variables;

/// <summary>What the boxes that work with variables share: which variable they chose, who holds
/// it in their merged slots, the numbers they were given, and the operations they do.</summary>
public static class WiredVariableUse
{
    /// <summary>The variable the box chose at an index of its variable ids; null when none or gone.</summary>
    public static WiredVariable? Variable(WiredBox box, IWiredRoom room, int index = 0) =>
        index < box.Settings.VariableIds.Count && box.Settings.VariableIds[index] is { Length: > 0 } id
            ? room.FindVariable(id)
            : null;

    /// <summary>Who a merged slot names for a target: its furni, its users, the room, or the run.</summary>
    public static IEnumerable<WiredHolder> Holders(WiredBox box, WiredRun run, int target, int slot = 0) => target switch
    {
        WiredVariableTargets.Furni => run.Furni(box, slot).Select(WiredHolder.Of).ToList(),
        WiredVariableTargets.User => run.Users(box, slot).Select(WiredHolder.Of).ToList(),
        WiredVariableTargets.Global => [WiredHolder.Room],
        WiredVariableTargets.Context => [WiredHolder.Context],
        _ => [],
    };

    /// <summary>A number the dialog sent as a long: a sign word, then the value, which fits an int.</summary>
    public static int Number(WiredBox box, int highIndex) => box.IntParam(highIndex + 1);

    /// <summary>A reference value: the dialog's number, or the value the reference variable has in
    /// the first of the reference slot's holders that has it; null when none does.</summary>
    public static int? Reference(WiredBox box, WiredRun run, bool fromVariable, int number, int target,
        int variableIndex = 1, int slot = 1)
    {
        if (!fromVariable)
            return number;
        if (Variable(box, run.Room, variableIndex) is not { } variable)
            return null;

        foreach (var holder in Holders(box, run, target, slot))
        {
            if (run.Room.VariableValue(variable, holder, run) is { } value)
                return value;
        }

        return null;
    }

    // wiredfurni.params.variables.operation.N
    public const int Assign = 0;
    public const int Add = 1;
    public const int Subtract = 2;
    public const int Multiply = 3;
    public const int Divide = 4;
    public const int Power = 5;
    public const int Modulo = 6;
    public const int SetMinimum = 40;
    public const int SetMaximum = 41;
    public const int RandomUpTo = 50;
    public const int Absolute = 60;
    public const int BitwiseAnd = 100;
    public const int BitwiseOr = 101;
    public const int BitwiseXor = 102;
    public const int BitwiseNot = 103;
    public const int ShiftLeft = 104;
    public const int ShiftRight = 105;
    public const int BitCount = 110;

    /// <summary>Whether an operation takes an operand: not, absolute value and bit count do not (Habbo's dialog shuts
    /// the operand section for them and saves it as nothing).</summary>
    public static bool TakesOperand(int operation) => operation is not (BitwiseNot or Absolute or BitCount);

    /// <summary>The value an operation leaves; null when it cannot be done (dividing by zero, an
    /// operation the dialog does not offer). A result beyond an int stops at its end.</summary>
    public static int? Apply(int operation, int value, int operand, Random random)
    {
        long? result = operation switch
        {
            Assign => operand,
            Add => (long)value + operand,
            Subtract => (long)value - operand,
            Multiply => (long)value * operand,
            Divide => operand == 0 ? null : (long)value / operand,
            Power => (long)Math.Clamp(Math.Pow(value, operand), int.MinValue, int.MaxValue),
            Modulo => operand == 0 ? null : (long)value % operand,
            SetMinimum => Math.Max(value, operand),
            SetMaximum => Math.Min(value, operand),
            RandomUpTo => operand >= 0 ? random.NextInt64(0, (long)operand + 1) : 0,
            Absolute => Math.Abs((long)value),
            BitwiseAnd => value & operand,
            BitwiseOr => value | operand,
            BitwiseXor => value ^ operand,
            BitwiseNot => ~value,
            ShiftLeft => (long)value << Math.Clamp(operand, 0, 31),
            ShiftRight => value >> Math.Clamp(operand, 0, 31),
            BitCount => BitOperations.PopCount((uint)value),
            _ => null,
        };

        return result is { } done ? (int)Math.Clamp(done, int.MinValue, int.MaxValue) : null;
    }
}
