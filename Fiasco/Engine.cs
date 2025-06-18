using System.Runtime.InteropServices;
using Google.FlatBuffers;
using Graphics;

namespace Fiasco;

// https://github.com/vaguevoid/engine/blob/main/crates/void_public/src/lib.rs#L701
public enum CallbackTypes {
    SpawnFn,
    DespawnFn,
    EventCountFn,
    EventGetFn,
    EventSendFn,
    QueryLenFn,
    QueryGetFn,
    QueryGetMutFn,
    QueryGetFirstFn,
    QueryGetFirstMutFn,
    QueryForEachFn,
    QueryParForEachFn,
}

public delegate nuint Spawn(nint ptr, byte[] data, int size);
public delegate nuint Despawn(nint ptr, byte[] data, int size);
public delegate nuint EventCount(nint ptr, byte[] data, int size);
public delegate nuint EventGet(nint ptr, byte[] data, int size);
public delegate nuint EventSend(nint ptr, byte[] data, int size);
public delegate nuint QueryLen(nint ptr, byte[] data, int size);
public delegate nuint QueryGet(nint ptr, byte[] data, int size);
public delegate nuint QueryGetMut(nint ptr, byte[] data, int size);
public delegate nuint QueryGetFirst(nint ptr, byte[] data, int size);
public delegate nuint QueryGetFirstMut(nint ptr, byte[] data, int size);
public delegate nuint QueryForEach(nint ptr, byte[] data, int size);
public delegate nuint QueryParForEach(nint ptr, byte[] data, int size);

public enum ComponentTypes {
    Component,
    Resource,
}

public enum ArgType {
    DataAccessMut,
    DataAccessRef,
    EventReader,
    EventWriter,
    Query,
}

public class JasonGame : IGame
{
    private static readonly nint DrawCirclePtr = Marshal.StringToCoTaskMemUTF8(typeof(DrawCircle).FullName);
    private static readonly nint DrawTextPtr = Marshal.StringToCoTaskMemUTF8(typeof(DrawText).FullName);
    private static readonly nint DrawLinePtr = Marshal.StringToCoTaskMemUTF8(typeof(DrawLine).FullName);
    private static readonly nint DrawRectanglePtr = Marshal.StringToCoTaskMemUTF8(typeof(DrawRectangle).FullName);
    private static readonly byte[] CircleBytes = CreateDrawCircleBytes();
    private static readonly byte[] TextBytes = CreateDrawTextBytes();
    private static readonly byte[] LineBytes = CreateDrawLineBytes();
    private static readonly byte[] RectangleBytes = CreateDrawRectangleBytes();
    private delegate nuint FnDelegate(nuint ptr);
    private static readonly FnDelegate Fn = ptr =>
    {
        Console.WriteLine("JasonGame.MySystem called!");
               
        Console.WriteLine($"Sending draw circle event, {CircleBytes.Length}");
        EcsModule.SendEvent(ptr, 0, CircleBytes);
        
        Console.WriteLine($"Sending draw text event, {TextBytes.Length}");
        EcsModule.SendEvent(ptr, 1, TextBytes);
        
        Console.WriteLine($"Sending draw line event, {LineBytes.Length}");
        EcsModule.SendEvent(ptr, 2, LineBytes);
        
        Console.WriteLine($"Sending draw rectangle event, {RectangleBytes.Length}");
        EcsModule.SendEvent(ptr, 3, RectangleBytes);
        return 0;
    };
    
    public SystemDefinition[] SystemDefinitions { get; } = [
        new(
            false,
            [ArgType.EventWriter, ArgType.EventWriter, ArgType.EventWriter, ArgType.EventWriter],
            [[DrawCirclePtr], [DrawTextPtr], [DrawLinePtr], [DrawRectanglePtr]],
            Marshal.GetFunctionPointerForDelegate(Fn)
        )
    ];
    
    private static byte[] CreateDrawRectangleBytes()
    {
        var fbb = new FlatBufferBuilder(1);
        DrawRectangle.StartDrawRectangle(fbb);
        var transformOffset = Transform.CreateTransform(fbb, 600, 600, 1, 100, 100, 0, 0, 0, 0, 0);
        DrawRectangle.AddTransform(fbb, transformOffset);
        DrawRectangle.AddColor(fbb, Color.CreateColor(fbb, 1, 1, 0, 1));
        fbb.Finish(DrawRectangle.EndDrawRectangle(fbb).Value);
        return fbb.DataBuffer.ToSizedArray();
    }
    
    private static byte[] CreateDrawCircleBytes()
    {
        var fbb = new FlatBufferBuilder(1);
        var offset = DrawCircle.CreateDrawCircle(fbb, 100, 100, 10, 100, 32, 0, 1, 1, 1, 1);
        fbb.Finish(offset.Value);
        return fbb.DataBuffer.ToSizedArray();
    }
    
