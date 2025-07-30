using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Utility class for working with predefined assemblies in the Unity project.
/// This class provides methods to retrieve types from specific assemblies
/// that implement a given interface.
/// </summary>
public static class PredefinedAssemblyUtil
{
    /// <summary>
    /// Enum representing the predefined assemblies in the Unity project.
    /// </summary>
    enum AssemblyType
    {
        AssemblyCSharp,
        AssemblyCSharpEditor,
        AssemblyCSharpEditorFirstPass,
        AssemblyCSharpFirstPass
    }

    /// <summary>
    /// Maps an assembly name to its corresponding AssemblyType enum value.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly.</param>
    /// <returns>The corresponding AssemblyType, or null if the assembly is not predefined.</returns>
    static AssemblyType? GetAssemblyType(string assemblyName)
    {
        return assemblyName switch
        {
            "Assembly-CSharp" => AssemblyType.AssemblyCSharp,
            "Assembly-CSharp-Editor" => AssemblyType.AssemblyCSharpEditor,
            "Assembly-CSharp-Editor-firstpass" => AssemblyType.AssemblyCSharpEditorFirstPass,
            "Assembly-CSharp-firstpass" => AssemblyType.AssemblyCSharpFirstPass,
            _ => null
        };
    }

    /// <summary>
    /// Adds types from a given assembly to a collection if they implement the specified interface.
    /// </summary>
    /// <param name="assembly">The array of types in the assembly.</param>
    /// <param name="types">The collection to which matching types will be added.</param>
    /// <param name="interfaceType">The interface type to match.</param>
    static void AddTypesFromAssembly(Type[] assembly, ICollection<Type> types, Type interfaceType)
    {
        if (assembly == null) return;

        for (int i = 0; i < assembly.Length; i++)
        {
            Type type = assembly[i];
            if (type != interfaceType && interfaceType.IsAssignableFrom(type))
            {
                types.Add(type);
            }
        }
    }

    /// <summary>
    /// Retrieves all types from predefined assemblies that implement the specified interface.
    /// </summary>
    /// <param name="interfaceType">The interface type to search for.</param>
    /// <returns>A list of types that implement the specified interface.</returns>
    public static List<Type> GetTypes(Type interfaceType)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        Dictionary<AssemblyType, Type[]> assemblyTypes = new Dictionary<AssemblyType, Type[]>();
        List<Type> types = new List<Type>();

        // Iterate through all assemblies and map their types to the predefined AssemblyType enum.
        for (int i = 0; i < assemblies.Length; i++)
        {
            AssemblyType? assemblyType = GetAssemblyType(assemblies[i].GetName().Name);
            if (assemblyType != null)
            {
                assemblyTypes.Add((AssemblyType)assemblyType, assemblies[i].GetTypes());
            }
        }

        // Add types from specific assemblies that implement the given interface.
        AddTypesFromAssembly(assemblyTypes[AssemblyType.AssemblyCSharp], types, interfaceType);
        AddTypesFromAssembly(assemblyTypes[AssemblyType.AssemblyCSharpFirstPass], types, interfaceType);

        return types;
    }
}
