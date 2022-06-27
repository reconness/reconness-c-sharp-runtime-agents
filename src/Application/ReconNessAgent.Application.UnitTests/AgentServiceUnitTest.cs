using FakeItEasy;
using ReconNessAgent.Application.DataAccess;
using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Providers;
using ReconNessAgent.Application.Services;
using ReconNessAgent.Domain.Core.Entities;

namespace ReconNessAgent.Application.UnitTests;

public class AgentServiceUnitTest
{
    [Fact]
    public async Task RunTerminalAsyncTestAsync()
    {
        var agentDataAccessServiceFake = A.Fake<IAgentDataAccessService>();
        var scriptEngineProvideFactoryFake = A.Fake<IScriptEngineProvideFactory>();
        var terminalProviderFactoryFake = A.Fake<ITerminalProviderFactory>();
        var unitOfWorkFake = A.Fake<IUnitOfWork>();

        const string channel = "#20220319.1_nmap_yahoo_yahoo.com_all";
        const string queueData = @"
        {
          ""Channel"": ""{{channel}}"",
          ""Payload"": ""www.yahoo.com"",
          ""Command"": ""nmap -T4 www.yahoo.com"",
          ""Count"": 5,
          ""AvailableServerNumber"": 1
        }";

        var agentRunner = new AgentRunner
        {
            Id = Guid.NewGuid(),
            Channel = channel,            
            AllowSkip = true,
            Stage = Domain.Core.Enums.AgentRunnerStage.ENQUEUE,
            Total = 10,
            ActivateNotification = true,
            
            AgentId = Guid.NewGuid()
        };

        A.CallTo(() => agentDataAccessServiceFake.GetAgentRunnerAsync(A<IUnitOfWork>._, A<string>._, A<CancellationToken>._)).Returns(agentRunner);

        var agentService = new AgentService(agentDataAccessServiceFake, scriptEngineProvideFactoryFake, terminalProviderFactoryFake);

        await agentService.RunAsync(null, queueData.Replace("{{channel}}", channel));

        A.CallTo(() => agentDataAccessServiceFake.GetAgentRunnerAsync(unitOfWorkFake, "#20220319.1_nmap_yahoo_yahoo.com_all", CancellationToken.None)).MustHaveHappened();
    }
}