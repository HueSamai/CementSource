﻿using CementGB.Mod.Utilities;
using Il2CppGB.Core;
using Il2CppGB.UI.Utils;
using Il2CppGB.Utils;
using Il2CppPlayFab.ClientModels;
using Il2CppSystem;
using Il2CppSystem.Linq.Expressions.Interpreter;
using Il2CppSystem.Runtime.Serialization.Formatters.Binary;
using Il2CppSystem.Xml.Serialization;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Services.Authentication;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using static Il2CppMono.Security.X509.X520;

namespace Tungsten;

public enum Opcode
{
    NOP, // no operation

    // pops, then peforms op on last two values on the stack, and pushes the result
    ADD,    
    SUB,
    MULT,
    DIV,
    // inverts. if false, pops then pushes true, vice versa.
    INV,

    // for pushing popping, and duplicating items on the stack
    PUSH,
    POP,
    DUP,
    
    // variables
    SETLOCAL,
    PUSHLOCAL,
    SETGLOBAL,
    PUSHGLOBAL,

    // comparisons
    // equals
    EQU,
    // less than
    LSS,
    // greater than
    GRT,

    // jumping to address
    JMP,
    // jump if false (zero)
    JFZ,
    // jump if not false (zero)
    JNZ,

    // for functions
    // pushes the top pointer of the stack, onto the call stack so that the stack window of the arguments are correct when a function is called
    PREPCALL, // also used for preping arrays
    // pushes PC, and top stack pointer, onto call stack and jumps to address
    CALL,
    // pops address off call stack, reverts stack, and jumps to address (keeps last value (return value) on the stack)
    // also acts as halt when there are no more addresses are on the call stack
    RETURN,

    // c# interaction
    // construct a c# object
    EXT_CTOR,
    // call an external c# instance method of an object
    EXT_INST_CALL,
    // call an external static method in c#
    EXT_STATIC_CALL,
    // set an external field/property of a c# object
    SET_INST_FIELD,
    // push an external field/property from a c# object onto the stack
    PUSH_INST_FIELD,
    // set an external static field/property of a c# object
    SET_STATIC_FIELD,
    // push an external static field/property from a c# object onto the stack
    PUSH_STATIC_FIELD,

    // arrays
    PUSHARR,
    SETARR,
    // for special case where '+=' or other set operator is used with arrays
    SETARR_CACHED,
    CRTARR

}

public struct Instruction
{
    public Opcode op;
    public object operand;

    public int lineIdx;

    public Instruction(Opcode op, object? operand=null, int lineIdx = -1)
    {
        this.op = op;
        this.operand = operand;
        this.lineIdx = lineIdx;
    }

    public string ToString()
    {
        if (operand == null)
        {
            return op.ToString();
        }
        return op.ToString() + " " + operand.ToString();
    }
}

public struct ProgramInfo
{
    public string[] globals;
    public Dictionary<string, object?> globalVariables;
    public Dictionary<string, FuncInfo> functions;
    public Instruction[] instructions;
    public ErrorManager errorManager;
    
    public ProgramInfo(
        string[] globals, 
        Dictionary<string, object?> globalVariables, 
        Dictionary<string, FuncInfo> functions, 
        Instruction[] instructions,
        ErrorManager errorManager
    )
    {
        this.globals = globals;
        this.globalVariables = globalVariables;
        this.functions = functions;
        this.instructions = instructions;
        this.errorManager = errorManager;
    }
}

public struct FuncInfo
{
    public int arity;
    public int start;

    public FuncInfo(int arity, int start)
    {
        this.arity = arity;
        this.start = start;
    }
}

public class Parser
{
    private Lexer lexer;
    public bool HadError => errorManager.HadError;

    private List<Instruction> instructions = new();
    private Dictionary<string, FuncInfo> functions = new()
    {
        { "__regglobal", new FuncInfo(0,0) }
    };

    private static Dictionary<TokenType, Opcode> operatorOpcodes = new()
    {
        { TokenType.Plus, Opcode.ADD },
        { TokenType.Minus, Opcode.SUB  },
        { TokenType.Asterisk, Opcode.MULT },
        { TokenType.ForwardSlash, Opcode.DIV },
        { TokenType.Greater, Opcode.GRT },
        { TokenType.Less, Opcode.LSS },
        { TokenType.GreaterEquals, Opcode.LSS },
        { TokenType.LessEquals, Opcode.GRT },
        { TokenType.DoubleEquals, Opcode.EQU },
        { TokenType.BangEquals, Opcode.EQU },
    };

