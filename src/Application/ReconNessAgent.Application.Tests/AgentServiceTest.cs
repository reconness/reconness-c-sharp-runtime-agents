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
        /// Test save a new rootdomain
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
        public async Task Run_Save_NewRootdomain_OK_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                           return new TerminalOutputParse { RootDomain = lineInput }; ";

            var agent = await CreateTestAgentAsync(script, "ASN {{target}}");
            var target = await CreateTestTargetAsync();

            var channel = $"#20220319.1_{agent.Name}_{target.Name}";
            var agentRunner = await CreateAgentRunner(agent, channel);

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
        public async Task Run_Save_NewSubdomain_OK_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                           return new TerminalOutputParse { Subdomain = lineInput }; ";

            var agent = await CreateTestAgentAsync(script, "sublister {{rootdomain}}");
            var target = await CreateTestTargetAsync();
            var rootDomain = await CreateTestRootDomainAsync(target);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_all";           
            var agentRunner = await CreateAgentRunner(agent, channel);

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
        /// Test save a data into a subdomain
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_myrootdomain.com_all",
        ///     "Payload": "www.myrootdomain.com",
        ///     "Command": "ping www.myrootdomain.com"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data."
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Save_Subdomain_Info_OK_Async()
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

            var agent = await CreateTestAgentAsync(script, "ping {{subdomain}}");
            var target = await CreateTestTargetAsync();
            var rootDomain = await CreateTestRootDomainAsync(target);
            var subdomain = await CreateTestSubDomainAsync(rootDomain);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_{rootDomain.Name}_all";
            var agentRunner = await CreateAgentRunner(agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""{{subdomain}}"",
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
        /// Test save empty data into a subdomain
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_myrootdomain.com_all",
        ///     "Payload": "www.myrootdomain.com",
        ///     "Command": "ping www.myrootdomain.com"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data."
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Save_Subdomain_Info_Empty_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                            return new TerminalOutputParse();
                            ";

            var agent = await CreateTestAgentAsync(script, "ping {{subdomain}}");
            var target = await CreateTestTargetAsync();
            var rootDomain = await CreateTestRootDomainAsync(target);
            var subdomain = await CreateTestSubDomainAsync(rootDomain);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_{rootDomain.Name}_all";
            var agentRunner = await CreateAgentRunner(agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""{{subdomain}}"",
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
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SUCCESS);
            agentRunnerCommandSaved!.Command.Should().Be("ping www.myrootdomain.com");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().NotBeNull();
            agentRunnerCommandOutputSaved!.Output.Should().Be("PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data.");
        }

        /// <summary>
        /// Test save data into a subdomain, but with bad script
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_myrootdomain.com_all",
        ///     "Payload": "www.myrootdomain.com",
        ///     "Command": "ping www.myrootdomain.com"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: "PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data."
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Save_Subdomain_Info_Bad_Script_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;

                            ...bad script....

                            return new TerminalOutputParse();
                            ";

            var agent = await CreateTestAgentAsync(script, "ping {{subdomain}}");
            var target = await CreateTestTargetAsync();
            var rootDomain = await CreateTestRootDomainAsync(target);
            var subdomain = await CreateTestSubDomainAsync(rootDomain);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_{rootDomain.Name}_all";
            var agentRunner = await CreateAgentRunner(agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""{{subdomain}}"",
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
            agentRunnerCommandSaved!.Error.Should().Be("(3,29): error CS8635: Unexpected character sequence '...'");
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.FAILED);
            agentRunnerCommandSaved!.Command.Should().Be("ping www.myrootdomain.com");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().NotBeNull();
            agentRunnerCommandOutputSaved!.Output.Should().Be("PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data.");
        }

        /// <summary>
        /// Test save empty data into a subdomain but if the terminal return empty, we are not running nothing
        /// 
        /// {
        ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_myrootdomain.com_all",
        ///     "Payload": "www.myrootdomain.com",
        ///     "Command": "ping www.myrootdomain.com"
        /// }
        /// 
        /// Note: We emulate the terminal process returning all the time: ""
        /// 
        /// </summary>
        [Fact]
        public async Task Run_Save_Subdomain_Info_Empty_TerminalReadLine_Async()
        {
            // arrange
            var script = @"using ReconNessAgent.Domain.Core.ValueObjects;
                    
                            return new TerminalOutputParse();
                            ";

            var agent = await CreateTestAgentAsync(script, "ping {{subdomain}}");
            var target = await CreateTestTargetAsync();
            var rootDomain = await CreateTestRootDomainAsync(target);
            var subdomain = await CreateTestSubDomainAsync(rootDomain);

            var channel = $"#20220319.1_{agent.Name}_{target.Name}_{rootDomain.Name}_all";
            var agentRunner = await CreateAgentRunner(agent, channel);

            const string queueData = @"
            {
              ""Channel"": ""{{channel}}"",
              ""Payload"": ""{{subdomain}}"",
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

            A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("");

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
            agentRunnerCommandSaved!.Status.Should().Be(Domain.Core.Enums.AgentRunnerCommandStatus.SUCCESS);
            agentRunnerCommandSaved!.Command.Should().Be("ping www.myrootdomain.com");
            agentRunnerCommandSaved!.Server.Should().Be(1);
            agentRunnerCommandSaved!.Number.Should().Be(5);

            agentRunnerCommandOutputSaved.Should().BeNull();
        }

        // TODO: Test the skip part

        /// <summary>
        /// Create a mock test agent
        /// </summary>
        /// <returns>The agent</returns>
        private async Task<Agent> CreateTestAgentAsync(string script, string command)
        {
            var agent = await unitOfWork.Repository<Agent>().GetByCriteriaAsync(a => a.Name == "TestAgentName");
            if (agent != null)
            {
                unitOfWork.Repository<Agent>().Delete(agent);
                await unitOfWork.CommitAsync();
            }

            agent = new Agent
            {
                Name = "TestAgentName",
                Command = command,
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
        /// Create a mock test subdomain
        /// </summary>
        /// <param name="rootDomain">The rootdomain that has a relation with the new subdomain</param>
        /// <returns>The subdomain</returns>
        private async Task<Subdomain> CreateTestSubDomainAsync(RootDomain rootDomain)
        {
            var subdomain = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www.myrootdomain.com");
            if (subdomain != null)
            {
                unitOfWork.Repository<Subdomain>().Delete(subdomain);
                await unitOfWork.CommitAsync();
            }

            subdomain = new Subdomain
            {
                Name = "www.myrootdomain.com",
                RootDomainId = rootDomain.Id
            };

            unitOfWork.Repository<Subdomain>().Add(subdomain);
            await unitOfWork.CommitAsync();

            return subdomain;
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