    private static byte[] CreateDrawLineBytes()
    {
        var fbb = new FlatBufferBuilder(1);
        var offset = DrawLine.CreateDrawLine(fbb, 400, 400, 600, 450, 1, 4, 0, 1, 1, 1);
        fbb.Finish(offset.Value);
        return fbb.DataBuffer.ToSizedArray();
    }
    
    private static byte[] CreateDrawTextBytes()
    {
        var fbb = new FlatBufferBuilder(1);
        var textOffset = fbb.CreateString("JASON");
        
        DrawText.StartDrawText(fbb);
        DrawText.AddZ(fbb, 10);
        DrawText.AddFontSize(fbb, 64);
        DrawText.AddTextAlignment(fbb, TextAlignment.Center);

        DrawText.AddText(fbb, textOffset);

        var boundsOffset = Vec2.CreateVec2(fbb, 200, 100);
        DrawText.AddBounds(fbb, boundsOffset);
        
        var colorOffset = Color.CreateColor(fbb, 1, 1, 1, 1);
        DrawText.AddColor(fbb, colorOffset);
        
        var transformOffset = Transform.CreateTransform(fbb, 300, 300, 1, 1, 1, 1, 1, 0, 0, 0);
        DrawText.AddTransform(fbb, transformOffset);
        
        fbb.Finish(DrawText.EndDrawText(fbb).Value);
        return fbb.DataBuffer.ToSizedArray();
    }
}

public static class EcsModule
{
    private static readonly IGame Game = new JasonGame();
    private static readonly Dictionary<string, nint> ComponentIds = new();
    private static uint MakeApiVersion(uint major, uint minor, uint patch) => (major << 25) | (minor << 15) | patch;
    private static uint EngineVersion() => MakeApiVersion(0, 0, 3);
    
    private static Spawn? _spawn;
    private static Despawn? _despawn;
    private static EventCount? _eventCount;
    private static EventGet? _eventGet;
    private static EventSend? _eventSend;
    private static QueryLen? _queryLen;
    private static QueryGet? _queryGet;
    private static QueryGetMut? _queryGetMut;
    private static QueryGetFirst? _queryGetFirst;
    private static QueryGetFirstMut? _queryGetFirstMut;
    private static QueryForEach? _queryForEach;
    private static QueryParForEach? _queryParForEach;
    
    public static void SendEvent(nuint sysPtr, nint offset, byte[] bytes)
    {
        var argPtr = Marshal.ReadIntPtr((nint)sysPtr + offset * 8);
        _eventSend!(argPtr, bytes, bytes.Length);
    }

    [UnmanagedCallersOnly(EntryPoint = "set_component_id")]
    public static void SetComponentId(nint strIdPtr, nint componentId)
    {
        var stringId = Marshal.PtrToStringUTF8(strIdPtr);
        ArgumentNullException.ThrowIfNull(stringId);
        
        ComponentIds.Add(stringId, componentId);
        Console.WriteLine($"SetComponentId: {stringId} <-> {componentId}");
    }
    
