using Ares.Datamodel;

namespace FlowSolver.Models;

public sealed class CompositionComponent
{
    public CompositionComponent(
        string component,
        CompositionUnit unit,
        double? value = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(component);

        if (unit != CompositionUnit.Balance)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!double.IsFinite(value.Value) ||
                value.Value < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Composition value must be finite and non-negative.");
            }
        }

        Component = component.Trim();
        Unit = unit;
        Value = value;
    }

    /// <summary>
    /// Gas or named mixture (e.g. CO2, N2, Air).
    /// </summary>
    public string Component { get; }

    /// <summary>
    /// User-specified concentration.
    /// Null when Unit == Balance.
    /// </summary>
    public double? Value { get; }

    /// <summary>
    /// Units supplied by the user/configuration.
    /// </summary>
    public CompositionUnit Unit { get; }

    public static CompositionComponent FromAresStruct(
        AresStruct aresStruct)
    {
        ArgumentNullException.ThrowIfNull(aresStruct);

        var component =
            aresStruct.Fields["Component"].StringValue;

        var unit =
            Enum.Parse<CompositionUnit>(
                aresStruct.Fields["Unit"].StringValue,
                ignoreCase: true);

        double? value =
            unit == CompositionUnit.Balance
                ? null
                : aresStruct.Fields["Value"].NumberValue;

        return new CompositionComponent(
            component,
            unit,
            value);
    }
}