    private static Dictionary<TokenType, int> precedenceTable = new()
    {
        { TokenType.Greater, 2 },
        { TokenType.GreaterEquals, 2 },
        { TokenType.Less, 2 },
        { TokenType.LessEquals, 2 },
        { TokenType.DoubleEquals, 2 },
        { TokenType.Plus, 3 },
        { TokenType.Minus, 3  },
        { TokenType.Asterisk, 4 },
        { TokenType.ForwardSlash, 4 },
        { TokenType.Bang, 5 },
        { TokenType.OpenSquareBracket, 6 },
        { TokenType.Colon, 7 },
    };

    private HashSet<string> globalVariables = new();
    private List<List<string>> scopes = new();
    private int totalVariables = 0;

    private Token Current;
    private Token Previous;

    private ErrorManager errorManager;

    public Parser(string code)
    {
        errorManager = new();
        lexer = new Lexer(code, errorManager);
    }

    private void Advance()
    {
        Previous = Current;
        Current = lexer.Next();
    }

    private bool Match(TokenType type)
    {
        if (Current.type == type)
        {
            Advance();
            return true;
        }

        return false;
    }

    private void Consume(TokenType type, string errorMessage)
    {
        if (!Match(type))
        {
            Error(Current, errorMessage);
            Advance();
        }
    }

    public ProgramInfo? Parse()
    {
        Advance();

        while (Current.type == TokenType.KeywordVar)
            GlobalVarDef();

        Add(Opcode.PUSH, null);
        Add(Opcode.RETURN);

        while (Current.type != TokenType.End)
        {
            switch (Current.type)
            {
                case TokenType.KeywordFunc:
                    Function();
                    break;
                case TokenType.KeywordVar:
                    Error(Current, "Global variables must all appear at the beginning of a script, before any function definitions.");
                    Advance();
                    break;
                default:
                    Error(Current, "Expected function definition. No statements can be outside of functions.");
                    Advance();
                    break;
            }
        }

        if (HadError)
            return null;

        return new ProgramInfo(
            new string[] { "main" },
            new(),
            functions,
            instructions.ToArray(),
            errorManager
        );
        /*
        Token token;
        do
        {
            token = lexer.Next();
            LoggingUtilities.VerboseLog($"{token.type} {token.lexeme}");
        } while (token.type != TokenType.End);

        Dictionary<string, int> functionPointers = new();
        functionPointers["main"] = 0;
        return new ProgramInfo(
            new string[] { "main" },
            new(),
            functionPointers,

            new Instruction[]
            {
                new Instruction(Opcode.PREPCALL),
                new Instruction(Opcode.PUSH, "Monday"),
                new Instruction(Opcode.PUSH, "Tuesday"),
                new Instruction(Opcode.PUSH, "Wednesday"),
                new Instruction(Opcode.CRTARR),
                new Instruction(Opcode.PUSH, 1),
                new Instruction(Opcode.GETARR),
                new Instruction(Opcode.RETURN)
            }
            
            {
                new Instruction(Opcode.PREPCALL),
                new Instruction(Opcode.PREPCALL),
                new Instruction(Opcode.PUSH, "Actor"),
                new Instruction(Opcode.EXT_STATIC_CALL, "VM.GetIl2CppType"),
                new Instruction(Opcode.EXT_STATIC_CALL, "UnityEngine.Object.FindObjectOfType"),
                new Instruction(Opcode.PREPCALL),
                new Instruction(Opcode.PUSH, 0.2f),
                new Instruction(Opcode.PUSH, 0.0f),
                new Instruction(Opcode.PUSH, 0.5f),
                new Instruction(Opcode.EXT_CTOR, "UnityEngine.Color"),
                new Instruction(Opcode.SET_INST_FIELD, "primaryColor"),
                new Instruction(Opcode.RETURN)
            }
            
            {
                new Instruction(Opcode.JMP, 19),
                new Instruction(Opcode.PUSHLOCAL, 0),
                new Instruction(Opcode.PUSH, 1),
                new Instruction(Opcode.EQU),
                new Instruction(Opcode.JFZ, 2),
                new Instruction(Opcode.PUSH, "1"),
                new Instruction(Opcode.RETURN),
                new Instruction(Opcode.PUSHLOCAL, 0),
                new Instruction(Opcode.PREPCALL),
                new Instruction(Opcode.PREPCALL),
                new Instruction(Opcode.PUSHLOCAL, 0),
                new Instruction(Opcode.PUSH, 1),
                new Instruction(Opcode.SUB, 0),
                new Instruction(Opcode.CALL, 1),
                new Instruction(Opcode.EXT_STATIC_CALL, "Int32.Parse"),
                new Instruction(Opcode.MULT),
                new Instruction(Opcode.PREPCALL),
                new Instruction(Opcode.EXT_INST_CALL, "ToString"),
                new Instruction(Opcode.RETURN),
                new Instruction(Opcode.PREPCALL),
                new Instruction(Opcode.PUSHLOCAL, 0),
                new Instruction(Opcode.CALL, 1),
                new Instruction(Opcode.PUSH_INST_FIELD, "Length"),
                new Instruction(Opcode.RETURN)
            }
        );
        */
    }

