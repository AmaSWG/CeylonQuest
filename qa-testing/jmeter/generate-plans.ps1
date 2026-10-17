# Generate all JMeter Test Plans for CeylonQuest (JM-01 to JM-10)

param (
    [string]$OutputDir = "$PSScriptRoot\plans"
)

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
}

function New-JmxHttpPlan {
    param (
        [string]$TestName,
        [int]$NumThreads,
        [int]$RampTime,
        [int]$LoopCount = 1,
        [int]$Duration = 0,
        [bool]$UseDuration = $false,
        [string]$Domain = "`${__P(host,localhost)}",
        [string]$Port = "5185",
        [string]$Protocol = "http",
        [string]$Method = "GET",
        [string]$Path = "/",
        [string]$Body = "",
        [string]$CsvPath = "",
        [string]$CsvVar = "",
        [bool]$IgnoreStatus409 = $false
    )

    $XmlPath = $Path.Replace("&amp;", "&").Replace("&", "&amp;")

    $loopControl = if ($UseDuration) {
        @"
          <boolProp name="LoopController.continue_forever">false</boolProp>
          <intProp name="LoopController.loops">-1</intProp>
"@
    } else {
        @"
          <boolProp name="LoopController.continue_forever">false</boolProp>
          <stringProp name="LoopController.loops">$LoopCount</stringProp>
"@
    }

    $schedulerConfig = if ($UseDuration) {
        @"
        <boolProp name="ThreadGroup.scheduler">true</boolProp>
        <stringProp name="ThreadGroup.duration">$Duration</stringProp>
        <stringProp name="ThreadGroup.delay">0</stringProp>
"@
    } else {
        @"
        <boolProp name="ThreadGroup.scheduler">false</boolProp>
        <stringProp name="ThreadGroup.duration"></stringProp>
        <stringProp name="ThreadGroup.delay"></stringProp>
"@
    }

    $csvElement = ""
    if ($CsvPath -and $CsvVar) {
        $csvElement = @"
      <CSVDataSet guiclass="TestBeanGUI" testclass="CSVDataSet" testname="CSV Data Set Config" enabled="true">
        <stringProp name="delimiter">,</stringProp>
        <stringProp name="fileEncoding">UTF-8</stringProp>
        <stringProp name="filename">$CsvPath</stringProp>
        <boolProp name="ignoreFirstLine">true</boolProp>
        <boolProp name="quotedData">false</boolProp>
        <boolProp name="recycle">true</boolProp>
        <stringProp name="shareMode">shareMode.all</stringProp>
        <boolProp name="stopThread">false</boolProp>
        <stringProp name="variableNames">$CsvVar</stringProp>
      </CSVDataSet>
      <hashTree/>
"@
    }

    $bodyArg = ""
    $postBodyRaw = "false"
    if ($Body) {
        $postBodyRaw = "true"
        $bodyArg = @"
          <elementProp name="HTTPsampler.Arguments" elementType="Arguments">
            <collectionProp name="Arguments.arguments">
              <elementProp name="" elementType="HTTPArgument">
                <boolProp name="HTTPArgument.always_encode">false</boolProp>
                <stringProp name="Argument.value"><![CDATA[$Body]]></stringProp>
                <stringProp name="Argument.metadata">=</stringProp>
              </elementProp>
            </collectionProp>
          </elementProp>
"@
    } else {
        $bodyArg = @"
          <elementProp name="HTTPsampler.Arguments" elementType="Arguments" guiclass="HTTPArgumentsPanel" testclass="Arguments" testname="User Defined Variables" enabled="true">
            <collectionProp name="Arguments.arguments"/>
          </elementProp>
"@
    }

    $assertionElement = ""
    if ($IgnoreStatus409) {
        $assertionElement = @"
        <ResponseAssertion guiclass="AssertionGui" testclass="ResponseAssertion" testname="Allow 201 or 409" enabled="true">
          <collectionProp name="Asserion.test_strings">
            <stringProp name="49586">201</stringProp>
            <stringProp name="51512">409</stringProp>
          </collectionProp>
          <stringProp name="Assertion.custom_message"></stringProp>
          <stringProp name="Assertion.test_field">Assertion.response_code</stringProp>
          <boolProp name="Assertion.assume_success">true</boolProp>
          <intProp name="Assertion.test_type">40</intProp>
        </ResponseAssertion>
        <hashTree/>
"@
    }

    $jmx = @"
<?xml version="1.0" encoding="UTF-8"?>
<jmeterTestPlan version="1.2" properties="5.0" jmeter="5.6.3">
  <hashTree>
    <TestPlan guiclass="TestPlanGui" testclass="TestPlan" testname="$TestName" enabled="true">
      <stringProp name="TestPlan.comments">CeylonQuest QA Performance Suite</stringProp>
      <boolProp name="TestPlan.functional_mode">false</boolProp>
      <boolProp name="TestPlan.tearDown_on_shutdown">true</boolProp>
      <boolProp name="TestPlan.serialize_threadgroups">false</boolProp>
      <elementProp name="TestPlan.user_defined_variables" elementType="Arguments" guiclass="ArgumentsPanel" testclass="Arguments" testname="User Defined Variables" enabled="true">
        <collectionProp name="Arguments.arguments">
          <elementProp name="host" elementType="Argument">
            <stringProp name="Argument.name">host</stringProp>
            <stringProp name="Argument.value">`${__P(host,localhost)}</stringProp>
            <stringProp name="Argument.metadata">=</stringProp>
          </elementProp>
          <elementProp name="token" elementType="Argument">
            <stringProp name="Argument.name">token</stringProp>
            <stringProp name="Argument.value">`${__P(token,)}</stringProp>
            <stringProp name="Argument.metadata">=</stringProp>
          </elementProp>
          <elementProp name="listing_id" elementType="Argument">
            <stringProp name="Argument.name">listing_id</stringProp>
            <stringProp name="Argument.value">`${__P(listing_id,00000000-0000-0000-0000-000000000001)}</stringProp>
            <stringProp name="Argument.metadata">=</stringProp>
          </elementProp>
        </collectionProp>
      </elementProp>
      <stringProp name="TestPlan.user_define_classpath"></stringProp>
    </TestPlan>
    <hashTree>
      <HeaderManager guiclass="HeaderPanel" testclass="HeaderManager" testname="HTTP Header Manager" enabled="true">
        <collectionProp name="HeaderManager.headers">
          <elementProp name="" elementType="Header">
            <stringProp name="Header.name">Content-Type</stringProp>
            <stringProp name="Header.value">application/json</stringProp>
          </elementProp>
          <elementProp name="" elementType="Header">
            <stringProp name="Header.name">Authorization</stringProp>
            <stringProp name="Header.value">Bearer `${token}</stringProp>
          </elementProp>
        </collectionProp>
      </HeaderManager>
      <hashTree/>
$csvElement
      <ThreadGroup guiclass="ThreadGroupGui" testclass="ThreadGroup" testname="Thread Group" enabled="true">
        <stringProp name="ThreadGroup.on_sample_error">continue</stringProp>
        <elementProp name="ThreadGroup.main_controller" elementType="LoopController" guiclass="LoopControlPanel" testclass="LoopController" testname="Loop Controller" enabled="true">
$loopControl
        </elementProp>
        <stringProp name="ThreadGroup.num_threads">$NumThreads</stringProp>
        <stringProp name="ThreadGroup.ramp_time">$RampTime</stringProp>
$schedulerConfig
        <boolProp name="ThreadGroup.same_user_on_next_iteration">true</boolProp>
      </ThreadGroup>
      <hashTree>
        <HTTPSamplerProxy guiclass="HttpTestSampleGui" testclass="HTTPSamplerProxy" testname="$TestName Sampler" enabled="true">
          <boolProp name="HTTPSampler.postBodyRaw">$postBodyRaw</boolProp>
$bodyArg
          <stringProp name="HTTPSampler.domain">$Domain</stringProp>
          <stringProp name="HTTPSampler.port">$Port</stringProp>
          <stringProp name="HTTPSampler.protocol">$Protocol</stringProp>
          <stringProp name="HTTPSampler.path">$XmlPath</stringProp>
          <stringProp name="HTTPSampler.method">$Method</stringProp>
          <boolProp name="HTTPSampler.follow_redirects">true</boolProp>
          <boolProp name="HTTPSampler.auto_redirects">false</boolProp>
          <boolProp name="HTTPSampler.use_keepalive">true</boolProp>
          <boolProp name="HTTPSampler.DO_MULTIPART_POST">false</boolProp>
          <boolProp name="HTTPSampler.BROWSER_COMPATIBLE_MULTIPART">false</boolProp>
          <boolProp name="HTTPSampler.image_parser">false</boolProp>
          <boolProp name="HTTPSampler.concurrentDwn">false</boolProp>
          <stringProp name="HTTPSampler.concurrentPool">6</stringProp>
          <boolProp name="HTTPSampler.md5">false</boolProp>
          <intProp name="HTTPSampler.ipSourceType">0</intProp>
        </HTTPSamplerProxy>
        <hashTree>
$assertionElement
        </hashTree>
        <ResultCollector guiclass="SummaryReport" testclass="ResultCollector" testname="Summary Report" enabled="true">
          <boolProp name="ResultCollector.error_logging">false</boolProp>
          <objProp>
            <name>saveConfig</name>
            <value class="SampleSaveConfiguration">
              <time>true</time>
              <latency>true</latency>
              <timestamp>true</timestamp>
              <success>true</success>
              <label>true</label>
              <code>true</code>
              <message>true</message>
              <threadName>true</threadName>
              <dataType>true</dataType>
              <encoding>false</encoding>
              <assertions>true</assertions>
              <subresults>true</subresults>
              <responseData>false</responseData>
              <samplerData>false</samplerData>
              <xml>false</xml>
              <fieldNames>true</fieldNames>
              <responseHeaders>false</responseHeaders>
              <requestHeaders>false</requestHeaders>
              <responseDataOnError>false</responseDataOnError>
              <saveAssertionResultsFailureMessage>true</saveAssertionResultsFailureMessage>
              <assertionsResultsToSave>0</assertionsResultsToSave>
              <bytes>true</bytes>
              <sentBytes>true</sentBytes>
              <url>true</url>
              <threadCounts>true</threadCounts>
              <idleTime>true</idleTime>
              <connectTime>true</connectTime>
            </value>
          </objProp>
          <stringProp name="filename"></stringProp>
        </ResultCollector>
        <hashTree/>
      </hashTree>
    </hashTree>
  </hashTree>
</jmeterTestPlan>
"@

    Set-Content -Path "$OutputDir\$TestName.jmx" -Value $jmx -Encoding UTF8
    Write-Host "Created: $OutputDir\$TestName.jmx"
}

