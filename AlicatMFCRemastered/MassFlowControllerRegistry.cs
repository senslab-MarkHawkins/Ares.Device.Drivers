using System.Collections.Concurrent;

namespace AlicatMFCRemastered;

public static class MassFlowControllerRegistry
{
    private static readonly ConcurrentDictionary<
        string,
        MassFlowController> Controllers =
        new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<string> RegisteredNames =>
        Controllers.Keys.ToArray();

    public static bool TryGet(
        string name,
        out MassFlowController? controller)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return Controllers.TryGetValue(
            name.Trim(),
            out controller);
    }

    public static MassFlowController GetRequired(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedName = name.Trim();

        if (!Controllers.TryGetValue(
                normalizedName,
                out var controller))
        {
            throw new KeyNotFoundException(
                $"No Alicat MFC named '{normalizedName}' is registered.");
        }

        return controller;
    }

    internal static void Register(
        MassFlowController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        var name = controller.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                "An Alicat MFC cannot be registered without a device name.");
        }

        if (!Controllers.TryAdd(name, controller))
        {
            throw new InvalidOperationException(
                $"An Alicat MFC named '{name}' is already registered.");
        }
    }

    internal static void Unregister(
        MassFlowController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        var name = controller.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        /*
         * Remove only when the registered object is this exact controller.
         * This prevents an old controller's delayed disposal from removing
         * a replacement controller registered under the same name.
         */
        Controllers.TryRemove(
            new KeyValuePair<string, MassFlowController>(
                name,
                controller));
    }
}