    private void Add(Opcode op, object? operand=null, int lineIdx=-1)
    {
        if (op == Opcode.POP && operand != null && (int)operand == 0) return;

        // this code checks if an operation between two literals is being performed so that it can pre-evaluate the result.
        if (instructions.Count > 1 && instructions[instructions.Count - 1].op == Opcode.PUSH &&
            instructions[instructions.Count - 2].op == Opcode.PUSH) {

            object? returnValue = null;
            switch (op)
            {
                case Opcode.ADD:
                case Opcode.SUB:
                case Opcode.MULT:
                case Opcode.DIV:
                case Opcode.GRT:
                case Opcode.LSS:
                    VM.suppressRuntimeErrors = true;
                    VM.Reset();
                    var info = new ProgramInfo(); // we can just create a dummy info struct, bc we know we won't need it
                    VM.RunInstruction(instructions[instructions.Count - 2], info);
                    VM.RunInstruction(instructions[instructions.Count - 1], info);
                    VM.RunInstruction(new Instruction(op, null), info);
                    VM.suppressRuntimeErrors = false;
                    if (!VM.HadError)
                    {
                        returnValue = VM.PeekStackTop();
                        instructions.RemoveAt(instructions.Count - 1);
                        instructions[instructions.Count - 1] = new Instruction(Opcode.PUSH, returnValue, instructions[instructions.Count - 1].lineIdx);
                        VM.suppressRuntimeErrors = false;
                        return;
                    }
                    else
                    {
                        Error(Current, "Invalid operation between types.");
                        LoggingUtilities.VerboseLog("ERROR! Invalid operation between types");
                        return;
                    }

                default:
                    break;
            }
        }
        else if (instructions.Count > 0 && instructions[instructions.Count - 1].op == Opcode.PUSH)
        {
            if (op == Opcode.INV)
            {
                Instruction ins = instructions[instructions.Count - 1];
                if (ins.operand.GetType() != typeof(bool))
                {
                    Error(Current, "Cannot invert non boolean type.");
                    return;
                }
                instructions[instructions.Count - 1] = new Instruction(Opcode.PUSH, !(bool)ins.operand, ins.lineIdx);
                return;
            }
        }

        if (lineIdx == -1)
            lineIdx = Previous.lineIdx;

        instructions.Add(new Instruction(op, operand, lineIdx));
    }

    private void GlobalVarDef()
    {
        Advance();

        string id = Current.lexeme;
        Consume(TokenType.Identifier, "Expected identifier. Global variables can't start with a capital letter.");

        if (Match(TokenType.SemiColon))
        {
            Add(Opcode.PUSH, null);
        }
        else
        {
            Consume(TokenType.Equals, "Expected '='");
            Expression();
            Add(Opcode.SETGLOBAL, id);
            Add(Opcode.POP, 1);
            Consume(TokenType.SemiColon, "Expected ';'");
        }

        if (!globalVariables.Add(id))
        {
            Error(Current, $"A global variable with the name '{id}' already exists.");
        }
    }

