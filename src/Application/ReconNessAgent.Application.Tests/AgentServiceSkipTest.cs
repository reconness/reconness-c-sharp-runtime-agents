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
    public class AgentServiceSkipTest
    {
        private readonly IAgentService agentService;
        private readonly IAgentDataAccessService agentDataAccessService;
        private readonly IUnitOfWork unitOfWork;

        private readonly ITerminalProvider terminalProviderFake;

        /// <summary>
        /// Initialice <see cref="IAgentService"/>
        /// </summary>
        public AgentServiceSkipTest()
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
        /// Test save but we are skipping it because it run before in that target
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_all",
        ///     "Payload": "TestTargetName",
        ///     "Command": "NAS TestTargetName"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "myrootdomain.com""
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Target_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;  

                            return new TerminalOutputParse { RootDomain = lineInput};
                          ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "NAS {{target}}", "Target", new AgentTrigger { SkipIfRunBefore = true });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork, agent.Name!);

            var channel = $"#20220319.1_{agent.Name}_all";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""{{target}}"",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
            }";

            bool finished = true;
            A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
            {
                finished = !finished;
                return finished;
            });

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("myrootdomain.com");

            var payload = queueData
                .Replace("{{channel}}", channel)
                .Replace("{{command}}", agent.Command)
                .Replace("{{target}}", target.Name);

            // act
            await agentService.RunAsync(unitOfWork, payload);

            var rootDomainSaved = await unitOfWork.Repository<RootDomain>().GetByCriteriaAsync(a => a.Name == "myrootdomain.com");
            var agentRunnerSaved = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            var agentRunnerCommandSaved = await unitOfWork.Repository<AgentRunnerCommand>().GetByCriteriaAsync(a => a.AgentRunnerId == agentRunner.Id);
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain and subdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            rootDomainSaved.Should().BeNull();

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SKIPPED);
            agentRunnerCommandSaved!.Command.Should().Be("NAS TestTargetName");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().BeNull();
        }

        /// <summary>
        /// Test save but we are skipping it because the target has bounty and we want to run only the target without bounty
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_all",
        ///     "Payload": "TestTargetName",
        ///     "Command": "NAS TestTargetName"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "myrootdomain.com""
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Target_HasBounty_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;  

                            return new TerminalOutputParse { RootDomain = lineInput};
                          ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "NAS {{target}}", "Target", new AgentTrigger { TargetHasBounty = true });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork, agent.Name!);

            var channel = $"#20220319.1_{agent.Name}_all";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""{{target}}"",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
            }";

            bool finished = true;
            A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
            {
                finished = !finished;
                return finished;
            });

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("myrootdomain.com");

            var payload = queueData
                .Replace("{{channel}}", channel)
                .Replace("{{command}}", agent.Command)
                .Replace("{{target}}", target.Name);

            // act
            await agentService.RunAsync(unitOfWork, payload);

            var rootDomainSaved = await unitOfWork.Repository<RootDomain>().GetByCriteriaAsync(a => a.Name == "myrootdomain.com");
            var agentRunnerSaved = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            var agentRunnerCommandSaved = await unitOfWork.Repository<AgentRunnerCommand>().GetByCriteriaAsync(a => a.AgentRunnerId == agentRunner.Id);
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain and subdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            rootDomainSaved.Should().BeNull();

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SKIPPED);
            agentRunnerCommandSaved!.Command.Should().Be("NAS TestTargetName");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().BeNull();
        }

        /// <summary>
        /// Test save but we are skipping it because we exclude the target name
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_all",
        ///     "Payload": "TestTargetName",
        ///     "Command": "NAS TestTargetName"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "myrootdomain.com""
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Target_ExcludeName_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;  

                            return new TerminalOutputParse { RootDomain = lineInput};
                          ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "NAS {{target}}", "Target", new AgentTrigger { TargetIncExcName = "EXCLUDE", TargetName = "TestTargetName" });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork, agent.Name!);

            var channel = $"#20220319.1_{agent.Name}_all";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""{{target}}"",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
            }";

            bool finished = true;
            A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
            {
                finished = !finished;
                return finished;
            });

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("myrootdomain.com");

            var payload = queueData
                .Replace("{{channel}}", channel)
                .Replace("{{command}}", agent.Command)
                .Replace("{{target}}", target.Name);

            // act
            await agentService.RunAsync(unitOfWork, payload);

            var rootDomainSaved = await unitOfWork.Repository<RootDomain>().GetByCriteriaAsync(a => a.Name == "myrootdomain.com");
            var agentRunnerSaved = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            var agentRunnerCommandSaved = await unitOfWork.Repository<AgentRunnerCommand>().GetByCriteriaAsync(a => a.AgentRunnerId == agentRunner.Id);
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain and subdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            rootDomainSaved.Should().BeNull();

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SKIPPED);
            agentRunnerCommandSaved!.Command.Should().Be("NAS TestTargetName");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().BeNull();
        }

        /// <summary>
        /// Test save but we are skipping it because we exclude the target name using RegExp
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_all",
        ///     "Payload": "TestTargetName",
        ///     "Command": "NAS TestTargetName"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "myrootdomain.com""
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Target_ExcludeName_RegExp_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;  

                            return new TerminalOutputParse { RootDomain = lineInput};
                          ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "NAS {{target}}", "Target", new AgentTrigger { TargetIncExcName = "EXCLUDE", TargetName = ".*TargetName$" });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork, agent.Name!);

            var channel = $"#20220319.1_{agent.Name}_all";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""{{target}}"",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
            }";

            bool finished = true;
            A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
            {
                finished = !finished;
                return finished;
            });

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("myrootdomain.com");

            var payload = queueData
                .Replace("{{channel}}", channel)
                .Replace("{{command}}", agent.Command)
                .Replace("{{target}}", target.Name);

            // act
            await agentService.RunAsync(unitOfWork, payload);

            var rootDomainSaved = await unitOfWork.Repository<RootDomain>().GetByCriteriaAsync(a => a.Name == "myrootdomain.com");
            var agentRunnerSaved = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            var agentRunnerCommandSaved = await unitOfWork.Repository<AgentRunnerCommand>().GetByCriteriaAsync(a => a.AgentRunnerId == agentRunner.Id);
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain and subdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            rootDomainSaved.Should().BeNull();

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SKIPPED);
            agentRunnerCommandSaved!.Command.Should().Be("NAS TestTargetName");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().BeNull();
        }

        /// <summary>
        /// Test save a new rootdomain, becouse we include the target name and has bounty
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestTargetName",
        ///     "Command": "ASN TestTargetName"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "myrootdomain.com"
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Target_IncludeName_And_HasBounty_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                           return new TerminalOutputParse { RootDomain = lineInput }; ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "ASN {{target}}", "Target", new AgentTrigger { TargetHasBounty = true, TargetIncExcName = "INCLUDE", TargetName = "TestTargetName" });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork, agent.Name!, true);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
            }";

            bool finished = true;
            A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
            {
                finished = !finished;
                return finished;
            });

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("myrootdomain.com");

            var payload = queueData
                .Replace("{{channel}}", channel)
                .Replace("{{command}}", agent.Command)
                .Replace("{{target}}", target.Name);

            // act
            await agentService.RunAsync(unitOfWork, payload);

            var rootdomainSaved = await unitOfWork.Repository<RootDomain>().GetByCriteriaAsync(a => a.Name == "myrootdomain.com");
            var agentRunnerSaved = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            var agentRunnerCommandSaved = await unitOfWork.Repository<AgentRunnerCommand>().GetByCriteriaAsync(a => a.AgentRunnerId == agentRunner.Id);
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            rootdomainSaved.Should().NotBeNull();

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SUCCESS);
            agentRunnerCommandSaved!.Command.Should().Be("ASN TestTargetName");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().NotBeNull();
            agentRunnerCommandOutputSaved!.Output.Should().Be("myrootdomain.com");
        }

        /// <summary>
        /// Test save a new rootdomain, becouse we include the target name and has bounty usign RegExp
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestTargetName",
        ///     "Command": "ASN TestTargetName"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "myrootdomain.com"
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Target_IncludeName_And_HasBounty_RegExp_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                           return new TerminalOutputParse { RootDomain = lineInput }; ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "ASN {{target}}", "Target", new AgentTrigger { TargetHasBounty = true, TargetIncExcName = "INCLUDE", TargetName = ".*argetName$" });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork, agent.Name!, true);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
            }";

            bool finished = true;
            A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
            {
                finished = !finished;
                return finished;
            });

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("myrootdomain.com");

            var payload = queueData
                .Replace("{{channel}}", channel)
                .Replace("{{command}}", agent.Command)
                .Replace("{{target}}", target.Name);

            // act
            await agentService.RunAsync(unitOfWork, payload);

            var rootdomainSaved = await unitOfWork.Repository<RootDomain>().GetByCriteriaAsync(a => a.Name == "myrootdomain.com");
            var agentRunnerSaved = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            var agentRunnerCommandSaved = await unitOfWork.Repository<AgentRunnerCommand>().GetByCriteriaAsync(a => a.AgentRunnerId == agentRunner.Id);
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            rootdomainSaved.Should().NotBeNull();

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SUCCESS);
            agentRunnerCommandSaved!.Command.Should().Be("ASN TestTargetName");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().NotBeNull();
            agentRunnerCommandOutputSaved!.Output.Should().Be("myrootdomain.com");
        }

        /// <summary>
        /// Test save but we are skipping it because it run before in that rootdomain
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestAgentName_all",
        ///     "Payload": "myrootdomain.com",
        ///     "Command": "NAS TestAgentName"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "www.myrootdomain.com"
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Rootdomain_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                           return new TerminalOutputParse { Subdomain = lineInput }; ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "sublister {{rootdomain}}", "RootDomain", new AgentTrigger { SkipIfRunBefore = true });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork);
            var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target, agent.Name!);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_all";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""{{rootdomain}}"",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
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
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            subdomain.Should().BeNull();

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SKIPPED);
            agentRunnerCommandSaved!.Command.Should().Be("sublister myrootdomain.com");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().BeNull();
        }

        /// <summary>
        /// Test save but we are skipping it because we are looking for rootdomain without bounty
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestAgentName_all",
        ///     "Payload": "myrootdomain.com",
        ///     "Command": "NAS TestAgentName"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "www.myrootdomain.com"
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Rootdomain_HasBounty_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                           return new TerminalOutputParse { Subdomain = lineInput }; ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "sublister {{rootdomain}}", "RootDomain", new AgentTrigger { RootdomainHasBounty = true });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork);
            var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target, agent.Name!);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_all";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""{{rootdomain}}"",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
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
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            subdomain.Should().BeNull();

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SKIPPED);
            agentRunnerCommandSaved!.Command.Should().Be("sublister myrootdomain.com");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().BeNull();
        }

        /// <summary>
        /// Test save but we are skipping it because we are exclude the name of the rootdomain
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestAgentName_all",
        ///     "Payload": "myrootdomain.com",
        ///     "Command": "NAS TestAgentName"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "www.myrootdomain.com"
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Rootdomain_ExcludeName_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                           return new TerminalOutputParse { Subdomain = lineInput }; ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "sublister {{rootdomain}}", "RootDomain", new AgentTrigger { RootdomainIncExcName = "EXCLUDE", RootdomainName = "myrootdomain.com" });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork);
            var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target, agent.Name!);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_all";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""{{rootdomain}}"",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
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
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            subdomain.Should().BeNull();

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SKIPPED);
            agentRunnerCommandSaved!.Command.Should().Be("sublister myrootdomain.com");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().BeNull();
        }

        /// <summary>
        /// Test save but we are skipping it because we are exclude the name of the rootdomain using RegExp
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestAgentName_all",
        ///     "Payload": "myrootdomain.com",
        ///     "Command": "NAS TestAgentName"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "www.myrootdomain.com"
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Rootdomain_ExcludeName_RegExp_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                           return new TerminalOutputParse { Subdomain = lineInput }; ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "sublister {{rootdomain}}", "RootDomain", new AgentTrigger { RootdomainIncExcName = "EXCLUDE", RootdomainName = ".*otdomain.com$" });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork);
            var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target, agent.Name!);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_all";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""{{rootdomain}}"",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
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
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            subdomain.Should().BeNull();

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SKIPPED);
            agentRunnerCommandSaved!.Command.Should().Be("sublister myrootdomain.com");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().BeNull();
        }

        /// <summary>
        /// Test save a new subdomain because, include the rootdomain name and has bounty
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
        public async Task Run_Skip_Rootdomain_IncludeName_And_HasBounty_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                           return new TerminalOutputParse { Subdomain = lineInput }; ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "sublister {{rootdomain}}", "RootDomain", new AgentTrigger { RootdomainIncExcName = "INCLUDE", RootdomainName = "myrootdomain.com", RootdomainHasBounty = true });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork);
            var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target, agent.Name!, true);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_all";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""{{rootdomain}}"",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
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
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

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
        /// Test save a new subdomain because, include the rootdomain name and has bounty using RegExp
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
        public async Task Run_Skip_Rootdomain_IncludeName_And_HasBounty_RegExp_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                           return new TerminalOutputParse { Subdomain = lineInput }; ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "sublister {{rootdomain}}", "RootDomain", new AgentTrigger { RootdomainIncExcName = "INCLUDE", RootdomainName = ".*rootdomain.com$", RootdomainHasBounty = true });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork);
            var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target, agent.Name!, true);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_all";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""{{rootdomain}}"",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
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
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

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
        /// Test save but we are skipping it because it run before in that subdomain
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_myrootdomain.com_www.myrootdomain.com",
        ///     "Payload": "",
        ///     "Command": "ping www.myrootdomain.com"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data."
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Subdomain_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                            var match = System.Text.RegularExpressions.Regex.Match(lineInput, @""PING\s(.*?)\s\((.*?)\)"");
                            if (match.Success && match.Groups.Count == 3)
                            {
                                return new TerminalOutputParse { Ip = match.Groups[2].Value, Subdomain = match.Groups[1].Value };
                            }

                            return new TerminalOutputParse();
                            ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "ping {{subdomain}}", "Subdomain", new AgentTrigger { SkipIfRunBefore = true });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork);
            var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target);
            var subdomain = await Helpers.CreateTestSubDomainAsync(unitOfWork, rootDomain, agent.Name!);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_{rootDomain.Name}_{subdomain.Name}";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": """",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
            }";

            bool finished = true;
            A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
            {
                finished = !finished;
                return finished;
            });

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data.");

            var payload = queueData
                .Replace("{{channel}}", channel)
                .Replace("{{command}}", agent.Command)
                .Replace("{{subdomain}}", subdomain.Name);

            // act
            await agentService.RunAsync(unitOfWork, payload);

            var subdomainSaved = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www.myrootdomain.com");
            var subdomain1Saved = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www1.myrootdomain.com");
            var agentRunnerSaved = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            var agentRunnerCommandSaved = await unitOfWork.Repository<AgentRunnerCommand>().GetByCriteriaAsync(a => a.AgentRunnerId == agentRunner.Id);
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain and subdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            subdomainSaved.Should().NotBeNull();
            subdomainSaved!.IpAddress.Should().BeNullOrEmpty();

            subdomain1Saved.Should().BeNull();

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SKIPPED);
            agentRunnerCommandSaved!.Command.Should().Be("ping www.myrootdomain.com");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().BeNull();
        }

        /// <summary>
        /// Test save but we are skipping it because we are looking for subdomain with bounty only
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_myrootdomain.com_www.myrootdomain.com",
        ///     "Payload": "",
        ///     "Command": "ping www.myrootdomain.com"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data."
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Subdomain_HasBounty_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                            var match = System.Text.RegularExpressions.Regex.Match(lineInput, @""PING\s(.*?)\s\((.*?)\)"");
                            if (match.Success && match.Groups.Count == 3)
                            {
                                return new TerminalOutputParse { Ip = match.Groups[2].Value, Subdomain = match.Groups[1].Value };
                            }

                            return new TerminalOutputParse();
                            ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "ping {{subdomain}}", "Subdomain", new AgentTrigger { SubdomainHasBounty = true });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork);
            var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target);
            var subdomain = await Helpers.CreateTestSubDomainAsync(unitOfWork, rootDomain, agent.Name!);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_{rootDomain.Name}_{subdomain.Name}";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": """",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
            }";

            bool finished = true;
            A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
            {
                finished = !finished;
                return finished;
            });

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data.");

            var payload = queueData
                .Replace("{{channel}}", channel)
                .Replace("{{command}}", agent.Command)
                .Replace("{{subdomain}}", subdomain.Name);

            // act
            await agentService.RunAsync(unitOfWork, payload);

            var subdomainSaved = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www.myrootdomain.com");
            var subdomain1Saved = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www1.myrootdomain.com");
            var agentRunnerSaved = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            var agentRunnerCommandSaved = await unitOfWork.Repository<AgentRunnerCommand>().GetByCriteriaAsync(a => a.AgentRunnerId == agentRunner.Id);
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain and subdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            subdomainSaved.Should().NotBeNull();
            subdomainSaved!.IpAddress.Should().BeNullOrEmpty();

            subdomain1Saved.Should().BeNull();

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SKIPPED);
            agentRunnerCommandSaved!.Command.Should().Be("ping www.myrootdomain.com");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().BeNull();
        }

        /// <summary>
        /// Test save but we are skipping it because we are looking for subdomain with bounty only
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_myrootdomain.com_www.myrootdomain.com",
        ///     "Payload": "",
        ///     "Command": "ping www.myrootdomain.com"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data."
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Subdomain_ExcludeName_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                            var match = System.Text.RegularExpressions.Regex.Match(lineInput, @""PING\s(.*?)\s\((.*?)\)"");
                            if (match.Success && match.Groups.Count == 3)
                            {
                                return new TerminalOutputParse { Ip = match.Groups[2].Value, Subdomain = match.Groups[1].Value };
                            }

                            return new TerminalOutputParse();
                            ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "ping {{subdomain}}", "Subdomain", new AgentTrigger { SubdomainIncExcName = "EXCLUDE", SubdomainName = "www.myrootdomain.com" });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork);
            var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target);
            var subdomain = await Helpers.CreateTestSubDomainAsync(unitOfWork, rootDomain, agent.Name!);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_{rootDomain.Name}_{subdomain.Name}";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": """",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
            }";

            bool finished = true;
            A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
            {
                finished = !finished;
                return finished;
            });

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data.");

            var payload = queueData
                .Replace("{{channel}}", channel)
                .Replace("{{command}}", agent.Command)
                .Replace("{{subdomain}}", subdomain.Name);

            // act
            await agentService.RunAsync(unitOfWork, payload);

            var subdomainSaved = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www.myrootdomain.com");
            var subdomain1Saved = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www1.myrootdomain.com");
            var agentRunnerSaved = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            var agentRunnerCommandSaved = await unitOfWork.Repository<AgentRunnerCommand>().GetByCriteriaAsync(a => a.AgentRunnerId == agentRunner.Id);
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain and subdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            subdomainSaved.Should().NotBeNull();
            subdomainSaved!.IpAddress.Should().BeNullOrEmpty();

            subdomain1Saved.Should().BeNull();

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SKIPPED);
            agentRunnerCommandSaved!.Command.Should().Be("ping www.myrootdomain.com");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().BeNull();
        }

        /// <summary>
        /// Test save but we are skipping it because we are looking for subdomain with bounty only using RegExp
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_myrootdomain.com_www.myrootdomain.com",
        ///     "Payload": "",
        ///     "Command": "ping www.myrootdomain.com"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data."
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Subdomain_ExcludeName_RegExp_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                            var match = System.Text.RegularExpressions.Regex.Match(lineInput, @""PING\s(.*?)\s\((.*?)\)"");
                            if (match.Success && match.Groups.Count == 3)
                            {
                                return new TerminalOutputParse { Ip = match.Groups[2].Value, Subdomain = match.Groups[1].Value };
                            }

                            return new TerminalOutputParse();
                            ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "ping {{subdomain}}", "Subdomain", new AgentTrigger { SubdomainIncExcName = "EXCLUDE", SubdomainName = ".*myrootdomain.com$" });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork);
            var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target);
            var subdomain = await Helpers.CreateTestSubDomainAsync(unitOfWork, rootDomain, agent.Name!);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_{rootDomain.Name}_{subdomain.Name}";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": """",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
            }";

            bool finished = true;
            A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
            {
                finished = !finished;
                return finished;
            });

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data.");

            var payload = queueData
                .Replace("{{channel}}", channel)
                .Replace("{{command}}", agent.Command)
                .Replace("{{subdomain}}", subdomain.Name);

            // act
            await agentService.RunAsync(unitOfWork, payload);

            var subdomainSaved = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www.myrootdomain.com");
            var subdomain1Saved = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www1.myrootdomain.com");
            var agentRunnerSaved = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            var agentRunnerCommandSaved = await unitOfWork.Repository<AgentRunnerCommand>().GetByCriteriaAsync(a => a.AgentRunnerId == agentRunner.Id);
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain and subdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            subdomainSaved.Should().NotBeNull();
            subdomainSaved!.IpAddress.Should().BeNullOrEmpty();

            subdomain1Saved.Should().BeNull();

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SKIPPED);
            agentRunnerCommandSaved!.Command.Should().Be("ping www.myrootdomain.com");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().BeNull();
        }

        /// <summary>
        /// Test save a data into a subdomain because, we are include the subdomain name and that has bounty
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_myrootdomain.com_www.myrootdomain.com",
        ///     "Payload": "",
        ///     "Command": "ping www.myrootdomain.com"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data."
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Subdomain_IncludeName_And_HasBounty_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                            var match = System.Text.RegularExpressions.Regex.Match(lineInput, @""PING\s(.*?)\s\((.*?)\)"");
                            if (match.Success && match.Groups.Count == 3)
                            {
                                return new TerminalOutputParse { Ip = match.Groups[2].Value, Subdomain = match.Groups[1].Value };
                            }

                            return new TerminalOutputParse();
                            ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "ping {{subdomain}}", "Subdomain", new AgentTrigger { SubdomainIncExcName = "INCLUDE", SubdomainName = "www.myrootdomain.com", SubdomainHasBounty = true });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork);
            var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target);
            var subdomain = await Helpers.CreateTestSubDomainAsync(unitOfWork, rootDomain, agent.Name!, true);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_{rootDomain.Name}_{subdomain.Name}";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": """",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
            }";

            bool finished = true;
            A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
            {
                finished = !finished;
                return finished;
            });

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data.");

            var payload = queueData
                .Replace("{{channel}}", channel)
                .Replace("{{command}}", agent.Command)
                .Replace("{{subdomain}}", subdomain.Name);

            // act
            await agentService.RunAsync(unitOfWork, payload);

            var subdomainSaved = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www.myrootdomain.com");
            var subdomain1Saved = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www1.myrootdomain.com");
            var agentRunnerSaved = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            var agentRunnerCommandSaved = await unitOfWork.Repository<AgentRunnerCommand>().GetByCriteriaAsync(a => a.AgentRunnerId == agentRunner.Id);
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain and subdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            subdomainSaved.Should().NotBeNull();
            subdomainSaved!.IpAddress.Should().Be("72.30.35.10");

            subdomain1Saved.Should().NotBeNull();
            subdomain1Saved!.IpAddress.Should().Be("72.30.35.10");

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SUCCESS);
            agentRunnerCommandSaved!.Command.Should().Be("ping www.myrootdomain.com");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().NotBeNull();
            agentRunnerCommandOutputSaved!.Output.Should().Be("PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data.");
        }

        /// <summary>
        /// Test save a data into a subdomain because, we are include the subdomain name and that has bounty using RegExp
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_myrootdomain.com_www.myrootdomain.com",
        ///     "Payload": "",
        ///     "Command": "ping www.myrootdomain.com"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data."
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Subdomain_IncludeName_And_HasBounty_RegExp_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                            var match = System.Text.RegularExpressions.Regex.Match(lineInput, @""PING\s(.*?)\s\((.*?)\)"");
                            if (match.Success && match.Groups.Count == 3)
                            {
                                return new TerminalOutputParse { Ip = match.Groups[2].Value, Subdomain = match.Groups[1].Value };
                            }

                            return new TerminalOutputParse();
                            ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "ping {{subdomain}}", "Subdomain", new AgentTrigger { SubdomainIncExcName = "INCLUDE", SubdomainName = ".*myrootdomain.com$", SubdomainHasBounty = true });
            var target = await Helpers.CreateTestTargetAsync(unitOfWork);
            var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target);
            var subdomain = await Helpers.CreateTestSubDomainAsync(unitOfWork, rootDomain, agent.Name!, true);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_{rootDomain.Name}_{subdomain.Name}";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": """",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
            }";

            bool finished = true;
            A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
            {
                finished = !finished;
                return finished;
            });

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data.");

            var payload = queueData
                .Replace("{{channel}}", channel)
                .Replace("{{command}}", agent.Command)
                .Replace("{{subdomain}}", subdomain.Name);

            // act
            await agentService.RunAsync(unitOfWork, payload);

            var subdomainSaved = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www.myrootdomain.com");
            var subdomain1Saved = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www1.myrootdomain.com");
            var agentRunnerSaved = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            var agentRunnerCommandSaved = await unitOfWork.Repository<AgentRunnerCommand>().GetByCriteriaAsync(a => a.AgentRunnerId == agentRunner.Id);
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain and subdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            subdomainSaved.Should().NotBeNull();
            subdomainSaved!.IpAddress.Should().Be("72.30.35.10");

            subdomain1Saved.Should().NotBeNull();
            subdomain1Saved!.IpAddress.Should().Be("72.30.35.10");

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SUCCESS);
            agentRunnerCommandSaved!.Command.Should().Be("ping www.myrootdomain.com");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().NotBeNull();
            agentRunnerCommandOutputSaved!.Output.Should().Be("PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data.");
        }

        /// <summary>
        /// Test save a data into a subdomain because, we are include the subdomain name and that has bounty
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_myrootdomain.com_all",
        ///     "Payload": "www.myrootdomain.com",
        ///     "Command": "ping www.myrootdomain.com"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "PING www.myrootdomain1.com (72.30.35.10) 56 bytes of data."
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Skip_Subdomain_Include_Command_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                            var match = System.Text.RegularExpressions.Regex.Match(lineInput, @""PING\s(.*?)\s\((.*?)\)"");
                            if (match.Success && match.Groups.Count == 3)
                            {
                                return new TerminalOutputParse { Ip = match.Groups[2].Value, Subdomain = match.Groups[1].Value };
                            }

                            return new TerminalOutputParse();
                            ";

            var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "ping {{subdomain}}", "Subdomain", new AgentTrigger 
            {
                SubdomainHasHttpOrHttpsOpen = true,
                SubdomainIsMainPortal = true,
                SubdomainIsAlive = true                
            });

            var target = await Helpers.CreateTestTargetAsync(unitOfWork);
            var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target);
            var subdomains = await Helpers.CreateTestSubDomainsAsync(unitOfWork, rootDomain, agent.Name!);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_{rootDomain.Name}_all";
            var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""www.myrootdomain.com"",
              ""Command"": ""{{command}}"",
              ""Number"": 5,
              ""ServerNumber"": 1
            }";

            bool finished = true;
            A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
            {
                finished = !finished;
                return finished;
            });

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("PING www.myrootdomain1.com (72.30.35.10) 56 bytes of data.");

            var payload = queueData
                .Replace("{{channel}}", channel)
                .Replace("{{command}}", agent.Command);

            // act
            await agentService.RunAsync(unitOfWork, payload);

            var subdomainSaved = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www.myrootdomain.com");
            var subdomain1Saved = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www1.myrootdomain.com");
            var agentRunnerSaved = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
            var agentRunnerCommandSaved = await unitOfWork.Repository<AgentRunnerCommand>().GetByCriteriaAsync(a => a.AgentRunnerId == agentRunner.Id);
            var agentRunnerCommandOutputSaved = await unitOfWork.Repository<AgentRunnerCommandOutput>().GetByCriteriaAsync(a => a.AgentRunnerCommandId == agentRunnerCommandSaved!.Id);

            // cleanup
            unitOfWork.Repository<Agent>().Delete(agent);
            unitOfWork.Repository<Target>().Delete(target); // this delete the rootdomain and subdomain on cascade
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner); // this delete the agentRunnerCommand on cascade
            await unitOfWork.CommitAsync();

            // assert
            subdomainSaved.Should().NotBeNull();
            subdomainSaved!.IpAddress.Should().Be("72.30.35.10");

            subdomain1Saved.Should().NotBeNull();
            subdomain1Saved!.IpAddress.Should().Be("72.30.35.10");

            agentRunnerSaved.Should().NotBeNull();
            agentRunnerSaved!.Stage.Should().Be(Domain.Core.Enums.AgentRunnerStage.RUNNING);

            agentRunnerCommandSaved.Should().NotBeNull();
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SUCCESS);
            agentRunnerCommandSaved!.Command.Should().Be("ping www.myrootdomain.com");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().NotBeNull();
            agentRunnerCommandOutputSaved!.Output.Should().Be("PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data.");
        }        
    }
}