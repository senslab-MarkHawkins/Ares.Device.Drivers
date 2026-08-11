using Ares.Datamodel;

namespace FlowSolver.Models;

public sealed class FlowComponent
{
    public FlowComponent(
        string deviceName,
        IReadOnlyList<CompositionComponent> components)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceName);
        ArgumentNullException.ThrowIfNull(components);

        DeviceName = deviceName.Trim();
        Components = components;
    }

    public string DeviceName { get; }

    public IReadOnlyList<CompositionComponent> Components { get; }

    public static IReadOnlyList<FlowComponent> ComponentsFromAres(
        AresStruct deviceSettings)
    {
        ArgumentNullException.ThrowIfNull(deviceSettings);

        if (!deviceSettings.Fields.TryGetValue(
                "FlowComponents",
                out var flowComponentsValue) ||
            flowComponentsValue.ListValue is null)
        {
            throw new InvalidOperationException(
                "Device settings do not contain a valid FlowComponents list.");
        }

        return flowComponentsValue
            .ListValue
            .Values
            .Where(value => value.StructValue is not null)
            .Select(value =>
                FromAres(value.StructValue!))
            .ToArray();
    }

    public static FlowComponent FromAres(
        AresStruct flowComponentStruct)
    {
        ArgumentNullException.ThrowIfNull(flowComponentStruct);

        var deviceName =
            flowComponentStruct
                .Fields["MFC"]
                .StringValue;

        if (!flowComponentStruct.Fields.TryGetValue(
                "Components",
                out var componentsValue) ||
            componentsValue.ListValue is null)
        {
            throw new InvalidOperationException(
                $"Flow component '{deviceName}' does not contain a valid Components list.");
        }

        var components =
            componentsValue
                .ListValue
                .Values
                .Where(value => value.StructValue is not null)
                .Select(value =>
                    CompositionComponent.FromAresStruct(
                        value.StructValue!))
                .ToArray();

        return new FlowComponent(
            deviceName,
            components);
    }
}