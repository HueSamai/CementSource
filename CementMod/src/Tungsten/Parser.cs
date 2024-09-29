using CementGB.Mod.Utilities;
using Il2CppGB.Utils;
using Il2CppSystem;
using Il2CppSystem.Linq.Expressions.Interpreter;
using Il2CppSystem.Xml.Serialization;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Tungsten;

public enum Opcode
{
    // pops, then peforms op on last two values on the stack, and pushes the result
    ADD,    
    SUB,
    MULT,
    DIV,
    // inverts. if false, pops then pushes true, vice versa.
    INV,

    // for pushing and popping literals
    PUSH,
    POP,
    
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
    // jump forward if false (zero)
    JFZ,
    // jump forward if not false (zero)
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
    GETARR,
    SETARR,
    CRTARR

}

public struct Instruction
{
    public Opcode op;
    public object operand;

    public Instruction(Opcode op, object? operand=null)
    {
        this.op = op;
        this.operand = operand;
    }
}

public struct ProgramInfo
{
    public string[] globals;
    public Dictionary<string, object?> globalVariables;
    public Dictionary<string, FuncInfo> functions;
    public Instruction[] instructions;

    public ProgramInfo(string[] globals,  Dictionary<string, object?> globalVariables, Dictionary<string, FuncInfo> functions, Instruction[] instructions)
    {
        this.globals = globals;
        this.globalVariables = globalVariables;
        this.functions = functions;
        this.instructions = instructions;
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
    public bool hadError {
        get;
        private set;
    }

    private List<Instruction> instructions = new();
    private Dictionary<string, FuncInfo> functions = new()
    {
        { "__regglobal", new FuncInfo(0,0) }
    };

    public Parser(string code)
    {
        lexer = new Lexer(code);
        hadError = false;
    }

    private Token Current;
    private Token Previous;
    
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
            // raise error
            LoggingUtilities.VerboseLog("ERROR! " + errorMessage);
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

        while (Current.type == TokenType.KeywordFunc)
            Function();

        if (Current.type != TokenType.End)
        {
            // raise error
            LoggingUtilities.VerboseLog("ERROR! First include all your global var definitions, then functions. No other code can appear outside a function.");
        }

        Add(Opcode.PUSH, null);
        Add(Opcode.RETURN);

        return new ProgramInfo(
            new string[] { "main" },
            new(),
            functions,
            instructions.ToArray()
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

    private void Add(Opcode op, object? operand=null)
    {
        if (op == Opcode.POP && operand != null && (int)operand == 0) return;

        instructions.Add(new Instruction(op, operand));
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
            // raise error
            LoggingUtilities.VerboseLog("ERROR! Global var with that name already exists");
        }
    }

    private void Function()
    {
        // skip func
        Advance();

        int arity = 0;

        string id = Current.lexeme;
        Consume(TokenType.Identifier, "ERROR! Expected identifier. Functions can't start with a capital letter.");

        if (functions.ContainsKey(id))
        {
            // raise error
            LoggingUtilities.VerboseLog("Function with this name already exists!");
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
                Advance();
                LocalVariableDef();
                break;
            case TokenType.KeywordReturn:
                Advance();
                Return();
                break;
            default:
                Expression();
                Add(Opcode.POP, 1);
                Consume(TokenType.SemiColon, "Expected ';'");
                break;
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

            case TokenType.Minus: {
                ParsePrefixMinus(precedenceTable[TokenType.Bang]); // we don't have infix and prefix precedence distinction
                break;
            }

            case TokenType.ClassIdentifier:
                // didn't actually fail yet but this is flagged so that we can see if HandleColon was called
                failedToHandleStatic = true;
                LoggingUtilities.VerboseLog("CLASS IDENTIFIER");
                Advance();
                break;

            case TokenType.Identifier:
                HandleIdentifier();
                break;

            case TokenType.OpenCurlyBracket:
                Advance();
                ParseArguments(TokenType.ClosedCurlyBracket);
                Add(Opcode.CRTARR);
                break;

            // parse literal
            case TokenType.Integer:
                Add(Opcode.PUSH, int.Parse(Current.lexeme));
                Advance();

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
                // raise error
                LoggingUtilities.VerboseLog("ERROR! Unexpected token");
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
        ParsePrecedence(precedence);
        Add(Opcode.INV);
    }

    private void ParsePrefixMinus(int precedence)
    {
        Add(Opcode.PUSH, 0);
        Advance();
        ParsePrecedence(precedence);
        Add(Opcode.SUB);
    }

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

    private List<string> accessedGlobals = new();

    private int GetVariable(string name)
    {
        int variablesPassed = 0;
        int vars = totalVariables;
        for (int i = scopes.Count - 1; i >= 0; --i)
        {
            List<string> scope = scopes[i];
            vars -= scope.Count;
            foreach (string varName in scope)
            {
                if (varName == name)
                    return vars + variablesPassed;

                ++variablesPassed;
            }
        }

        if (scopes.Count == 0 && !globalVariables.Contains(name))
        {
            // raise error
            LoggingUtilities.VerboseLog("ERROR! No variable found!");
            return -1;
        }

        // if we can't find a local variable it must be a global

        // add it to a list, so that we can check at the end if all of the global values used were defined
        accessedGlobals.Add(name);
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
                    Opcode op = operatorOpcodes[Current.type];
                    Advance();
                    ParsePrecedence(currentTokenPrecedence + 1);
                    Add(op);
                    break;
            }

            if (failedToHandleStatic)
            {
                // raise error
                LoggingUtilities.VerboseLog("ERROR!");
            }

            if (!precedenceTable.ContainsKey(Current.type))
                return;

            currentTokenPrecedence = precedenceTable[Current.type];
        }
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
            Add(isStatic ? Opcode.EXT_STATIC_CALL : Opcode.EXT_INST_CALL, methodOrFieldName);
        }
        else
        {
            if (Current.type == TokenType.Equals)
            {
                Advance();
                ParsePrecedence(0);
                Add(isStatic ? Opcode.SET_STATIC_FIELD : Opcode.SET_INST_FIELD, methodOrFieldName);
            }
            else
            {
                Add(isStatic ? Opcode.PUSH_STATIC_FIELD : Opcode.PUSH_INST_FIELD, methodOrFieldName);
            }
        }
    }

    private void ParseArguments(TokenType endToken)
    {
        Add(Opcode.PREPCALL);
        while (Current.type != TokenType.End && Current.type != endToken)
        {
            Expression();
            if (Current.type == endToken)
                break;
            Consume(TokenType.Comma, "Expected comma");
        }
        Consume(endToken, "Expected " + endToken.ToString());
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
        Advance();
        Expression(); // index
        Consume(TokenType.ClosedSquareBracket, "Expected ']'");
        
        if (Current.type == TokenType.Equals)
        {
            ParsePrecedence(0);
            Add(Opcode.SETARR);
        }
        else
            Add(Opcode.GETARR);
    }
}