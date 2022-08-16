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

namespace ReconNessAgent.Application.Tests;

[Collection(nameof(SystemTestCollectionDefinition))]
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

        var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "ASN {{target}}", "Target");
        var target = await Helpers.CreateTestTargetAsync(unitOfWork);

        var channel = $"#20220319.1_{agent.Name}_{target.Name}";
        var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);
        
        bool finished = true;
        A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
        {
            finished = !finished;
            return finished;
        });

        A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("myrootdomain.com");

        const string queueData = @"
        {
            ""Channel"": ""{{channel}}"",
            ""Command"": ""{{command}}"",
            ""Number"": 5,
            ""ServerNumber"": 1
        }";

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

        var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "sublister {{rootdomain}}", "RootDomain");
        var target = await Helpers.CreateTestTargetAsync(unitOfWork);
        var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target);

        var channel = $"#20220319.1_{agent.Name}_{target.Name}_all";           
        var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

        bool finished = true;
        A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
        {
            finished = !finished;
            return finished;
        });

        A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("www.myrootdomain.com");

        const string queueData = @"
        {
            ""Channel"": ""{{channel}}"",
            ""Payload"": ""{{rootdomain}}"",
            ""Command"": ""{{command}}"",
            ""Number"": 5,
            ""ServerNumber"": 1
        }";

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
    ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_myrootdomain.com_www.myrootdomain.com",
    ///     "Payload": "",
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

        var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "ping {{subdomain}}", "Subdomain");
        var target = await Helpers.CreateTestTargetAsync(unitOfWork);
        var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target);
        var subdomain = await Helpers.CreateTestSubDomainAsync(unitOfWork, rootDomain);

        var channel = $"#20220319.1_{agent.Name}_{target.Name}_{rootDomain.Name}_{subdomain.Name}";
        var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

        bool finished = true;
        A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
        {
            finished = !finished;
            return finished;
        });

        A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data.");

        const string queueData = @"
        {
            ""Channel"": ""{{channel}}"",
            ""Payload"": """",
            ""Command"": ""{{command}}"",
            ""Number"": 5,
            ""ServerNumber"": 1
        }";

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
    ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_myrootdomain.com_www.myrootdomain.com"",
    ///     "Payload": "",
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

        var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "ping {{subdomain}}", "Subdomain");
        var target = await Helpers.CreateTestTargetAsync(unitOfWork);
        var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target);
        var subdomain = await Helpers.CreateTestSubDomainAsync(unitOfWork, rootDomain);

        var channel = $"#20220319.1_{agent.Name}_{target.Name}_{rootDomain.Name}_{subdomain.Name}";
        var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

        bool finished = true;
        A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
        {
            finished = !finished;
            return finished;
        });

        A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data.");

        const string queueData = @"
        {
            ""Channel"": ""{{channel}}"",
            ""Payload"": """",
            ""Command"": ""{{command}}"",
            ""Number"": 5,
            ""ServerNumber"": 1
        }";

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
    ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_myrootdomain.com_www.myrootdomain.com",
    ///     "Payload": "",
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

        var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "ping {{subdomain}}", "Subdomain");
        var target = await Helpers.CreateTestTargetAsync(unitOfWork);
        var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target);
        var subdomain = await Helpers.CreateTestSubDomainAsync(unitOfWork, rootDomain);

        var channel = $"#20220319.1_{agent.Name}_{target.Name}_{rootDomain.Name}_{subdomain.Name}";
        var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

        bool finished = true;
        A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
        {
            finished = !finished;
            return finished;
        });

        A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("PING www1.myrootdomain.com (72.30.35.10) 56 bytes of data.");

        const string queueData = @"
        {
            ""Channel"": ""{{channel}}"",
            ""Payload"": """",
            ""Command"": ""{{command}}"",
            ""Number"": 5,
            ""ServerNumber"": 1
        }";

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
    ///     "Channel": "#20220319.1_TestAgentName_TestTargetName_myrootdomain.com_www.myrootdomain.com",
    ///     "Payload": "",
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

        var agent = await Helpers.CreateTestAgentAsync(unitOfWork, script, "ping {{subdomain}}", "Subdomain");
        var target = await Helpers.CreateTestTargetAsync(unitOfWork);
        var rootDomain = await Helpers.CreateTestRootDomainAsync(unitOfWork, target);
        var subdomain = await Helpers.CreateTestSubDomainAsync(unitOfWork, rootDomain);

        var channel = $"#20220319.1_{agent.Name}_{target.Name}_{rootDomain.Name}_{subdomain.Name}";
        var agentRunner = await Helpers.CreateAgentRunner(unitOfWork, agent, channel);

        bool finished = true;
        A.CallTo(() => terminalProviderFake.Finished).ReturnsLazily(() =>
        {
            finished = !finished;
            return finished;
        });

        A.CallTo(() => terminalProviderFake.ReadLineAsync()).Returns("");

        const string queueData = @"
        {
            ""Channel"": ""{{channel}}"",
            ""Payload"": """",
            ""Command"": ""{{command}}"",
            ""Number"": 5,
            ""ServerNumber"": 1
        }";

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
}