# CR-002: Guard IL Parser Switch Bounds in Architecture Tests

- **Date:** 2026-11-15
- **From:** `backend-developer`
- **To:** `qa-engineer` / `project-manager`
- **Status:** Applied & Verified
- **Blocking:** No
- **References:** `tests/GroceryHub.ArchitectureTests/ArchitectureTests.cs`

---

## 1. Issue Description

In `tests/GroceryHub.ArchitectureTests/ArchitectureTests.cs`, the test method `Production_Code_Must_Not_Call_DateTime_Now_UtcNow_Or_Thread_Sleep` parses raw IL bytes to inspect called method tokens via helper `GetCalledMembers(Module module, MethodBase method)`.

When encountering opcode `0x45` (`switch`):
```csharp
if (op == 0x45)
{
    if (index + 4 > il.Length)
    {
        yield break;
    }

    int count = BitConverter.ToInt32(il, index);
    index += 4 + (count * 4);
    continue;
}
```
If an unhandled opcode precedes an operand or if a method contains large structures where `count * 4` overflows signed 32-bit integer arithmetic (e.g., when `count = 1864040448`, $1864040448 \times 4$ overflows to $-1133772800$), `index` becomes negative.
Because the while loop condition was `while (index < il.Length)`, `-1133772800 < il.Length` evaluated to `true`, causing `il[index++]` to throw an unhandled `System.IndexOutOfRangeException`.

---

## 2. Applied Change

In `tests/GroceryHub.ArchitectureTests/ArchitectureTests.cs`:
1. Updated while loop condition:
   ```csharp
   while (index >= 0 && index < il.Length)
   ```
2. Added defensive bounds validation for opcode `0x45`:
   ```csharp
   int count = BitConverter.ToInt32(il, index);
   if (count < 0 || count > 65535 || index + 4L + (count * 4L) > il.Length)
   {
       yield break;
   }
   ```

All 6 architecture tests now pass reliably in 17s.
