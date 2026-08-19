# Model.Console.Diagnostics

The logging subsystem shared by the Model.Console toolchain. `ResultLog` is
the process-wide log (a static `DefaultLog`), storing `IMessageLogEntry`
messages with `SeverityLevel` and exposing `LogMessageHandler`, a static event
any listener can subscribe to. `ILogService` is the DI wrapper that bridges
that static event into the container — UI panels subscribe once through it.

**Dependencies:** none.

**Usage**

```csharp
var log = ResultLog.DefaultLog;
log.LogMessageHandler += entry => Console.WriteLine(entry.Message);
log.Add("Renderer", SeverityLevel.Information, "Context ready.");
```
