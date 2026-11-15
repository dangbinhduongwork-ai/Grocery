using System.Reflection;
using System.Xml.Linq;
using GroceryHub.Modules.Audit.Api;
using GroceryHub.Modules.Cart.Api;
using GroceryHub.Modules.Catalog.Api;
using GroceryHub.Modules.Customers.Api;
using GroceryHub.Modules.Identity.Api;
using GroceryHub.Modules.Inventory.Api;
using GroceryHub.Modules.Ordering.Api;
using GroceryHub.Modules.Payments.Api;
using GroceryHub.Modules.Promotions.Api;
using GroceryHub.Modules.Purchasing.Api;
using GroceryHub.Modules.Reporting.Api;
using GroceryHub.Modules.Reviews.Api;
using GroceryHub.Persistence;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NetArchTest.Rules;
using Xunit;

namespace GroceryHub.ArchitectureTests;

public sealed class ArchitectureTests
{
    private static readonly Assembly[] ModuleAssemblies =
    [
        typeof(AuditModuleExtensions).Assembly,
        typeof(CartModuleExtensions).Assembly,
        typeof(CatalogModuleExtensions).Assembly,
        typeof(CustomersModuleExtensions).Assembly,
        typeof(IdentityModuleExtensions).Assembly,
        typeof(InventoryModuleExtensions).Assembly,
        typeof(OrderingModuleExtensions).Assembly,
        typeof(PaymentsModuleExtensions).Assembly,
        typeof(PromotionsModuleExtensions).Assembly,
        typeof(PurchasingModuleExtensions).Assembly,
        typeof(ReportingModuleExtensions).Assembly,
        typeof(ReviewsModuleExtensions).Assembly,
    ];

    private static readonly Assembly[] ProductionAssemblies =
    [
        typeof(Money).Assembly,
        typeof(GroceryHubDbContext).Assembly,
        typeof(Program).Assembly,
        .. ModuleAssemblies,
    ];

    private static readonly string[] BannedLibraryPrefixes =
    [
        "MediatR",
        "AutoMapper",
        "FluentAssertions",
        "MassTransit",
        "Duende",
    ];