    [UnmanagedCallersOnly(EntryPoint = "resource_init")]
    public static nint ResourceInit(nint strIdPtr, int val)
    {
        var stringId = Marshal.PtrToStringUTF8(strIdPtr);
        Console.WriteLine($"ResourceInit: {stringId} <-> {val}");
        return 0;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "component_size")]
    public static nuint ComponentSize(nint strIdPtr)
    {
        Console.WriteLine($"ComponentSize Called for component id: {Marshal.PtrToStringUTF8(strIdPtr)} - returning 0");
        return 0;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "component_string_id")]
    public static nint ComponentStringId(nuint index)
    {
        Console.WriteLine($"ComponentStringId Called for: {index}. Returning 0");
        return 0;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "component_align")]
    public static nuint ComponentAlignment(nuint strIdPtr)
    {
        var strId = Marshal.PtrToStringUTF8((nint)strIdPtr);
        Console.WriteLine($"ComponentAlignment Called for: {strIdPtr} {strId}. Returning 1");
        return 1;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "component_type")]
    public static ComponentTypes ComponentType(nuint strIdPtr)
    {
        var strId = Marshal.PtrToStringUTF8((nint)strIdPtr);
        Console.WriteLine($"ComponentType Called for: {strIdPtr} {strId}. Returning {ComponentTypes.Component}");
        return ComponentTypes.Component;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "systems_len")]
    public static nint SystemLength()
    {
        Console.WriteLine($"SystemLength Called - returning {Game.SystemDefinitions.Length}");
        return Game.SystemDefinitions.Length;
    }

    [UnmanagedCallersOnly(EntryPoint = "system_is_once")]
    public static bool SystemIsOnce(int systemIndex)
    {
        var once = Game.SystemDefinitions[systemIndex].Once;
        Console.WriteLine($"SystemIsOnce Called for system: {systemIndex} - returning {once}");
        return once;
    }

    [UnmanagedCallersOnly(EntryPoint = "system_fn")]
    public static nint SystemFunction(nuint systemIndex)
    {
        Console.WriteLine($"SystemFunction Called for system: {systemIndex}");
        return Game.SystemDefinitions[systemIndex].Fn;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "system_args_len")]
    public static int SystemArgumentLength(nuint systemIndex)
    {
        var len = Game.SystemDefinitions[systemIndex].ArgTypes.Length;
        Console.WriteLine($"SystemArgumentLength Called for system: {systemIndex}. Returning {len}");
        return len;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "system_arg_type")]
    public static ArgType SystemArgumentType(nuint systemIndex, nuint argIndex)
    {
        var argType = Game.SystemDefinitions[systemIndex].ArgTypes[argIndex];
        Console.WriteLine($"SystemArgumentType Called for system: {systemIndex} - argIndex: {argIndex}. Returning {argType}");
        return argType;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "system_arg_component")]
    public static nuint? SystemArgumentComponent(nuint systemIndex, nuint argIndex)
    {
        Console.WriteLine($"SystemArgumentComponent Called for system: {systemIndex} - argIndex: {argIndex}");
        return null;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "system_arg_event")]
    public static nint SystemArgumentEvent(nuint systemIndex, nuint argIndex)
    {
        var eventType = Game.SystemDefinitions[systemIndex].ArgIds[argIndex][0];
        Console.WriteLine($"SystemArgumentEvent Called for system: {systemIndex} - argIndex: {argIndex}. Returning {eventType}");
        return eventType;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "system_query_args_len")]
    public static nuint SystemQueryArgumentsLength(nuint systemIndex, nuint argIndex)
    {
        Console.WriteLine($"SystemQueryArgumentsLength Called for system: {systemIndex} - argIndex: {argIndex}");
        return 0;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "system_query_arg_type")]
    public static nuint SystemQueryArgumentType(nuint systemIndex, nuint systemArgIndex, nuint queryArgIndex)
    {
        Console.WriteLine($"SystemQueryArgumentType Called for system: {systemIndex} - systemArgIndex: {systemArgIndex} - queryArgIndex {queryArgIndex}");
        return 0;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "system_query_arg_component")]
    public static nuint SystemQueryArgumentComponent(nuint systemIndex, nuint systemArgIndex, nuint queryArgIndex)
    {
        Console.WriteLine($"SystemQueryArgumentComponent Called for system: {systemIndex} - systemArgIndex: {systemArgIndex} - queryArgIndex {queryArgIndex}");
        return 0;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "void_target_version")]
    public static uint TargetVersion()
    {
        var version = EngineVersion();
        Console.WriteLine($"TargetVersion Called! Returning {version}");
        return version;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "set_callback_fn")]
    public static void SetCallbackFunc(CallbackTypes callbackType, nint callbackFuncPtr)
    {
        Console.WriteLine($"SetCallbackFunc Called for type: {callbackType} - ptr: {callbackFuncPtr}");
        switch (callbackType)
        {
            case CallbackTypes.SpawnFn:
                _spawn = Marshal.GetDelegateForFunctionPointer<Spawn>(callbackFuncPtr);
                break;
            case CallbackTypes.DespawnFn:
                _despawn = Marshal.GetDelegateForFunctionPointer<Despawn>(callbackFuncPtr);
                break;
            case CallbackTypes.EventCountFn:
                _eventCount = Marshal.GetDelegateForFunctionPointer<EventCount>(callbackFuncPtr);
                break;
            case CallbackTypes.EventGetFn:
                _eventGet = Marshal.GetDelegateForFunctionPointer<EventGet>(callbackFuncPtr);
                break;
            case CallbackTypes.EventSendFn:
                _eventSend = Marshal.GetDelegateForFunctionPointer<EventSend>(callbackFuncPtr);
                break;
            case CallbackTypes.QueryLenFn:
                _queryLen = Marshal.GetDelegateForFunctionPointer<QueryLen>(callbackFuncPtr);
                break;
            case CallbackTypes.QueryGetFn:
                _queryGet = Marshal.GetDelegateForFunctionPointer<QueryGet>(callbackFuncPtr);
                break;
            case CallbackTypes.QueryGetMutFn:
                _queryGetMut = Marshal.GetDelegateForFunctionPointer<QueryGetMut>(callbackFuncPtr);
                break;
            case CallbackTypes.QueryGetFirstFn:
                _queryGetFirst = Marshal.GetDelegateForFunctionPointer<QueryGetFirst>(callbackFuncPtr);
                break;
            case CallbackTypes.QueryGetFirstMutFn:
                _queryGetFirstMut = Marshal.GetDelegateForFunctionPointer<QueryGetFirstMut>(callbackFuncPtr);
                break;
            case CallbackTypes.QueryForEachFn:
                _queryForEach = Marshal.GetDelegateForFunctionPointer<QueryForEach>(callbackFuncPtr);
                break;
            case CallbackTypes.QueryParForEachFn:
                _queryParForEach = Marshal.GetDelegateForFunctionPointer<QueryParForEach>(callbackFuncPtr);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(callbackType), callbackType, "Unknown callback type");
        }
    }
}