    private void Function()
    {
        // skip func
        Advance();

        int arity = 0;

        string id = Current.lexeme;
        Consume(TokenType.Identifier, "Expected identifier. Functions can't start with a capital letter.");

        if (functions.ContainsKey(id))
        {
            Error(Current, $"A function with the name '{id}' already exists.");
        }

        IncScope();

        Consume(TokenType.OpenParenthesis, "Expected '('");
        while (Current.type != TokenType.End && Current.type != TokenType.ClosedParenthesis) 
        {
            string param = Current.lexeme;
            Consume(TokenType.Identifier, "Expected parameter name. Function parameters can't start with a capital letter.");

            RegisterLocalVariable(param);
            arity++;

            if (Current.type == TokenType.ClosedParenthesis)
                break;

            Consume(TokenType.Comma, "Expected ','. Parameter names must be separated with commas.");
        }

        Consume(TokenType.ClosedParenthesis, "Expected ')'");

        functions[id] = new FuncInfo(arity, instructions.Count);

        Block(false);

        Add(Opcode.PUSH, null);
        Add(Opcode.RETURN);
    }

    private void Return()
    {
        Advance();
        if (Match(TokenType.SemiColon))
            Add(Opcode.PUSH, null);
        else
        {
            Expression();
            Consume(TokenType.SemiColon, "Expected ';'");
        }

        Add(Opcode.RETURN);
    }

    private void Statement()
    {
        switch (Current.type)
        {
            case TokenType.KeywordVar:
                LocalVariableDef(); break;
            case TokenType.KeywordReturn:
                Return(); break;
            case TokenType.KeywordIf:
                If(); break;
            // i specifically chose not to include blocks as statements, bc it seems counter intuitive
            case TokenType.KeywordFor:
                For(); break;
            case TokenType.KeywordWhile:
                While(); break;
            case TokenType.KeywordContinue:
                Continue(); break;
            case TokenType.KeywordBreak:
                Break(); break;

            default:
                Expression();
                Add(Opcode.POP, 1);
                Consume(TokenType.SemiColon, "Expected ';'");
                break;
        }
    }

    private void PatchJump(int instructionIndex)
    {
        instructions[instructionIndex] = new Instruction(instructions[instructionIndex].op, instructions.Count);
    }

    private void For()
    {
        Advance();

        string id = Current.lexeme;
        Consume(TokenType.Identifier, "Expected variable name (variable names can't start with capital letters). " +
            "The syntax of a for loop is as follows: for [variable name] in [expression] { ... }");

        IncScope();
        // used in the enumeration
        RegisterLocalVariable("$");

        IncScope();
        RegisterLocalVariable(id);

        Consume(TokenType.KeywordIn, "Expected 'in'. For loop syntax: for [variable name] in [expression] { ... }");

        // enumerator
        Token tokBeforeExpression = Current;
        Expression();

        Add(Opcode.PREPCALL);
        Add(Opcode.EXT_INST_CALL, "GetEnumerator", tokBeforeExpression.lineIdx); // get the enumerator

        // the enumerator now sits in $'s position

        int loopStart = instructions.Count;

        // while (variable != null)

        // check if enumerator is done
        Add(Opcode.DUP);
        Add(Opcode.PREPCALL);
        Add(Opcode.EXT_INST_CALL, "MoveNext");

        int jumpToEnd = instructions.Count;
        Add(Opcode.JFZ);

        // get current enum
        Add(Opcode.DUP);
        Add(Opcode.PUSH_INST_FIELD, "Current");

        // now we do the loop body
        Loop(loopStart, jumpToEnd);

        DecScope();
    }

    private void While()
    {
        Advance();
        int loopStart = instructions.Count;
        Expression(); // expression to always compute

        int jumpToEnd = instructions.Count;
        Add(Opcode.JFZ);

        IncScope();
        Loop(loopStart, jumpToEnd);
    }

    bool inLoop => currentLoopStart != -1;
    Stack<int> breaksToPatch = new();
    int currentLoopStart = -1;
    int currentLoopScope = -1;
    private void Loop(int loopStart, int jumpToEnd)
    {
        int tempLoopStart = currentLoopStart;
        currentLoopStart = loopStart;
        int tempLoopScope = currentLoopScope;
        currentLoopScope = scopes.Count - 1;

        int breaksStart = breaksToPatch.Count;
        
        Block(false);

        Add(Opcode.JMP, loopStart);
        PatchJump(jumpToEnd);

        for (int i = breaksStart; i < breaksToPatch.Count; ++i)
            PatchJump(breaksToPatch.Pop());

        currentLoopStart = tempLoopStart;
        currentLoopScope = tempLoopScope;
    }

