namespace Fiasco;

public interface IGame
{
    public SystemDefinition[] SystemDefinitions { get; }
}

public readonly struct SystemDefinition(bool once, ArgType[] argTypes, nint[][] argIds, nint fn)
{
    public readonly bool Once = once;
    public readonly ArgType[] ArgTypes = argTypes;
    public readonly nint[][] ArgIds = argIds;
    public readonly nint Fn = fn;
}

