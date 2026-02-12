using FactorySnap.Shared.Entities;
using Opc.Ua;
using Opc.Ua.Client;

namespace FactorySnap.Agent.Services;

public class OpcUaClient(ISessionFactory sessionFactory, ILogger<OpcUaClient> logger)
{
    private ApplicationConfiguration? _config;
    private ISession? _session;
    private Subscription? _subscription;

    public Action<Measurement>? OnDataReceived { get; set; }

    public async Task InitializeAsync()
    {
        _config = new ApplicationConfiguration()
        {
            ApplicationName = "FactorySnap.Agent",
            ApplicationUri = Utils.Format(@"urn:{0}:FactorySnap:Agent", System.Net.Dns.GetHostName()),
            ApplicationType = ApplicationType.Client,
            
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier 
                { 
                    StoreType = @"Directory", 
                    StorePath = @"pki/own", 
                    SubjectName = "FactorySnap.Agent" 
                },
                TrustedIssuerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"pki/issuer" },
                TrustedPeerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"pki/trusted" },
                RejectedCertificateStore = new CertificateTrustList { StoreType = @"Directory", StorePath = @"pki/rejected" },
                AutoAcceptUntrustedCertificates = true,
                AddAppCertToTrustedStore = true
            },
            
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
            ServerConfiguration = new ServerConfiguration(),
            DiscoveryServerConfiguration = new DiscoveryServerConfiguration()
        };

        await _config.ValidateAsync(ApplicationType.Client);
    }

    public async Task ConnectAsync(string opcUrl)
    {
        try
        {
            if (_config is null) 
                throw new InvalidOperationException("Nie zainicjowano konfiguracji aplikacji.");
            
            var endpointDescription = await GetBestEndpoint(opcUrl);
            var endpointConfig = EndpointConfiguration.Create(_config);
            var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfig);
        
            _session = await sessionFactory.CreateAsync(
                _config,
                endpoint,
                false,
                false, 
                "FactorySnapSession",
                60000,
                new UserIdentity(new AnonymousIdentityToken()),
                preferredLocales: null
            );

            _subscription = new Subscription(_session.DefaultSubscription) 
            {
                PublishingInterval = 1000,
                PublishingEnabled = true,
                Priority = 1,
                KeepAliveCount = 10,
                LifetimeCount = 100,
                MaxNotificationsPerPublish = 1000
            };

            _session.AddSubscription(_subscription);
    
            await _subscription.CreateAsync(); 
        }
        catch (Exception e)
        {
            logger.LogError("ConnectAsync error:{error}", e.Message);
        }
    }

    private async Task<EndpointDescription> GetBestEndpoint(string opcUrl)
    {
        var uri = new Uri(opcUrl);
        var endpointConfiguration = EndpointConfiguration.Create(_config);

        using var client = await DiscoveryClient.CreateAsync(uri, endpointConfiguration,_config);
        var collection = await client.GetEndpointsAsync(null); 

        var endpoint = collection.FirstOrDefault(e => 
            e.TransportProfileUri == Profiles.UaTcpTransport && 
            e.SecurityMode == MessageSecurityMode.None) ?? collection.FirstOrDefault(e => e.TransportProfileUri == Profiles.UaTcpTransport);

        return endpoint ?? throw new Exception($"Nie znaleziono endpointa TCP pod adresem {opcUrl}");
    }

    public void MonitorNode(string nodeId, string tagName)
    {
        if (_session is not { Connected: true })
            throw new InvalidOperationException("Nie połączono z serwerem OPC.");
        
        if (_subscription is null) 
            throw new InvalidOperationException("Nie utworzono subskrypcji.");

        var monitoredItem = new MonitoredItem(_subscription.DefaultItem)
        {
            DisplayName = tagName,
            StartNodeId = nodeId,
            AttributeId = Attributes.Value,
            MonitoringMode = MonitoringMode.Reporting,
            SamplingInterval = 1000,
        };

        monitoredItem.Notification += (_, args) => OnNotification(args, tagName);

        _subscription.AddItem(monitoredItem);
        _subscription.ApplyChangesAsync();
    }

    private void OnNotification(MonitoredItemNotificationEventArgs args, string tagName)
    {
        if (args.NotificationValue is not MonitoredItemNotification notification) return;

        var value = notification.Value;

        var measurement = new Measurement
        {
            Timestamp = DateTime.UtcNow,
            MachineId = "S7-1500", 
            TagName = tagName,
            Quality = value.StatusCode.Code,
            OriginalType = OpcTypeMapper.Map(value.WrappedValue.TypeInfo.BuiltInType)
        };

        MapValueToMeasurement(value.Value, measurement);

        OnDataReceived?.Invoke(measurement);
    }

    private static void MapValueToMeasurement(object? val, Measurement m)
    {
        if (val == null) return;

        switch (val)
        {
            case double d: m.ValNum = d; break;
            case float f: m.ValNum = f; break;
            case int i: m.ValNum = i; break;
            case uint ui: m.ValNum = ui; break;
            case short s: m.ValNum = s; break;
            case ushort us: m.ValNum = us; break;
            case byte b: m.ValNum = b; break;
            case sbyte sb: m.ValNum = sb; break;

            case bool boolean:
                m.ValNum = boolean ? 1.0 : 0.0;
                break;

            case string str: m.ValText = str; break;
            case DateTime dt: m.ValText = dt.ToString("O"); break;
            case byte[] bytes: m.ValText = Convert.ToBase64String(bytes); break;

            default: m.ValText = val.ToString(); break;
        }
    }
}