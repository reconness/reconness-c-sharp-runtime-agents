using FakeItEasy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ReconNessAgent.Application.DataAccess;
using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Application.Providers;
using ReconNessAgent.Application.Services;
using ReconNessAgent.Domain.Core.Entities;
using ReconNessAgent.Infrastructure.Data.EF;
using ReconNessAgent.Infrastructure.Worker;

namespace ReconNessAgent.Application.Tests
{
    public class AgentServiceTest
    {
        private readonly IAgentService agentService;
        private readonly IAgentDataAccessService agentDataAccessService;
        private readonly IUnitOfWork unitOfWork;

        private readonly IScriptEngineProvider scriptEngineProviderFake;
        private readonly ITerminalProvider terminalProviderFake;

        public AgentServiceTest()
        {
            var setting = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "appsettings.Development.json");
            var configuration = new ConfigurationBuilder().AddJsonFile(setting).Build();

            agentDataAccessService = new AgentDataAccessService();

            var scriptEngineProvideFactoryFake = A.Fake<IScriptEngineProvideFactory>();
            var terminalProviderFactoryFake = A.Fake<ITerminalProviderFactory>();

            scriptEngineProviderFake = A.Fake<IScriptEngineProvider>();
            terminalProviderFake = A.Fake<ITerminalProvider>();
            
            A.CallTo(() => scriptEngineProvideFactoryFake.CreateScriptEngineProvider(A<string>._, Models.Enums.ScriptEngineLanguage.C_CHARP)).Returns(scriptEngineProviderFake);
            A.CallTo(() => terminalProviderFactoryFake.CreateTerminalProvider(Models.Enums.TerminalType.BASH)).Returns(terminalProviderFake);

            agentService = new AgentService(agentDataAccessService, scriptEngineProvideFactoryFake, terminalProviderFactoryFake);

            var options = new DbContextOptionsBuilder<ReconnessDbContext>();
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            unitOfWork = new UnitOfWork(new ReconnessDbContext(options.Options));
        }

        [Fact]
        public async Task RunAsync()
        {
            var agent = await CreateTestAgentAsync();
            var target = await CreateTestTargetAsync();
            var rootDomain = await CreateTestRootDomainAsync(target);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_all";           
            var agentRunner = await CreateAgentRunner(agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""{{rootdomain}}"",
              ""Command"": ""{{command}}"",
              ""Count"": 5,
              ""AvailableServerNumber"": 1
            }";

            var output = new TerminalOutputParse
            {
                Subdomain = "www.myrootdomain.com"                
            };

            A.CallTo(() => scriptEngineProviderFake.ParseAsync(A<string>._, A<int>._, A<CancellationToken>._)).Returns(output);
            int count = 0;
            A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
            {
                return count++ % 2 == 0;
            });

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("some output");

            var payload = queueData
                .Replace("{{channel}}", channel)
                .Replace("{{command}}", agent.Command)
                .Replace("{{rootdomain}}", rootDomain.Name);

            await agentService.RunAsync(unitOfWork, payload);

            var subdomain = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www.myrootdomain.com");
            subdomain.Should().NotBeNull();

            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target);
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner);

            await unitOfWork.CommitAsync();
        }

        private async Task<AgentRunner> CreateAgentRunner(Agent agent, string channel)
        {
            var agentRunner = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            if (agentRunner != null)
            {
                return agentRunner;
            }

            agentRunner = new AgentRunner
            {
                Channel = channel,
                AllowSkip = true,
                Stage = Domain.Core.Enums.AgentRunnerStage.ENQUEUE,
                Total = 10,
                ActivateNotification = true,
                AgentId = agent.Id
            };

            unitOfWork.Repository<AgentRunner>().Add(agentRunner);
            await unitOfWork.CommitAsync();

            return agentRunner;
        }

        private async Task<Agent> CreateTestAgentAsync()
        {
            var agent = await unitOfWork.Repository<Agent>().GetByCriteriaAsync(a => a.Name == "TestAgentName");
            if (agent != null)
            {
                return agent;
            }

            agent = new Agent
            {
                Name = "TestAgentName",
                Command = "sublister {{rootdomain}}",
                AgentType = "RootDomain",
                Script = "... some script here ..."
            };

            unitOfWork.Repository<Agent>().Add(agent);
            await unitOfWork.CommitAsync();

            return agent;
        }

        private async Task<Target> CreateTestTargetAsync()
        {
            var target = await unitOfWork.Repository<Target>().GetByCriteriaAsync(a => a.Name == "TestTargetName");
            if (target != null)
            {
                return target;
            }

            target = new Target
            {
                Name = "TestTargetName"                
            };

            unitOfWork.Repository<Target>().Add(target);
            await unitOfWork.CommitAsync();

            return target;
        }

        private async Task<RootDomain> CreateTestRootDomainAsync(Target target)
        {
            var rootDomain = await unitOfWork.Repository<RootDomain>().GetByCriteriaAsync(a => a.Name == "myrootdomain.com");
            if (rootDomain != null)
            {
                return rootDomain;
            }

            rootDomain = new RootDomain
            {
                Name = "myrootdomain.com",
                TargetId = target.Id
            };

            unitOfWork.Repository<RootDomain>().Add(rootDomain);
            await unitOfWork.CommitAsync();

            return rootDomain;
        }
    }
}