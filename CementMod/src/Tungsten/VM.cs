using System.Reflection;
using System;
using System.Collections.Generic;
using CementGB.Mod.Utilities;
using Il2CppInterop.Runtime;
using System.Linq;
using System.Collections;
using Il2CppSystem.ComponentModel;
using Il2CppCoatsink.Platform.Systems.Online.Connections;
using Il2CppIonic.Zlib;
using System.Runtime.Intrinsics.Arm;

namespace Tungsten;
public static class VM
{
    private static MutStack<object?> stack = new(100);
    private static Stack<int> callStack = new();
    private static Stack<int> _tempTopStackStore = new();

    private static int cachedArrayIndex;
    private static object cachedArray;

    public static bool suppressRuntimeErrors = false;

    private static ErrorManager currentErrorManager;

    private static int pc;
    public static bool HadError {
        get;
        private set;
    }

    public static void Reset()
    {
        HadError = false;
        stack.Clear();
    }

    public static object? PeekStackTop()
    {
        return stack.Peek();
    }

    public static object? RunFunction(ProgramInfo info, string functionName, params object?[] args)
    {
        Reset();

        currentErrorManager = info.errorManager;

        if (args != null)
            foreach (object? arg in args)
                stack.Push(arg);

        if (!info.functions.ContainsKey(functionName))
        {
            Error(-1, $"Couldn't find function '{functionName}' on script.");
            return null;
        }

        if (args == null)
        {
            if (info.functions[functionName].arity > 0)
            {
                Error(-1, "Incorrect number of arguments was passed to the function.");
                return null;
            }
        }
        else if (info.functions[functionName].arity != args.Length)
        {
            Error(-1, "Incorrect number of arguments was passed to the function.");
            return null;
        }

        pc = info.functions[functionName].start;

        while (true)
        {
            Instruction ins = info.instructions[pc++];
            // LoggingUtilities.VerboseLog($"{ins.op} {ins.operand}");

            if (RunInstruction(ins, info)) {
                if (HadError)
                    return null;

                return stack.Pop();
            }
        }
    }

