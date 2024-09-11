using System;
using System.Collections.Generic;
using System.Reflection;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

public abstract class SubComponentRegistry(DataTypes dataTypes)
{
    private readonly Dictionary<string, Type> _subComponentParsers = new();

    protected void RegisterSubComponent<T>(string name) where T : SubComponent
    {
        if(_subComponentParsers.TryGetValue(name, out _)) 
            throw new Exception($"Sub component {name} already registered!");

        _subComponentParsers.Add(name, typeof(T));
    }

    public SubComponent ParseSubComponent(string name, Queue<byte> data)
    {
        if(!_subComponentParsers.TryGetValue(name, out var subComponentParserType)) 
            throw new Exception($"Sub component {name} not registered!");

        var instance=  Activator.CreateInstance(subComponentParserType, dataTypes, this) as SubComponent ??
            throw new InvalidOperationException($"Could not create instance of a sub component parser type: {subComponentParserType.Name}");
        
        var parseMethod = instance.GetType().GetMethod("Parse", BindingFlags.Instance | BindingFlags.NonPublic);
        
        if (parseMethod == null)
            throw new InvalidOperationException($"Sub component parser type {subComponentParserType.Name} does not have a Parse method.");
        
        parseMethod.Invoke(instance, new object[] { data });
        return instance;
    }
}