    private void Break()
    {
        if (!inLoop)
        {
            Error(Current, "Can't use 'break' outside of a loop.");
        }
        Advance();
        Consume(TokenType.SemiColon, "Expected ';'");
        DescopeUntil(currentLoopScope);
        breaksToPatch.Push(instructions.Count);
        Add(Opcode.JMP);
    }
    private void Continue()
    {
        if (!inLoop)
        {
            Error(Current, "Can't use 'continue' outside of a loop.");
        }
        Advance();
        Consume(TokenType.SemiColon, "Expected ';'");
        DescopeUntil(currentLoopScope);
        Add(Opcode.JMP, currentLoopStart);
    }
    
    private void DescopeUntil(int scopeIndex)
    {
        int totalLoss = 0;

        for (int i = scopeIndex; i < scopes.Count; ++i)
            totalLoss += scopes[i].Count;

        Add(Opcode.POP, totalLoss);
    }

    private void If()
    {
        Advance(); // pass over if
        var tokBeforeExpr = Current;
        Expression(); // expression to evaluate
        int jumpToElse = instructions.Count;
        Add(Opcode.JFZ, lineIdx: tokBeforeExpr.lineIdx);

        Block(); // if body

        if (Match(TokenType.KeywordElse))
        {
            int jumpToEnd = instructions.Count;
            Add(Opcode.JMP);

            PatchJump(jumpToElse);

            if (Current.type == TokenType.KeywordIf)
                If();
            else
                Block();
            PatchJump(jumpToEnd);
        }
        else
        {
            PatchJump(jumpToElse);
        }
    }

    private void Block(bool incScope=true)
    {
        Consume(TokenType.OpenCurlyBracket, "Expected '{'");
        if (incScope)
            IncScope();

        while (Current.type != TokenType.End && Current.type != TokenType.ClosedCurlyBracket)
            Statement();

        Consume(TokenType.ClosedCurlyBracket, "Expected '}'");
        DecScope();
    }

    CultureInfo floatCulture;
    private void HandleInfix(int precedence)
    {
        switch (Current.type) {
            case TokenType.Bang:
            {
                ParseInvert(precedenceTable[TokenType.Bang]);
                break;
            }

            case TokenType.OpenParenthesis:
                Advance();
                Expression();
                Consume(TokenType.ClosedParenthesis, "Expected ')'");
                break;

            case TokenType.Minus: 
                ParsePrefixMinus(precedenceTable[TokenType.Bang]); // we don't have infix and prefix precedence distinction
                break;

            case TokenType.ClassIdentifier:
                // didn't actually fail yet but this is flagged so that we can see if HandleColon was called
                failedToHandleStatic = true;
                Advance();
                break;

            case TokenType.Identifier:
                HandleIdentifier();
                break;

            case TokenType.OpenSquareBracket:
                Advance();
                ParseArguments(TokenType.ClosedSquareBracket);
                Add(Opcode.CRTARR);
                break;

            // parse literal
            case TokenType.Integer:
                Add(Opcode.PUSH, int.Parse(Current.lexeme));
                Advance();
                break;

            case TokenType.KeywordNew:
                HandleCtor();
                break;

            case TokenType.Float:
                if (floatCulture == null)
                {
                    floatCulture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                    floatCulture.NumberFormat.CurrencyDecimalSeparator = ".";
                }
                Add(Opcode.PUSH, float.Parse(Current.lexeme, NumberStyles.Any, floatCulture));
                Advance();
                break;

            case TokenType.String:
                Add(Opcode.PUSH, Current.lexeme);
                Advance();
                break;

            default:
                Error(Current, "Unexpected token.");
                Advance();
                break;
        }
    }

    private string GetAnyId()
    {
        string id = Current.lexeme;
        if (Current.type == TokenType.ClassIdentifier)
            Advance();
        else
            Consume(TokenType.Identifier, "Expected identifier");
        return id;
    }

    private void ParseInvert(int precedence)
    {
        Advance();
        var tokBeforeExpr = Current;
        ParsePrecedence(precedence);
        Add(Opcode.INV, lineIdx: tokBeforeExpr.lineIdx);
    }

