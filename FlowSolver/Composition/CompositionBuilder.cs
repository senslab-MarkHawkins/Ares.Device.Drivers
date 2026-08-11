using FlowSolver.Models;

namespace FlowSolver.Composition;

public sealed class CompositionBuilder
{
    private const double SumTolerance = 1e-10;
    private readonly Dictionary<string, IReadOnlyDictionary<string, double>>
        _knownMixtures;

    public CompositionBuilder(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>>? knownMixtures = null)
    {
        _knownMixtures =
            new Dictionary<string, IReadOnlyDictionary<string, double>>(
                StringComparer.OrdinalIgnoreCase);

        //
        // Built-in mixtures
        //
        var air =
            new Dictionary<string, double>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["N2"] = 0.79,
                ["O2"] = 0.21
            };

        _knownMixtures["Air"] = air;
        _knownMixtures["Zero Air"] = air;
        _knownMixtures["Synthetic Air"] = air;

        //
        // User supplied mixtures override built-ins
        //
        if (knownMixtures != null)
        {
            foreach (var mixture in knownMixtures)
            {
                _knownMixtures[mixture.Key] =
                    mixture.Value;
            }
        }
    }

    public IReadOnlyDictionary<string, double> Build(IEnumerable<CompositionComponent> components)
    {
        ArgumentNullException.ThrowIfNull(components);

        var specifications =
            components.ToArray();

        if (specifications.Length == 0)
        {
            throw new InvalidOperationException("Target composition is empty.");
        }

        Validate(specifications);

        //
        // Stage 1
        // Convert explicit values to mole fractions.
        //
        var unresolved = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        CompositionComponent? balance = null;

        foreach (var component in specifications)
        {
            if (component.Unit == CompositionUnit.Balance)
            {
                balance = component;
                continue;
            }

            var fraction = ToMoleFraction(component.Value!.Value, component.Unit);

            Add(unresolved, component.Component, fraction);
        }

        //
        // Stage 2
        // Resolve balance.
        //
        var assigned = unresolved.Values.Sum();

        if (assigned > 1.0 + SumTolerance)
        {
            throw new InvalidOperationException($"Specified composition totals {assigned:G12}, exceeding 1.");
        }

        if (balance != null)
        {
            Add(unresolved, balance.Component, Math.Max(0.0, 1.0 - assigned));
        }
        else if (Math.Abs(assigned - 1.0) > SumTolerance)
        {
            throw new InvalidOperationException(
                $"Composition totals {assigned:G12}. " +
                "Specify a Balance component or a complete composition.");
        }

        //
        // Stage 3
        // Expand known mixtures recursively.
        //
        var expanded = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in unresolved)
        {
            Expand(
                pair.Key,
                pair.Value,
                expanded,
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase));
        }

        //
        // Stage 4
        // Remove tiny components and normalize floating point error.
        //
        return Normalize(expanded);
    }

    private void Expand(
        string component,
        double fraction,
        Dictionary<string, double> result,
        HashSet<string> recursion)
    {
        if (!_knownMixtures.TryGetValue(component, out var mixture))
        {
            Add(result, component, fraction);

            return;
        }

        if (!recursion.Add(component))
        {
            throw new InvalidOperationException($"Circular mixture definition involving '{component}'.");
        }

        foreach (var pair in mixture)
        {
            Expand(pair.Key, pair.Value * fraction, result, recursion);
        }

        recursion.Remove(component);
    }

    private static void Validate(
        IReadOnlyList<CompositionComponent> components)
    {
        var balanceCount = components.Count(c => c.Unit == CompositionUnit.Balance);

        if (balanceCount > 1)
        {
            throw new InvalidOperationException(
                "Only one Balance component may be specified.");
        }

        foreach (var component in components)
        {
            if (string.IsNullOrWhiteSpace(component.Component))
            {
                throw new InvalidOperationException("Component name is required.");
            }

            if (component.Unit == CompositionUnit.Balance)
            {
                continue;
            }

            if (!component.Value.HasValue)
            {
                throw new InvalidOperationException($"Component '{component.Component}' requires a value.");
            }

            if (!double.IsFinite(component.Value.Value) || component.Value.Value < 0.0)
            {
                throw new InvalidOperationException(
                    $"Invalid value for '{component.Component}'.");
            }
        }
    }

    private static IReadOnlyDictionary<string, double>
        Normalize(            Dictionary<string, double> composition)
    {
        var cleaned =
            composition
                .Where(
                    c => c.Value > SumTolerance)
                .ToDictionary(
                    c => c.Key,
                    c => c.Value,
                    StringComparer.OrdinalIgnoreCase);

        var total =
            cleaned.Values.Sum();

        if (Math.Abs(total - 1.0) >
            SumTolerance)
        {
            throw new InvalidOperationException(
                $"Expanded composition totals {total:G12}.");
        }

        foreach (var gas in cleaned.Keys.ToArray())
        {
            cleaned[gas] /= total;
        }

        return cleaned;
    }

    private static void Add(IDictionary<string, double> dictionary, string component, double value)
    {
        if (dictionary.TryGetValue(component, out var existing))
        {
            dictionary[component] = existing + value;
        }
        else
        {
            dictionary[component] = value;
        }
    }

    private static double ToMoleFraction(double value, CompositionUnit unit)
    {
        return unit switch
        {
            CompositionUnit.MoleFraction => value,

            CompositionUnit.Percent => value / 100.0,

            CompositionUnit.PPM => value / 1_000_000.0,

            CompositionUnit.PPB => value / 1_000_000_000.0,

            CompositionUnit.Balance =>
                throw new InvalidOperationException(
                    "Balance cannot be directly converted."),

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(unit))
        };
    }
}