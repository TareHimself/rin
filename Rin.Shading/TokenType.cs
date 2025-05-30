﻿namespace Rin.Shading;

public enum TokenType
{
    Unknown,
    [KeywordToken("=")] Assign,
    [KeywordToken(".")] Access,
    [KeywordToken("&&")] OpAnd,
    [KeywordToken("||")] OpOr,
    [KeywordToken("!")] OpNot,
    [KeywordToken("++")] OpIncrement,
    [KeywordToken("--")] OpDecrement,
    [KeywordToken("+=")] OpAddAssign,
    [KeywordToken("-=")] OpSubtractAssign,
    [KeywordToken("/=")] OpDivideAssign,
    [KeywordToken("*=")] OpMultiplyAssign,
    [KeywordToken("+")] OpAdd,
    [KeywordToken("-")] OpSubtract,
    [KeywordToken("/")] OpDivide,
    [KeywordToken("*")] OpMultiply,
    [KeywordToken("%")] OpMod,
    [KeywordToken("==")] OpEqual,
    [KeywordToken("!=")] OpNotEqual,
    [KeywordToken("<")] OpLess,
    [KeywordToken(">")] OpGreater,
    [KeywordToken("<=")] OpLessEqual,
    [KeywordToken(">=")] OpGreaterEqual,
    [KeywordToken("{")] OpenBrace,
    [KeywordToken("}")] CloseBrace,
    [KeywordToken("(")] OpenParen,
    [KeywordToken(")")] CloseParen,
    [KeywordToken("[")] OpenBracket,
    [KeywordToken("]")] CloseBracket,
    Identifier,
    [KeywordToken("fn")] Function,
    [KeywordToken("return")] Return,
    [KeywordToken(",")] Comma,
    BooleanLiteral,
    [KeywordToken("for")] For,
    [KeywordToken("continue")] Continue,
    [KeywordToken("break")] Break,
    [KeywordToken(";")] StatementEnd,
    String,
    Numeric,
    [KeywordToken("struct")] TypeStruct,
    [KeywordToken("var")] TypeVar,
    [KeywordToken("float")] TypeFloat,
    [KeywordToken("float2")] TypeFloat2,
    [KeywordToken("float3")] TypeFloat3,
    [KeywordToken("float4")] TypeFloat4,
    [KeywordToken("float3x3")] TypeFloat3X3,
    [KeywordToken("float4x4")] TypeFloat4X4,
    [KeywordToken("int")] TypeInt,
    [KeywordToken("int2")] TypeInt2,
    [KeywordToken("int3")] TypeInt3,
    [KeywordToken("int4")] TypeInt4,
    [KeywordToken("bool")] TypeBoolean,
    [KeywordToken("void")] TypeVoid,
    [KeywordToken("image")] TypeImage,
    [KeywordToken("texture")] TypeTexture,
    [KeywordToken("cubemap")] TypeCubemap,
    [KeywordToken("in")] DataIn,
    [KeywordToken("out")] DataOut,
    [KeywordToken("layout")] Layout,
    [KeywordToken("uniform")] Uniform,
    [KeywordToken("readonly")] ReadOnly,
    [KeywordToken("discard")] Discard,
    [KeywordToken("#include")] Include,
    [KeywordToken("#define")] Define,
    [KeywordToken("const")] Const,
    [KeywordToken("static")] Static,
    [KeywordToken("push")] PushConstant,
    [KeywordToken("if")] If,
    [KeywordToken("else")] Else,
    [KeywordToken("?")] Conditional,
    [KeywordToken(":")] Colon,
    [KeywordToken("->")] Arrow,
    [KeywordToken("scope")] NamedScopeBegin,
    [KeywordToken("as")] As
}