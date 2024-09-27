using CementGB.Mod.Utilities;
using System.Collections.Generic;

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
    // greater than or equal
    GTE,
    // less than
    LSS,

    // jumping to address
    JMP,
    // jump forward if false (zero)
    JFZ,
    // jump forward if not false (zero)
    JNZ,

    // for functions
    // pushes the top pointer of the stack, onto the call stack so that the stack window of the arguments are correct when a function is called
    PREPCALL,
    // pushes PC, and top stack pointer, onto call stack and jumps to address
    CALL,
    // pops address off call stack, reverts stack, and jumps to address (keeps last value (return value) on the stack)
    // also acts as halt when there are no more addresses are on the call stack
    RETURN,

    // c# interaction
    // call an external c# instance method of an object
    EXT_INST_CALL,
    // call an external static method in c#
    EXT_STATIC_CALL,
    // set an external field/member of a c# object
    SETFIELD,
    // push an external field/member from a c# object onto the stack
    PUSHFIELD,

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
    public Dictionary<string, int> functionPointers;
    public Instruction[] instructions;

    public ProgramInfo(string[] globals,  Dictionary<string, object?> globalVariables, Dictionary<string, int> functionPointers, Instruction[] instructions)
    {
        this.globals = globals;
        this.globalVariables = globalVariables;
        this.functionPointers = functionPointers;
        this.instructions = instructions;
    }
}

public class Parser
{
    private Lexer lexer;
    public bool hadError {
        get;
        private set;
    }

    public Parser(string code)
    {
        lexer = new Lexer(code);
        hadError = false;
    }

    public ProgramInfo Parse()
    {
        Token token;
        do
        {
            token = lexer.Next();
            LoggingUtilities.VerboseLog($"{token.type} {token.lexeme}");
        } while (token.type != TokenType.End);

        Dictionary<string, int> functionPointers = new();
        functionPointers["fact"] = 0;
        return new ProgramInfo(
            new string[] { "fact" },
            new(),
            functionPointers,
            /*  
             *  PUSHLOCAL 0
             *  PUSH 1
             *  EQU
             *  JFZ 2
             *  PUSH 1
             *  RETURN
             *  PUSHLOCAL 0 
             *  PREPCALL
             *  PUSHLOCAL 0
             *  PUSH 1
             *  SUB
             *  CALL 0
             *  MULT
             *  RETURN
             */
            new Instruction[]
            {
                new Instruction(Opcode.PUSHLOCAL, 0),
                new Instruction(Opcode.PUSH, 1),
                new Instruction(Opcode.EQU),
                new Instruction(Opcode.JFZ, 2),
                new Instruction(Opcode.PUSH, 1),
                new Instruction(Opcode.RETURN),
                new Instruction(Opcode.PUSHLOCAL, 0),
                new Instruction(Opcode.PREPCALL),
                new Instruction(Opcode.PUSHLOCAL, 0),
                new Instruction(Opcode.PUSH, 1),
                new Instruction(Opcode.SUB, 0),
                new Instruction(Opcode.CALL, 0),
                new Instruction(Opcode.MULT),
                new Instruction(Opcode.RETURN),
            }
        );
    }
}