using System.Collections.Generic;
using System;

namespace Tungsten;

public enum TokenType
{
    // starts with a lowercase letter
    Identifier,
    // starts with a capital letter
    ClassIdentifier,
    Integer,
    Float,
    String,

    KeywordIf,
    KeywordElse,
    KeywordFor,
    KeywordWhile,
    KeywordReturn,
    KeywordFunc,
    KeywordVar,
    KeywordFalse,
    KeywordTrue,
    KeywordNull,
    KeywordNew,
    KeywordBreak,
    KeywordContinue,
    KeywordIn,

    OpenCurlyBracket,
    ClosedCurlyBracket,
    OpenSquareBracket,
    ClosedSquareBracket,
    OpenParenthesis,
    ClosedParenthesis,

    Equals,
    Greater,
    Less,
    LessEquals,
    GreaterEquals,
    DoubleEquals,
    BangEquals,

    Plus,
    Minus,
    Asterisk,
    ForwardSlash,
    Bang,

    SemiColon,
    Colon,
    Comma,

    End,
    Error
}

public struct Token
{
    public TokenType type;
    public string lexeme;

    public Token(TokenType type, string lexeme="")
    {
        this.type = type;
        this.lexeme = lexeme;
    }
}

public class Lexer
{
    private string _code;
    private int i;

    private Dictionary<string, TokenType> _keywords = new() {
        { "if", TokenType.KeywordIf },
        { "else", TokenType.KeywordElse },
        { "for", TokenType.KeywordFor },
        { "while", TokenType.KeywordWhile },
        { "return", TokenType.KeywordReturn },
        { "func", TokenType.KeywordFunc },
        { "var", TokenType.KeywordVar },
        { "true", TokenType.KeywordTrue },
        { "false", TokenType.KeywordFalse },
        { "null", TokenType.KeywordNull },
        { "new", TokenType.KeywordNew },
        { "break", TokenType.KeywordBreak },
        { "continue", TokenType.KeywordContinue },
        { "in", TokenType.KeywordIn }
    };

    public Lexer(string code)
    {
        _code = code;
        i = 0;
    }

    private void RemoveWhitespace()
    {
        while (!IsEnd && char.IsWhiteSpace(_code[i]))
            ++i;
    }

    private char Current => _code[i];

    private bool Match(char c)
    {
        if (Current == c)
        {
            ++i;
            return true;
        }

        return false;
    }

    private bool IsEnd => i >= _code.Length;
    
    private bool IsIdentifier(char c)
    {
        return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c == '_') || (c =='.') || IsDigit(c);
    }

    private bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }


    public Token Next()
    {
        RemoveWhitespace();
        if (IsEnd) return new Token(TokenType.End);

        switch (Current)
        {
            case '+':
                ++i;
                return new Token(TokenType.Plus);
            case '-':
                ++i;
                return new Token(TokenType.Minus);
            case '*':
                ++i;
                return new Token(TokenType.Asterisk);
            case '/':
                ++i;
                return new Token(TokenType.ForwardSlash);
            case '(':
                ++i;
                return new Token(TokenType.OpenParenthesis);
            case ')':
                ++i;
                return new Token(TokenType.ClosedParenthesis);
            case '[':
                ++i;
                return new Token(TokenType.OpenSquareBracket);
            case ']':
                ++i;
                return new Token(TokenType.ClosedSquareBracket);
            case '{':
                ++i;
                return new Token(TokenType.OpenCurlyBracket);
            case '}':
                ++i;
                return new Token(TokenType.ClosedCurlyBracket);
            case '=':
                ++i;
                if (Match('=')) return new Token(TokenType.DoubleEquals);
                return new Token(TokenType.Equals);
            case '>':
                ++i;
                if (Match('=')) return new Token(TokenType.GreaterEquals);
                return new Token(TokenType.Greater);
            case '<':
                ++i;
                if (Match('=')) return new Token(TokenType.LessEquals);
                return new Token(TokenType.Less);
            case '!':
                ++i;
                if (Match('=')) return new Token(TokenType.BangEquals);
                return new Token(TokenType.Bang);
            case ',':
                ++i;
                return new Token(TokenType.Comma);
            case ';':
                ++i;
                return new Token(TokenType.SemiColon);
            case ':':
                ++i;
                return new Token(TokenType.Colon);
            case '"':
                return LexString();
            default:
                if (IsDigit(Current))
                    return LexNum();
                if (IsIdentifier(Current))
                    return LexId();

                break;
                
        }

        ++i;
        return new Token(TokenType.Error);
    }

    private Token LexNum()
    {
        string num = "";
        while (!IsEnd && IsDigit(Current))
        {
            num += Current;
            ++i;
        }

        bool isInt = true;
        if (!IsEnd && Current == '.')
        {
            isInt = false;
            ++i;
            num += '.';
            bool raiseError = true;
            while (!IsEnd && IsDigit(Current))
            {
                num += Current;
                ++i;
                raiseError = false;
            }

            if (raiseError)
            {
                // raise error
            }
        }

        return new Token(isInt ? TokenType.Integer : TokenType.Float, num);
    }

    private Token LexString()
    {
        ++i; // advance past "
        if (IsEnd)
        {
            // raise error
            return new Token(TokenType.String, "");
        }

        string s = "";
        while (Current != '"')
        {
            if (Current == '\n')
            {
                // raise an error
                break;
            }
            if (Current == '\\')
            {
                ++i;
                if (IsEnd)
                {
                    // raise error
                    return new Token(TokenType.String, s);
                }
                switch (Current)
                {
                    case '\\':
                        s += '\\';
                        break;
                    case 'n':
                        s += '\n';
                        break;
                    case 'r':
                        s += '\r';
                        break;
                    case '"':
                        s += '"';
                        break;
                    case 't':
                        s += '\t';
                        break;
                    default:
                        // throw an error
                        break;
                }
            }
            else
            {
                s += Current;
            }
            ++i;
            if (IsEnd)
            {
                // raise error
                break;
            }
        }
        if (Current == '"') ++i;
        return new Token(TokenType.String, s);
    }

    private Token LexId()
    {
        string id = "";
        while (!IsEnd && IsIdentifier(Current))
        {
            id += Current;
            ++i;
        }

        if (_keywords.ContainsKey(id))
            return new Token(_keywords[id]);

        return new Token(char.IsUpper(id[0]) ? TokenType.ClassIdentifier : TokenType.Identifier, id);
    }
}