    private void ParsePrefixMinus(int precedence)
    {
        Add(Opcode.PUSH, 0);
        Advance();
        ParsePrecedence(precedence);
        Add(Opcode.SUB);
    }

    private int GetVariable(string name)
    {
        int vars = totalVariables;
        for (int i = scopes.Count - 1; i >= 0; --i)
        {
            int variablesPassed = 0;
            List<string> scope = scopes[i];
            vars -= scope.Count;
            foreach (string varName in scope)
            {
                if (varName == name)
                    return vars + variablesPassed;

                ++variablesPassed;
            }
        }

        if (!globalVariables.Contains(name))
        {
            Error(Current, $"No variable with the name '{name}' was found. Are you sure it is still in scope?");
        }

        return -1;
    }

    private void IncScope()
    {
        scopes.Add(new());
    }

    private void DecScope()
    {
        List<string> scope = scopes[scopes.Count - 1];
        totalVariables -= scope.Count;
        Add(Opcode.POP, scope.Count);
        scopes.RemoveAt(scopes.Count - 1);
    }
    
    private void LocalVariableDef()
    {
        Advance();

        string id = Current.lexeme;
        Consume(TokenType.Identifier, "Expected identifier. Local variables can't start with a capital letter.");

        if (Match(TokenType.SemiColon))
        {
            Add(Opcode.PUSH, null);
        }
        else
        {
            Consume(TokenType.Equals, "Expected '='");
            Expression();
            Consume(TokenType.SemiColon, "Expected ';'");
        }

        RegisterLocalVariable(id);
    }

    private void RegisterLocalVariable(string name)
    {
        scopes.Last().Add(name);
        ++totalVariables;
    }

    private void Expression()
    {
        ParsePrecedence(0);
    }

    private void ParsePrecedence(int precedence)
    {
        HandleInfix(precedence);

        if (!precedenceTable.ContainsKey(Current.type))
        {
            return;
        }

        int currentTokenPrecedence = precedenceTable[Current.type];

        while (currentTokenPrecedence >= precedence)
        {
            switch (Current.type)
            {
                case TokenType.Colon:
                    HandleColon();
                    break;

                case TokenType.DoubleEquals:
                case TokenType.BangEquals:
                case TokenType.Greater:
                case TokenType.GreaterEquals:
                case TokenType.Less:
                case TokenType.LessEquals:
                    HandleComparison();
                    break;

                case TokenType.OpenSquareBracket:
                    HandleSubscript();
                    break;

                // just a clasic binop
                default:
                    HandleOp(currentTokenPrecedence);
                    break;
            }

            if (failedToHandleStatic)
            {
                Error(Previous, "Expected ':' after class identifier. Class identifiers can't stand alone." + 
                    "You must either use a static field, or a call a static method.");
            }

            if (!precedenceTable.ContainsKey(Current.type))
                return;

            currentTokenPrecedence = precedenceTable[Current.type];
        }
    }

    private static Dictionary<Opcode, Opcode> variablePushToSet = new()
    {
        { Opcode.PUSHGLOBAL, Opcode.SETGLOBAL },
        { Opcode.PUSHLOCAL, Opcode.SETLOCAL },
        { Opcode.PUSH_INST_FIELD, Opcode.SET_INST_FIELD },
        { Opcode.PUSH_STATIC_FIELD, Opcode.SET_STATIC_FIELD },
        { Opcode.PUSHARR, Opcode.SETARR_CACHED }
    };
    private void HandleOp(int currentTokenPrecedence)
    {

        Opcode op = operatorOpcodes[Current.type];
        Advance();
        Instruction instructionToAdd = new Instruction();
        bool addIns = false;
        if (Current.type == TokenType.Equals)
        {
            currentTokenPrecedence = 0;
            addIns = true;
            Instruction last = instructions.Last();
            if (!variablePushToSet.ContainsKey(last.op))
            {
                Error(Current, "Invalid left hand side for an '=' operator.");
            }
            else 
            {
                instructionToAdd = new Instruction(variablePushToSet[last.op], last.operand);
            }
            Advance();

        }
        ParsePrecedence(currentTokenPrecedence + 1);
        Add(op);

        if (addIns)
            Add(instructionToAdd.op, instructionToAdd.operand);
    }

