using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Services;
using static VoltaX.Tests.Ocpp.Matrix.MatrixStation;

namespace VoltaX.Tests.Ocpp.Matrix;

/// <summary>What the charger waits for: its CALLs are answered without waiting for slow side effects.</summary>
public class ResponsivenessMatrixTests
{
    /// <summary>A mail server that takes its time to accept the session quote.</summary>
    public class SlowMail : DispatchProxy
    {
        public TimeSpan Delay { get; set; }
        public TaskCompletionSource QuoteSent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IMailService.SendChargingSessionQuoteMailRequest))
                return Task.Run(async () =>
                {
                    await Task.Delay(Delay);
                    QuoteSent.TrySetResult();
                });
            return Stub<IMailService>.DefaultResult(method.ReturnType);
        }
    }

    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task The_stop_is_answered_without_waiting_for_the_session_quote_mail(string protocol)
    {
        var mail = DispatchProxy.Create<IMailService, SlowMail>();
        var slow = (SlowMail)(object)mail;
        slow.Delay = TimeSpan.FromSeconds(5);
        var (host, charger, cardId) = await Open(protocol, configure: services => services.AddSingleton(mail));
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);

        var answered = Stopwatch.StartNew();
        await charger.Stop(uid, 13000, T0.AddMinutes(30), StopKind.LocalCard, idTag: "TAG-1");
        answered.Stop();

        Assert.True(answered.Elapsed < TimeSpan.FromSeconds(3), $"the stop was answered after {answered.Elapsed.TotalSeconds:0.0}s");
        await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
        Assert.Same(slow.QuoteSent.Task, await Task.WhenAny(slow.QuoteSent.Task, Task.Delay(TimeSpan.FromSeconds(15))));
    }
}
