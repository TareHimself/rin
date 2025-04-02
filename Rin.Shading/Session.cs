using System.Collections.Concurrent;
using System.Text;
using Rin.Shading.Ast.Nodes;

namespace Rin.Shading;

public class Session
{
    private readonly IFileSystem _fileSystem;
    private readonly ConcurrentDictionary<string, Module> _modules = [];

    public Session(IFileSystem? fileSystem = null)
    {
        _fileSystem = fileSystem ?? new OsFileSystem();
    }

    private NamedScopeNode ResolveIncludes(string fullSourcePath, NamedScopeNode namedScope)
    {
        List<INode> result = [];
        var statements = namedScope.Statements;
        foreach (var statement in statements)
            if (statement is IncludeNode asInclude)
            {
                var sourcePath = _fileSystem.GetFullIncludePath(fullSourcePath, asInclude.Path);
                var module = LoadSource(sourcePath);
                result.AddRange(module.Statements);
            }
            else
            {
                result.Add(statement);
            }

        return new NamedScopeNode
        {
            Name = namedScope.Name,
            Statements = result.ToArray()
        };
    }

    private INode[] ResolveIncludes(string fullSourcePath, INode[] statements)
    {
        List<INode> result = [];

        foreach (var statement in statements)
            if (statement is IncludeNode asInclude)
            {
                var sourcePath = _fileSystem.GetFullIncludePath(fullSourcePath, asInclude.Path);
                var module = LoadSource(sourcePath);
                result.AddRange(module.Statements);
            }
            else if (statement is NamedScopeNode asNamedScope)
            {
                result.Add(ResolveIncludes(fullSourcePath, asNamedScope));
            }
            else
            {
                result.Add(statement);
            }

        return result.ToArray();
    }

    private INode[] ParseTokens(string fullSourcePath, ref TokenList tokens)
    {
        var ast = Parser.Parse(ref tokens);
        ast = ResolveIncludes(fullSourcePath, ast);
        return ast;
    }

    private Module HandleSourceLoad(string fullSourcePath, Stream source)
    {
        var tokens = Tokenizer.Run(fullSourcePath, source);
        var ast = ParseTokens(fullSourcePath, ref tokens);

        var module = new Module(fullSourcePath, ast);

        _modules.AddOrUpdate(_fileSystem.GetUniqueIdentifier(fullSourcePath), module, (_, _) => module);
        return module;
    }

    public Module LoadSource(string path)
    {
        var fullPath = _fileSystem.GetFullSourcePath(path);
        return HandleSourceLoad(fullPath, _fileSystem.GetContent(fullPath));
    }

    public Module LoadSourceString(string path, Stream content)
    {
        return HandleSourceLoad(_fileSystem.GetFullSourcePath(path), content);
    }

    public Module LoadSourceString(string path, string content)
    {
        return HandleSourceLoad(_fileSystem.GetFullSourcePath(path), new MemoryStream(Encoding.UTF8.GetBytes(content)));
    }
}