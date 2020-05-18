using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Threading;
using Xunit;
using FluentAssertions;
using static AutoStep.CommandLine.CreateProjectCommand;
using FluentAssertions.Primitives;

namespace AutoStep.CommandLine.Tests
{
    public class CommandInvocationTests
    {
        private static readonly CreateProjectDelegate successProjectCreation = MakeCreateProjectDelegateFromStatusCode(0);

        private static CreateProjectDelegate MakeCreateProjectDelegateFromStatusCode(int returnStatusCode) =>
            new CreateProjectDelegate((runArgs, iLogger) => returnStatusCode);

        private static readonly AutoStepFiles defaultMockAutoStepFiles = GetMockAutoStepFiles(successProjectCreation, successProjectCreation);

        private static AutoStepFiles GetMockAutoStepFiles(CreateProjectDelegate createBlankproject, CreateProjectDelegate createWebProject)
            => new AutoStepFiles(createBlankproject, createWebProject);

        private static ParseResult TryCreateProject(params string[] args) => TryCreateProject(defaultMockAutoStepFiles, args);

        private static ParseResult TryCreateProject(AutoStepFiles autoStepFiles, params string[] args) =>
            MakeParserWithBakedInCmdLnArguments(args, new NewProjectCommand(autoStepFiles)).Invoke();

        private static Func<ParseResult> MakeParserWithBakedInCmdLnArguments(string[] args, params Command[] cmds) =>
            () => MakeParserWith(cmds).Parse(args);

        private static Parser MakeParserWith(Command[] cmds)
        {
            var builder = new CommandLineBuilder();
            foreach (var cmd in cmds)
            {
                builder.AddCommand(cmd);
            }
            return builder.Build();
        }

        [Fact]
        public void cannot_issue_new_cmd_without_subcommands()
        {
            var cmd = TryCreateProject(args: "new").CommandResult.Command.As<IAutostepCommand>();

            cmd.Should().NotBeNull();
            cmd.CommandIsValid().Should().BeFalse(because: "new command cannot be invoked without subcommands");
        }

        [Fact]
        public void can_only_create_projects_if_issued_in_the_right_order()
        {
            IAutostepCommand TryCreateProjectAndGetResult(params string[] args) => TryCreateProject(args).CommandResult.Command.As<IAutostepCommand>();

            var cmd = TryCreateProjectAndGetResult("new", "project");
            cmd.Should().NotBeNull();
            cmd.CommandIsValid().Should().BeTrue();

            cmd = TryCreateProjectAndGetResult("project", "new");
            cmd.Should().NotBeNull();
            cmd.CommandIsValid().Should().BeFalse();

            cmd = TryCreateProjectAndGetResult("new", "project", "web");
            cmd.Should().NotBeNull();
            cmd.CommandIsValid().Should().BeTrue();

            cmd = TryCreateProjectAndGetResult("project", "new", "web");
            cmd.Should().NotBeNull();
            cmd.CommandIsValid().Should().BeFalse();
        }
    }
}
