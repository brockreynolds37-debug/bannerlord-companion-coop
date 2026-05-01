using System;
using System.Linq;
using System.Threading.Tasks;
using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Diagnostics;
using BannerlordCompanionCoop.Integration;
using TaleWorlds.Diamond;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.PlatformService;

namespace BannerlordCompanionCoop.Missions;

public sealed class CompanionCampaignCustomServerRegistrationBehavior : MissionLogic
{
    private const string GameModuleId = "BannerlordCompanionCoop";
    private const string GameTypeId = "CompanionDropIn";
    private const int HostPort = 9999;
    private const int MinPublishedPlayerCount = 2;
    private const int MaxPublishedPlayerCount = 8;
    private static readonly TimeSpan RegistrationTimeout = TimeSpan.FromSeconds(10);

    private ICompanionMissionHost? _host;
    private LobbyGameClientHandler? _connectedLobbyHandler;
    private Task<LobbyClientConnectResult>? _connectTask;
    private Task<ILoginAccessProvider>? _loginProviderTask;
    private DateTime _registrationSubmittedAtUtc;
    private string? _lastPublishedScene;
    private string? _lastStatusMessage;
    private int _lastPublishedPlayerCount;
    private bool _registrationSubmitted;
    private bool _registrationComplete;
    private bool _startedMultiplayerSession;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();

        if (CompanionMissionSceneScopeResolver.ResolveForMission(Mission) != CompanionMissionJoinScope.Battles)
        {
            return;
        }

