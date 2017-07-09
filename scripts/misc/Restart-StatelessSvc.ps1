Connect-ServiceFabricCluster azure-performance.jessebenson.me:19000 `
    -X509Credential `
    -ServerCertThumbprint "41AEC7DD46696FA88DAC6A5878F92E90C727726F" `
    -FindType "FindByThumbprint" `
    -FindValue "41AEC7DD46696FA88DAC6A5878F92E90C727726F" `
    -StoreLocation "CurrentUser" `
    -StoreName "My"

Get-ServiceFabricApplication "fabric:/Latency.App" | Get-ServiceFabricService -ServiceName "fabric:/Latency.App/StatelessSvc" | Get-ServiceFabricPartition | Get-ServiceFabricReplica | Remove-ServiceFabricReplica -ForceRemove