    [Fact]
    public void Modules_Must_Only_Depend_On_SharedKernel_And_Other_Module_Contracts()
    {
        Assert.Equal(12, ModuleAssemblies.Distinct().Count());

        string[] moduleNames = ModuleAssemblies
            .Select(static a => a.GetName().Name!)
            .OrderBy(static n => n, StringComparer.Ordinal)
            .ToArray();

        List<string> violations = [];

        foreach (Assembly moduleAssembly in ModuleAssemblies)
        {
            string moduleName = moduleAssembly.GetName().Name!;

            string[] forbiddenNamespaces = moduleNames
                .Where(other => !string.Equals(other, moduleName, StringComparison.Ordinal))
                .SelectMany(static other => new[]
                {
                    $"{other}.Domain",
                    $"{other}.Application",
                    $"{other}.Infrastructure",
                    $"{other}.Api",
                })
                .Append("GroceryHub.Api")
                .ToArray();

            TestResult namespaceResult = Types.InAssembly(moduleAssembly)
                .ShouldNot()
                .HaveDependencyOnAny(forbiddenNamespaces)
                .GetResult();

            if (!namespaceResult.IsSuccessful)
            {
                string failingTypes = string.Join(", ", namespaceResult.FailingTypeNames ?? []);
                violations.Add($"{moduleName} has forbidden namespace dependencies in types: [{failingTypes}]");
            }

            foreach (AssemblyName referencedAssembly in moduleAssembly.GetReferencedAssemblies())
            {
                string refName = referencedAssembly.Name ?? string.Empty;
                if (!refName.StartsWith("GroceryHub.", StringComparison.Ordinal))
                {
                    continue;
                }

                bool allowedReference =
                    string.Equals(refName, "GroceryHub.SharedKernel", StringComparison.Ordinal) ||
                    string.Equals(refName, "GroceryHub.Persistence", StringComparison.Ordinal) ||
                    refName.EndsWith(".Contracts", StringComparison.Ordinal);

                if (!allowedReference)
                {
                    violations.Add($"{moduleName} assembly references forbidden internal assembly '{refName}'.");
                }
            }
        }

        string repoRoot = LocateRepositoryRoot();
        string srcDirectory = Path.Combine(repoRoot, "src");
        string[] moduleProjectFiles = Directory.GetFiles(srcDirectory, "GroceryHub.Modules.*.csproj", SearchOption.AllDirectories);
        Assert.Equal(12, moduleProjectFiles.Length);

        foreach (string csprojPath in moduleProjectFiles)
        {
            XDocument doc = XDocument.Parse(File.ReadAllText(csprojPath));
            string projectFileName = Path.GetFileName(csprojPath);

            IEnumerable<string> projectReferences = doc
                .Descendants()
                .Where(static e => string.Equals(e.Name.LocalName, "ProjectReference", StringComparison.Ordinal))
                .Select(static e => e.Attribute("Include")?.Value ?? string.Empty);

            foreach (string includePath in projectReferences)
            {
                string normalized = includePath.Replace('\\', '/');
                bool isAllowed =
                    normalized.EndsWith("/GroceryHub.SharedKernel.csproj", StringComparison.Ordinal) ||
                    normalized.Equals("GroceryHub.SharedKernel.csproj", StringComparison.Ordinal) ||
                    normalized.EndsWith("/GroceryHub.Persistence.csproj", StringComparison.Ordinal) ||
                    normalized.Equals("GroceryHub.Persistence.csproj", StringComparison.Ordinal) ||
                    normalized.EndsWith(".Contracts.csproj", StringComparison.Ordinal);

                if (!isAllowed)
                {
                    violations.Add($"{projectFileName} contains forbidden <ProjectReference Include=\"{includePath}\" />.");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Module boundary violations detected:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Api_Types_Must_Not_Depend_On_Persistence_Entities()
    {
        Assembly apiAssembly = typeof(Program).Assembly;

        TestResult noPersistenceEntityDependency = Types.InAssembly(apiAssembly)
            .ShouldNot()
            .HaveDependencyOn("GroceryHub.Persistence.Entities")
            .GetResult();

        Assert.True(
            noPersistenceEntityDependency.IsSuccessful,
            $"GroceryHub.Api types must not depend on GroceryHub.Persistence.Entities. Failing types: {string.Join(", ", noPersistenceEntityDependency.FailingTypeNames ?? [])}");

        TestResult noDomainBaseClassInheritance = Types.InAssembly(apiAssembly)
            .ShouldNot()
            .Inherit(typeof(Entity<>))
            .And()
            .Inherit(typeof(AggregateRoot<>))
            .GetResult();

        Assert.True(
            noDomainBaseClassInheritance.IsSuccessful,
            $"GroceryHub.Api types must not inherit Entity<> or AggregateRoot<>. Failing types: {string.Join(", ", noDomainBaseClassInheritance.FailingTypeNames ?? [])}");

        string[] openGenericEntityInheritors = GetLoadableTypes(apiAssembly)
            .Where(static t => InheritsOpenGeneric(t, typeof(Entity<>)) || InheritsOpenGeneric(t, typeof(AggregateRoot<>)))
            .Select(static t => t.FullName ?? t.Name)
            .ToArray();

        Assert.Empty(openGenericEntityInheritors);
    }

    [Fact]
    public void Default_Deny_Endpoints_Require_Authorization_Or_AllowAnonymous()
    {
        All_Mapped_Endpoints_Must_Have_Authorization_Policy_Or_AllowAnonymous();
    }

    [Fact]
    public void All_Mapped_Endpoints_Must_Have_Authorization_Policy_Or_AllowAnonymous()
    {
        using WebApplicationFactory<Program> factory = new();
        AssertEndpointsProtected(factory);
    }

    private static void AssertEndpointsProtected(WebApplicationFactory<Program> factory)
    {
        IEnumerable<EndpointDataSource> dataSources = factory.Services.GetRequiredService<IEnumerable<EndpointDataSource>>();

        RouteEndpoint[] routeEndpoints = dataSources
            .SelectMany(static ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();

        Assert.NotEmpty(routeEndpoints);

        List<string> unprotectedEndpoints = [];

        foreach (RouteEndpoint endpoint in routeEndpoints)
        {
            bool hasAuthorize = endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null;
            bool hasAllowAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;

            if (!hasAuthorize && !hasAllowAnonymous)
            {
                unprotectedEndpoints.Add(endpoint.DisplayName ?? endpoint.RoutePattern.RawText ?? "<unknown>");
            }
        }

        Assert.True(
            unprotectedEndpoints.Count == 0,
            "Every mapped RouteEndpoint must declare either an authorization policy or AllowAnonymous. Missing on: " +
            string.Join(", ", unprotectedEndpoints));
    }

    [Fact]
    public void Production_Code_Must_Not_Call_DateTime_Now_UtcNow_Or_Thread_Sleep()
    {
        List<string> violations = [];

        foreach (Assembly assembly in ProductionAssemblies)
        {
            foreach (Module module in assembly.GetModules())
            {
                foreach (Type type in GetLoadableTypes(module.Assembly))
                {
                    BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

                    IEnumerable<MethodBase> methods = type.GetMethods(flags)
                        .Cast<MethodBase>()
                        .Concat(type.GetConstructors(flags));

                    foreach (MethodBase method in methods)
                    {
                        foreach (MemberInfo calledMember in GetCalledMembers(module, method))
                        {
                            if (IsForbiddenTimeOrSleepMember(calledMember))
                            {
                                violations.Add($"{type.FullName}.{method.Name} calls {calledMember.DeclaringType?.FullName}.{calledMember.Name}");
                            }
                        }
                    }
                }
            }
        }

        string repoRoot = LocateRepositoryRoot();
        string srcRoot = Path.Combine(repoRoot, "src");
        const string sleepMethodName = "Sleep";
        string forbiddenSleepToken = string.Concat(nameof(Thread), ".", sleepMethodName);
        string[] forbiddenSourceTokens =
        [
            "DateTime.Now",
            "DateTime.UtcNow",
            "DateTimeOffset.Now",
            "DateTimeOffset.UtcNow",
            forbiddenSleepToken,
        ];

        foreach (string csFile in Directory.GetFiles(srcRoot, "*.cs", SearchOption.AllDirectories))
        {
            string normalizedPath = csFile.Replace('\\', '/');
            if (normalizedPath.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.Contains("/obj/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string sourceText = File.ReadAllText(csFile);
            foreach (string token in forbiddenSourceTokens)
            {
                if (sourceText.Contains(token, StringComparison.Ordinal))
                {
                    violations.Add($"Source file '{Path.GetRelativePath(repoRoot, csFile)}' contains forbidden call '{token}'.");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Forbidden non-deterministic time or blocking sleep calls detected:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Solution_Must_Not_Reference_Banned_Commercial_Libraries()
    {
        List<string> violations = [];

        foreach (Assembly assembly in ProductionAssemblies.Append(typeof(ArchitectureTests).Assembly).Distinct())
        {
            string assemblyName = assembly.GetName().Name ?? string.Empty;
            if (IsBannedPackageName(assemblyName))
            {
                violations.Add($"Loaded assembly '{assemblyName}' is a banned commercial library.");
            }

            foreach (AssemblyName referenced in assembly.GetReferencedAssemblies())
            {
                string refName = referenced.Name ?? string.Empty;
                if (IsBannedPackageName(refName))
                {
                    violations.Add($"Assembly '{assemblyName}' references banned library '{refName}'.");
                }
            }
        }

        string repoRoot = LocateRepositoryRoot();
        List<string> xmlFilesToCheck =
        [
            Path.Combine(repoRoot, "Directory.Packages.props"),
            .. Directory.GetFiles(Path.Combine(repoRoot, "src"), "*.csproj", SearchOption.AllDirectories),
            .. Directory.GetFiles(Path.Combine(repoRoot, "tests"), "*.csproj", SearchOption.AllDirectories),
        ];

        foreach (string xmlFile in xmlFilesToCheck.Where(File.Exists))
        {
            XDocument doc = XDocument.Parse(File.ReadAllText(xmlFile));
            IEnumerable<string> packageIds = doc
                .Descendants()
                .Where(static e =>
                    string.Equals(e.Name.LocalName, "PackageReference", StringComparison.Ordinal) ||
                    string.Equals(e.Name.LocalName, "PackageVersion", StringComparison.Ordinal))
                .Select(static e => e.Attribute("Include")?.Value ?? e.Attribute("Update")?.Value ?? string.Empty);

            foreach (string packageId in packageIds)
            {
                if (IsBannedPackageName(packageId))
                {
                    violations.Add($"File '{Path.GetRelativePath(repoRoot, xmlFile)}' references banned package '{packageId}'.");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Banned commercial libraries detected:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    private static bool InheritsOpenGeneric(Type candidate, Type openGenericBase)
    {
        Type? current = candidate.BaseType;
        while (current is not null && current != typeof(object))
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == openGenericBase)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static bool IsBannedPackageName(string name) =>
        BannedLibraryPrefixes.Any(prefix =>
            string.Equals(name, prefix, StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase));

    private static bool IsForbiddenTimeOrSleepMember(MemberInfo member)
    {
        Type? declaringType = member.DeclaringType;
        if (declaringType is null)
        {
            return false;
        }

        if ((declaringType == typeof(DateTime) || declaringType == typeof(DateTimeOffset)) &&
            (string.Equals(member.Name, "get_Now", StringComparison.Ordinal) ||
             string.Equals(member.Name, "get_UtcNow", StringComparison.Ordinal)))
        {
            return true;
        }

        if (declaringType == typeof(Thread) &&
            string.Equals(member.Name, "Sleep", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(static t => t is not null).Cast<Type>();
        }
    }

    private static IEnumerable<MemberInfo> GetCalledMembers(Module module, MethodBase method)
    {
        MethodBody? body;
        try
        {
            body = method.GetMethodBody();
        }
        catch
        {
            yield break;
        }

        byte[]? il = body?.GetILAsByteArray();
        if (il is null || il.Length == 0)
        {
            yield break;
        }

        Type[] genericTypeArguments = method.DeclaringType?.IsGenericType == true
            ? method.DeclaringType.GetGenericArguments()
            : [];
        Type[] genericMethodArguments = method.IsGenericMethod
            ? method.GetGenericArguments()
            : [];

        int index = 0;
        while (index >= 0 && index < il.Length)
        {
            byte op = il[index++];
            if (op == 0xFE)
            {
                if (index >= il.Length)
                {
                    yield break;
                }

                byte second = il[index++];
                index += GetTwoByteOpCodeOperandSize(second);
                continue;
            }

            // 0x28 = call, 0x6F = callvirt, 0x73 = newobj
            if (op is 0x28 or 0x6F or 0x73)
            {
                if (index + 4 > il.Length)
                {
                    yield break;
                }

                int metadataToken = BitConverter.ToInt32(il, index);
                index += 4;

                MemberInfo? resolved = null;
                try
                {
                    resolved = module.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);
                }
                catch
                {
                    // Ignore unresolvable tokens in synthetic/compiler methods.
                }

                if (resolved is not null)
                {
                    yield return resolved;
                }

                continue;
            }

            if (op == 0x45)
            {
                // switch instruction: 4-byte count N followed by N * 4-byte targets
                if (index + 4 > il.Length)
                {
                    yield break;
                }

                int count = BitConverter.ToInt32(il, index);
                if (count < 0 || count > 65535 || index + 4L + (count * 4L) > il.Length)
                {
                    yield break;
                }

                index += 4 + (count * 4);
                continue;
            }

            index += GetSingleByteOpCodeOperandSize(op);
        }
    }

    private static int GetSingleByteOpCodeOperandSize(byte op) => op switch
    {
        0x0E or 0x11 or 0x12 or 0x13 or 0x1F or 0x2B or 0x2C or 0x2D or
        0x2E or 0x2F or 0x30 or 0x31 or 0x32 or 0x33 or 0x34 or 0x35 or
        0x36 or 0x37 or 0xDE => 1,

        0x20 or 0x22 or 0x26 or 0x27 or 0x29 or 0x38 or 0x39 or 0x3A or
        0x3B or 0x3C or 0x3D or 0x3E or 0x3F or 0x40 or 0x41 or 0x42 or
        0x43 or 0x44 or 0x70 or 0x71 or 0x72 or 0x74 or 0x75 or 0x79 or
        0x7B or 0x7C or 0x7D or 0x7E or 0x7F or 0x80 or 0x81 or 0x8C or
        0x8D or 0x8F or 0xA3 or 0xA4 or 0xA5 or 0xC2 or 0xC6 or 0xD0 or
        0xDD => 4,

        0x21 or 0x23 => 8,

        _ => 0,
    };

    private static int GetTwoByteOpCodeOperandSize(byte secondByte) => secondByte switch
    {
        0x09 or 0x0A or 0x0B or 0x0C or 0x0D or 0x0E => 2,
        0x06 or 0x07 or 0x15 or 0x16 or 0x17 or 0x18 or 0x1A or 0x1C or 0x1D or 0x1E => 4,
        0x19 => 1,
        _ => 0,
    };

    private static string LocateRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "GroceryHub.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing GroceryHub.sln.");
    }
}