    // returns whether or not execution must stop
    public static bool RunInstruction(Instruction ins, ProgramInfo info)
    {
        switch (ins.op)
        {
            case Opcode.NOP:
                break;

            case Opcode.JMP:
                pc = (int)ins.operand;
                break;

            case Opcode.JFZ:
                object? cond = stack.Pop();

                if (cond == null || cond.GetType() != typeof(bool))
                {
                    Error(ins.lineIdx, "Conditional was not of type boolean.");
                    return true;
                }

                bool shouldJump = !(bool)cond;
                pc = Convert.ToInt32(shouldJump) * ((int)ins.operand - pc) + pc;
                break;

            case Opcode.JNZ:
                cond = stack.Pop();

                if (cond == null || cond.GetType() != typeof(bool))
                {
                    Error(ins.lineIdx, "Conditional was not of type boolean.");
                    return true;
                }

                shouldJump = (bool)cond;
                pc = Convert.ToInt32(shouldJump) * ((int)ins.operand - pc) + pc;
                break;

            case Opcode.SETGLOBAL:
                string globalName = (string)ins.operand;
                info.globalVariables[globalName] = stack.Peek();
                break;

            case Opcode.PUSHGLOBAL:
                globalName = (string)ins.operand;
                if (!info.globalVariables.ContainsKey(globalName))
                {
                    Error(ins.lineIdx, $"The global variable '{globalName}' doesn't exist.");
                    return true;
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
                // if there is nothing on the call stack, we have finished executing the function
                if (callStack.Count == 0)
                    return true;

                object? temp = stack.Pop();

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

                if (a == null || a.GetType() != typeof(bool))
                {
                    Error(ins.lineIdx, "Attempted to invert ('!') a non-boolean value.");
                    return true;
                }

                stack.Push(!(bool)a);

                break;

            case Opcode.LSS:
                b = stack.Pop();
                a = stack.Pop();

                if (a == null || b == null)
                {
                    Error(ins.lineIdx, "Null reference exception");
                    return true;
                }

                if (a.GetType() == typeof(int) && b.GetType() == typeof(int))
                    stack.Push((int)a < (int)b);

                else if ((a.GetType() == typeof(int) && b.GetType() == typeof(float)) || (a.GetType() == typeof(float) && b.GetType() == typeof(int)))
                    stack.Push(Convert.ToSingle(a) < Convert.ToSingle(b));

                else if (a.GetType() == typeof(float) && b.GetType() == typeof(float))
                    stack.Push((float)a < (float)b);
                else
                {
                    Error(ins.lineIdx, $"Invalid comparison between types '{a.GetType()}' and '{b.GetType()}'");
                    return true;
                }

                break;

            case Opcode.GRT:
                b = stack.Pop();
                a = stack.Pop();

                if (a == null || b == null)
                {
                    Error(ins.lineIdx, "Null reference exception");
                    return true;
                }

                if (a.GetType() == typeof(int) && b.GetType() == typeof(int))
                    stack.Push((int)a > (int)b);

                else if ((a.GetType() == typeof(int) && b.GetType() == typeof(float)) || (a.GetType() == typeof(float) && b.GetType() == typeof(int)))
                    stack.Push(Convert.ToSingle(a) > Convert.ToSingle(b));

                else if (a.GetType() == typeof(float) && b.GetType() == typeof(float))
                    stack.Push((float)a > (float)b);
                else
                {
                    Error(ins.lineIdx, $"Invalid comparison between types '{a.GetType()}' and '{b.GetType()}'.");
                    return true;
                }

                break;

            case Opcode.ADD:
                b = stack.Pop();
                a = stack.Pop();

                if (a == null || b == null)
                {
                    Error(ins.lineIdx, "Null reference exception while adding.");
                    return true;
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
                    Error(ins.lineIdx, $"Invalid operation '+' between types '{a.GetType()}' and '{b.GetType()}'.");
                    return true;
                }

                break;

            case Opcode.SUB:
                b = stack.Pop();
                a = stack.Pop();

                if (a == null || b == null)
                {
                    Error(ins.lineIdx, "Null reference exception while subtracting.");
                    return true;
                }

                if (a.GetType() == typeof(int) && b.GetType() == typeof(int))
                    stack.Push((int)a - (int)b);

                else if ((a.GetType() == typeof(int) && b.GetType() == typeof(float)) || (a.GetType() == typeof(float) && b.GetType() == typeof(int)))
                    stack.Push(Convert.ToSingle(a) - Convert.ToSingle(b));

                else if (a.GetType() == typeof(float) && b.GetType() == typeof(float))
                    stack.Push((float)a - (float)b);
                else
                {
                    Error(ins.lineIdx, $"Invalid operation '-' between types '{a.GetType()}' and '{b.GetType()}'.");
                    return true;
                }

                break;

            case Opcode.MULT:
                b = stack.Pop();
                a = stack.Pop();

                if (a == null || b == null)
                {
                    Error(ins.lineIdx, "Null reference exception while multiplying.");
                    return true;
                }

                if (a.GetType() == typeof(int) && b.GetType() == typeof(int))
                    stack.Push((int)a * (int)b);

                else if ((a.GetType() == typeof(int) && b.GetType() == typeof(float)) || (a.GetType() == typeof(float) && b.GetType() == typeof(int)))
                    stack.Push(Convert.ToSingle(a) * Convert.ToSingle(b));

                else if (a.GetType() == typeof(float) && b.GetType() == typeof(float))
                    stack.Push((float)a * (float)b);
                else
                {
                    Error(ins.lineIdx, $"Invalid operation '*' between types '{a.GetType()}' and '{b.GetType()}'.");
                    return true;
                }

                break;

            case Opcode.DUP:
                stack.Push(stack.Peek());
                break;

            case Opcode.DIV:
                b = stack.Pop();
                a = stack.Pop();

                if (a == null || b == null)
                {
                    Error(ins.lineIdx, "Null reference exception while dividing.");
                    return true;
                }

                if (a.GetType() == typeof(int) && b.GetType() == typeof(int))
                    if ((int)a % (int)b == 0)
                        stack.Push((int)a / (int)b);
                    else
                        stack.Push((float)a / (float)b);

                else if ((a.GetType() == typeof(int) && b.GetType() == typeof(float)) || (a.GetType() == typeof(float) && b.GetType() == typeof(int)))
                    stack.Push(Convert.ToSingle(a) / Convert.ToSingle(b));

                else if (a.GetType() == typeof(float) && b.GetType() == typeof(float))
                    stack.Push((float)a / (float)b);
                else
                {
                    Error(ins.lineIdx, $"Invalid operation '/' between types '{a.GetType()}' and '{b.GetType()}'.");
                    return true;
                }
                break;

            case Opcode.EXT_INST_CALL:
                if (!ExtCall(ins, false, false, (string)ins.operand)) return true;
                break;

            case Opcode.EXT_STATIC_CALL:
                if (!ExtCall(ins, true, false, (string)ins.operand)) return true;
                break;

            case Opcode.EXT_CTOR:
                if (!ExtCall(ins, true, true, (string)ins.operand)) return true;
                break;

            case Opcode.PUSH_INST_FIELD:
                a = stack.Pop();

                if (a == null)
                {
                    Error(ins.lineIdx, $"Null reference exception. Cannot get the field '{ins.operand}', from a null value.");
                    return true;
                }

                // check if failed
                if (!PushField(ins, a.GetType(), (string)ins.operand, a))
                    return true;

                break;

            case Opcode.SET_INST_FIELD:
                b = stack.Pop(); // b is the value
                a = stack.Pop();

                if (a == null)
                {
                    Error(ins.lineIdx, $"Null reference exception. Cannot set the field '{ins.operand}', of a null value.");
                    return true;
                }

                if (!SetField(ins, a.GetType(), (string)ins.operand, a, b))
                    return true;

                stack.Push(b);
                break;

            case Opcode.PUSH_STATIC_FIELD:
                (string className, string fieldName) = GetClassAndSecondary((ins.operand as string)!);

                Type? type = GetType(className);
                if (type == null)
                {
                    Error(ins.lineIdx, $"Null reference exception. Couldn't find type '{className}'.");
                    return true;
                }

                if (!PushField(ins, type, fieldName, null))
                    return true;

                break;

            case Opcode.SET_STATIC_FIELD:
                b = stack.Peek(); // value

                (className, fieldName) = GetClassAndSecondary((ins.operand as string)!);

                type = GetType(className);
                if (type == null)
                {
                    Error(ins.lineIdx, $"Null reference exception. Couldn't find type '{className}'.");
                    return true;
                }

                if (!SetField(ins, type, fieldName, null, b))
                    return true;

                break;

            case Opcode.PUSHARR:
                object? idxRaw = stack.Pop();
                int idx = 0;

                if (idxRaw == null)
                {
                    Error(ins.lineIdx, $"Null reference. Cannot index array with null.");
                    return true;
                }

                if (idxRaw.GetType() != typeof(int))
                {
                    Error(ins.lineIdx, $"Cannot index array with a non-integer type.");
                    return true;
                }

                idx = (int)idxRaw!;
                cachedArrayIndex = idx;

                a = stack.Pop();

                if (a == null)
                {
                    Error(ins.lineIdx, $"Null reference exception. Cannot index 'null' like an array.");
                    return true;
                }
                cachedArray = a;

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
                    Error(ins.lineIdx, $"Type '{a.GetType()}' cannot be indexed.");
                    return true;
                }

                break;

            case Opcode.SETARR:
                b = stack.Pop();

                idxRaw = stack.Pop();

                if (idxRaw == null)
                {
                    Error(ins.lineIdx, $"Null reference. Cannot index array with null.");
                    return true;
                }

                if (idxRaw.GetType() != typeof(int))
                {
                    Error(ins.lineIdx, $"Cannot index array with a non-integer type.");
                    return true;
                }

                idx = (int)idxRaw!;
                a = stack.Pop();

                if (a == null)
                {
                    Error(ins.lineIdx, $"Null reference exception. Cannot index 'null' like an array.");
                    return true;
                }

                if (a.GetType().IsArray)
                {
                    (a as Array)!.SetValue(b, idx);
                }
                else if (a.GetType().IsAssignableTo(typeof(IList)))
                {
                    a.GetType().GetGenericArguments();
                    (a as IList)![idx] = b;
                }
                else
                {
                    Error(ins.lineIdx, $"Type '{a.GetType()}' cannot be indexed.");
                    return true;
                }

                stack.Push(b);
                break;

            case Opcode.SETARR_CACHED:
                b = stack.Pop();
                idx = cachedArrayIndex;
                a = cachedArray;

                if (a == null)
                {
                    Error(ins.lineIdx, $"Null reference exception. Cannot index 'null' like an array.");
                    return true;
                }

                if (a.GetType().IsArray)
                {
                    (a as Array)!.SetValue(b, idx);
                }
                else if (a.GetType().IsAssignableTo(typeof(IList)))
                {
                    (a as IList)![idx] = b;
                }
                else
                {
                    Error(ins.lineIdx, $"Type '{a.GetType()}' cannot be indexed.");
                    return true;
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

        return false;
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

    private static bool ExtCall(Instruction ins, bool isStatic, bool isCtor, string name)
    {
        int topStack = _tempTopStackStore.Pop();
        int argsPassed = stack.top - topStack;

        object?[] objects = stack.PeekTop(argsPassed);
        stack.Pop(argsPassed);


        object? a = null;
        if (!isStatic)
            a = stack.Pop();

        if (!isStatic && a == null)
        {
            Error(ins.lineIdx, $"Null reference. Cannot call method '{name}' on 'null'.");
            return false;
        }

        Type? type;
        if (isStatic)
        {
            if (isCtor)
            {
                type = GetType(name);

                if (type == null)
                {
                    Error(ins.lineIdx, $"Couldn't find type with the name '{name}'.");
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
                    Error(ins.lineIdx, $"Couldn't find type with the name '{name}'.");
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
                Error(ins.lineIdx, $"Couldn't find a constructor with that overload.");
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
                methodInfo = type.GetMethod(name, BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            catch
            {
                Type[] types = new Type[argsPassed];
                int i = 0;
                foreach (object? obj in objects)
                {
                    types[i++] = obj!.GetType();
                }
                methodInfo = type.GetMethod(name, BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, types);
            }

            if (methodInfo == null)
            {
                Error(ins.lineIdx, $"Couldn't find a method '{name}' with that overload.");
                return false;
            }

            if (argsPassed != methodInfo.GetParameters().Length)
            {
                Error(ins.lineIdx, $"Incorrect number of arguments passed to method '{name}'.");
                return false;
            }

            tempIndexBase = stack.indexBase;
            stack.indexBase = topStack;

            result = methodInfo.Invoke(a, objects);
        }

        stack.indexBase = tempIndexBase;

        stack.Push(result);

        return true;
    }

    private static bool PushField(Instruction ins, Type type, string name, object? reference)
    {
        FieldInfo? fieldInfo = type.GetField(name);

        PropertyInfo? propertyInfo;
        if (fieldInfo == null)
        {
            propertyInfo = type.GetProperty(name, BindingFlags.FlattenHierarchy | BindingFlags.Instance |
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (propertyInfo == null)
            {
                propertyInfo = type.GetProperty(name, BindingFlags.DeclaredOnly | BindingFlags.Instance |
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (propertyInfo == null)
            {
                Error(ins.lineIdx, $"Couldn't get property. A property with the name '{name}' doesn't exist.");
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

    private static bool SetField(Instruction ins, Type type, string name, object? reference, object? value)
    {
        var fieldInfo = type.GetField(name);

        if (fieldInfo == null)
        {
            var propertyInfo = type.GetProperty(name, BindingFlags.FlattenHierarchy | BindingFlags.Instance | 
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (propertyInfo == null)
            {
                propertyInfo = type.GetProperty(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | 
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (propertyInfo == null)
            {
                Error(ins.lineIdx, $"Couldn't set property. A property with the name '{name}' doesn't exist.");
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


    private static MemberInfo GetMember(Type type, string name, MemberTypes memberTypes)
    {
        do
        {
            var members = type.GetMember(name, BindingFlags.FlattenHierarchy | BindingFlags.Instance |
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MemberInfo member in members)
            {
                if (member.MemberType == memberTypes)
                {
                    return member;
                }
            }
            type = type.BaseType;
        } while (type != null);

        return null;
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

    public static Range Range(int end)
    {
        return new Range(0, end, 1);
    }

    public static Range Range(int start, int end)
    {
        return new Range(start, end, 1);
    }

    public static Range Range(int start, int end, int step)
    {
        return new Range(start, end, step);
    }

    private static void Error(int lineIdx, string message)
    {
        HadError = true;
        currentErrorManager.RaiseRuntimeError(lineIdx, message);
    }

}