# JM-01: List notifications (50 users)
New-JmxHttpPlan -TestName "JM-01-ListNotifications" `
    -NumThreads 50 -RampTime 10 -Port "`${__P(notification_port,5185)}" `
    -Method "GET" -Path "/api/notifications?page=1&pageSize=20"

# JM-02: List notifications at peak (100 users)
New-JmxHttpPlan -TestName "JM-02-ListNotificationsPeak" `
    -NumThreads 100 -RampTime 15 -Port "`${__P(notification_port,5185)}" `
    -Method "GET" -Path "/api/notifications?page=1&pageSize=20"

# JM-03: Unread badge polling (100 users)
New-JmxHttpPlan -TestName "JM-03-UnreadBadgePolling" `
    -NumThreads 100 -RampTime 10 -Port "`${__P(notification_port,5185)}" `
    -Method "GET" -Path "/api/notifications/unread-count"

# JM-04: Mark one as read (20 users, CSV)
New-JmxHttpPlan -TestName "JM-04-MarkOneAsRead" `
    -NumThreads 20 -RampTime 5 -Port "`${__P(notification_port,5185)}" `
    -Method "PATCH" -Path "/api/notifications/`${notification_id}/read" `
    -CsvPath "`${__P(csv_notifications,qa-testing/jmeter/data/notifications.csv)}" -CsvVar "notification_id"