    private void HandleComparison()
    {
        Opcode op = operatorOpcodes[Current.type];
        switch (Current.type)
        {
            case TokenType.LessEquals:
            case TokenType.GreaterEquals:
            case TokenType.BangEquals:
                Advance();
                ParsePrecedence(precedenceTable[TokenType.Less]);
                Add(op);
                Add(Opcode.INV);
                break;
            default:
                Advance();
                ParsePrecedence(precedenceTable[TokenType.Less]);
                Add(op);
                break;
        }
    }

    private bool failedToHandleStatic = false;

    private void HandleColon()
    {
        failedToHandleStatic = false;
        Token prevToken = Previous;

        Advance();
        var idTok = Current;
        string methodOrFieldName = GetAnyId();
        bool isStatic = false;

        // handle a static class
        if (prevToken.type == TokenType.ClassIdentifier)
        {
            isStatic = true;
            methodOrFieldName = prevToken.lexeme + "." + methodOrFieldName;
        }

        if (Current.type == TokenType.OpenParenthesis)
        {
            Advance();
            ParseArguments(TokenType.ClosedParenthesis);
            Add(isStatic ? Opcode.EXT_STATIC_CALL : Opcode.EXT_INST_CALL, methodOrFieldName, idTok.lineIdx);
        }
        else
        {
            if (Current.type == TokenType.Equals)
            {
                Advance();
                ParsePrecedence(0);
                Add(isStatic ? Opcode.SET_STATIC_FIELD : Opcode.SET_INST_FIELD, methodOrFieldName, idTok.lineIdx);
            }
            else
            {
                Add(isStatic ? Opcode.PUSH_STATIC_FIELD : Opcode.PUSH_INST_FIELD, methodOrFieldName, idTok.lineIdx);
            }
        }
    }

    private static Dictionary<TokenType, char> endTokenToChar = new()
    {
        { TokenType.ClosedCurlyBracket, '}' },
        { TokenType.ClosedSquareBracket, ']' },
        { TokenType.ClosedParenthesis, ')' }
    };
    private void ParseArguments(TokenType endToken)
    {
        Add(Opcode.PREPCALL);
        while (Current.type != TokenType.End && Current.type != endToken)
        {
            Expression();
            if (Current.type == endToken)
                break;
            Consume(TokenType.Comma, "Expected ','");
        }
        Consume(endToken, "Expected '" + endTokenToChar[endToken] + "'");
    }

    private void HandleIdentifier()
    {
        string variableName = GetAnyId();
        if (Current.type == TokenType.OpenParenthesis) 
        {
            Advance();
            ParseArguments(TokenType.ClosedParenthesis);
            Add(Opcode.CALL, variableName);
        }
        else
        {
            int varLocation = GetVariable(variableName);
            if (varLocation < 0)
            {
                if (Current.type == TokenType.Equals)
                {
                    Advance();
                    ParsePrecedence(0);
                    Add(Opcode.SETGLOBAL, variableName);
                }
                else
                    Add(Opcode.PUSHGLOBAL, variableName);
            }
            else
            {
                if (Current.type == TokenType.Equals)
                {
                    Advance();
                    ParsePrecedence(0);
                    Add(Opcode.SETLOCAL, varLocation);
                }
                else
                    Add(Opcode.PUSHLOCAL, varLocation);
            }
        }

    }
    private void HandleSubscript()
    {
        var tokBeforeExpr = Current;
        Advance();
        Expression(); // index
        Consume(TokenType.ClosedSquareBracket, "Expected ']'");
        
        if (Current.type == TokenType.Equals)
        {
            ParsePrecedence(0);
            Add(Opcode.SETARR, lineIdx: tokBeforeExpr.lineIdx);
        }
        else
            Add(Opcode.PUSHARR, lineIdx: tokBeforeExpr.lineIdx);
    }

    private void HandleCtor()
    {
        Advance(); // past new
        var id = Current;
        Consume(TokenType.ClassIdentifier, "Expected class name.");

        Add(Opcode.PREPCALL);
        Consume(TokenType.OpenParenthesis, "Expected '(' after class name. Create new classes with: new ClassName(arg1,arg2,arg3)");

        ParseArguments(TokenType.ClosedParenthesis);

        Add(Opcode.EXT_CTOR, id.lexeme, id.lineIdx);
    }

    private void Error(Token token, string message)
    {
        if (token.type == TokenType.Error) return;
        errorManager.RaiseSyntaxError(token.lineIdx, token.charIdx, message);
    }
}