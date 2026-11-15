# CR-001: Allow `GroceryHub.Persistence` Reference in Architecture Tests

- **Date:** 2026-11-15
- **From:** `backend-developer`
- **To:** `qa-engineer` / `project-manager`
- **Status:** Approved (by `project-manager`)
- **Blocking:** Yes (Blocks Task `T05-02`)
- **References:** `docs/spec/adr/ADR-001-modular-monolith.md` §2, `docs/spec/05-SYSTEM-ARCHITECTURE.md` §10.1, `docs/spec/module-contracts.md` §2, `tests/GroceryHub.ArchitectureTests/ArchitectureTests.cs`

---

## 1. Issue Description

In `tests/GroceryHub.ArchitectureTests/ArchitectureTests.cs`, the test `Modules_Must_Only_Depend_On_SharedKernel_And_Other_Module_Contracts` currently checks:
```csharp
bool allowedReference =
    string.Equals(refName, "GroceryHub.SharedKernel", StringComparison.Ordinal) ||
    refName.EndsWith(".Contracts", StringComparison.Ordinal);
```
and:
```csharp
bool isAllowed =
    normalized.EndsWith("/GroceryHub.SharedKernel.csproj", StringComparison.Ordinal) ||
    normalized.Equals("GroceryHub.SharedKernel.csproj", StringComparison.Ordinal) ||
    normalized.EndsWith(".Contracts.csproj", StringComparison.Ordinal);
```

This omits `GroceryHub.Persistence`, which directly contradicts the approved architectural specifications:
1. **`ADR-001` §2.2:**
   > "A module implementation project `GroceryHub.Modules.<Caller>` may reference only:
   > - `GroceryHub.SharedKernel`
   > - `GroceryHub.Persistence` (for unit-of-work / repository / `DbSet` access within its own schema)
   > - `GroceryHub.Modules.<Caller>.Contracts`
   > - Other modules' **Contracts** projects (`GroceryHub.Modules.<Target>.Contracts`)"
2. **`docs/spec/05-SYSTEM-ARCHITECTURE.md` §10.1:**
   > "1. Module Isolation: `GroceryHub.Modules.<Caller>` references only `GroceryHub.SharedKernel`, `GroceryHub.Persistence`, its own `.Contracts`, and other modules' `.Contracts` assemblies (never another module's implementation assembly)."
3. **`docs/spec/module-contracts.md` §2 (Topological Order):**
   > "4. All 12 `GroceryHub.Modules.*` implementation assemblies (depend only on `SharedKernel`, `Persistence`, own `Contracts`, and target `Contracts` from Tier 2)"
4. **Phase 05 Task `T05-02` Prompt:**
   > "Implement `AuditLogService` writing to `audit.AuditLogs` via `GroceryHubDbContext`."

Because `GroceryHubDbContext` and EF entities live in `GroceryHub.Persistence`, `GroceryHub.Modules.Audit` and `GroceryHub.Modules.Identity` must reference `GroceryHub.Persistence` to compile. When adding `<ProjectReference Include="..\GroceryHub.Persistence\GroceryHub.Persistence.csproj" />`, `ArchitectureTests` fails because `GroceryHub.Persistence` was omitted from `allowedReference` and `isAllowed`.

---

## 2. Proposed Change

In `tests/GroceryHub.ArchitectureTests/ArchitectureTests.cs`:
1. In `Modules_Must_Only_Depend_On_SharedKernel_And_Other_Module_Contracts`:
   Update `allowedReference`:
   ```csharp
   bool allowedReference =
       string.Equals(refName, "GroceryHub.SharedKernel", StringComparison.Ordinal) ||
       string.Equals(refName, "GroceryHub.Persistence", StringComparison.Ordinal) ||
       refName.EndsWith(".Contracts", StringComparison.Ordinal);
   ```
2. In csproj references check:
   Update `isAllowed`:
   ```csharp
   bool isAllowed =
       normalized.EndsWith("/GroceryHub.SharedKernel.csproj", StringComparison.Ordinal) ||
       normalized.Equals("GroceryHub.SharedKernel.csproj", StringComparison.Ordinal) ||
       normalized.EndsWith("/GroceryHub.Persistence.csproj", StringComparison.Ordinal) ||
       normalized.Equals("GroceryHub.Persistence.csproj", StringComparison.Ordinal) ||
       normalized.EndsWith(".Contracts.csproj", StringComparison.Ordinal);
   ```

Note: Cross-module implementation isolation remains 100% enforced — no module can reference another module's implementation assembly. Only `SharedKernel`, `Persistence`, and `.Contracts` are permitted, exactly per ADR-001.
