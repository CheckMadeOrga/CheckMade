// using CheckMade.ChatBot.Logic.Workflows.Concrete;
// using CheckMade.Common.Interfaces.ChatBot.Logic;
// using CheckMade.Common.Model.ChatBot.Input;
// using CheckMade.Common.Model.ChatBot.UserInteraction;
// using CheckMade.Common.Model.ChatBot.UserInteraction.BotCommands.DefinitionsByBot;
// using CheckMade.Common.Model.Core;
// using CheckMade.Tests.Startup;
// using CheckMade.Tests.Utils;
// using Microsoft.Extensions.DependencyInjection;
// using static CheckMade.ChatBot.Logic.Workflows.Concrete.NewIssueWorkflow.States;
//
// namespace CheckMade.Tests.Unit.ChatBot.Logic.Workflows;
//
// public class NewIssueWorkflowDetermineCurrentStateTests
// {
//     private ServiceProvider? _services;
//
//     [Fact]
//     public void DetermineCurrentState_ReturnsInitialTradeUnknown_OnNewIssueFromLiveEventAdminRole()
//     {
//         _services = new UnitTestStartup().Services.BuildServiceProvider();
//
//         var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
//         var tlgAgent = PrivateBotChat_Operations;
//
//         List<TlgInput> interactiveHistory = [
//             inputGenerator.GetValidTlgInputCommandMessage(
//                 tlgAgent.Mode, 
//                 (int)OperationsBotCommands.NewIssue,
//                 roleSpecified: LiveEventAdmin_DanielEn_X2024)];
//
//         var workflow = _services.GetRequiredService<INewIssueWorkflow>();
//
//         var actualState =
//             workflow.DetermineCurrentState(
//                 interactiveHistory,
//                 [],
//                 X2024);
//         
//         Assert.Equal(
//             Initial_TradeUnknown,
//             actualState);
//     }
//     
//     [Fact]
//     public void DetermineCurrentState_ReturnsInitialSphereUnknown_OnNewIssueWithoutRecentLocationUpdates()
//     {
//         _services = new UnitTestStartup().Services.BuildServiceProvider();
//
//         var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
//         var tlgAgent = PrivateBotChat_Operations;
//
//         List<TlgInput> interactiveHistory = [
//             inputGenerator.GetValidTlgInputTextMessage(),
//             inputGenerator.GetValidTlgInputCommandMessage(
//                 tlgAgent.Mode, 
//                 (int)OperationsBotCommands.NewIssue)];
//
//         var workflow = _services.GetRequiredService<INewIssueWorkflow>();
//
//         var actualState =
//             workflow.DetermineCurrentState(
//                 interactiveHistory, 
//                 [],
//                 X2024);
//         
//         Assert.Equal(
//             Initial_SphereUnknown,
//             actualState);
//     }
//
//     [Theory]
//     [InlineData(true, Initial_SphereKnown)]
//     [InlineData(false, Initial_SphereUnknown)]
//     public void DetermineCurrentState_ReturnsCorrectInitialSphereState_OnNewIssueForSaniClean(
//         bool isNearSphere, Enum expectedState)
//     {
//         _services = new UnitTestStartup().Services.BuildServiceProvider();
//
//         var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
//         var tlgAgent = PrivateBotChat_Operations;
//
//         List<TlgInput> recentLocationHistory = [
//             inputGenerator.GetValidTlgInputLocationMessage(
//                 GetLocationFarFromAnySaniCleanSphere(),
//                 dateTime: DateTime.UtcNow.AddSeconds(-10))];
//
//         if (isNearSphere)
//         {
//             recentLocationHistory.Add(
//                 inputGenerator.GetValidTlgInputLocationMessage(
//                     GetLocationNearSaniCleanSphere()));
//         }
//         
//         List<TlgInput> interactiveHistory = [
//             inputGenerator.GetValidTlgInputTextMessage(), 
//             inputGenerator.GetValidTlgInputCommandMessage(
//                 tlgAgent.Mode, 
//                 (int)OperationsBotCommands.NewIssue)];
//
//         var workflow = _services.GetRequiredService<INewIssueWorkflow>();
//
//         var actualState = 
//             workflow.DetermineCurrentState(
//                 interactiveHistory,
//                 recentLocationHistory,
//                 X2024);
//
//         Assert.Equal(expectedState, actualState);
//     }
//
//     [Fact]
//     public void DetermineCurrentState_ReturnsSphereConfirmed_WhenUserConfirmsAutomaticNearSphere()
//     {
//         _services = new UnitTestStartup().Services.BuildServiceProvider();
//
//         var glossary = _services.GetRequiredService<IDomainGlossary>();
//         var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
//         var tlgAgent = PrivateBotChat_Operations;
//         var workflowId = glossary.IdAndUiByTerm[Dt(typeof(NewIssueWorkflow))].callbackId;
//     
//         List<TlgInput> recentLocationHistory = [
//             inputGenerator.GetValidTlgInputLocationMessage(
//                 GetLocationNearSaniCleanSphere(),
//                 dateTime: DateTime.UtcNow)];
//     
//         List<TlgInput> interactiveHistory = [
//             inputGenerator.GetValidTlgInputCommandMessage(
//                 tlgAgent.Mode,
//                 (int)OperationsBotCommands.NewIssue,
//                 resultantWorkflowInfo: new ResultantWorkflowInfo(
//                     workflowId,
//                     Initial_SphereKnown)),
//             inputGenerator.GetValidTlgInputCallbackQueryForControlPrompts(
//                 ControlPrompts.Yes)];
//
//         var workflow = _services.GetRequiredService<INewIssueWorkflow>();
//
//         var actualState =
//             workflow.DetermineCurrentState(
//                 interactiveHistory,
//                 recentLocationHistory,
//                 X2024);
//         
//         Assert.Equal(
//             SphereConfirmed, 
//             actualState);
//     }
//
//     [Theory]
//     [InlineData(Sphere1_AtX2024_Name, SphereConfirmed)]
//     [InlineData("Invalid sphere name", Initial_SphereUnknown)]
//     public void DetermineCurrentState_ReturnsCorrectState_WhenUserManuallyEntersSphere(
//         string enteredSphereName, Enum expectedState)
//     {
//         _services = new UnitTestStartup().Services.BuildServiceProvider();
//
//         var glossary = _services.GetRequiredService<IDomainGlossary>();
//         var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
//         var tlgAgent = PrivateBotChat_Operations;
//         var workflowId = glossary.IdAndUiByTerm[Dt(typeof(NewIssueWorkflow))].callbackId;
//     
//         List<TlgInput> recentLocationHistory = [
//             inputGenerator.GetValidTlgInputLocationMessage(
//                 GetLocationFarFromAnySaniCleanSphere(),
//                 dateTime: DateTime.UtcNow)];
//     
//         List<TlgInput> interactiveHistory = [
//             inputGenerator.GetValidTlgInputCommandMessage(
//                 tlgAgent.Mode,
//                 (int)OperationsBotCommands.NewIssue,
//                 resultantWorkflowInfo: new ResultantWorkflowInfo(
//                     workflowId,
//                     Initial_SphereUnknown)),
//             inputGenerator.GetValidTlgInputTextMessage(
//                 text: enteredSphereName)];
//
//         var workflow = _services.GetRequiredService<INewIssueWorkflow>();
//
//         var actualState =
//             workflow.DetermineCurrentState(
//                 interactiveHistory,
//                 recentLocationHistory,
//                 X2024);
//         
//         Assert.Equal(expectedState, actualState);
//     }
//
//     private Geo GetLocationNearSaniCleanSphere() =>
//         new Geo(
//             Sphere1_Location.Latitude + 0.00001, // ca. 1 meter off
//             Sphere1_Location.Longitude + 0.00001,
//             Option<double>.None());
//
//     private Geo GetLocationFarFromAnySaniCleanSphere() =>
//         new Geo(
//             Sphere1_Location.Latitude + 1, // ca. 100km off
//             Sphere1_Location.Longitude,
//             Option<double>.None());
// }