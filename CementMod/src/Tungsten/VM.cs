using System.Reflection;
using System;
using System.Collections.Generic;
using CementGB.Mod.Utilities;

namespace Tungsten;
public static class VM
{
    private static MutStack<object?> stack = new();
    private static Stack<int> callStack = new();

    private static int _tempTopStackStore = 0;
    public static object? RunFunction(ProgramInfo info, string functionName, params object?[] args)
    {
        stack.Clear();
        if (args != null)
            foreach (object? arg in args)
                stack.Push(arg);

        if (!info.functionPointers.ContainsKey(functionName))
        {
            // raise an error
            return null;
        }
        int pc = info.functionPointers[functionName];
        
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
                    if (!info.globalVariables.ContainsKey(globalName))
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }
                    info.globalVariables[globalName] = stack.Pop();
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
                    stack[stackOffset] = stack.Pop();
                    break;
                case Opcode.PUSHLOCAL:
                    stackOffset = (int)ins.operand;
                    stack.Push(stack[stackOffset]);
                    break;

                case Opcode.PREPCALL:
                    // this will be where the arguments begin
                    _tempTopStackStore = stack.top;
                    break;

                case Opcode.CALL:
                    callStack.Push(stack.indexBase);
                    callStack.Push(pc);

                    stack.indexBase = _tempTopStackStore;
                    pc = (int)ins.operand;

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

                case Opcode.GTE:
                    b = stack.Pop();
                    a = stack.Pop();

                    if (a == null || b == null)
                    {
                        // raise error: null reference exception
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    if (a.GetType() == typeof(int) && b.GetType() == typeof(int))
                        stack.Push((int)a >= (int)b);

                    else if ((a.GetType() == typeof(int) && b.GetType() == typeof(float)) || (a.GetType() == typeof(float) && b.GetType() == typeof(int)))
                        stack.Push((float)a >= (float)b);

                    else if (a.GetType() == typeof(float) && b.GetType() == typeof(float))
                        stack.Push((float)a >= (float)b);
                    else
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

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
                        stack.Push((float)a < (float)b);

                    else if (a.GetType() == typeof(float) && b.GetType() == typeof(float))
                        stack.Push((float)a < (float)b);
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
                        stack.Push((float)a + (float)b);

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
                        stack.Push((float)a - (float)b);

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
                        stack.Push((float)a * (float)b);

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
                        stack.Push((float)a / (float)b);

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
                    int argsPassed = stack.top - _tempTopStackStore;
                    a = stack.Pop();

                    if (a == null)
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    MethodInfo? methodInfo = a.GetType().GetMethod((string)ins.operand);

                    if (methodInfo == null || argsPassed != methodInfo.GetParameters().Length)
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    int tempIndexBase = stack.indexBase;
                    stack.indexBase = _tempTopStackStore;

                    object? result = methodInfo.Invoke(a, stack.PeekTop(argsPassed));

                    stack.Pop(argsPassed);
                    stack.indexBase = tempIndexBase;

                    stack.Push(result);
                    break;

                case Opcode.EXT_STATIC_CALL:
                    argsPassed = stack.top - _tempTopStackStore;

                    string[] split = (ins.operand as string)!.Split('.');
                    string className = split[0];
                    string methodName = split[1];

                    Type? type = GetType(className);
                    if (type == null)
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    methodInfo = type.GetMethod(methodName);

                    if (methodInfo == null || argsPassed != methodInfo.GetParameters().Length)
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    tempIndexBase = stack.indexBase;
                    stack.indexBase = _tempTopStackStore;

                    result = methodInfo.Invoke(null, stack.PeekTop(argsPassed));

                    stack.Pop(argsPassed);
                    stack.indexBase = tempIndexBase;

                    stack.Push(result);
                    break;

                case Opcode.PUSHFIELD:
                    a = stack.Pop();

                    if (a == null)
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    FieldInfo? fieldInfo = a.GetType().GetField((string)ins.operand);

                    PropertyInfo? propertyInfo;
                    if (fieldInfo == null)
                    {
                        propertyInfo = a.GetType().GetProperty((string)ins.operand);
                        if (propertyInfo == null)
                        {
                            // raise error
                            LoggingUtilities.VerboseLog("ERROR!");
                            return null;
                        }
                        stack.Push(propertyInfo.GetValue(a));
                    }
                    else
                    {
                        stack.Push(fieldInfo.GetValue(a));
                    }

                    break;

                case Opcode.SETFIELD:
                    b = stack.Pop(); // b is the value
                    a = stack.Pop();

                    if (a == null)
                    {
                        // raise error
                        LoggingUtilities.VerboseLog("ERROR!");
                        return null;
                    }

                    fieldInfo = a.GetType().GetField((string)ins.operand);

                    if (fieldInfo == null)
                    {
                        propertyInfo = a.GetType().GetProperty((string)ins.operand);
                        if (propertyInfo == null)
                        {
                            // raise error
                            LoggingUtilities.VerboseLog("ERROR!");
                            return null;
                        }
                        propertyInfo.SetValue(a, b);
                    }
                    else
                    {
                        fieldInfo.SetValue(a, b);
                    }

                    break;
            }
        }
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
}