        _host = Mission.MissionBehaviors.OfType<ICompanionMissionHost>().FirstOrDefault();
        CompanionModLogger.Info(
            "CustomServer",
            $"Initializing campaign custom server registration for mission '{Mission.SceneName}' (hostFound={_host is not null}).");
        if (_host is not null)
        {
            _host.MissionPlanChanged += HandleMissionPlanChanged;
        }
    }

    public override void OnMissionTick(float dt)
    {
        if (_registrationComplete
            || _host is null
            || CompanionMissionSceneScopeResolver.ResolveForMission(Mission) != CompanionMissionJoinScope.Battles)
        {
            return;
        }

        TryProgressRegistration();
    }

    public override void OnRemoveBehavior()
    {
        if (_host is not null)
        {
            _host.MissionPlanChanged -= HandleMissionPlanChanged;
        }

        CompanionModLogger.Info("CustomServer", "Removing campaign custom server registration behavior.");
        EndHostedCustomGame();
        base.OnRemoveBehavior();
    }

    private void TryProgressRegistration()
    {
        if (!EnsureMultiplayerInitialized())
        {
            return;
        }

        LobbyClient? lobbyClient = MultiplayerMain.GameClient;
        if (lobbyClient is null)
        {
            ShowStatus("Companion Co-op could not access the multiplayer lobby client.", isError: true);
            return;
        }

        if (!EnsureLobbyClientReady(lobbyClient))
        {
            return;
        }

        if (!EnsureNetworkSessionStarted())
        {
            return;
        }

        if (_registrationSubmitted)
        {
            if (lobbyClient.IsHostingCustomGame)
            {
                MarkRegistrationComplete(lobbyClient);
                return;
            }

            if (DateTime.UtcNow - _registrationSubmittedAtUtc >= RegistrationTimeout)
            {
                _registrationSubmitted = false;
                ShowStatus("Companion Co-op battle registration timed out before the server became visible.", isError: true);
            }

            return;
        }

        if (lobbyClient.IsHostingCustomGame)
        {
            MarkRegistrationComplete(lobbyClient);
            return;
        }

        SubmitCustomGameRegistration(lobbyClient);
    }

    private bool EnsureMultiplayerInitialized()
    {
        if (MultiplayerMain.IsInitialized)
        {
            return true;
        }

        try
        {
            MultiplayerMain.Initialize(new GameNetworkHandler());
            CompanionModLogger.Info("CustomServer", "Initialized Bannerlord multiplayer services.");
            return true;
        }
        catch (Exception exception)
        {
            ShowStatus(
                $"Companion Co-op could not initialize Bannerlord multiplayer services: {exception.Message}",
                isError: true);
            return false;
        }
    }

    private bool EnsureLobbyClientReady(LobbyClient lobbyClient)
    {
        if (lobbyClient.Connected && lobbyClient.LoggedIn)
        {
            CompanionModLogger.Info("CustomServer", "Lobby client is connected and logged in.");
            return true;
        }

        if (_connectTask is null)
        {
            IPlatformServices? platformServices = PlatformServices.Instance;
            if (platformServices is null || !platformServices.UserLoggedIn)
            {
                ShowStatus(
                    "Companion Co-op could not sign into the multiplayer lobby. Open Bannerlord multiplayer once on this machine, then retry the battle.",
                    isError: true);
                return false;
            }

            _connectedLobbyHandler ??= new LobbyGameClientHandler();

            if (_loginProviderTask is null)
            {
                try
                {
                    _loginProviderTask = platformServices.CreateLobbyClientLoginProvider();
                }
                catch (Exception exception)
                {
                    ShowStatus(
                        $"Companion Co-op could not request multiplayer lobby credentials: {exception.Message}",
                        isError: true);
                }

                return false;
            }

            if (!_loginProviderTask.IsCompleted)
            {
                return false;
            }

            if (_loginProviderTask.IsFaulted)
            {
                string reason = _loginProviderTask.Exception?.GetBaseException().Message ?? "Unknown credential provider error.";
                ShowStatus(
                    $"Companion Co-op could not prepare multiplayer lobby credentials: {reason}",
                    isError: true);
                return false;
            }

            ILoginAccessProvider loginProvider = _loginProviderTask.Result;

            try
            {
                _connectTask = lobbyClient.Connect(
                    _connectedLobbyHandler,
                    loginProvider,
                    platformServices.UserDisplayName,
                    hasUserGeneratedContentPrivilege: true,
                    platformServices.GetInitParams(),
                    NoOpPreLoginTaskAsync);
                CompanionModLogger.Info(
                    "CustomServer",
                    $"Started lobby client connect flow for '{platformServices.UserDisplayName}'.");
            }
            catch (Exception exception)
            {
                ShowStatus(
                    $"Companion Co-op could not begin multiplayer lobby sign-in: {exception.Message}",
                    isError: true);
            }

            return false;
        }

        if (!_connectTask.IsCompleted)
        {
            return false;
        }

        if (_connectTask.IsFaulted)
        {
            string reason = _connectTask.Exception?.GetBaseException().Message ?? "Unknown lobby sign-in error.";
            ShowStatus(
                $"Companion Co-op could not sign into the multiplayer lobby: {reason}",
                isError: true);
            return false;
        }

        LobbyClientConnectResult result = _connectTask.Result;
        if (!result.Connected)
        {
            string reason = result.Error?.ToString() ?? "Unknown lobby sign-in error.";
            ShowStatus(
                $"Companion Co-op lobby sign-in was rejected: {reason}",
                isError: true);
            return false;
        }

        CompanionModLogger.Info("CustomServer", "Lobby client sign-in completed successfully.");
        return lobbyClient.Connected && lobbyClient.LoggedIn;
    }

    private bool EnsureNetworkSessionStarted()
    {
        if (GameNetwork.IsSessionActive)
        {
            return true;
        }

        try
        {
            GameNetwork.PreStartMultiplayerOnServer();
            GameNetwork.StartMultiplayerOnServer(HostPort);
            BannerlordNetwork.CreateServerPeer();

            if (GameNetwork.MyPeer is not null)
            {
                GameNetwork.ClientFinishedLoading(GameNetwork.MyPeer);
            }

            _startedMultiplayerSession = true;
            CompanionModLogger.Info(
                "CustomServer",
                $"Started multiplayer battle host session on port {HostPort}.");
            return GameNetwork.IsSessionActive;
        }
        catch (Exception exception)
        {
            ShowStatus(
                $"Companion Co-op could not start the battle host session: {exception.Message}",
                isError: true);
            return false;
        }
    }

    private void SubmitCustomGameRegistration(LobbyClient lobbyClient)
    {
        string sceneName = GetPublishedSceneName();
        int publishedPlayerCount = GetPublishedPlayerCount(_host?.LatestPlan);
        string serverName = BuildServerName();

        try
        {
            lobbyClient.RegisterCustomGame(
                GameModuleId,
                GameTypeId,
                serverName,
                publishedPlayerCount,
                sceneName,
                sceneName,
                string.Empty,
                string.Empty,
                HostPort);

            _registrationSubmitted = true;
            _registrationSubmittedAtUtc = DateTime.UtcNow;
            _lastPublishedScene = sceneName;
            _lastPublishedPlayerCount = publishedPlayerCount;
            CompanionModLogger.Info(
                "CustomServer",
                $"Submitted custom game registration name='{serverName}', scene='{sceneName}', players={publishedPlayerCount}, port={HostPort}.");
        }
        catch (Exception exception)
        {
            ShowStatus(
                $"Companion Co-op could not publish the battle to the custom server list: {exception.Message}",
                isError: true);
        }
    }

    private void MarkRegistrationComplete(LobbyClient lobbyClient)
    {
        _registrationSubmitted = false;
        _registrationComplete = true;
        PublishServerMetadata(lobbyClient, _host?.LatestPlan);

        string sceneName = string.IsNullOrWhiteSpace(lobbyClient.CustomGameScene)
            ? GetPublishedSceneName()
            : lobbyClient.CustomGameScene;
        CompanionModLogger.Info(
            "CustomServer",
            $"Custom game registration completed name='{BuildServerName()}', scene='{sceneName}', players={GetPublishedPlayerCount(_host?.LatestPlan)}.");

        ShowStatus(
            $"Companion Co-op battle published as '{BuildServerName()}' on scene '{sceneName}' (port {HostPort}).",
            isError: false);
    }

    private void HandleMissionPlanChanged(CompanionMissionPlan? plan)
    {
        if (!_registrationComplete)
        {
            return;
        }

        LobbyClient? lobbyClient = MultiplayerMain.GameClient;
        if (lobbyClient is null || !lobbyClient.IsHostingCustomGame)
        {
            return;
        }

        CompanionModLogger.Info(
            "CustomServer",
            $"Mission plan changed while hosting. seatOffers={plan?.SeatOffers.Count ?? 0}, assignments={plan?.Assignments.Count ?? 0}.");
        PublishServerMetadata(lobbyClient, plan);
    }

    private void PublishServerMetadata(LobbyClient lobbyClient, CompanionMissionPlan? plan)
    {
        string sceneName = GetPublishedSceneName();
        int publishedPlayerCount = GetPublishedPlayerCount(plan);

        if (string.Equals(_lastPublishedScene, sceneName, StringComparison.Ordinal)
            && _lastPublishedPlayerCount == publishedPlayerCount)
        {
            return;
        }

        try
        {
            lobbyClient.UpdateCustomGameData(GameTypeId, sceneName, publishedPlayerCount);
            _lastPublishedScene = sceneName;
            _lastPublishedPlayerCount = publishedPlayerCount;
            CompanionModLogger.Info(
                "CustomServer",
                $"Updated custom game metadata scene='{sceneName}', players={publishedPlayerCount}.");
        }
        catch (Exception exception)
        {
            ShowStatus(
                $"Companion Co-op could not refresh the published battle details: {exception.Message}",
                isError: true);
        }
    }

    private void EndHostedCustomGame()
    {
        LobbyClient? lobbyClient = MultiplayerMain.IsInitialized ? MultiplayerMain.GameClient : null;

        if (lobbyClient is not null
            && lobbyClient.IsHostingCustomGame
            && string.Equals(lobbyClient.CustomGameType, GameTypeId, StringComparison.Ordinal))
        {
            try
            {
                lobbyClient.EndCustomGame();
                CompanionModLogger.Info("CustomServer", "Ended hosted custom game registration.");
            }
            catch (Exception exception)
            {
                Debug.DisplayDebugMessage(
                    $"[BannerlordCompanionCoop] Failed to end custom battle registration: {exception.Message}");
            }
        }

        if (_startedMultiplayerSession && GameNetwork.IsSessionActive)
        {
            try
            {
                GameNetwork.EndMultiplayer();
                CompanionModLogger.Info("CustomServer", "Ended multiplayer host session.");
            }
            catch (Exception exception)
            {
                Debug.DisplayDebugMessage(
                    $"[BannerlordCompanionCoop] Failed to stop the multiplayer host session: {exception.Message}");
            }
        }
    }

    private static Task<bool> NoOpPreLoginTaskAsync()
    {
        return Task.FromResult(true);
    }

    private static int GetPublishedPlayerCount(CompanionMissionPlan? plan)
    {
        int seatCount = plan?.SeatOffers.Count ?? 0;
        int totalPlayerCount = seatCount + 1;
        totalPlayerCount = Math.Max(MinPublishedPlayerCount, totalPlayerCount);
        return Math.Min(MaxPublishedPlayerCount, totalPlayerCount);
    }

    private string GetPublishedSceneName()
    {
        return string.IsNullOrWhiteSpace(Mission.SceneName)
            ? "campaign_battle"
            : Mission.SceneName;
    }

    private static string BuildServerName()
    {
        string hostName = Hero.MainHero?.Name?.ToString() ?? string.Empty;
        string battleName = string.IsNullOrWhiteSpace(hostName)
            ? "Companion Co-op Battle"
            : $"{hostName}'s Companion Co-op Battle";

        CompanionCampaignSpectatorSnapshot? spectatorSnapshot = BannerlordCompanionCoopSubModule.LatestCampaignSpectatorSnapshot;
        string? locationName = spectatorSnapshot?.CurrentSettlementName ?? spectatorSnapshot?.NearestSettlementName;

        return string.IsNullOrWhiteSpace(locationName)
            ? battleName
            : $"{battleName} near {locationName}";
    }

    private void ShowStatus(string message, bool isError)
    {
        if (string.Equals(_lastStatusMessage, message, StringComparison.Ordinal))
        {
            return;
        }

        _lastStatusMessage = message;
        if (isError)
        {
            CompanionModLogger.Error("CustomServer", message);
        }
        else
        {
            CompanionModLogger.Info("CustomServer", message);
        }
        Debug.DisplayDebugMessage($"[BannerlordCompanionCoop] {message}");
        InformationManager.DisplayMessage(new InformationMessage(message));

        if (isError)
        {
            _registrationSubmitted = false;
        }
    }
}
