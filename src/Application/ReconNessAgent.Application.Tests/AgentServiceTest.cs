using FakeItEasy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ReconNessAgent.Application.DataAccess;
using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Providers;
using ReconNessAgent.Application.Services;
using ReconNessAgent.Application.Services.Factories;
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

        private readonly ITerminalProvider terminalProviderFake;

        /// <summary>
        /// Initialice <see cref="IAgentService"/>
        /// </summary>
        public AgentServiceTest()
        {
            var setting = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "appsettings.Development.json");
            var configuration = new ConfigurationBuilder().AddJsonFile(setting).Build();

            agentDataAccessService = new AgentDataAccessService();

            var terminalProviderFactoryFake = A.Fake<ITerminalProviderFactory>();

            terminalProviderFake = A.Fake<ITerminalProvider>();
            
            A.CallTo(() => terminalProviderFactoryFake.CreateTerminalProvider(Models.Enums.TerminalType.BASH)).Returns(terminalProviderFake);

            var scriptEngineProvideFactoryFake = new ScriptEngineProvideFactory();
            agentService = new AgentService(agentDataAccessService, scriptEngineProvideFactoryFake, terminalProviderFactoryFake);

            var options = new DbContextOptionsBuilder<ReconnessDbContext>();
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            unitOfWork = new UnitOfWork(new ReconnessDbContext(options.Options));
        }

        /// <summary>
        /// Test save a new subdomain
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_all",
        ///     "Payload": "myrootdomain.com",
        ///     "Command": "sublister myrootdomain.com"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "www.myrootdomain.com"
        /// 
        /// </summary>
        [Fact]
        public async Task RunAsync()
        {
            // arrange
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

            bool finished = true;
            A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
            {
                finished = !finished;
                return finished;
            });

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("www.myrootdomain.com");

            var payload = queueData
                .Replace("{{channel}}", channel)
                .Replace("{{command}}", agent.Command)
                .Replace("{{rootdomain}}", rootDomain.Name);

            // act
            await agentService.RunAsync(unitOfWork, payload);

            var subdomain = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www.myrootdomain.com");
            var agentRunnerSaved = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            var agentRunnerCommandSaved = await unitOfWork.Repository<AgentRunnerCommand>().GetByCriteriaAsync(a => a.AgentRunnerId == agentRunner.Id);
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            subdomain.Should().NotBeNull();

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SUCCESS);
            agentRunnerCommandSaved!.Command.Should().Be("sublister myrootdomain.com");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().NotBeNull();
            agentRunnerCommandOutputSaved!.Output.Should().Be("www.myrootdomain.com");
        }        

        /// <summary>
        /// Create a mock test agent
        /// </summary>
        /// <returns>The agent</returns>
        private async Task<Agent> CreateTestAgentAsync()
        {
            var agent = await unitOfWork.Repository<Agent>().GetByCriteriaAsync(a => a.Name == "TestAgentName");
            if (agent != null)
            {
                unitOfWork.Repository<Agent>().Delete(agent);
                await unitOfWork.CommitAsync();
            }

            var script = @"using ReconNessAgent.Domain.Core;
                    
                           return new TerminalOutputParse { Subdomain = lineInput }; ";

            agent = new Agent
            {
                Name = "TestAgentName",
                Command = "sublister {{rootdomain}}",
                AgentType = "RootDomain",
                Script = script
            };

            unitOfWork.Repository<Agent>().Add(agent);
            await unitOfWork.CommitAsync();

            return agent;
        }

        /// <summary>
        /// Create a mock test target
        /// </summary>
        /// <returns>The target</returns>
        private async Task<Target> CreateTestTargetAsync()
        {
            var target = await unitOfWork.Repository<Target>().GetByCriteriaAsync(a => a.Name == "TestTargetName");
            if (target != null)
            {
                unitOfWork.Repository<Target>().Delete(target);
                await unitOfWork.CommitAsync();
            }

            target = new Target
            {
                Name = "TestTargetName"                
            };

            unitOfWork.Repository<Target>().Add(target);
            await unitOfWork.CommitAsync();

            return target;
        }

        /// <summary>
        /// Create a mock test rootdomain
        /// </summary>
        /// <param name="target">The target that has a relation with the new rootdomain</param>
        /// <returns>The rootdomain</returns>
        private async Task<RootDomain> CreateTestRootDomainAsync(Target target)
        {
            var rootDomain = await unitOfWork.Repository<RootDomain>().GetByCriteriaAsync(a => a.Name == "myrootdomain.com");
            if (rootDomain != null)
            {
                unitOfWork.Repository<RootDomain>().Delete(rootDomain);
                await unitOfWork.CommitAsync();
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

        /// <summary>
        /// Create an mock test AgentRunner
        /// </summary>
        /// <param name="agent">The agent</param>
        /// <param name="channel">The channel for that AgentRunner</param>
        /// <returns>The AgentRunner</returns>
        private async Task<AgentRunner> CreateAgentRunner(Agent agent, string channel)
        {
            var agentRunner = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            if (agentRunner != null)
            {
                unitOfWork.Repository<AgentRunner>().Delete(agentRunner);
                await unitOfWork.CommitAsync();
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
    }
}