# JM-05: Mark all as read (20 users)
New-JmxHttpPlan -TestName "JM-05-MarkAllAsRead" `
    -NumThreads 20 -RampTime 5 -Port "`${__P(notification_port,5185)}" `
    -Method "PATCH" -Path "/api/notifications/read-all"

# JM-06: List reviews (50 users)
New-JmxHttpPlan -TestName "JM-06-ListReviews" `
    -NumThreads 50 -RampTime 10 -Port "`${__P(booking_port,5229)}" `
    -Method "GET" -Path "/api/bookings/reviews/`${listing_id}?BookingType=Experience&Page=1&PageSize=20"

# JM-07: Rating summary (100 users)
New-JmxHttpPlan -TestName "JM-07-RatingSummary" `
    -NumThreads 100 -RampTime 15 -Port "`${__P(booking_port,5229)}" `
    -Method "GET" -Path "/api/bookings/reviews/`${listing_id}/summary?BookingType=Experience"

# JM-08: Submit reviews (10 users, CSV)
New-JmxHttpPlan -TestName "JM-08-SubmitReviews" `
    -NumThreads 10 -RampTime 5 -Port "`${__P(booking_port,5229)}" `
    -Method "POST" -Path "/api/bookings/reviews" `
    -Body '{"bookingId":"${booking_id}","bookingType":"Experience","rating":5,"comment":"JMeter automated performance review"}' `
    -CsvPath "`${__P(csv_bookings,qa-testing/jmeter/data/completed_bookings.csv)}" -CsvVar "booking_id"

# JM-09: Duplicate review race (10 users, ramp-up 0s)
New-JmxHttpPlan -TestName "JM-09-DuplicateReviewRace" `
    -NumThreads 10 -RampTime 0 -Port "`${__P(booking_port,5229)}" `
    -Method "POST" -Path "/api/bookings/reviews" `
    -Body '{"bookingId":"${__P(race_booking_id,00000000-0000-0000-0000-000000000001)}","bookingType":"Experience","rating":4,"comment":"Concurrent race test"}' `
    -IgnoreStatus409 $true

Write-Host "Generated JM-01 through JM-09 successfully."
