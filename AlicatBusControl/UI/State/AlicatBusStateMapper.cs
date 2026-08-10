using Ares.Datamodel;

namespace AlicatBusControl.UI.State;

public static class AlicatBusStateMapper
{
    private const string AppliedStateName = "Applied";
    private const string TargetStateName = "Target";

    public static AlicatBusState FromAresStruct(
        AresStruct state,
        AresStructSchema stateSchema)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(stateSchema);

        var propertyNames =
            GetTargetPropertyNames(stateSchema);

        var appliedFields =
            GetNestedFields(state, AppliedStateName);

        var targetFields =
            GetNestedFields(state, TargetStateName);

        var properties = propertyNames
            .Select(propertyName =>
            {
                var appliedValue = GetNumber(
                    appliedFields,
                    propertyName);

                var targetValue = GetNumber(
                    targetFields,
                    propertyName);

                return new AlicatBusPropertyState
                {
                    Name = propertyName,
                    AppliedValue = appliedValue,
                    TargetValue = targetValue,
                    InputValue = targetValue
                };
            })
            .ToArray();

        return new AlicatBusState
        {
            Name =
                state.Fields.GetValueOrDefault("Name")
                    ?.StringValue
                ?? string.Empty,

            FlowVerificationState =
                state.Fields.GetValueOrDefault(
                        "FlowVerificationState")
                    ?.StringValue
                ?? "Unknown",

            ChangesPending =
                state.Fields.GetValueOrDefault(
                        "ChangesPending")
                    ?.BoolValue
                ?? false,

            Properties = properties
        };
    }

    public static AlicatBusState EmptyFromSchema(
        AresStructSchema stateSchema)
    {
        ArgumentNullException.ThrowIfNull(stateSchema);

        return new AlicatBusState
        {
            Properties = GetTargetPropertyNames(stateSchema)
                .Select(name =>
                    new AlicatBusPropertyState
                    {
                        Name = name,
                        AppliedValue = 0.0,
                        TargetValue = 0.0,
                        InputValue = 0.0
                    })
                .ToArray()
        };
    }

    private static IReadOnlyList<string> GetTargetPropertyNames(
        AresStructSchema stateSchema)
    {
        if (!stateSchema.Fields.TryGetValue(
                TargetStateName,
                out var targetSchema))
        {
            return Array.Empty<string>();
        }

        var fields =
            targetSchema.StructSchema?.Fields;

        if (fields is null)
        {
            return Array.Empty<string>();
        }

        return fields.Keys
            .OrderBy(name =>
                string.Equals(
                    name,
                    "TotalFlow",
                    StringComparison.OrdinalIgnoreCase)
                    ? 1
                    : 0)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, AresValue>
        GetNestedFields(
            AresStruct state,
            string fieldName)
    {
        if (!state.Fields.TryGetValue(
                fieldName,
                out var value) ||
            value.StructValue is null)
        {
            return new Dictionary<string, AresValue>(
                StringComparer.OrdinalIgnoreCase);
        }

        return value.StructValue.Fields;
    }

    private static double GetNumber(
        IReadOnlyDictionary<string, AresValue> fields,
        string fieldName)
    {
        return fields.TryGetValue(
                   fieldName,
                   out var value)
            ? value.NumberValue
            : 0.0;
    }
}