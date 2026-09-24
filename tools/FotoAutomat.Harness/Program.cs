using System.Text.Json;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

string[] scenarios = ["happy", "camera-failure", "print-unknown", "restart-recovery"];
if (args is ["--list"])
{
    Console.WriteLine(string.Join(Environment.NewLine, scenarios));
    return 0;
}
if (args.Length > 0 && (args.Length != 2 || args[0] != "--scenario" || !scenarios.Contains(args[1])))
{
    Console.Error.WriteLine("Usage: FotoAutomat.Harness [--list | --scenario happy|camera-failure|print-unknown|restart-recovery]");
    return 2;
}
var selected = args.Length == 0 ? scenarios : [args[1]];
var failed = false;
foreach (var scenario in selected)
{
    var directory = Path.Combine(Path.GetTempPath(), "FotoAutomat.Harness", Guid.NewGuid().ToString("N"));
    var database = Path.Combine(directory, "booth.db");
    try
    {
        var store = new SqliteBoothStore(database);
        var printer = new SimulatedPrinter(scenario == "print-unknown" ? PrintOutcome.Unknown : PrintOutcome.Completed);
        var workflow = new BoothWorkflow(store,
            new SimulatedCamera(Path.Combine(directory, "captures"), scenario == "camera-failure"), printer);
        var session = workflow.Start();
        session = workflow.ConfirmDemoPayment(session.Id, "demo-receipt");
        var duplicate = workflow.ConfirmDemoPayment(session.Id, "demo-receipt");
        Require(duplicate.Revision == session.Revision, "Duplicate receipt changed state.");
        if (scenario == "camera-failure")
        {
            try { await workflow.CaptureAsync(session.Id); throw new Exception("Expected camera failure."); }
            catch (DeviceUnavailableException) { }
            Require(store.Get(session.Id).Stage == SessionStage.Paid, "Camera failure lost the paid state.");
            Require(printer.SubmissionCount == 0, "Camera failure submitted a print.");
        }
        else
        {
            session = await workflow.CaptureAsync(session.Id);
            if (scenario == "restart-recovery")
            {
                store.Transition(session, SessionStage.Printing);
                var reopened = new SqliteBoothStore(database);
                Require(reopened.RecoverInterruptedSessions() == 1, "Interrupted print was not recovered.");
                Require(reopened.Get(session.Id).Stage == SessionStage.NeedsReview, "Interrupted print was not held for review.");
                Require(printer.SubmissionCount == 0, "Recovery resubmitted a print.");
            }
            else
            {
                session = await workflow.PrintAsync(session.Id);
                Require(session.Stage == (scenario == "happy" ? SessionStage.Completed : SessionStage.NeedsReview),
                    "Unexpected print result state.");
                Require(printer.SubmissionCount == 1, "Unexpected submission count.");
            }
            try { await workflow.PrintAsync(session.Id); throw new Exception("Duplicate print was allowed."); }
            catch (InvalidOperationException) { }
        }
        Console.WriteLine(JsonSerializer.Serialize(new { scenario, result = "passed", directory }));
    }
    catch (Exception exception)
    {
        failed = true;
        Console.WriteLine(JsonSerializer.Serialize(new { scenario, result = "failed", error = exception.Message, directory }));
    }
}
return failed ? 1 : 0;

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
