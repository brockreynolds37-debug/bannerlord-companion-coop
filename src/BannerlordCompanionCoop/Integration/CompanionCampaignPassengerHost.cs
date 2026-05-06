using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Diagnostics;
using BannerlordCompanionCoop.Services;

namespace BannerlordCompanionCoop.Integration;

public sealed class CompanionCampaignPassengerHost
{
    public const int DefaultPort = 9998;

    private readonly Func<CompanionCampaignSpectatorSnapshot?> _snapshotProvider;
    private readonly int _port;
    private TcpListener? _listener;
    private Thread? _listenerThread;
    private volatile bool _running;

    public CompanionCampaignPassengerHost(Func<CompanionCampaignSpectatorSnapshot?> snapshotProvider, int port = DefaultPort)
    {
        _snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
        _port = port;
    }

    public bool IsRunning => _running;

    public int Port => _port;

    public void Start()
    {
        if (_running)
        {
            return;
        }

        try
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _running = true;
            _listenerThread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "BannerlordCompanionCoopPassengerHost",
            };
            _listenerThread.Start();

            CompanionModLogger.Info(
                "PassengerHost",
                $"Started campaign passenger web feed on port {_port}. Open http://localhost:{_port}/ on this machine or http://<host-ip>:{_port}/ from another PC.");
        }
        catch (Exception exception)
        {
            _running = false;
            _listener = null;
            CompanionModLogger.Error(
                "PassengerHost",
                $"Could not start campaign passenger web feed on port {_port}.",
                exception);
        }
    }

    public void Stop()
    {
        if (!_running)
        {
            return;
        }

        _running = false;

        try
        {
            _listener?.Stop();
        }
        catch (Exception exception)
        {
            CompanionModLogger.Warn("PassengerHost", $"Failed while stopping passenger web feed: {exception.Message}");
        }

        _listener = null;
        CompanionModLogger.Info("PassengerHost", "Stopped campaign passenger web feed.");
    }

    private void ListenLoop()
    {
        while (_running)
        {
            try
            {
                TcpClient client = _listener!.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
            }
            catch (SocketException)
            {
                if (_running)
                {
                    CompanionModLogger.Warn("PassengerHost", "Socket error while accepting passenger web feed connection.");
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception exception)
            {
                CompanionModLogger.Error("PassengerHost", "Unexpected passenger web feed listener error.", exception);
            }
        }
    }

    private void HandleClient(TcpClient client)
    {
        using (client)
        {
            try
            {
                client.ReceiveTimeout = 2000;
                client.SendTimeout = 2000;

                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream, Encoding.ASCII, false, 1024, leaveOpen: true);

                string? requestLine = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(requestLine))
                {
                    return;
                }

                while (!string.IsNullOrEmpty(reader.ReadLine()))
                {
                }

                string path = ParsePath(requestLine);
                if (path.Equals("/snapshot", StringComparison.OrdinalIgnoreCase))
                {
                    WriteResponse(stream, "application/json; charset=utf-8", BuildSnapshotJson());
                    return;
                }

                if (path.Equals("/health", StringComparison.OrdinalIgnoreCase))
                {
                    WriteResponse(stream, "text/plain; charset=utf-8", "ok");
                    return;
                }

                WriteResponse(stream, "text/html; charset=utf-8", BuildPassengerPageHtml());
            }
            catch (Exception exception)
            {
                CompanionModLogger.Warn("PassengerHost", $"Passenger web feed request failed: {exception.Message}");
            }
        }
    }

    private string BuildSnapshotJson()
    {
        CompanionCampaignSpectatorSnapshot? snapshot = _snapshotProvider();
        if (snapshot is null)
        {
            return "{\"available\":false,\"summary\":\"No campaign snapshot is available yet.\"}";
        }

        return CompanionCampaignSpectatorProtocol.SerializeSnapshot(snapshot);
    }

    private static string ParsePath(string requestLine)
    {
        string[] parts = requestLine.Split(' ');
        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
        {
            return "/";
        }

        return parts[1].Split('?')[0];
    }

    private static void WriteResponse(Stream stream, string contentType, string body)
    {
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
        string header =
            "HTTP/1.1 200 OK\r\n" +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {bodyBytes.Length}\r\n" +
            "Cache-Control: no-store\r\n" +
            "Access-Control-Allow-Origin: *\r\n" +
            "Connection: close\r\n\r\n";

        byte[] headerBytes = Encoding.ASCII.GetBytes(header);
        stream.Write(headerBytes, 0, headerBytes.Length);
        stream.Write(bodyBytes, 0, bodyBytes.Length);
    }

    private static string BuildPassengerPageHtml()
    {
        return @"<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>Bannerlord Companion Co-op Passenger View</title>
  <style>
    :root {
      color-scheme: dark;
      --bg: #10130f;
      --panel: #181d17;
      --panel-2: #222818;
      --text: #f1ead9;
      --muted: #b9b09a;
      --line: #39452d;
      --accent: #d2ad52;
      --danger: #d36c55;
    }
    * { box-sizing: border-box; }
    body {
      margin: 0;
      min-height: 100vh;
      font-family: Georgia, ""Times New Roman"", serif;
      color: var(--text);
      background:
        radial-gradient(circle at 20% 0%, rgba(210, 173, 82, 0.18), transparent 30rem),
        linear-gradient(135deg, #0b0f0c, var(--bg) 45%, #17160e);
    }
    main {
      width: min(1100px, calc(100vw - 32px));
      margin: 0 auto;
      padding: 34px 0;
    }
    header {
      display: flex;
      justify-content: space-between;
      gap: 18px;
      align-items: end;
      border-bottom: 1px solid var(--line);
      padding-bottom: 18px;
      margin-bottom: 18px;
    }
    h1 {
      margin: 0;
      font-size: clamp(30px, 5vw, 58px);
      line-height: 0.95;
      font-weight: 700;
    }
    .status {
      color: var(--accent);
      font-size: 14px;
      text-align: right;
      min-width: 170px;
    }
    .summary {
      font-size: clamp(22px, 3vw, 36px);
      line-height: 1.15;
      margin: 22px 0;
      max-width: 920px;
    }
    .grid {
      display: grid;
      grid-template-columns: repeat(4, minmax(0, 1fr));
      gap: 12px;
    }
    .tile {
      border: 1px solid var(--line);
      background: linear-gradient(180deg, rgba(255,255,255,0.04), rgba(255,255,255,0.01)), var(--panel);
      padding: 16px;
      min-height: 92px;
    }
    .tile span {
      display: block;
      color: var(--muted);
      font-size: 12px;
      text-transform: uppercase;
      letter-spacing: 0;
      margin-bottom: 8px;
    }
    .tile strong {
      display: block;
      font-size: 20px;
      line-height: 1.15;
      overflow-wrap: anywhere;
    }
    .events {
      margin-top: 14px;
      border: 1px solid var(--line);
      background: rgba(24, 29, 23, 0.78);
      padding: 16px;
    }
    .events h2 {
      margin: 0 0 10px;
      font-size: 18px;
    }
    ul {
      margin: 0;
      padding-left: 20px;
      color: var(--muted);
      line-height: 1.55;
    }
    .empty {
      color: var(--danger);
    }
    @media (max-width: 760px) {
      header { display: block; }
      .status { text-align: left; margin-top: 10px; }
      .grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
    }
    @media (max-width: 460px) {
      .grid { grid-template-columns: 1fr; }
    }
  </style>
</head>
<body>
  <main>
    <header>
      <h1 id=""host"">Passenger View</h1>
      <div id=""status"" class=""status"">Connecting...</div>
    </header>
    <section id=""summary"" class=""summary"">Waiting for the host campaign.</section>
    <section class=""grid"">
      <div class=""tile""><span>Faction</span><strong id=""faction"">-</strong></div>
      <div class=""tile""><span>Location</span><strong id=""location"">-</strong></div>
      <div class=""tile""><span>Target</span><strong id=""target"">-</strong></div>
      <div class=""tile""><span>Party</span><strong id=""party"">-</strong></div>
      <div class=""tile""><span>Gold</span><strong id=""gold"">-</strong></div>
      <div class=""tile""><span>Food</span><strong id=""food"">-</strong></div>
      <div class=""tile""><span>Map Position</span><strong id=""position"">-</strong></div>
      <div class=""tile""><span>State</span><strong id=""state"">-</strong></div>
    </section>
    <section class=""events"">
      <h2>Recent Campaign Events</h2>
      <ul id=""events""><li>Waiting for updates.</li></ul>
    </section>
  </main>
  <script>
    const $ = (id) => document.getElementById(id);
    const text = (value, fallback = '-') => {
      if (value === null || value === undefined || value === '') return fallback;
      return String(value);
    };
    const escapeHtml = (value) => String(value).replace(/[&<>]/g, (c) => ({'&':'&amp;','<':'&lt;','>':'&gt;'}[c]));
    async function refresh() {
      try {
        const response = await fetch('/snapshot', { cache: 'no-store' });
        const data = await response.json();
        if (data.available === false) {
          $('status').textContent = 'Waiting';
          $('status').className = 'status empty';
          $('summary').textContent = data.summary || 'No campaign snapshot is available yet.';
          return;
        }
        $('status').textContent = 'Live';
        $('status').className = 'status';
        $('host').textContent = text(data.hostDisplayName, 'Host');
        $('summary').textContent = text(data.summary, 'Watching the campaign.');
        $('faction').textContent = text(data.factionName);
        $('location').textContent = text(data.currentSettlementName || data.nearestSettlementName);
        $('target').textContent = text(data.targetDescription);
        $('party').textContent = `${text(data.partySize, '0')} healthy`;
        $('gold').textContent = `${text(data.gold, '0')} denars`;
        $('food').textContent = `${Number(data.foodDaysRemaining || 0).toFixed(1)} days`;
        $('position').textContent = `${Number(data.mapPositionX || 0).toFixed(1)}, ${Number(data.mapPositionY || 0).toFixed(1)}`;
        $('state').textContent = data.isInMapEvent ? 'Encounter' : (data.isInSettlement ? 'In settlement' : 'On map');
        const events = Array.isArray(data.recentEvents) && data.recentEvents.length ? data.recentEvents : ['No recent events yet.'];
        $('events').innerHTML = events.map((event) => `<li>${escapeHtml(event)}</li>`).join('');
      } catch (error) {
        $('status').textContent = 'Disconnected';
        $('status').className = 'status empty';
      }
    }
    refresh();
    setInterval(refresh, 1000);
  </script>
</body>
</html>";
    }
}
