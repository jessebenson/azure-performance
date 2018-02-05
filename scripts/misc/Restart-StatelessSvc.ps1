Connect-ServiceFabricCluster azure-performance.jessebenson.me:19000 `
    -X509Credential `
    -ServerCertThumbprint "B7B0D8BDEE49AE2DFD752684A63C62BD1D9BD64B" `
    -FindType "FindByThumbprint" `
    -FindValue "B7B0D8BDEE49AE2DFD752684A63C62BD1D9BD64B" `
    -StoreLocation "CurrentUser" `
    -StoreName "My"

Get-ServiceFabricApplication "fabric:/Latency.App" | Get-ServiceFabricService -ServiceName "fabric:/Latency.App/StatelessSvc" | Get-ServiceFabricPartition | Get-ServiceFabricReplica | Remove-ServiceFabricReplica -ForceRemove

