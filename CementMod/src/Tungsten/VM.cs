using System.Reflection;
using System;
using System.Collections.Generic;
using CementGB.Mod.Utilities;
using UnityEngine.Playables;
using static Il2CppMono.Security.X509.X520;
using Il2CppInterop.Runtime;
using System.Linq;
using Il2CppSystem.Xml.Serialization;
using System.Collections;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace Tungsten;
public static class VM
{
    private static MutStack<object?> stack = new(100);
    private static Stack<int> callStack = new();
    private static Stack<int> _tempTopStackStore = new();

    public static object? RunFunction(ProgramInfo info, string functionName, params object?[] args)
    {
        stack.Clear();
        if (args != null)
            foreach (object? arg in args)
                stack.Push(arg);

        if (!info.functions.ContainsKey(functionName))
        {
            // raise an error
            LoggingUtilities.VerboseLog("ERROR! No function.");
            return null;
        }

        if (args == null)
        {
            if (info.functions[functionName].arity > 0)
            {
                // raise error
                LoggingUtilities.VerboseLog("ERROR! Incorrect number of args");
                return null;
            }
        }
        else if (info.functions[functionName].arity != args.Length)
        {
            // raise an error
            LoggingUtilities.VerboseLog("ERROR! Incorrect number of args");
            return null;
        }

        int pc = info.functions[functionName].start;
        
