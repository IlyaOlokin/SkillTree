# Combat regression checks

Run `dotnet run --project Tests/CombatRegression` with .NET 9.

The runner compiles the linked production combat files and exercises them with
small substitutes for Unity and surrounding game systems. No NuGet packages are
needed. It verifies arithmetic, callback ordering, and ownership requests; it does
not verify Unity's deferred native-object destruction, scenes, rendering, the full
damage pipeline, or performance. Run a Unity play session and inspect the Console
and Profiler separately. Full project compilation is a separate check.
