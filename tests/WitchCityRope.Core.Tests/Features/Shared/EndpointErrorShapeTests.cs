using System.Runtime.CompilerServices;
using Xunit;

namespace WitchCityRope.Core.Tests.Features.Shared;

/// <summary>
/// Architectural test that enforces the Error Handling Standard documented in
/// <c>docs/standards-processes/backend/error-handling-standard.md</c>. An endpoint handler
/// MUST return error responses via <c>result.ToProblem(title)</c> (from
/// <c>WitchCityRope.Api.Features.Shared.Extensions.ResultExtensions</c>) — never by calling
/// <c>Results.Problem()</c>, <c>Results.BadRequest()</c>, <c>Results.NotFound()</c>, or
/// <c>Results.Conflict()</c> directly. MVC controllers (if any remain) must likewise avoid
/// <c>return Problem(...)</c>, <c>return BadRequest(...)</c>, etc.
///
/// <para>
/// Why this test exists: before the standard landed, every endpoint open-coded the
/// <c>ResultErrorKind</c> → HTTP status mapping inline. Several endpoints
/// (e.g. <c>EventEndpoints.cs</c>) string-matched the error text (<c>"not found"</c> → 404,
/// <c>"date"</c> → 400) to pick a status, so rephrasing an error message in the service
/// silently changed the HTTP status. Centralizing the mapping in <c>ToProblem()</c> fixes
/// the drift; this test prevents it from coming back.
/// </para>
///
/// <para>
/// Allowed exceptions, via an inline <c>// ARCH-ALLOW: &lt;reason&gt;</c> comment on the same
/// line as the forbidden call. Known ARCH-ALLOW sites when this test first landed:
///   • <c>AdminBackupEndpoints.cs</c> (15 sites) — backup feature uses try/catch around
///     Hangfire/Spaces calls, not <c>Result&lt;T&gt;</c>. Pending TD-BE-BACKUP-MIGRATION.
///   • Tuple-pattern service consumers (~149 sites across 12 endpoint files) — pending
///     TD-BE-TUPLE-MIGRATION migration of those services from tuples to <c>Result&lt;T&gt;</c>.
/// </para>
///
/// <para>
/// Also always allowed (no ARCH-ALLOW needed):
///   • <c>Results.ValidationProblem(...)</c> — FluentValidation results produce the
///     per-field errors dict that the frontend's ProblemDetails parser can consume. Not
///     routed through ToProblem() because the shape is different.
/// </para>
///
/// <para>
/// If this test fails, the xUnit message lists every offending file:line with the recommended
/// fix. Two ways to resolve:
///   1. Replace <c>Results.Problem(...)</c> with <c>result.ToProblem(title)</c> using
///      the service Result already in scope.
///   2. If you have a genuine reason to bypass the helper (rare — usually a code smell
///      that belongs in the service layer), add <c>// ARCH-ALLOW: &lt;short reason&gt;</c>
///      to the end of the line. Reviewers should push back unless the reason is concrete.
/// </para>
/// </summary>
public class EndpointErrorShapeTests
{
    // Patterns that should not appear in endpoint or controller files without ARCH-ALLOW.
    // Minimal-API-style (Results.*) dominates in this repo; the MVC-style `return Problem(` etc.
    // is covered for the handful of MVC controllers still present.
    private static readonly string[] ForbiddenPatterns =
    [
        "Results.Problem(",
        "Results.BadRequest(",
        "Results.NotFound(",
        "Results.Conflict(",
        "Results.UnprocessableEntity(",
        "return Problem(",
        "return BadRequest(",
        "return NotFound(",
        "return Conflict(",
    ];

    [Fact]
    public void Endpoint_handlers_do_not_construct_error_responses_directly()
    {
        var endpointFiles = FindEndpointFiles();
        Assert.NotEmpty(endpointFiles); // sanity — otherwise the test is silently passing

        var violations = new List<string>();

        foreach (var file in endpointFiles)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                // Skip lines that are inside /// XML doc comments or plain // comments.
                // Cheap heuristic: if the first non-whitespace chars are // or *, the line is
                // a comment. Does not handle block comments precisely, but mis-flagging an
                // example inside a block comment is acceptable — reviewer can fix the comment
                // or add ARCH-ALLOW.
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("*"))
                    continue;

                foreach (var pattern in ForbiddenPatterns)
                {
                    if (!line.Contains(pattern, StringComparison.Ordinal)) continue;
                    if (line.Contains("ARCH-ALLOW", StringComparison.Ordinal)) continue;

                    var relPath = Path.GetRelativePath(RepoRoot, file);
                    violations.Add($"  {relPath}:{i + 1}  →  {trimmed.Trim()}");
                    break;
                }
            }
        }

        if (violations.Count == 0) return;

        var message = string.Join(
            Environment.NewLine,
            new[]
            {
                "Endpoint handlers must NOT call Results.Problem / BadRequest / NotFound / Conflict directly.",
                "Use result.ToProblem(title) from WitchCityRope.Api.Features.Shared.Extensions.ResultExtensions.",
                "See docs/standards-processes/backend/error-handling-standard.md for the full standard.",
                "",
                "Violations:"
            }.Concat(violations));

        Assert.Fail(message);
    }

    /// <summary>
    /// Enumerates every endpoint source file under <c>apps/api/Features/&lt;area&gt;/Endpoints/</c>
    /// plus any remaining legacy MVC controllers in <c>apps/api/Controllers/</c>. Path
    /// resolution is bound to <see cref="RepoRoot"/> (computed from the CallerFilePath of
    /// this file) so it works both locally and in CI regardless of the working directory
    /// the test runner uses.
    /// </summary>
    private static IReadOnlyList<string> FindEndpointFiles()
    {
        var apiFeaturesRoot = Path.Combine(RepoRoot, "apps", "api", "Features");
        var controllersRoot = Path.Combine(RepoRoot, "apps", "api", "Controllers");

        if (!Directory.Exists(apiFeaturesRoot))
            throw new DirectoryNotFoundException(
                $"Expected API Features directory at '{apiFeaturesRoot}'. " +
                "Either the repo layout changed or RepoRoot resolution is broken.");

        var files = Directory.EnumerateDirectories(apiFeaturesRoot, "Endpoints", SearchOption.AllDirectories)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.cs", SearchOption.TopDirectoryOnly))
            .ToList();

        // Legacy MVC controllers. Directory is optional — if it's gone after ProtectedController
        // migration, skip rather than fail.
        if (Directory.Exists(controllersRoot))
        {
            files.AddRange(Directory.EnumerateFiles(controllersRoot, "*.cs", SearchOption.TopDirectoryOnly));
        }

        return files;
    }

    // Walks up from the source location of this test file to find the repo root (the
    // nearest parent containing an `apps/` directory). Using CallerFilePath makes the
    // resolution compile-time — immune to the test runner's working directory.
    private static readonly string RepoRoot = ResolveRepoRoot(ThisFilePath());

    private static string ResolveRepoRoot(string thisFile)
    {
        var dir = Path.GetDirectoryName(thisFile);
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "apps")))
            dir = Path.GetDirectoryName(dir);
        return dir ?? throw new InvalidOperationException(
            "Could not locate repo root (no parent contains an 'apps/' directory).");
    }

    private static string ThisFilePath([CallerFilePath] string path = "") => path;
}