        while (true)
        {
            Instruction ins = info.instructions[pc++];
            LoggingUtilities.VerboseLog($"{ins.op} {ins.operand}");
            switch (ins.op)
            {
                case Opcode.JMP:
                    pc = (int)ins.operand;
                    break;

                case Opcode.JFZ:
                    object? cond = stack.Pop();
                    if (cond == null)
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    if (cond.GetType() != typeof(bool))
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    bool shouldJump = !(bool)cond;
                    pc += Convert.ToInt32(shouldJump) * (int)ins.operand;
                    break;

                case Opcode.JNZ:
                    cond = stack.Pop();
                    if (cond == null)
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    if (cond.GetType() != typeof(bool))
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    shouldJump = (bool)cond;
                    pc += Convert.ToInt32(shouldJump) * (int)ins.operand;
                    break;

                case Opcode.SETGLOBAL:
                    string globalName = (string)ins.operand;
                    info.globalVariables[globalName] = stack.Peek();
                    break;

                case Opcode.PUSHGLOBAL:
                    globalName = (string)ins.operand;
                    if (!info.globalVariables.ContainsKey(globalName))
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }
                    stack.Push(info.globalVariables[globalName]);
                    break;

                case Opcode.SETLOCAL:
                    int stackOffset = (int)ins.operand;
                    stack[stackOffset] = stack.Peek();
                    break;

                case Opcode.PUSHLOCAL:
                    stackOffset = (int)ins.operand;
                    stack.Push(stack[stackOffset]);
                    break;

                case Opcode.PREPCALL:
                    // this will be where the arguments begin
                    _tempTopStackStore.Push(stack.top);
                    break;

                case Opcode.CALL:
                    callStack.Push(stack.indexBase);
                    callStack.Push(pc);

                    stack.indexBase = _tempTopStackStore.Pop();
                    pc = info.functions[(string)ins.operand].start;

                    break;

                case Opcode.RETURN:
                    object? temp = stack.Pop();

                    // if there is nothing on the call stack, we have finished executing the function
                    if (callStack.Count == 0)
                        return temp;

                    int newPc = callStack.Pop();
                    pc = newPc;
                    stack.Pop(stack.top - stack.indexBase);
                    stack.indexBase = callStack.Pop();

                    stack.Push(temp);
                    break;

                case Opcode.PUSH:
                    stack.Push(ins.operand);
                    break;

                case Opcode.POP:
                    stack.Pop((int)ins.operand);
                    break;

                case Opcode.EQU:
                    object? b = stack.Pop();
                    object? a = stack.Pop();

                    if (a == null)
                    {
                        if (b == null)
                        {
                            stack.Push(true);
                            break;
                        }

                        stack.Push(b.Equals(a));
                        break;
                    }
                        
                    stack.Push(a.Equals(b));
                    break;

                case Opcode.INV:
                    a = stack.Pop();
                    if (a == null)
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    if (a.GetType() != typeof(bool))
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    stack.Push(!(bool)a);

                    break;

                case Opcode.LSS:
                    b = stack.Pop();
                    a = stack.Pop();

                    if (a == null || b == null)
                    {
                        // raise error: null reference exception
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    if (a.GetType() == typeof(int) && b.GetType() == typeof(int))
                        stack.Push((int)a < (int)b);

                    else if ((a.GetType() == typeof(int) && b.GetType() == typeof(float)) || (a.GetType() == typeof(float) && b.GetType() == typeof(int)))
                        stack.Push(Convert.ToSingle(a) < Convert.ToSingle(b));

                    else if (a.GetType() == typeof(float) && b.GetType() == typeof(float))
                        stack.Push((float)a < (float)b);
                    else
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    break;

                case Opcode.GRT:
                    b = stack.Pop();
                    a = stack.Pop();

                    if (a == null || b == null)
                    {
                        // raise error: null reference exception
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    if (a.GetType() == typeof(int) && b.GetType() == typeof(int))
                        stack.Push((int)a > (int)b);

                    else if ((a.GetType() == typeof(int) && b.GetType() == typeof(float)) || (a.GetType() == typeof(float) && b.GetType() == typeof(int)))
                        stack.Push(Convert.ToSingle(a) > Convert.ToSingle(b));

                    else if (a.GetType() == typeof(float) && b.GetType() == typeof(float))
                        stack.Push((float)a > (float)b);
                    else
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    break;

                case Opcode.ADD:
                    b = stack.Pop();
                    a = stack.Pop();

                    if (a == null || b == null)
                    {
                        // raise error: null reference exception
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    if (a.GetType() == typeof(int) && b.GetType() == typeof(int))
                        stack.Push((int)a + (int)b);

                    else if ((a.GetType() == typeof(int) && b.GetType() == typeof(float)) || (a.GetType() == typeof(float) && b.GetType() == typeof(int)))
                        stack.Push(Convert.ToSingle(a) + Convert.ToSingle(b));

                    else if (a.GetType() == typeof(float) && b.GetType() == typeof(float))
                        stack.Push((float)a + (float)b);

                    else if (a.GetType() == typeof(string) && b.GetType() == typeof(string))
                        stack.Push((string)a + (string)b);
                    else
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    break;

                case Opcode.SUB:
                    b = stack.Pop();
                    a = stack.Pop();

                    if (a == null || b == null)
                    {
                        // raise error: null reference exception
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    if (a.GetType() == typeof(int) && b.GetType() == typeof(int))
                        stack.Push((int)a - (int)b);

                    else if ((a.GetType() == typeof(int) && b.GetType() == typeof(float)) || (a.GetType() == typeof(float) && b.GetType() == typeof(int)))
                        stack.Push(Convert.ToSingle(a) - Convert.ToSingle(b));

                    else if (a.GetType() == typeof(float) && b.GetType() == typeof(float))
                        stack.Push((float)a - (float)b);
                    else
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    break;

                case Opcode.MULT:
                    b = stack.Pop();
                    a = stack.Pop();

                    if (a == null || b == null)
                    {
                        // raise error: null reference exception
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    if (a.GetType() == typeof(int) && b.GetType() == typeof(int))
                        stack.Push((int)a * (int)b);

                    else if ((a.GetType() == typeof(int) && b.GetType() == typeof(float)) || (a.GetType() == typeof(float) && b.GetType() == typeof(int)))
                        stack.Push(Convert.ToSingle(a) * Convert.ToSingle(b));

                    else if (a.GetType() == typeof(float) && b.GetType() == typeof(float))
                        stack.Push((float)a * (float)b);
                    else
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    break;

                case Opcode.DIV:
                    b = stack.Pop();
                    a = stack.Pop();

                    if (a == null || b == null)
                    {
                        // raise error: null reference exception
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    if (a.GetType() == typeof(int) && b.GetType() == typeof(int))
                        stack.Push((int)a / (int)b);

                    else if ((a.GetType() == typeof(int) && b.GetType() == typeof(float)) || (a.GetType() == typeof(float) && b.GetType() == typeof(int)))
                        stack.Push(Convert.ToSingle(a) / Convert.ToSingle(b));

                    else if (a.GetType() == typeof(float) && b.GetType() == typeof(float))
                        stack.Push((float)a / (float)b);
                    else
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }
                    break;

                case Opcode.EXT_INST_CALL:
                    if (!ExtCall(false, false, (string)ins.operand)) return null;
                    break;
                    
                case Opcode.EXT_STATIC_CALL:
                    if (!ExtCall(true, false, (string)ins.operand)) return null;
                    break;

                case Opcode.EXT_CTOR:
                    if (!ExtCall(true, true, (string)ins.operand)) return null;
                    break;

                case Opcode.PUSH_INST_FIELD:
                    a = stack.Pop();

                    if (a == null)
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    // check if failed
                    if (!PushField(a.GetType(), (string)ins.operand, a))
                        return null;

                    break;

                case Opcode.SET_INST_FIELD:
                    b = stack.Pop(); // b is the value
                    a = stack.Pop();

                    if (a == null)
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    if (!SetField(a.GetType(), (string)ins.operand, a, b))
                        return null;

                    stack.Push(b);
                    break;

                case Opcode.PUSH_STATIC_FIELD:
                    (string className, string fieldName) = GetClassAndSecondary((ins.operand as string)!);

                    Type? type = GetType(className);
                    if (type == null) {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    if (!PushField(type, fieldName, null))
                        return null;

                    break;

                case Opcode.SET_STATIC_FIELD:
                    b = stack.Peek(); // value

                    (className, fieldName) = GetClassAndSecondary((ins.operand as string)!);

                    type = GetType(className);
                    if (type == null)
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    if (!SetField(type, fieldName, null, b))
                        return null;

                    break;

                case Opcode.GETARR:
                    object? idxRaw = stack.Pop();
                    int idx = 0;

                    if (idxRaw == null || idxRaw.GetType() != typeof(int)) {

                        // raise error 
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    idx = (int)idxRaw!;
                    
                    a = stack.Pop();

                    if (a == null)
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    if (a.GetType().IsArray)
                    {
                        stack.Push((a as Array)!.GetValue(idx));
                    }
                    else if (a.GetType().IsAssignableTo(typeof(IList)))
                    {
                        stack.Push((a as IList)![idx]);
                    }
                    else if (a.GetType().FullName!.Split('`')[0] == typeof(Il2CppSystem.Collections.Generic.List<>).FullName!.Split('`')[0])
                    {
                        var items = a.GetType().GetProperty("_items").GetValue(a);
                        var enumerator = (IEnumerator)items.GetType().GetMethod("GetEnumerator").Invoke(items, null)!;
                        enumerator.MoveNext();
                        for (int i = 0; i < idx; ++i)
                            enumerator.MoveNext();
                        stack.Push(enumerator.Current);
                    }
                    else 
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    break;

                case Opcode.SETARR:
                    b = stack.Pop();
                    idx = (int)stack.Pop()!;
                    a = stack.Pop();

                    if (a == null)
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    if (a.GetType().IsArray)
                    {
                        (a as Array)!.SetValue(b, idx);
                    }
                    else if (a.GetType() == typeof(List<>))
                    {
                        a.GetType().GetGenericArguments();
                        (a as IList)![idx] = b;
                    }
                    else
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    stack.Push(b);
                    break;

                case Opcode.CRTARR:
                    int topStack = _tempTopStackStore.Pop();
                    int elementCount = stack.top - topStack;

                    List<object?> arr = new();
                    for (int i = 0; i < elementCount; ++i)
                        arr.Add(stack.Pop());

                    arr.Reverse();
                    stack.Push(arr);

                    break;
            }

        }
    }

    private static (string, string) GetClassAndSecondary(string name)
    {
        string[] split = name.Split('.');
        string className = "";
        string sec = split.Last();
        for (int i = 0; i < split.Length - 1; ++i)
        {
            className += split[i];
            if (i < split.Length - 2)
            {
                className += '.';
            }
        }

        return (className, sec);
    }

    private static bool ExtCall(bool isStatic, bool isCtor, string name)
    {
        int topStack = _tempTopStackStore.Pop();
        int argsPassed = stack.top - topStack;

        object? a = null;
        if (!isStatic)
            a = stack.Pop();

        if (!isStatic && a == null)
        {
            // raise error
            LoggingUtilities.VerboseLog("ERROR!");
            return false;
        }

        object?[] objects = stack.PeekTop(argsPassed);

        Type? type;
        if (isStatic)
        {
            if (isCtor)
            {
                type = GetType(name);

                if (type == null)
                {
                    // raise error
                    LoggingUtilities.VerboseLog($"ERROR! Couldn't find type {type}");
                    return false;
                }
            }
            else
            {
                string[] split = name.Split('.');
                string className = string.Join('.', split.AsSpan(0, split.Length - 1).ToArray());
                name = split.Last();

                type = GetType(className);

                if (type == null)
                {
                    // raise error
                    LoggingUtilities.VerboseLog($"ERROR! Couldn't find the type {className}");
                    return false;
                }
            }
        }
        else
        {
            type = a!.GetType();
        }

        object? result;
        int tempIndexBase;
        if (isCtor)
        {
            Type[] types = new Type[argsPassed];
            int i = 0;
            foreach (object? obj in objects)
            {
                types[i++] = obj!.GetType();
            }
            ConstructorInfo? ctorInfo = type.GetConstructor(types);

            if (ctorInfo == null || argsPassed != ctorInfo.GetParameters().Length)
            {
                // raise error
                LoggingUtilities.VerboseLog("ERROR! Couldn't find method with that overload.");
                return false;
            }

            tempIndexBase = stack.indexBase;
            stack.indexBase = topStack;

            result = ctorInfo.Invoke(objects);
        }
        else {
            MethodInfo? methodInfo;
            try
            {
                LoggingUtilities.VerboseLog("Trying to find method without types.");
                methodInfo = type.GetMethod(name);
            }
            catch
            {
                LoggingUtilities.VerboseLog("Couldn't find method without types... Trying with types");
                Type[] types = new Type[argsPassed];
                int i = 0;
                foreach (object? obj in objects)
                {
                    types[i++] = obj!.GetType();
                }
                methodInfo = type.GetMethod(name, types);
            }

            if (methodInfo == null)
            {
                // raise error
                LoggingUtilities.VerboseLog("ERROR! Couldn't find method!");
                return false;
            }

            if (argsPassed != methodInfo.GetParameters().Length)
            {
                // raise error
                LoggingUtilities.VerboseLog("ERROR! Not correct amount of args!");
                return false;
            }

            tempIndexBase = stack.indexBase;
            stack.indexBase = topStack;

            result = methodInfo.Invoke(a, objects);
        }

        stack.Pop(argsPassed);
        stack.indexBase = tempIndexBase;

        stack.Push(result);

        return true;
    }

    private static bool PushField(Type type, string name, object? reference)
    {
        FieldInfo? fieldInfo = type.GetField(name);

        PropertyInfo? propertyInfo;
        if (fieldInfo == null)
        {
            propertyInfo = type.GetProperty(name);
            if (propertyInfo == null)
            {
                // raise error
                LoggingUtilities.VerboseLog("ERROR!");
                return false;
            }
            stack.Push(propertyInfo.GetValue(reference));
        }
        else
        {
            stack.Push(fieldInfo.GetValue(reference));
        }

        return true;
    }

    private static bool SetField(Type type, string name, object? reference, object? value)
    {

        var fieldInfo = type.GetField(name);

        if (fieldInfo == null)
        {
            var propertyInfo = type.GetProperty(name);
            if (propertyInfo == null)
            {
                // raise error
                LoggingUtilities.VerboseLog("ERROR!");
                return false;
            }
            propertyInfo.SetValue(reference, value);
        }
        else
        {
            fieldInfo.SetValue(reference, value);
        }

        return true;
    }

    private static Dictionary<string, Type?> _nameToTypeCache = new();
    public static Type? GetType(string name)
    {
        if (_nameToTypeCache.ContainsKey(name))
            return _nameToTypeCache[name];

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (Type type in assembly.GetTypes())
                if (type.Name == name || type.FullName == name)
                {
                    //var il2cppType = Il2CppType.From(type);
                    _nameToTypeCache[name] = type;
                    return type;
                }

        return null;
    }

    public static Il2CppSystem.Type? GetIl2CppType(string name)
    {
        Type? type = GetType(name);
        if (type == null) return null;
        return Il2CppType.From(type);
    }

    public static void Print(string msg)
    {
        LoggingUtilities.VerboseLog(msg);
    }
}
