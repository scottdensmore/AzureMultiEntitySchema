<?xml version="1.0" encoding="utf-8"?>
<serviceModel xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" name="MultiEntitySchemaDemo" generation="1" functional="0" release="0" Id="43cad318-b405-45d8-8f4c-a11275135801" dslVersion="1.2.0.0" xmlns="http://schemas.microsoft.com/dsltools/RDSM">
  <groups>
    <group name="MultiEntitySchemaDemoGroup" generation="1" functional="0" release="0">
      <componentports>
        <inPort name="aExpense:Endpoint1" protocol="http">
          <inToChannel>
            <lBChannelMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/LB:aExpense:Endpoint1" />
          </inToChannel>
        </inPort>
      </componentports>
      <settings>
        <aCS name="aExpense.Workers:DataConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/MapaExpense.Workers:DataConnectionString" />
          </maps>
        </aCS>
        <aCS name="aExpense.Workers:DiagnosticsConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/MapaExpense.Workers:DiagnosticsConnectionString" />
          </maps>
        </aCS>
        <aCS name="aExpense.Workers:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/MapaExpense.Workers:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" />
          </maps>
        </aCS>
        <aCS name="aExpense.WorkersInstances" defaultValue="[1,1,1]">
          <maps>
            <mapMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/MapaExpense.WorkersInstances" />
          </maps>
        </aCS>
        <aCS name="aExpense:DataConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/MapaExpense:DataConnectionString" />
          </maps>
        </aCS>
        <aCS name="aExpense:DiagnosticsConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/MapaExpense:DiagnosticsConnectionString" />
          </maps>
        </aCS>
        <aCS name="aExpense:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/MapaExpense:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" />
          </maps>
        </aCS>
        <aCS name="aExpenseInstances" defaultValue="[1,1,1]">
          <maps>
            <mapMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/MapaExpenseInstances" />
          </maps>
        </aCS>
      </settings>
      <channels>
        <lBChannel name="LB:aExpense:Endpoint1">
          <toPorts>
            <inPortMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpense/Endpoint1" />
          </toPorts>
        </lBChannel>
      </channels>
      <maps>
        <map name="MapaExpense.Workers:DataConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpense.Workers/DataConnectionString" />
          </setting>
        </map>
        <map name="MapaExpense.Workers:DiagnosticsConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpense.Workers/DiagnosticsConnectionString" />
          </setting>
        </map>
        <map name="MapaExpense.Workers:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpense.Workers/Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" />
          </setting>
        </map>
        <map name="MapaExpense.WorkersInstances" kind="Identity">
          <setting>
            <sCSPolicyIDMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpense.WorkersInstances" />
          </setting>
        </map>
        <map name="MapaExpense:DataConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpense/DataConnectionString" />
          </setting>
        </map>
        <map name="MapaExpense:DiagnosticsConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpense/DiagnosticsConnectionString" />
          </setting>
        </map>
        <map name="MapaExpense:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpense/Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" />
          </setting>
        </map>
        <map name="MapaExpenseInstances" kind="Identity">
          <setting>
            <sCSPolicyIDMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpenseInstances" />
          </setting>
        </map>
      </maps>
      <components>
        <groupHascomponents>
          <role name="aExpense" generation="1" functional="0" release="0" software="C:\Users\Scott\Dev\AzureMultiEntitySchema\MultiEntitySchemaDemo\csx\Debug\roles\aExpense" entryPoint="base\x64\WaHostBootstrapper.exe" parameters="base\x64\WaIISHost.exe " memIndex="1792" hostingEnvironment="frontendadmin" hostingEnvironmentVersion="2">
            <componentports>
              <inPort name="Endpoint1" protocol="http" portRanges="80" />
            </componentports>
            <settings>
              <aCS name="DataConnectionString" defaultValue="" />
              <aCS name="DiagnosticsConnectionString" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" defaultValue="" />
              <aCS name="__ModelData" defaultValue="&lt;m role=&quot;aExpense&quot; xmlns=&quot;urn:azure:m:v1&quot;&gt;&lt;r name=&quot;aExpense&quot;&gt;&lt;e name=&quot;Endpoint1&quot; /&gt;&lt;/r&gt;&lt;r name=&quot;aExpense.Workers&quot; /&gt;&lt;/m&gt;" />
            </settings>
            <resourcereferences>
              <resourceReference name="DiagnosticStore" defaultAmount="[4096,4096,4096]" defaultSticky="true" kind="Directory" />
              <resourceReference name="EventStore" defaultAmount="[1000,1000,1000]" defaultSticky="false" kind="LogStore" />
            </resourcereferences>
          </role>
          <sCSPolicy>
            <sCSPolicyIDMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpenseInstances" />
            <sCSPolicyUpdateDomainMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpenseUpgradeDomains" />
            <sCSPolicyFaultDomainMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpenseFaultDomains" />
          </sCSPolicy>
        </groupHascomponents>
        <groupHascomponents>
          <role name="aExpense.Workers" generation="1" functional="0" release="0" software="C:\Users\Scott\Dev\AzureMultiEntitySchema\MultiEntitySchemaDemo\csx\Debug\roles\aExpense.Workers" entryPoint="base\x64\WaHostBootstrapper.exe" parameters="base\x64\WaWorkerHost.exe " memIndex="1792" hostingEnvironment="consoleroleadmin" hostingEnvironmentVersion="2">
            <settings>
              <aCS name="DataConnectionString" defaultValue="" />
              <aCS name="DiagnosticsConnectionString" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" defaultValue="" />
              <aCS name="__ModelData" defaultValue="&lt;m role=&quot;aExpense.Workers&quot; xmlns=&quot;urn:azure:m:v1&quot;&gt;&lt;r name=&quot;aExpense&quot;&gt;&lt;e name=&quot;Endpoint1&quot; /&gt;&lt;/r&gt;&lt;r name=&quot;aExpense.Workers&quot; /&gt;&lt;/m&gt;" />
            </settings>
            <resourcereferences>
              <resourceReference name="DiagnosticStore" defaultAmount="[4096,4096,4096]" defaultSticky="true" kind="Directory" />
              <resourceReference name="EventStore" defaultAmount="[1000,1000,1000]" defaultSticky="false" kind="LogStore" />
            </resourcereferences>
          </role>
          <sCSPolicy>
            <sCSPolicyIDMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpense.WorkersInstances" />
            <sCSPolicyUpdateDomainMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpense.WorkersUpgradeDomains" />
            <sCSPolicyFaultDomainMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpense.WorkersFaultDomains" />
          </sCSPolicy>
        </groupHascomponents>
      </components>
      <sCSPolicy>
        <sCSPolicyUpdateDomain name="aExpenseUpgradeDomains" defaultPolicy="[5,5,5]" />
        <sCSPolicyUpdateDomain name="aExpense.WorkersUpgradeDomains" defaultPolicy="[5,5,5]" />
        <sCSPolicyFaultDomain name="aExpenseFaultDomains" defaultPolicy="[2,2,2]" />
        <sCSPolicyFaultDomain name="aExpense.WorkersFaultDomains" defaultPolicy="[2,2,2]" />
        <sCSPolicyID name="aExpense.WorkersInstances" defaultPolicy="[1,1,1]" />
        <sCSPolicyID name="aExpenseInstances" defaultPolicy="[1,1,1]" />
      </sCSPolicy>
    </group>
  </groups>
  <implements>
    <implementation Id="e41236f8-e348-4c61-a154-2379e7f264c1" ref="Microsoft.RedDog.Contract\ServiceContract\MultiEntitySchemaDemoContract@ServiceDefinition">
      <interfacereferences>
        <interfaceReference Id="2550d25b-dd91-433f-8f8a-3f6e6acd1354" ref="Microsoft.RedDog.Contract\Interface\aExpense:Endpoint1@ServiceDefinition">
          <inPort>
            <inPortMoniker name="/MultiEntitySchemaDemo/MultiEntitySchemaDemoGroup/aExpense:Endpoint1" />
          </inPort>
        </interfaceReference>
      </interfacereferences>
    </implementation>
  </implements>
</serviceModel>